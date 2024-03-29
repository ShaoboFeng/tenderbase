namespace TenderBase
{
    using System;
    
    /// <summary> Base class for persistent comparator used in SortedCollection class</summary>
    [Serializable]
    public abstract class PersistentComparator : Persistent
    {
        /// <summary> Compare two members of collection</summary>
        /// <param name="m1">first members
        /// </param>
        /// <param name="m2">second members
        /// </param>
        /// <returns> negative number if m1 &lt; m2, zero if m1 == m2 and positive number if m1 &gt; m2
        /// </returns>
        public abstract int CompareMembers(IPersistent m1, IPersistent m2);

        /// <summary> Compare member with specified search key</summary>
        /// <param name="mbr">collection member
        /// </param>
        /// <param name="key">search key
        /// </param>
        /// <returns> negative number if mbr &lt; key, zero if mbr == key and positive number if mbr &gt; key
        /// </returns>
        public abstract int CompareMemberWithKey(IPersistent mbr, object key);
    }
}

