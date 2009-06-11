using System;
using TenderBase;

public class TestIndex2
{
    internal class Record : Persistent
    {
        internal string strKey;
        internal long intKey;
    }

    internal class Indices : Persistent
    {
        internal SortedCollection strIndex;
        internal SortedCollection intIndex;
    }

    internal class IntRecordComparator : PersistentComparator
    {
        public override int CompareMembers(IPersistent m1, IPersistent m2)
        {
            long diff = ((Record) m1).intKey - ((Record) m2).intKey;
            return diff < 0 ? -1 : (diff == 0 ? 0 : 1);
        }

        public override int CompareMemberWithKey(IPersistent mbr, System.Object key)
        {
            long diff = ((Record) mbr).intKey - (long) ((System.Int64) key);
            return diff < 0 ? -1 : (diff == 0 ? 0 : 1);
        }
    }

    internal class StrRecordComparator : PersistentComparator
    {
        public override int CompareMembers(IPersistent m1, IPersistent m2)
        {
            return String.CompareOrdinal(((Record) m1).strKey, ((Record) m2).strKey);
        }

        public override int CompareMemberWithKey(IPersistent mbr, object key)
        {
            return String.CompareOrdinal(((Record) mbr).strKey, (string) key);
        }
    }

    internal const int nRecords = 100000;
    internal const int pagePoolSize = StorageConstants.INFINITE_PAGE_POOL; // 32*1024*1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("testidx2.dbs", pagePoolSize);
        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.strIndex = db.CreateSortedCollection(new StrRecordComparator(), true);
            root.intIndex = db.CreateSortedCollection(new IntRecordComparator(), true);
            db.SetRoot(root);
        }

        SortedCollection intIndex = root.intIndex;
        SortedCollection strIndex = root.strIndex;
        long start = (System.DateTime.Now.Ticks - 621355968000000000) / 10000;
        long key = 1999;
        int i;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = System.Convert.ToString(key);
            intIndex.Add(rec);
            strIndex.Add(rec);
        }
        db.Commit();
        db.Gc();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec1 = (Record) intIndex.Get((long) key);
            Record rec2 = (Record) strIndex.Get(Convert.ToString(key));
            Assert.That(rec1 != null && rec1 == rec2);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.Collections.IEnumerator iterator = intIndex.GetEnumerator();
        key = Int64.MinValue;
        for (i = 0; iterator.MoveNext(); i++)
        {
            Record rec = (Record) iterator.Current;
            Assert.That(rec.intKey >= key);
            key = rec.intKey;
        }
        Assert.That(i == nRecords);
        iterator = strIndex.GetEnumerator();
        string strKey = "";
        for (i = 0; iterator.MoveNext(); i++)
        {
            Record rec = (Record) iterator.Current;
            Assert.That(String.CompareOrdinal(rec.strKey, strKey) >= 0);
            strKey = rec.strKey;
        }
        Assert.That(i == nRecords);
        Console.Out.WriteLine("Elapsed time for iterating through " + (nRecords * 2) + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        System.Collections.Hashtable map = db.GetMemoryDump();
        iterator = map.Values.GetEnumerator();
        Console.Out.WriteLine("Memory usage");
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        while (iterator.MoveNext())
        {
            MemoryUsage usage = (MemoryUsage) iterator.Current;
            System.Console.Out.WriteLine(" " + usage.cls.FullName + ": instances=" + usage.nInstances + ", total size=" + usage.totalSize + ", allocated size=" + usage.allocatedSize);
        }
        System.Console.Out.WriteLine("Elapsed time for memory dump: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec = (Record) intIndex.Get((long) key);
            intIndex.Remove(rec);
            strIndex.Remove(rec);
            rec.Deallocate();
        }
        Assert.That(!intIndex.GetEnumerator().MoveNext());
        Assert.That(!strIndex.GetEnumerator().MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null).MoveNext());
        Console.Out.WriteLine("Elapsed time for deleting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}
