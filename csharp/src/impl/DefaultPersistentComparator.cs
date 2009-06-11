namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    [Serializable]
    public class DefaultPersistentComparator : PersistentComparator
    {
        public override int CompareMembers(IPersistent m1, IPersistent m2)
        {
            return ((System.IComparable) m1).CompareTo(m2);
        }

        public override int CompareMemberWithKey(IPersistent mbr, object key)
        {
            return ((System.IComparable) mbr).CompareTo(key);
        }
    }
}

