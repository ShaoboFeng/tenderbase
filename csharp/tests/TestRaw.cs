using System;
using System.Collections;
using TenderBase;

[Serializable]
class L1List
{
    internal L1List next;
    internal object obj;
    internal object root;

    internal L1List(object val, object tree, L1List list)
    {
        obj = val;
        root = tree;
        next = list;
    }
}

[Serializable]
public class TestRaw : Persistent
{
    internal L1List list;
    internal Hashtable map;
    internal object nil;

    internal const int nListMembers = 100;
    internal const int nHashMembers = 1000;

    [STAThread]
    public static void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.SetProperty("perst.serialize.transient.objects", true);
        db.Open("testraw.dbs");
        TestRaw root = (TestRaw) db.GetRoot();
        if (root == null)
        {
            root = new TestRaw();
            db.SetRoot(root);
            L1List list = null;
            for (int i = 0; i < nListMembers; i++)
                list = new L1List((System.Object) i, root, list);

            root.list = list;
            root.map = new System.Collections.Hashtable();
            for (int i = 0; i < nHashMembers; i++)
                root.map["key-" + i] = "value-" + i;

            root.Store();
            Console.Out.WriteLine("Initialization of database completed");
        }

        L1List list2 = root.list;
        for (int i = nListMembers; --i >= 0; )
        {
            Assert.That(list2.obj.Equals((System.Int32) i));
            Assert.That(root == list2.root);
            list2 = list2.next;
        }

        for (int i = nHashMembers; --i >= 0; )
        {
            Assert.That(root.map["key-" + i].Equals("value-" + i));
        }

        Console.Out.WriteLine("Database is OK");
        db.Close();
    }
}

