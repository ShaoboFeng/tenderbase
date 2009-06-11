using System;
using TenderBase;

public class TestIndexIterator
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

    internal const int nRecords = 1000;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        if (args.Length > 0)
        {
            if ("altbtree".Equals(args[0]))
            {
                db.SetProperty("perst.alternative.btree", true);
            }
            else
            {
                Console.Error.WriteLine("Unrecognized option: " + args[0]);
            }
        }
        db.Open("testiter.dbs", pagePoolSize);
        Indices root = (Indices) db.GetRoot();
        if (root == null)
        {
            root = new Indices();
            root.strIndex = db.CreateIndex(typeof(string), false);
            root.intIndex = db.CreateIndex(typeof(long), false);
            db.SetRoot(root);
        }

        Index intIndex = root.intIndex;
        Index strIndex = root.strIndex;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long key = 1999;
        int i, j;
        for (i = 0; i < nRecords; i++)
        {
            Record rec = new Record();
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            rec.intKey = key;
            rec.strKey = Convert.ToString(key);
            for (j = (int) (key % 10); --j >= 0; )
            {
                intIndex.Put(new Key(rec.intKey), rec);
                strIndex.Put(new Key(rec.strKey), rec);
            }
        }
        db.Commit();
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        key = 1999;
        for (i = 0; i < nRecords; i += 2)
        {
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Key fromInclusive = new Key(key);
            Key fromInclusiveStr = new Key(Convert.ToString(key));
            Key fromExclusive = new Key(key, false);
            Key fromExclusiveStr = new Key(Convert.ToString(key), false);
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            Key tillInclusive = new Key(key);
            Key tillInclusiveStr = new Key(Convert.ToString(key));
            Key tillExclusive = new Key(key, false);
            Key tillExclusiveStr = new Key(Convert.ToString(key), false);

            IPersistent[] records;
            System.Collections.IEnumerator iterator;

            records = intIndex.Get(fromInclusive, tillInclusive);
            iterator = intIndex.GetEnumerator(fromInclusive, tillInclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromInclusive, tillExclusive);
            iterator = intIndex.GetEnumerator(fromInclusive, tillExclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromExclusive, tillInclusive);
            iterator = intIndex.GetEnumerator(fromExclusive, tillInclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromExclusive, tillExclusive);
            iterator = intIndex.GetEnumerator(fromExclusive, tillExclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromInclusive, null);
            iterator = intIndex.GetEnumerator(fromInclusive, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromExclusive, null);
            iterator = intIndex.GetEnumerator(fromExclusive, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(null, tillInclusive);
            iterator = intIndex.GetEnumerator(null, tillInclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(null, tillExclusive);
            iterator = intIndex.GetEnumerator(null, tillExclusive, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(null, null);
            iterator = intIndex.GetEnumerator(null, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = intIndex.Get(fromInclusive, tillInclusive);
            iterator = intIndex.GetEnumerator(fromInclusive, tillInclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(fromInclusive, tillExclusive);
            iterator = intIndex.GetEnumerator(fromInclusive, tillExclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(fromExclusive, tillInclusive);
            iterator = intIndex.GetEnumerator(fromExclusive, tillInclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(fromExclusive, tillExclusive);
            iterator = intIndex.GetEnumerator(fromExclusive, tillExclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(fromInclusive, null);
            iterator = intIndex.GetEnumerator(fromInclusive, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(fromExclusive, null);
            iterator = intIndex.GetEnumerator(fromExclusive, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(null, tillInclusive);
            iterator = intIndex.GetEnumerator(null, tillInclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(null, tillExclusive);
            iterator = intIndex.GetEnumerator(null, tillExclusive, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = intIndex.Get(null, null);
            iterator = intIndex.GetEnumerator(null, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromInclusiveStr, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, tillInclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromInclusiveStr, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, tillExclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromExclusiveStr, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, tillInclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromExclusiveStr, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, tillExclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromInclusiveStr, null);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromExclusiveStr, null);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(null, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(null, tillInclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(null, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(null, tillExclusiveStr, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(null, null);
            iterator = strIndex.GetEnumerator(null, null, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = 0; iterator.MoveNext(); j++)
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[j]);
            }
            Assert.That(j == records.Length);

            records = strIndex.Get(fromInclusiveStr, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, tillInclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromInclusiveStr, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, tillExclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromExclusiveStr, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, tillInclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromExclusiveStr, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, tillExclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromInclusiveStr, null);
            iterator = strIndex.GetEnumerator(fromInclusiveStr, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(fromExclusiveStr, null);
            iterator = strIndex.GetEnumerator(fromExclusiveStr, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(null, tillInclusiveStr);
            iterator = strIndex.GetEnumerator(null, tillInclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(null, tillExclusiveStr);
            iterator = strIndex.GetEnumerator(null, tillExclusiveStr, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            records = strIndex.Get(null, null);
            iterator = strIndex.GetEnumerator(null, null, IndexSortOrder.Descent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior.  
            for (j = records.Length; iterator.MoveNext(); )
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior.  
                Assert.That(iterator.Current == records[--j]);
            }
            Assert.That(j == 0);

            if (i % 100 == 0)
            {
                Console.Out.Write("Iteration " + i + "\r");
            }
        }
        Console.Out.WriteLine("\nElapsed time for performing " + nRecords * 36 + " index range searches: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        strIndex.Clear();
        intIndex.Clear();

        Assert.That(!strIndex.GetEnumerator().MoveNext());
        Assert.That(!intIndex.GetEnumerator().MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Ascent).MoveNext());
        Assert.That(!strIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        Assert.That(!intIndex.GetEnumerator(null, null, IndexSortOrder.Descent).MoveNext());
        db.Commit();
        db.Gc();
        db.Close();
    }
}
