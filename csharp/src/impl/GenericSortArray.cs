namespace TenderBaseImpl
{
    using System;

    public interface GenericSortArray
    {
        int Size();
        int Compare(int i, int j);
        void Swap(int i, int j);
    }
}

