using System;
using System.Collections;
using TenderBase;

public class TestSet
{
    internal class Indices : Persistent
    {
        internal IPersistentSet Set;
        internal Index Index;
    }

    internal class Record : Persistent
    {
        internal int id;

        internal Record()
        {
        }

        internal Record(int id)
        {
            this.id = id;
        }
    }

    internal const int nRecords = 1000;
    internal const int maxInitSize = 500;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testset.dbs", pagePoolSize);

        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.Set = db.CreateSet();
            root.Index = db.CreateIndex(typeof(long), true);
            db.SetRoot(root);
        }
        int i, n;
        long key = 1999;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (i = 0, n = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int r = (int) (key % maxInitSize);
            IPersistentSet ps = db.CreateScalableSet(r);
            for (int j = 0; j < r; j++)
            {
                ps.Add(new Record(j));
                n += 1;
            }
            root.Set.Add(ps);
            root.Index.Put(new Key(key), ps);
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int r = (int) (key % maxInitSize);
            IPersistentSet ps = (IPersistentSet) root.Index.Get(new Key(key));
            Assert.That(root.Set.Contains(ps));
            Assert.That(ps.Count == r);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        i = 0;
        foreach (IPersistentSet ps in root.Set)
        {
            int sum = 0;
            foreach (IPersistent si in ps)
            {
                var rec = si as Record;
                sum += rec.id;
            }
            int sumExpected = ps.Count * (ps.Count - 1) / 2;
            Assert.That(sumExpected == sum);
            i += ps.Count;
        }
        Assert.That(i == n);
        Console.Out.WriteLine("Elapsed time for iterating through " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int r = (int) (key % maxInitSize);
            IPersistentSet ps = (IPersistentSet) root.Index.Get(new Key(key));
            Assert.That(ps.Count == r);
            for (int j = r; j < r * 2; j++)
            {
                Record rec = new Record(j);
                ps.Add(rec);
                ps.Add(rec);
            }
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for adding " + n + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        i = 0;
        foreach (IPersistentSet ps in root.Set)
        {
            IEnumerator si = ps.GetEnumerator();
            int sum = 0;
            while (si.MoveNext())
            {
                sum += ((Record) si.Current).id;
            }
            Assert.That(ps.Count * (ps.Count - 1) / 2 == sum);
            i += ps.Count;
        }
        Assert.That(i == n * 2);
        Console.Out.WriteLine("Elapsed time for iterating through " + n * 2 + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            IPersistentSet ps = (IPersistentSet) root.Index.Remove(new Key(key));
            int r = (int) (key % maxInitSize) * 2;
            Assert.That(ps.Count == r);
            root.Set.Remove(ps);
            foreach (IPersistent tmp in ps)
            {
                tmp.Deallocate();
            }
            ps.Deallocate();
        }
        Assert.That(root.Set.Count == 0);
        Assert.That(root.Index.Size() == 0);
        Assert.That(!root.Set.GetEnumerator().MoveNext());
        Assert.That(!root.Index.GetEnumerator().MoveNext());
        Console.Out.WriteLine("Elapsed time for deleting " + n * 2 + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}
