using System;
using TenderBase;

class Record : Persistent
{
    internal string strKey;
    internal long intKey;
}

public class TestSimple
{
    static int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testsimple.dbs", pagePoolSize);
        long key = 1999;
        Record root = (Record)db.GetRoot();
        if (root == null)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = Convert.ToString(key);
            db.SetRoot(rec);
            db.Commit();
        }
        else
        {
            Console.Out.WriteLine("intKey = {0}, strKey = {1}", root.intKey, root.strKey);
        }
        db.Close();
    }
}

