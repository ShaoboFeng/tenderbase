#if !OMIT_REPLICATION
namespace TenderBase
{
    using System;
    
    /// <summary> Storage performing replication of changed pages to specified slave nodes.</summary>
    public interface ReplicationMasterStorage : Storage
    {
        /// <summary> Get number of currently available slave nodes</summary>
        /// <returns> number of online replication slaves
        /// </returns>
        int NumberOfAvailableHosts
        {
            get;
        }
    }
}
#endif

