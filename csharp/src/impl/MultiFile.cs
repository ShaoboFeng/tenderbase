#if !OMIT_MULTIFILE
namespace TenderBaseImpl
{
    using System;
    using System.IO;
    using TenderBase;
    
    public class MultiFile : IFile
    {
        internal class MultiFileSegment
        {
            //UPGRADE_TODO: Class 'java.io.RandomAccessFile' was converted to 'System.IO.FileStream' which has a different behavior.
            internal FileStream f;
            internal string name;
            internal long size;
        }

        internal virtual void Seek(long pos)
        {
            currSeg = 0;
            currOffs = 0;
            currPos = 0;
            while (pos > segment[currSeg].size)
            {
                currPos += segment[currSeg].size;
                pos -= segment[currSeg].size;
                currSeg += 1;
            }
            segment[currSeg].f.Seek(pos, SeekOrigin.Begin);
            pos = segment[currSeg].f.Position;
            currOffs += pos;
            currPos += pos;
        }

        public virtual void Write(long pos, byte[] b)
        {
            try
            {
                Seek(pos);
                int len = b.Length;
                int off = 0;
                while (len > 0)
                {
                    int toWrite = len;
                    if (len + currOffs > segment[currSeg].size)
                    {
                        toWrite = (int) (segment[currSeg].size - currOffs);
                    }
                    segment[currSeg].f.Write(b, off, toWrite);
                    currPos += toWrite;
                    currOffs += toWrite;
                    off += toWrite;
                    len -= toWrite;
                    if (currOffs == segment[currSeg].size)
                    {
                        segment[++currSeg].f.Seek(0, SeekOrigin.Begin);
                        currOffs = 0;
                    }
                }
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual int Read(long pos, byte[] b)
        {
            try
            {
                Seek(pos);
                int totalRead = 0;
                int len = b.Length;
                int off = 0;
                while (len > 0)
                {
                    int toRead = len;
                    if (len + currOffs > segment[currSeg].size)
                    {
                        toRead = (int) (segment[currSeg].size - currOffs);
                    }
                    int rc = segment[currSeg].f.Read(b, off, toRead);
                    if (rc >= 0)
                    {
                        currPos += rc;
                        currOffs += rc;
                        totalRead += rc;
                        if (currOffs == segment[currSeg].size)
                        {
                            segment[++currSeg].f.Seek(0, SeekOrigin.Begin);
                            currOffs = 0;
                        }
                    }
                    else
                    {
                        return (totalRead == 0) ? rc : totalRead;
                    }
                    if (rc != toRead)
                    {
                        return totalRead;
                    }
                    off += rc;
                    len -= rc;
                }
                return totalRead;
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual void Sync()
        {
            if (!noFlush)
            {
                try
                {
                    for (int i = segment.Length; --i >= 0; )
                    {
                        segment[i].f.Flush();
                    }
                }
                catch (IOException x)
                {
                    throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
                }
            }
        }

        public virtual bool Lock()
        {
            FileStream f = segment[0].f;
            try
            {
                f.Lock(0, f.Length);
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }

        public virtual void Close()
        {
            try
            {
                for (int i = segment.Length; --i >= 0; )
                {
                    segment[i].f.Close();
                }
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public MultiFile(string[] segmentPath, long[] segmentSize, bool readOnly, bool noFlush)
        {
            this.noFlush = noFlush;
            segment = new MultiFileSegment[segmentPath.Length];
            try
            {
                for (int i = 0; i < segment.Length; i++)
                {
                    MultiFileSegment seg = new MultiFileSegment();
                    if (readOnly)
                        seg.f =  new System.IO.FileStream(segmentPath[i], System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    else
                        seg.f =  new System.IO.FileStream(segmentPath[i], System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);
                    seg.size = segmentSize[i];
                    fixedSize += seg.size;
                    segment[i] = seg;
                }
                fixedSize -= segment[segment.Length - 1].size;
                segment[segment.Length - 1].size = Int64.MaxValue;
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public MultiFile(string filePath, bool readOnly, bool noFlush)
        {
            try
            {
                //UPGRADE_TODO: The differences in the expected value of parameters for constructor 'java.io.BufferedReader.BufferedReader' may cause compilation errors.
                //UPGRADE_WARNING: At least one expression was used more than once in the target code.
                //UPGRADE_TODO: Constructor 'java.io.FileReader.FileReader' was converted to 'System.IO.StreamReader' which has a different behavior.
                SupportClass.StreamTokenizerSupport streamIn = new SupportClass.StreamTokenizerSupport(new StreamReader(new StreamReader(filePath, System.Text.Encoding.Default).BaseStream, new StreamReader(filePath, System.Text.Encoding.Default).CurrentEncoding));
                this.noFlush = noFlush;
                segment = new MultiFileSegment[0];
                int tkn = streamIn.NextToken();
                do
                {
                    MultiFileSegment seg = new MultiFileSegment();
                    if (tkn != SupportClass.StreamTokenizerSupport.TT_WORD)
                    {
                        throw new IOException("Multifile segment name expected");
                    }
                    seg.name = streamIn.sval;
                    tkn = streamIn.NextToken();
                    if (tkn != SupportClass.StreamTokenizerSupport.TT_EOF)
                    {
                        if (tkn != SupportClass.StreamTokenizerSupport.TT_NUMBER)
                        {
                            throw new StorageError(StorageError.FILE_ACCESS_ERROR, "Multifile segment size expected");
                        }
                        //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                        seg.size = (long) streamIn.nval * 1024; // kilobytes
                        tkn = streamIn.NextToken();
                    }
                    fixedSize += seg.size;
                    if (readOnly)
                        seg.f =  new System.IO.FileStream(seg.name, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    else
                        seg.f =  new System.IO.FileStream(seg.name, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);

                    MultiFileSegment[] newSegment = new MultiFileSegment[segment.Length + 1];
                    Array.Copy(segment, 0, newSegment, 0, segment.Length);
                    newSegment[segment.Length] = seg;
                    segment = newSegment;
                }
                while (tkn != SupportClass.StreamTokenizerSupport.TT_EOF);

                fixedSize -= segment[segment.Length - 1].size;
                segment[segment.Length - 1].size = Int64.MaxValue;
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual long Length()
        {
            try
            {
                return fixedSize + segment[segment.Length - 1].f.Length;
            }
            catch (IOException)
            {
                return -1;
            }
        }

        internal MultiFileSegment[] segment;
        internal long currPos;
        internal long currOffs;
        internal long fixedSize;
        internal int currSeg;
        internal bool noFlush;
    }
}
#endif
