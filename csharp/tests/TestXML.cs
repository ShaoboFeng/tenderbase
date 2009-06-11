using System;
using TenderBase;

public class TestXML
{
    internal class Record : Persistent
    {
        internal string strKey;
        internal long intKey;
        internal double realKey;
    }

    internal class Indices : Persistent
    {
        internal Index strIndex;
        internal FieldIndex intIndex;
        internal FieldIndex compoundIndex;
    }

    internal const int nRecords = 100000;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("test1.dbs", pagePoolSize);
        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.strIndex = db.CreateIndex(typeof(string), true);
            root.intIndex = db.CreateFieldIndex(typeof(Record), "intKey", true);
            root.compoundIndex = db.CreateFieldIndex(typeof(Record), new string[]{"strKey", "intKey"}, true);
            db.SetRoot(root);
        }

        FieldIndex intIndex = root.intIndex;
        FieldIndex compoundIndex = root.compoundIndex;
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
            rec.realKey = (double) key;
            intIndex.Put(rec);
            strIndex.Put(new Key(rec.strKey), rec);
            compoundIndex.Put(rec);
        }

        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.IO.StreamWriter writer = new System.IO.StreamWriter("test.xml", false, System.Text.Encoding.Default);
        db.ExportXML(writer);
        writer.Close();
        System.Console.Out.WriteLine("Elapsed time for XML export " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
        db.Open("test2.dbs", pagePoolSize);

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.IO.StreamReader reader = new System.IO.StreamReader("test.xml", System.Text.Encoding.Default);
        db.ImportXML(reader);
        reader.Close();
        Console.Out.WriteLine("Elapsed time for XML import " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        root = (Indices) db.GetRoot();
        intIndex = root.intIndex;
        strIndex = root.strIndex;
        compoundIndex = root.compoundIndex;

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            System.String strKey = Convert.ToString(key);
            Record rec1 = (Record) intIndex.Get(new Key(key));
            Record rec2 = (Record) strIndex.Get(new Key(strKey));
            Record rec3 = (Record) compoundIndex.Get(new Key(strKey, (long) key));
            Assert.That(rec1 != null);
            Assert.That(rec1 == rec2);
            Assert.That(rec1 == rec3);
            Assert.That(rec1.intKey == key);
            Assert.That(rec1.realKey == (double) key);
            Assert.That(strKey.Equals(rec1.strKey));
        }

        Console.Out.WriteLine("Elapsed time for performing " + nRecords * 2 + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        db.Close();
    }
}
