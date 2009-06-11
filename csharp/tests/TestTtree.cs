using System;
using TenderBase;

class Name
{
    internal string first;
    internal string last;
}

class Person : Persistent
{
    internal string firstName;
    internal string lastName;
    internal int age;

    private Person()
    {
    }

    internal Person(string firstName, string lastName, int age)
    {
        this.firstName = firstName;
        this.lastName = lastName;
        this.age = age;
    }
}

class PersonList : Persistent
{
    internal SortedCollection list;
}

class NameComparator : PersistentComparator
{
    public override int CompareMembers(IPersistent m1, IPersistent m2)
    {
        Person p1 = (Person) m1;
        Person p2 = (Person) m2;
        int diff = String.CompareOrdinal(p1.firstName, p2.firstName);
        if (diff != 0)
        {
            return diff;
        }
        return String.CompareOrdinal(p1.lastName, p2.lastName);
    }

    public override int CompareMemberWithKey(IPersistent mbr, object key)
    {
        Person p = (Person) mbr;
        Name name = (Name) key;
        int diff = String.CompareOrdinal(p.firstName, name.first);
        if (diff != 0)
        {
            return diff;
        }
        return String.CompareOrdinal(p.lastName, name.last);
    }
}

public class TestTtree
{
    internal const int nRecords = 100000;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("testtree.dbs", pagePoolSize);
        PersonList root = (PersonList) db.GetRoot();
        if (root == null)
        {
            root = new PersonList();
            root.list = db.CreateSortedCollection(new NameComparator(), true);
            db.SetRoot(root);
        }

        SortedCollection list = root.list;
        long key = 1999;
        int i;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            string str = Convert.ToString(key);
            int m = str.Length / 2;
            string firstName = str.Substring(0, (m) - (0));
            string lastName = str.Substring(m);
            int age = (int) key % 100;
            Person p = new Person(firstName, lastName, age);
            list.Add(p);
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i++)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            string str = Convert.ToString(key);
            int m = str.Length / 2;
            Name name = new Name();
            int age = (int) key % 100;
            name.first = str.Substring(0, (m) - (0));
            name.last = str.Substring(m);

            Person p = (Person) list.Get(name);
            Assert.That(p != null);
            Assert.That(list.Contains(p));
            Assert.That(p.age == age);
        }
        Console.Out.WriteLine("Elapsed time for performing " + nRecords + " index searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        System.Collections.IEnumerator iterator = list.GetEnumerator();
        Name name2 = new Name();
        name2.first = name2.last = "";
        PersistentComparator comparator = list.Comparator;
        for (i = 0; iterator.MoveNext(); i++)
        {
            Person p = (Person) iterator.Current;
            Assert.That(comparator.CompareMemberWithKey(p, name2) > 0);
            name2.first = p.firstName;
            name2.last = p.lastName;
            //UPGRADE_ISSUE: Method 'java.util.Iterator.remove' was not converted.  
            //iterator.Remove();
        }
        Assert.That(i == nRecords);
        Console.Out.WriteLine("Elapsed time for removing " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        Assert.That(!list.GetEnumerator().MoveNext());
        db.Close();
    }
}

