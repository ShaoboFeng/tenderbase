using System;
using System.Collections;
using TenderBase;

class Record : Persistent
{
    internal string strKey;
    internal long intKey;
}

class Indices : Persistent
{
    internal Index strIndex;
    internal Index intIndex;
}

public class TestIndex
{
    internal const int nRecords = 100000;
    internal static int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(String[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        bool serializableTransaction = false;
        for (int i = 0; i < args.Length; i++)
        {
            if ("inmemory".Equals(args[i]))
                pagePoolSize = StorageConstants.INFINITE_PAGE_POOL;
            else if ("altbtree".Equals(args[i]))
            {
                db.SetProperty("perst.alternative.btree", true);
                //db.SetProperty("perst.object.cache.kind", "weak");
                db.SetProperty("perst.object.cache.init.size", 1013);
            }
            else if ("serializable".Equals(args[i]))
            {
                db.SetProperty("perst.alternative.btree", true);
                serializableTransaction = true;
            }
            else
                Console.Error.WriteLine("Unrecognized option: " + args[i]);
        }
        db.Open("testidx.dbs", pagePoolSize);

        if (serializableTransaction)
            db.BeginThreadTransaction(StorageConstants.SERIALIZABLE_TRANSACTION);

        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.strIndex = db.CreateIndex(typeof(string), true);
            root.intIndex = db.CreateIndex(typeof(long), true);
            db.SetRoot(root);
        }

        Index intIndex = root.intIndex;
        Index strIndex = root.strIndex;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long key = 1999;
        int i2;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = Convert.ToString(key);
            intIndex.Put(new Key(rec.intKey), rec);
            strIndex.Put(new Key(rec.strKey), rec);
            /*
            if (i % 100000 == 0) {
            System.out.print("Insert " + i + " records\r");
            db.Commit();
            }
            */
        }

        if (serializableTransaction)
        {
            db.EndThreadTransaction();
            db.BeginThreadTransaction(StorageConstants.SERIALIZABLE_TRANSACTION);
        }
        else
        {
            db.Commit();
        }
        //db.Gc();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec1 = (Record) intIndex.Get(new Key(key));
            Record rec2 = (Record) strIndex.Get(new Key(Convert.ToString(key)));
            Assert.That(rec1 != null && rec1 == rec2);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = System.Int64.MinValue;
        i2 = 0;
        foreach (Record rec in intIndex)
        {
            Assert.That(rec.intKey >= key);
            key = rec.intKey;
            ++i2;
        }
        Assert.That(i2 == nRecords);
        string strKey = "";
        i2 = 0;
        foreach (Record rec in strIndex)
        {
            Assert.That(String.CompareOrdinal(rec.strKey, strKey) >= 0);
            strKey = rec.strKey;
            i2++;
        }
        Assert.That(i2 == nRecords);
        Console.Out.WriteLine("Elapsed time for iterating through " + (nRecords * 2) + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.  
        Hashtable map = db.GetMemoryDump();
        Console.Out.WriteLine("Memory usage");
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        foreach (MemoryUsage usage in map.Values)
        {
            Console.Out.WriteLine(" " + usage.cls.FullName + ": instances=" + usage.nInstances + ", total size=" + usage.totalSize + ", allocated size=" + usage.allocatedSize);
        }
        Console.Out.WriteLine("Elapsed time for memory dump: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec = (Record) intIndex.Get(new Key(key));
            Record removed = (Record) intIndex.Remove(new Key(key));
            Assert.That(removed == rec);
            //strIndex.Remove(new Key(Long.toString(key)), rec);
            strIndex.Remove(new Key(Convert.ToString(key)));
            rec.Deallocate();
        }

        Assert.That(!intIndex.GetEnumerator().MoveNext());
        Assert.That(!strIndex.GetEnumerator().MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Console.Out.WriteLine("Elapsed time for deleting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}
