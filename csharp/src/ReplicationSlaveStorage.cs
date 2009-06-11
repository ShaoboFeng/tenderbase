#if !OMIT_REPLICATION
namespace TenderBase
{
    using System;
    
    /// <summary> Storage reciving modified pages from replication master and
    /// been able to run read-only transactions
    /// </summary>
    public interface ReplicationSlaveStorage : Storage
    {
        /// <summary> Check if socket is connected to the master host</summary>
        /// <returns> <code>true</code> if connection between slave and master is sucessfully established
        /// </returns>
        bool Connected
        {
            get;
        }

        /// <summary> Wait until database is modified by master
        /// This method blocks current thread until master node commits trasanction and
        /// this transanction is completely delivered to this slave node
        /// </summary>
        void WaitForModification();
    }
}
#endif

