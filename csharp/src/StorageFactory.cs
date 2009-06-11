namespace TenderBase
{
    using System;
    using TenderBaseImpl;
    
    /// <summary> Storage factory</summary>
    public class StorageFactory
    {
        /// <summary> Get instance of storage factory.
        /// So new storages should be create in application in the following way:
        /// <code>StorageFactory.Instance.CreateStorage()</code>
        /// </summary>
        /// <returns> instance of the storage factory
        /// </returns>
        public static StorageFactory Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary> Create new instance of the storage</summary>
        /// <returns> new instance of the storage (unopened, you should explicitely invoke open method)
        /// </returns>
        public virtual Storage CreateStorage()
        {
            return new StorageImpl();
        }

#if !OMIT_REPLICATION
        /// <summary> Create new instance of the master node of replicated storage</summary>
        /// <param name="replicationSlaveNodes">addresses of hosts to which replication will be performed.
        /// Address as specified as NAME:PORT
        /// </param>
        /// <param name="asyncBufSize">if value of this parameter is greater than zero then replication will be
        /// asynchronous, done by separate thread and not blocking main application.
        /// Otherwise data is send to the slave nodes by the same thread which updates the database.
        /// If space asynchronous buffer is exhausted, then main thread willbe also blocked until the
        /// data is send.
        /// </param>
        /// <returns> new instance of the master storage (unopened, you should explicitely invoke open method)
        /// </returns>
        public virtual ReplicationMasterStorage CreateReplicationMasterStorage(string[] replicationSlaveNodes, int asyncBufSize)
        {
            return new ReplicationMasterStorageImpl(replicationSlaveNodes, asyncBufSize);
        }

        /// <summary> Create new instance of the slave node of replicated storage</summary>
        /// <param name="port"> socket port at which connection from master will be established
        /// </param>
        /// <returns> new instance of the slave storage (unopened, you should explicitely invoke open method)
        /// </returns>
        public virtual ReplicationSlaveStorage CreateReplicationSlaveStorage(int port)
        {
            return new ReplicationSlaveStorageImpl(port);
        }
#endif

        protected internal static readonly StorageFactory instance = new StorageFactory();
    }
}

