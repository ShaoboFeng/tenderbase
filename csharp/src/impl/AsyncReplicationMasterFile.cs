#if !OMIT_REPLICATION
namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    /// <summary> File performing asynchronous replication of changed pages to specified slave nodes.</summary>
    public class AsyncReplicationMasterFile : ReplicationMasterFile, IThreadRunnable
    {
        /// <summary> Constructor of replication master file</summary>
        /// <param name="storage">replication storage
        /// </param>
        /// <param name="file">local file used to store data locally
        /// </param>
        /// <param name="asyncBufSize">size of asynchronous buffer
        /// </param>
        public AsyncReplicationMasterFile(ReplicationMasterStorageImpl storage, IFile file, int asyncBufSize)
            : base(storage, file)
        {
            this.asyncBufSize = asyncBufSize;
            Start();
        }

        /// <summary> Constructor of replication master file</summary>
        /// <param name="file">local file used to store data locally
        /// </param>
        /// <param name="hosts">slave node hosts to which replicastion will be performed
        /// </param>
        /// <param name="asyncBufSize">size of asynchronous buffer
        /// </param>
        /// <param name="ack">whether master should wait acknowledgment from slave node during trasanction commit
        /// </param>
        public AsyncReplicationMasterFile(IFile file, string[] hosts, int asyncBufSize, bool ack)
            : base(file, hosts, ack)
        {
            this.asyncBufSize = asyncBufSize;
            Start();
        }

        private void Start()
        {
            go = new object();
            async = new object();
            thread = new SupportClass.ThreadClass(new System.Threading.ThreadStart(this.Run));
            thread.Start();
        }

        internal class Parcel
        {
            internal byte[] data;
            internal long pos;
            internal int host;
            internal Parcel next;
        }

        public override void Write(long pos, byte[] buf)
        {
            file.Write(pos, buf);
            for (int i = 0; i < streamOut.Length; i++)
            {
                if (streamOut[i] != null)
                {
                    byte[] data = new byte[8 + buf.Length];
                    Bytes.Pack8(data, 0, pos);
                    Array.Copy(buf, 0, data, 8, buf.Length);
                    Parcel p = new Parcel();
                    p.data = data;
                    p.pos = pos;
                    p.host = i;

                    try
                    {
                        lock (async)
                        {
                            buffered += data.Length;
                            while (buffered > asyncBufSize)
                            {
                                System.Threading.Monitor.Wait(async);
                            }
                        }
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                    }

                    lock (go)
                    {
                        if (head == null)
                        {
                            head = tail = p;
                        }
                        else
                        {
                            tail = tail.next = p;
                        }

                        System.Threading.Monitor.Pulse(go);
                    }
                }
            }
        }

        public virtual void Run()
        {
            try
            {
                while (true)
                {
                    Parcel p;
                    lock (go)
                    {
                        while (head == null)
                        {
                            if (closed)
                            {
                                return;
                            }

                            System.Threading.Monitor.Wait(go);
                        }

                        p = head;
                        head = p.next;
                    }

                    lock (async)
                    {
                        if (buffered > asyncBufSize)
                        {
                            System.Threading.Monitor.PulseAll(async);
                        }

                        buffered -= p.data.Length;
                    }

                    int i = p.host;
                    while (streamOut[i] != null)
                    {
                        try
                        {
                            streamOut[i].Write(p.data, 0, p.data.Length);
                            if (!ack || p.pos != 0 || (streamIn[i].Read(rcBuf, 0, rcBuf.Length) == 1))
                            {
                                break;
                            }
                        }
                        catch (System.IO.IOException)
                        {
                        }

                        streamOut[i] = null;
                        sockets[i] = null;
                        nHosts -= 1;
                        if (HandleError(hosts[i]))
                        {
                            Connect(i);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (System.Threading.ThreadInterruptedException)
            {
            }
        }

        public override void Close()
        {
            try
            {
                lock (go)
                {
                    closed = true;
                    System.Threading.Monitor.Pulse(go);
                }

                thread.Join();
            }
            catch (System.Threading.ThreadInterruptedException)
            {
            }

            base.Close();
        }

        private int asyncBufSize;
        private int buffered;
        private bool closed;
        private object go;
        private object async;
        private Parcel head;
        private Parcel tail;
        private SupportClass.ThreadClass thread;
    }
}
#endif
