#if !OMIT_REPLICATION
namespace TenderBaseImpl
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using TenderBase;

    public class ReplicationSlaveStorageImpl : StorageImpl, ReplicationSlaveStorage, IThreadRunnable
    {
        /// <summary> Check if socket is connected to the master host</summary>
        /// <returns> <code>true</code> if connection between slave and master is sucessfully established
        /// </returns>
        public virtual bool Connected
        {
            get
            {
                return socket != null;
            }
        }

        public ReplicationSlaveStorageImpl(int port)
        {
            this.port = port;
        }

        public override void Open(IFile file, int pagePoolSize)
        {
            try
            {
                TcpListener temp_tcpListener;
                //IPAddress ipAddr = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
                IPAddress ipAddr = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
                temp_tcpListener = new TcpListener(ipAddr, port);
                temp_tcpListener.Start();
                acceptor = temp_tcpListener;
            }
            catch (System.IO.IOException)
            {
                return;
            }
            byte[] rootPage = new byte[Page.pageSize];
            int rc = file.Read(0, rootPage);
            if (rc == Page.pageSize)
            {
                prevIndex = rootPage[DB_HDR_CURR_INDEX_OFFSET];
                initialized = rootPage[DB_HDR_INITIALIZED_OFFSET] != 0;
            }
            else
            {
                initialized = false;
                prevIndex = -1;
            }

            this.file = file;
            Lock = new PersistentResource();
            init = new object();
            done = new object();
            _commit = new object();
            listening = true;
            Connect();
            pool = new PagePool(pagePoolSize / Page.pageSize);
            pool.Open(file);
            thread = new SupportClass.ThreadClass(new ThreadStart(this.Run));
            thread.Start();
            WaitInitializationCompletion();
            base.Open(file, pagePoolSize);
        }

        public override void BeginThreadTransaction(int mode)
        {
            if (mode != TenderBase.StorageConstants.REPLICATION_SLAVE_TRANSACTION)
                throw new ArgumentException("Illegal transaction mode");

            Lock.SharedLock();
            Page pg = pool.GetPage(0);
            header.Unpack(pg.data);
            pool.Unfix(pg);
            currIndex = 1 - header.curr;
            currIndexSize = header.root[1 - currIndex].indexUsed;
            committedIndexSize = currIndexSize;
            usedSize = header.root[currIndex].size;
        }

        public override void EndThreadTransaction(int maxDelay)
        {
            Lock.Unlock();
        }

        protected internal virtual void WaitInitializationCompletion()
        {
            try
            {
                lock (init)
                {
                    while (!initialized)
                    {
                        Monitor.Wait(init);
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        /// <summary> Wait until database is modified by master
        /// This method blocks current thread until master node commits trasanction and
        /// this transanction is completely delivered to this slave node
        /// </summary>
        public virtual void WaitForModification()
        {
            try
            {
                lock (_commit)
                {
                    if (socket != null)
                    {
                        System.Threading.Monitor.Wait(_commit);
                    }
                }
            }
            catch (System.Threading.ThreadInterruptedException)
            {
            }
        }

        private const int DB_HDR_CURR_INDEX_OFFSET = 0;
        private const int DB_HDR_DIRTY_OFFSET = 1;
        private const int DB_HDR_INITIALIZED_OFFSET = 2;
        private const int PAGE_DATA_OFFSET = 8;

        public static int LINGER_TIME = 10; // linger parameter for the socket

        private void Connect()
        {
            try
            {
                socket = acceptor.AcceptTcpClient();
                try
                {
                    socket.LingerState = new System.Net.Sockets.LingerOption(true, LINGER_TIME);
                }
                catch (System.MethodAccessException)
                {
                }

                try
                {
                    socket.NoDelay = true;
                }
                catch (System.Exception)
                {
                }

                inStream = socket.GetStream();
                if (replicationAck)
                {
                    outStream = socket.GetStream();
                }
            }
            catch (System.IO.IOException)
            {
                socket = null;
                inStream = null;
            }
        }

        /// <summary> When overriden by base class this method perfroms socket error handling</summary>
        /// <returns> <code>true</code> if host should be reconnected and attempt to send data to it should be
        /// repeated, <code>false</code> if no more attmpts to communicate with this host should be performed
        /// </returns>
        public virtual bool HandleError()
        {
            if (listener == null)
                return false;
            return listener.ReplicationError(null);
        }

        public virtual void Run()
        {
            byte[] buf = new byte[Page.pageSize + PAGE_DATA_OFFSET];
            byte[] page = new byte[Page.pageSize];

            while (listening)
            {
                int offs = 0;
                do
                {
                    int rc;
                    try
                    {
                        if (inStream is TenderBaseImpl.BlobImpl.BlobInputStream)
                            rc = ((TenderBaseImpl.BlobImpl.BlobInputStream) inStream).Read(buf, offs, buf.Length - offs);
                        else
                            rc = inStream.Read(buf, offs, buf.Length - offs);
                    }
                    catch (System.IO.IOException)
                    {
                        rc = -1;
                    }
                    lock (done)
                    {
                        if (!listening)
                        {
                            return;
                        }
                    }

                    if (rc < 0)
                    {
                        if (HandleError())
                        {
                            Connect();
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        offs += rc;
                    }
                }
                while (offs < buf.Length);

                long pos = Bytes.Unpack8(buf, 0);
                bool transactionCommit = false;
                if (pos == 0)
                {
                    if (replicationAck)
                    {
                        try
                        {
                            outStream.Write(buf, 0, 1);
                        }
                        catch (System.IO.IOException)
                        {
                            HandleError();
                        }
                    }
                    if (buf[PAGE_DATA_OFFSET + DB_HDR_CURR_INDEX_OFFSET] != prevIndex)
                    {
                        prevIndex = buf[PAGE_DATA_OFFSET + DB_HDR_CURR_INDEX_OFFSET];
                        Lock.ExclusiveLock();
                        transactionCommit = true;
                    }
                }
                else if (pos < 0)
                {
                    lock (_commit)
                    {
                        Hangup();
                        Monitor.PulseAll(_commit);
                    }
                    return;
                }

                Page pg = pool.PutPage(pos);
                Array.Copy(buf, PAGE_DATA_OFFSET, pg.data, 0, Page.pageSize);
                pool.Unfix(pg);

                if (pos == 0)
                {
                    if (!initialized && buf[PAGE_DATA_OFFSET + DB_HDR_INITIALIZED_OFFSET] != 0)
                    {
                        lock (init)
                        {
                            initialized = true;
                            Monitor.Pulse(init);
                        }
                    }
                    if (transactionCommit)
                    {
                        Lock.Unlock();
                        lock (_commit)
                        {
                            Monitor.PulseAll(_commit);
                        }
                        pool.Flush();
                    }
                }
            }
        }

        public override void Close()
        {
            lock (done)
            {
                listening = false;
            }
            try
            {
                thread.Interrupt();
                thread.Join();
            }
            catch (ThreadInterruptedException)
            {
            }

            Hangup();

            pool.Flush();
            base.Close();
        }

        private void Hangup()
        {
            if (socket == null)
                return;

            try
            {
                inStream.Close();
                if (outStream != null)
                {
                    outStream.Close();
                }
                socket.Close();
            }
            catch (System.IO.IOException)
            {
            }
            inStream = null;
            socket = null;
        }

        protected internal override bool IsDirty()
        {
            return false;
        }

        protected internal System.IO.Stream inStream;
        protected internal System.IO.Stream outStream;
        protected internal TcpClient socket;
        protected internal int port;
        protected internal IFile file;
        protected internal bool initialized;
        protected internal bool listening;
        protected internal object init;
        protected internal object done;
        protected internal object _commit;
        protected internal int prevIndex;
        protected internal IResource Lock;
        protected internal TcpListener acceptor;
        protected internal SupportClass.ThreadClass thread;
    }
}
#endif

