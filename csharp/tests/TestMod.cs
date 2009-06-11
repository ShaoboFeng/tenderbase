using System;
using TenderBase;

public class TestMod
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

    internal const int nRecords = 100000;
    internal const int nIterations = 3;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    internal static string reverseString(string s)
    {
        byte[] dummy = new byte[16 * 1024];
        char[] chars = new char[s.Length];
        for (int i = 0, n = chars.Length; i < n; i++)
        {
            chars[i] = s[n - i - 1];
        }
        return new string(chars);
    }

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("testmod.dbs", pagePoolSize);
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
        int i;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = Convert.ToString(key);
            intIndex.Put(new Key(rec.intKey), rec);
            strIndex.Put(new Key(rec.strKey), rec);
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (int j = 0; j < nIterations; j++)
        {
            key = 1999;
            for (i = 0; i < nRecords; i++)
            {
                key = (3141592621L * key + 2718281829L) % 1000000007L;
                Record rec = (Record) intIndex.Get(new Key(key));
                rec.strKey = reverseString(rec.strKey);
                if ((i & 255) == 0)
                {
                    rec.Store();
                }
                else
                {
                    rec.Modify();
                }
            }
            Console.Out.WriteLine("Iteration " + j);
            db.Commit();
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * nIterations + " updates: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec1 = (Record) intIndex.Get(new Key(key));
            Record rec2 = (Record) strIndex.Get(new Key(Convert.ToString(key)));
            Assert.That(rec1 != null && rec1 == rec2);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Record rec = (Record) intIndex.Get(new Key(key));
            intIndex.Remove(new Key(key));
            strIndex.Remove(new Key(Convert.ToString(key)), rec);
            rec.Deallocate();
        }
        Console.Out.WriteLine("Elapsed time for deleting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}

