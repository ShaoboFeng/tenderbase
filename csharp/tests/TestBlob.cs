using System;
using System.IO;

using TenderBase;

public class TestBlob
{
    [STAThread]
    public static void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testblob.dbs");
        Index root = (Index) db.GetRoot();
        byte[] buf = new byte[1024];
        int rc;
        FileInfo dir = new FileInfo("../../src");
        string[] files = Directory.GetFileSystemEntries(dir.FullName);
        if (root == null)
        {
            root = db.CreateIndex(typeof(string), true);
            db.SetRoot(root);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".cs"))
                {
                    FileStream streamIn = new FileStream(files[i], FileMode.Open, FileAccess.Read);
                    IBlob blob = db.CreateBlob();
                    Stream streamOut = blob.GetOutputStream(false);
                    while ((rc = streamIn.Read(buf, 0, buf.Length)) > 0)
                    {
                        streamOut.Write(buf, 0, rc);
                    }
                    root.Put(files[i], blob);
                    streamIn.Close();
                    streamOut.Close();
                }
            }
            Console.Out.WriteLine("Database is initialized");
        }

        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].EndsWith(".cs"))
            {
                byte[] buf2 = new byte[1024];
                IBlob blob = (IBlob) root.Get(files[i]);
                if (blob == null)
                {
                    Console.Error.WriteLine("File " + files[i] + " not found in database");
                    continue;
                }
                Stream bin = blob.InputStream;
                Stream fin = new FileStream(files[i], FileMode.Open, FileAccess.Read);
                while ((rc = fin.Read(buf, 0, buf.Length)) > 0)
                {
                    int rc2 = bin.Read(buf2, 0, buf2.Length);
                    if (rc != rc2)
                    {
                        Console.Error.WriteLine("Different file size: " + rc + " .vs. " + rc2);
                        break;
                    }
                    while (--rc >= 0 && buf[rc] == buf2[rc])
                        ;
                    if (rc >= 0)
                    {
                        Console.Error.WriteLine("Content of the files is different: " + buf[rc] + " .vs. " + buf2[rc2]);
                        break;
                    }
                }
                bin.Close();
                fin.Close();
            }
        }

        Console.Out.WriteLine("Verification completed");
        db.Close();
    }
}
