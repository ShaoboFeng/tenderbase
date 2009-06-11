using System;
using TenderBase;

public class TestThickIndex
{
    internal class Record : Persistent
    {
        internal string strKey;
        internal long intKey;
    }

    internal class Indices : Persistent
    {
        internal Index strIndex;
        internal Index intIndex;
    }

    internal const int nRecords = 1000;
    internal const int maxDuplicates = 1000;
    internal static int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testthick.dbs", pagePoolSize);

        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.strIndex = db.CreateThickIndex(typeof(string));
            root.intIndex = db.CreateThickIndex(typeof(long));
            db.SetRoot(root);
        }

        Index intIndex = root.intIndex;
        Index strIndex = root.strIndex;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long key = 1999;
        int i;
        int n = 0;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int d = (int) (key % maxDuplicates);
            for (int j = 0; j < d; j++)
            {
                Record rec = new Record();
                rec.intKey = key;
                rec.strKey = Convert.ToString(key);
                intIndex.Put(new Key(rec.intKey), rec);
                strIndex.Put(new Key(rec.strKey), rec);
                n += 1;
            }
        }

        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            IPersistent[] res1 = intIndex.Get(new Key(key), new Key(key));
            IPersistent[] res2 = strIndex.Get(new Key(Convert.ToString(key)), new Key(Convert.ToString(key)));
            int d = (int) (key % maxDuplicates);
            Assert.That(res1.Length == res2.Length && res1.Length == d);
            for (int j = 0; j < d; j++)
            {
                Assert.That(((Record) res1[j]).intKey == key && ((Record) res2[j]).intKey == key);
            }
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.Collections.IEnumerator iterator = intIndex.GetEnumerator();
        key = System.Int64.MinValue;
        for (i = 0; iterator.MoveNext(); i++)
        {
            Record rec = (Record) iterator.Current;
            Assert.That(rec.intKey >= key);
            key = rec.intKey;
        }
        Assert.That(i == n);
        iterator = strIndex.GetEnumerator();
        string strKey = "";
        for (i = 0; iterator.MoveNext(); i++)
        {
            Record rec = (Record) iterator.Current;
            Assert.That(String.CompareOrdinal(rec.strKey, strKey) >= 0);
            strKey = rec.strKey;
        }
        Assert.That(i == n);
        Console.Out.WriteLine("Elapsed time for iterating through " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            IPersistent[] res = intIndex.Get(new Key(key), new Key(key));
            int d = (int) (key % maxDuplicates);
            Assert.That(res.Length == d);
            for (int j = 0; j < d; j++)
            {
                intIndex.Remove(new Key(key), res[j]);
                strIndex.Remove(new Key(Convert.ToString(key)), res[j]);
                res[j].Deallocate();
            }
        }
        Assert.That(!intIndex.GetEnumerator().MoveNext());
        Assert.That(!strIndex.GetEnumerator().MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Console.Out.WriteLine("Elapsed time for deleting " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}

