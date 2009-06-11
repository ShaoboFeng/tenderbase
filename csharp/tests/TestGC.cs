using System;
using TenderBase;

public class PObject : Persistent
{
    internal long intKey;
    internal PObject next;
    internal string strKey;
}

public class StorageRoot : Persistent
{
    internal PObject list;
    internal Index strIndex;
    internal Index intIndex;
}

public class TestGC
{
    internal const int nObjectsInTree = 10000;
    internal const int nIterations = 100000;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        for (int i = 0; i < args.Length; i++)
        {
            if ("background".Equals(args[i]))
            {
                db.SetProperty("perst.background.gc", true);
            }
            else if ("altbtree".Equals(args[i]))
            {
                db.SetProperty("perst.alternative.btree", true);
            }
            else
            {
                Console.Error.WriteLine("Unrecognized option: " + args[i]);
            }
        }

        db.Open("testgc.dbs");
        db.GcThreshold = 1000000;
        StorageRoot root = new StorageRoot();
        root.strIndex = db.CreateIndex(typeof(string), true);
        root.intIndex = db.CreateIndex(typeof(long), true);
        db.SetRoot(root);
        Index intIndex = root.intIndex;
        Index strIndex = root.strIndex;
        long insKey = 1999;
        long remKey = 1999;
        int i2;

        for (i2 = 0; i2 < nIterations; i2++)
        {
            if (i2 > nObjectsInTree)
            {
                remKey = (3141592621L * remKey + 2718281829L) % 1000000007L;
                intIndex.Remove(new Key(remKey));
                strIndex.Remove(new Key(Convert.ToString(remKey)));
            }
            PObject obj = new PObject();
            insKey = (3141592621L * insKey + 2718281829L) % 1000000007L;
            obj.intKey = insKey;
            obj.strKey = Convert.ToString(insKey);
            obj.next = new PObject();
            intIndex.Put(new Key(obj.intKey), obj);
            strIndex.Put(new Key(obj.strKey), obj);
            if (i2 > 0)
            {
                Assert.That(root.list.intKey == i2 - 1);
            }
            root.list = new PObject();
            root.list.intKey = i2;
            root.Store();
            if (i2 % 1000 == 0)
            {
                Console.Out.Write("Iteration " + i2 + "\r");
                Console.Out.Flush();
                db.Commit();
            }
        }
        db.Close();
    }
}

