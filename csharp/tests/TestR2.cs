using System;
using TenderBase;

public class TestR2 : Persistent
{
    internal class SpatialObject : Persistent
    {
        internal RectangleR2 rect;

        public override string ToString()
        {
            return rect.ToString();
        }
    }

    internal SpatialIndexR2 index;
    internal const int nObjectsInTree = 1000;
    internal const int nIterations = 100000;

    [STAThread]
    public static void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        if (args.Length > 0 && "noflush".Equals(args[0]))
            db.SetProperty("perst.file.noflush", true);

        db.Open("testr2.dbs");
        TestR2 root = (TestR2) db.GetRoot();
        if (root == null)
        {
            root = new TestR2();
            root.index = db.CreateSpatialIndexR2();
            db.SetRoot(root);
        }

        RectangleR2[] rectangles = new RectangleR2[nObjectsInTree];
        long key = 1999;
        for (int i = 0; i < nIterations; i++)
        {
            int j = i % nObjectsInTree;
            if (i >= nObjectsInTree)
            {
                RectangleR2 r = rectangles[j];
                IPersistent[] sos = root.index.Get(r);
                IPersistent po = null;
                int n = 0;
                for (int k = 0; k < sos.Length; k++)
                {
                    SpatialObject so = (SpatialObject) sos[k];
                    if (r.Equals(so.rect))
                    {
                        po = so;
                    }
                    else
                    {
                        Assert.That(r.Intersects(so.rect));
                    }
                }

                Assert.That(po != null);
                for (int k = 0; k < nObjectsInTree; k++)
                {
                    if (r.Intersects(rectangles[k]))
                    {
                        n += 1;
                    }
                }

                Assert.That(n == sos.Length);

                System.Collections.IEnumerator iterator = root.index.GetEnumerator(r);
                for (int k = 0; iterator.MoveNext(); k++)
                {
                    n -= 1;
                    Assert.That(iterator.Current == sos[k]);
                }

                Assert.That(n == 0);

                root.index.Remove(r, po);
                po.Deallocate();
            }

            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int top = (int) (key % 1000);
            int left = (int) (key / 1000 % 1000);
            key = (3141592621L * key + 2718281829L) % 1000000007L;
            int bottom = top + (int) (key % 100);
            int right = left + (int) (key / 100 % 100);
            SpatialObject so2 = new SpatialObject();
            RectangleR2 r2 = new RectangleR2(top, left, bottom, right);
            so2.rect = r2;
            rectangles[j] = r2;
            root.index.Put(r2, so2);

            if (i % 100 == 0)
            {
                Console.Out.Write("Iteration " + i + "\r");
                Console.Out.Flush();
                db.Commit();
            }

        }
        root.index.Clear();
        Console.Out.WriteLine("\nElapsed time " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start));
        db.Close();
    }
}

