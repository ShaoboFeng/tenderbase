using System;
using TenderBase;

public class TestMaxOid
{
    internal const int nRecords = 1000000000;
    internal static int pagePoolSize = 256 * 1024 * 1024;

    internal class Record : Persistent
    {
        internal int key;

        internal Record()
        {
        }
        internal Record(int key)
        {
            this.key = key;
        }
    }

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testmaxoid.dbs", pagePoolSize);
        int i;
        FieldIndex root = (FieldIndex) db.GetRoot();
        if (root == null)
        {
            root = db.CreateFieldIndex(typeof(Record), "key", true);
            db.SetRoot(root);
        }
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = new Record(i);
            root.Put(rec);
            if (i % 1000000 == 0)
            {
                Console.Out.Write("Insert " + i + " records\r");
                db.Commit();
            }
        }

        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = (Record) root.Get(new Key(i));
            Assert.That(rec != null && rec.key == i);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.Collections.IEnumerator iterator = root.GetEnumerator();
        for (i = 0; iterator.MoveNext(); i++)
        {
            Record rec = (Record) iterator.Current;
            Assert.That(rec.key == i);
        }
        Assert.That(i == nRecords);
        Console.Out.WriteLine("Elapsed time for iterating through " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = (Record) root.Remove(new Key(i));
            Assert.That(rec.key == i);
            rec.Deallocate();
        }
        Console.Out.WriteLine("Elapsed time for deleting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}

