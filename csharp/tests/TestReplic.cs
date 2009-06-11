using System;
using System.Collections;
using TenderBase;

public class TestReplic
{
    internal class Record : Persistent
    {
        internal int key;
    }

    internal const int nIterations = 1000000;
    internal const int nRecords = 1000;
    internal const int transSize = 100;
    internal const int defaultPort = 6000;
    internal const int asyncBufSize = 1024 * 1024;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    private static void Usage()
    {
        Console.Error.WriteLine("Usage: TestReplic (master|slave) [port] [-async] [-ack]");
    }

    static public void Master(int port, bool async, bool ack)
    {
        ReplicationMasterStorage db = StorageFactory.Instance.CreateReplicationMasterStorage(new string[] { "localhost:" + port }, async ? asyncBufSize : 0);
        db.SetProperty("perst.file.noflush", true);
        db.SetProperty("perst.replication.ack", ack);
        db.Open("master.dbs", pagePoolSize);

        FieldIndex root = (FieldIndex)db.GetRoot();
        if (root == null)
        {
            root = db.CreateFieldIndex(typeof(Record), "key", true);
            db.SetRoot(root);
        }

        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        for (int i = 0; i < nIterations; i++)
        {
            if (i >= nRecords)
                root.Remove(new Key(i - nRecords));

            Record rec = new Record();
            rec.key = i;
            root.Put(rec);
            if (i >= nRecords && i % transSize == 0)
                db.Commit();
        }

        db.Close();
        Console.Out.WriteLine("Elapsed time for " + nIterations + " iterations: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
    }

    static public void Slave(int port, bool async, bool ack)
    {
        ReplicationSlaveStorage db = StorageFactory.Instance.CreateReplicationSlaveStorage(port);
        db.SetProperty("perst.file.noflush", true);
        db.SetProperty("perst.replication.ack", ack);
        db.Open("slave.dbs", pagePoolSize);
        long total = 0;
        int n = 0;
        while (db.Connected)
        {
            db.WaitForModification();
            db.BeginThreadTransaction(StorageConstants.REPLICATION_SLAVE_TRANSACTION);
            FieldIndex root = (FieldIndex)db.GetRoot();
            if (root != null && root.Size() == nRecords)
            {
                long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
                IEnumerator iterator = root.GetEnumerator();
                int prevKey = ((Record)iterator.Current).key;
                int i;
                for (i = 1; iterator.MoveNext(); i++)
                {
                    int key = ((Record)iterator.Current).key;
                    Assert.That(key == prevKey + 1);
                    prevKey = key;
                }

                Assert.That(i == nRecords);
                n += 1;
                total += ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start);
            }

            db.EndThreadTransaction();
        }
        db.Close();
        Console.Out.WriteLine("Elapsed time for " + n + " iterations: " + total + " milliseconds");

    }

    [STAThread]
    static public void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Usage();
            return;
        }

        int port = defaultPort;
        bool ack = false;
        bool async = false;
        foreach (string arg in args)
        {
            if (arg.StartsWith("-"))
            {
                if (arg.Equals("-async"))
                    async = true;
                else if (arg.Equals("-ack"))
                    ack = true;
                else
                    Usage();
            }
            else
                port = System.Int32.Parse(arg);
        }

        string mode = args[0];
        if ("master".Equals(mode))
        {
            Master(port, async, ack);
        }
        else if ("slave".Equals(mode))
        {
            Slave(port, async, ack);
        }
        else
        {
            Usage();
        }
    }
}
