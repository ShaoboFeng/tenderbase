using System;
using TenderBase;

class L2List : PersistentResource
{
    internal L2Elem head;
}

class L2Elem : Persistent
{
    internal L2Elem next;
    internal L2Elem prev;
    internal int count;

    public override bool RecursiveLoading
    {
        get
        {
            return false;
        }
    }

    internal virtual void unlink()
    {
        next.prev = prev;
        prev.next = next;
        next.Store();
        prev.Store();
    }

    internal virtual void linkAfter(L2Elem elem)
    {
        elem.next.prev = this;
        next = elem.next;
        elem.next = this;
        prev = elem;
        Store();
        next.Store();
        prev.Store();
    }
}

public class TestConcur : SupportClass.ThreadClass
{
    internal const int nElements = 100000;
    internal const int nIterations = 100;
    internal const int nThreads = 4;

    internal TestConcur(Storage db)
    {
        this.db = db;
    }

    public override void Run()
    {
        L2List list = (L2List) db.GetRoot();
        for (int i = 0; i < nIterations; i++)
        {
            long sum = 0, n = 0;
            list.SharedLock();
            L2Elem head = list.head;
            L2Elem elem = head;
            do
            {
                elem.Load();
                sum += elem.count;
                n += 1;
            }
            while ((elem = elem.next) != head);
            Assert.That(n == nElements && sum == (long) nElements * (nElements - 1) / 2);
            list.Unlock();
            list.ExclusiveLock();
            L2Elem last = list.head.prev;
            last.unlink();
            last.linkAfter(list.head);
            list.Unlock();
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("testconcur.dbs");
        L2List list = (L2List) db.GetRoot();
        if (list == null)
        {
            list = new L2List();
            list.head = new L2Elem();
            list.head.next = list.head.prev = list.head;
            db.SetRoot(list);
            for (int i = 1; i < nElements; i++)
            {
                L2Elem elem = new L2Elem();
                elem.count = i;
                elem.linkAfter(list.head);
            }
        }
        TestConcur[] threads = new TestConcur[nThreads];
        for (int i = 0; i < nThreads; i++)
        {
            threads[i] = new TestConcur(db);
            threads[i].Start();
        }
        for (int i = 0; i < nThreads; i++)
        {
            threads[i].Join();
        }
        db.Close();
    }

    internal Storage db;
}
