namespace TenderBaseImpl
{
    using System;
    using IPersistent = TenderBase.IPersistent;
    
    public interface OidHashTable
    {
        bool Remove(int oid);
        void Put(int oid, IPersistent obj);
        IPersistent Get(int oid);
        void Flush();
        void Invalidate();
        int Size();
        void SetDirty(int oid);
        void ClearDirty(int oid);
    }
}

