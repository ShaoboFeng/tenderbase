#if !OMIT_REPLICATION
namespace TenderBaseImpl
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using TenderBase;
    
    /// <summary> File performing replication of changed pages to specified slave nodes.</summary>
    public class ReplicationMasterFile : IFile
    {
        public virtual int NumberOfAvailableHosts
        {
            get
            {
                return nHosts;
            }
        }

        /// <summary> Constructor of replication master file</summary>
        /// <param name="storage">replication storage
        /// </param>
        /// <param name="file">local file used to store data locally
        /// </param>
        public ReplicationMasterFile(ReplicationMasterStorageImpl storage, IFile file)
            : this(file, storage.hosts, storage.replicationAck)
        {
            this.storage = storage;
        }

        /// <summary> Constructor of replication master file</summary>
        /// <param name="file">local file used to store data locally
        /// </param>
        /// <param name="hosts">slave node hosts to which replicastion will be performed
        /// </param>
        /// <param name="ack">whether master should wait acknowledgment from slave node during trasanction commit
        /// </param>
        public ReplicationMasterFile(IFile file, string[] hosts, bool ack)
        {
            this.file = file;
            this.hosts = hosts;
            this.ack = ack;
            sockets = new System.Net.Sockets.TcpClient[hosts.Length];
            streamOut = new System.IO.Stream[hosts.Length];
            if (ack)
            {
                streamIn = new System.IO.Stream[hosts.Length];
                rcBuf = new byte[1];
            }

            txBuf = new byte[8 + Page.pageSize];
            nHosts = 0;
            for (int i = 0; i < hosts.Length; i++)
            {
                Connect(i);
            }
        }

        protected internal virtual void Connect(int i)
        {
            string host = hosts[i];
            int colon = host.IndexOf(':');
            int port = Int32.Parse(host.Substring(colon + 1));
            host = host.Substring(0, (colon) - (0));
            TcpClient socket = null;
            try
            {
                for (int j = 0; j < MAX_CONNECT_ATTEMPTS; j++)
                {
                    try
                    {
                        //UPGRADE_TODO: The equivalent in .NET for method 'java.net.InetAddress.getByName' may return a different value.
                        //IPAddress ipAddr = Dns.Resolve(host).AddressList[0];
                        IPAddress ipAddr = Dns.GetHostEntry(host).AddressList[0];
                        socket = new TcpClient(ipAddr.ToString(), port);
                        if (socket != null)
                        {
                            break;
                        }
                        //UPGRADE_TODO: Method 'java.lang.Thread.sleep' was converted to 'System.Threading.Thread.Sleep' which has a different behavior.
                        System.Threading.Thread.Sleep(new System.TimeSpan((Int64) 10000 * CONNECTION_TIMEOUT));
                    }
                    catch (System.IO.IOException)
                    {
                    }
                }
            }
            catch (System.Threading.ThreadInterruptedException)
            {
            }

            if (socket != null)
            {
                try
                {
                    try
                    {
                        socket.LingerState = new LingerOption(true, LINGER_TIME);
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
                    sockets[i] = socket;
                    streamOut[i] = socket.GetStream();
                    if (ack)
                    {
                        streamIn[i] = socket.GetStream();
                    }
                    nHosts += 1;
                }
                catch (System.IO.IOException)
                {
                    HandleError(hosts[i]);
                    sockets[i] = null;
                    streamOut[i] = null;
                }
            }
        }

        /// <summary> When overriden by base class this method perfroms socket error handling</summary>
        /// <returns> <code>true</code> if host should be reconnected and attempt to send data to it should be
        /// repeated, <code>false</code> if no more attmpts to communicate with this host should be performed
        /// </returns>
        public virtual bool HandleError(string host)
        {
            Console.Error.WriteLine("Failed to establish connection with host " + host);
            return (storage != null && storage.listener != null) ? storage.listener.ReplicationError(host) : false;
        }

        public virtual void Write(long pos, byte[] buf)
        {
            for (int i = 0; i < streamOut.Length; i++)
            {
                while (streamOut[i] != null)
                {
                    try
                    {
                        Bytes.Pack8(txBuf, 0, pos);
                        Array.Copy(buf, 0, txBuf, 8, buf.Length);
                        streamOut[i].Write(txBuf, 0, txBuf.Length);
                        if (!ack || pos != 0 || (streamIn[i].Read(rcBuf, 0, rcBuf.Length) == 1))
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
            file.Write(pos, buf);
        }

        public virtual int Read(long pos, byte[] buf)
        {
            return file.Read(pos, buf);
        }

        public virtual void Sync()
        {
            file.Sync();
        }

        public virtual bool Lock()
        {
            return file.Lock();
        }

        public virtual void Close()
        {
            file.Close();
            Bytes.Pack8(txBuf, 0, -1);
            for (int i = 0; i < streamOut.Length; i++)
            {
                if (sockets[i] != null)
                {
                    try
                    {
                        streamOut[i].Write(txBuf, 0, txBuf.Length);
                        streamOut[i].Close();
                        if (streamIn != null)
                        {
                            streamIn[i].Close();
                        }
                        sockets[i].Close();
                    }
                    catch (System.IO.IOException)
                    {
                    }
                }
            }
        }

        public virtual long Length()
        {
            return file.Length();
        }

        public static int LINGER_TIME = 10; // linger parameter for the socket
        public static int MAX_CONNECT_ATTEMPTS = 10; // attempts to establish connection with slave node
        public static int CONNECTION_TIMEOUT = 1000; // timeout between attempts to conbbect to the slave

        internal System.IO.Stream[] streamOut;
        internal System.IO.Stream[] streamIn;
        internal System.Net.Sockets.TcpClient[] sockets;
        internal byte[] txBuf;
        internal byte[] rcBuf;
        internal IFile file;
        internal string[] hosts;
        internal int nHosts;
        internal bool ack;

        internal ReplicationMasterStorageImpl storage;
    }
}
#endif

