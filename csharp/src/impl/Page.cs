namespace TenderBaseImpl
{
    using System;

    public class Page : LRU, System.IComparable
    {
        internal Page collisionChain;
        internal int accessCount;
        internal int writeQueueIndex;
        internal int state;
        internal long offs;
        internal byte[] data;

        internal const int psDirty = 0x01; // page has been modified
        internal const int psRaw = 0x02; // page is loaded from the disk
        internal const int psWait = 0x04; // other thread(s) wait load operation completion

        public const int pageBits = 12;
        public const int pageSize = 1 << pageBits;

        public virtual int CompareTo(object o)
        {
            long po = ((Page) o).offs;
            return offs < po ? -1 : (offs == po ? 0 : 1);
        }

        internal Page()
        {
            data = new byte[pageSize];
        }
    }
}

