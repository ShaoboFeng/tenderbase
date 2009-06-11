using System;
using TenderBase;

public class TestCompoundIndex
{
    internal const int nRecords = 100000;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    internal class Record : Persistent
    {
        internal string strKey;
        internal int intKey;
    }

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        for (int i = 0; i < args.Length; i++)
        {
            if ("altbtree".Equals(args[i]))
            {
                db.SetProperty("perst.alternative.btree", true);
            }
        }
        db.Open("testcidx.dbs", pagePoolSize);
        FieldIndex root = (FieldIndex) db.GetRoot();
        if (root == null)
        {
            root = db.CreateFieldIndex(typeof(Record), new string[]{"intKey", "strKey"}, true);
            db.SetRoot(root);
        }
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long key = 1999;
        int i2;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = (int) (SupportClass.URShift(key, 32));
            rec.strKey = Convert.ToString((int) key);
            root.Put(rec);
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        int minKey = System.Int32.MaxValue;
        int maxKey = System.Int32.MinValue;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int intKey = (int) (SupportClass.URShift(key, 32));
            string strKey = Convert.ToString((int) key);
            Record rec = (Record) root.Get(new Key(new System.Object[]{(System.Int32) intKey, strKey}));
            Assert.That(rec != null && rec.intKey == intKey && rec.strKey.Equals(strKey));
            if (intKey < minKey)
            {
                minKey = intKey;
            }
            if (intKey > maxKey)
            {
                maxKey = intKey;
            }
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.Collections.IEnumerator iterator = root.GetEnumerator(new Key((System.Int32)minKey, ""), new Key((System.Int32)(maxKey + 1), "???"), IndexSortOrder.Ascent);
        int n = 0;
        string prevStr = "";
        int prevInt = minKey;
        while (iterator.MoveNext())
        {
            Record rec = (Record) iterator.Current;
            Assert.That(rec.intKey > prevInt || rec.intKey == prevInt && String.CompareOrdinal(rec.strKey, prevStr) > 0);
            prevStr = rec.strKey;
            prevInt = rec.intKey;
            n += 1;
        }
        Assert.That(n == nRecords);

        iterator = root.GetEnumerator(new Key((System.Int32)minKey, "", false), new Key((System.Int32)(maxKey + 1), "???", false), IndexSortOrder.Descent);
        n = 0;
        prevInt = maxKey + 1;
        while (iterator.MoveNext())
        {
            Record rec = (Record) iterator.Current;
            Assert.That(rec.intKey < prevInt || rec.intKey == prevInt && String.CompareOrdinal(rec.strKey, prevStr) < 0);
            prevStr = rec.strKey;
            prevInt = rec.intKey;
            n += 1;
        }
        Assert.That(n == nRecords);
        Console.Out.WriteLine("Elapsed time for iterating through " + (nRecords * 2) + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i2 = 0; i2 < nRecords; i2++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int intKey = (int) (SupportClass.URShift(key, 32));
            string strKey = Convert.ToString((int) key);
            Record rec = (Record) root.Get(new Key(new System.Object[]{(System.Int32) intKey, strKey}));
            Assert.That(rec != null && rec.intKey == intKey && rec.strKey.Equals(strKey));
            Assert.That(root.Contains(rec));
            root.Remove(rec);
            rec.Deallocate();
        }
        Assert.That(!root.GetEnumerator().MoveNext());
        Assert.That(!root.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!root.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Console.Out.WriteLine("Elapsed time for deleting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}
