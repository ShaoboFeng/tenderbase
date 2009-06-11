#if !OMIT_REPLICATION
namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class ReplicationMasterStorageImpl : StorageImpl, ReplicationMasterStorage
    {
        public virtual int NumberOfAvailableHosts
        {
            get
            {
                return ((ReplicationMasterFile) pool.file).NumberOfAvailableHosts;
            }
        }

        public ReplicationMasterStorageImpl(string[] hosts, int asyncBufSize)
        {
            this.hosts = hosts;
            this.asyncBufSize = asyncBufSize;
        }

        public override void Open(IFile file, int pagePoolSize)
        {
            base.Open(asyncBufSize != 0 ? (ReplicationMasterFile) new AsyncReplicationMasterFile(this, file, asyncBufSize) : new ReplicationMasterFile(this, file), pagePoolSize);
        }

        internal string[] hosts;
        internal int asyncBufSize;
    }
}
#endif

