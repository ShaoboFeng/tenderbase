// TODOPORT: this needs to be ported and optimized for CLR's InputStream semantics
namespace TenderBaseImpl
{
    using System;
    using System.IO;
    using TenderBase;

    [Serializable]
    public class BlobImpl : PersistentResource, IBlob
    {
        /// <summary> Gets input stream. InputStream.availabel method can be used to get BLOB size</summary>
        /// <returns> input stream with BLOB data
        /// </returns>
        public virtual Stream InputStream
        {
            get
            {
                return new BlobInputStream(this);
            }
        }

        internal int size;
        internal BlobImpl next;
        internal byte[] body;

        internal class BlobInputStream : Stream
        {
            protected internal BlobImpl curr;
            protected internal int pos;
            protected internal int rest;

            public override int ReadByte()
            {
                byte[] b = new byte[1];
                int len = Read(b, 0, 1);
                if (len == 1)
                    return b[0];
                else
                    return -1;
            }

            public override int Read(byte[] b, int off, int len)
            {
                if (len > rest)
                    len = rest;

                int beg = off;
                while (len > 0)
                {
                    if (pos == curr.body.Length)
                    {
                        BlobImpl prev = curr;
                        curr = curr.next;
                        curr.Load();
                        prev.Invalidate();
                        prev.next = null;
                        pos = 0;
                    }

                    int n = len > curr.body.Length - pos ? curr.body.Length - pos : len;
                    Array.Copy(curr.body, pos, b, off, n);
                    pos += n;
                    off += n;
                    len -= n;
                    rest -= n;
                }

                return off - beg;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.io.InputStream.skip' is not an override method.
            public long Skip(long offs)
            {
                if (offs > rest)
                {
                    offs = rest;
                }

                int len = (int) offs;
                while (len > 0)
                {
                    if (pos == curr.body.Length)
                    {
                        BlobImpl prev = curr;
                        curr = curr.next;
                        curr.Load();
                        prev.Invalidate();
                        prev.next = null;
                        pos = 0;
                    }

                    int n = len > curr.body.Length - pos ? curr.body.Length - pos : len;
                    pos += n;
                    len -= n;
                    rest -= n;
                }

                return offs;
            }

            public override void Close()
            {
                curr = null;
                rest = 0;
            }

            protected internal BlobInputStream(BlobImpl first)
            {
                first.Load();
                curr = first;
                rest = first.size;
            }

            public override void Flush()
            {
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public override Int64 Seek(Int64 offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(Int64 value)
            {
                throw new IOException();
            }

            public override void Write(byte[] buffer, Int32 offset, Int32 count)
            {
                throw new IOException();
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override Int64 Length
            {
                get
                {
                    return pos + rest;
                }
            }

            public override Int64 Position
            {
                get
                {
                    return pos;
                }

                set
                {
                }
            }
        }

        internal class BlobOutputStream : Stream
        {
            protected internal BlobImpl first;
            protected internal BlobImpl curr;
            protected internal int pos;
            protected internal bool multisession;

            public void WriteByte(int b)
            {
                byte[] buf = new byte[1];
                buf[0] = (byte) b;
                Write(buf, 0, 1);
            }

            public override void WriteByte(byte b)
            {
                WriteByte((int) b);
            }

            public override void Write(byte[] b, int off, int len)
            {
                while (len > 0)
                {
                    if (pos == curr.body.Length)
                    {
                        BlobImpl next = new BlobImpl(curr.Storage, curr.body.Length);
                        BlobImpl prev = curr;
                        curr = prev.next = next;
                        if (prev != first)
                        {
                            prev.Store();
                            prev.Invalidate();
                            prev.next = null;
                        }

                        pos = 0;
                    }

                    int n = (len > curr.body.Length - pos) ? curr.body.Length - pos : len;
                    Array.Copy(b, off, curr.body, pos, n);
                    off += n;
                    pos += n;
                    len -= n;
                    first.size += n;
                }
            }

            public override void Close()
            {
                if (!multisession && pos < curr.body.Length)
                {
                    byte[] tmp = new byte[pos];
                    Array.Copy(curr.body, 0, tmp, 0, pos);
                    curr.body = tmp;
                }

                curr.Store();
                if (curr != first)
                {
                    first.Store();
                }

                first = curr = null;
            }

            internal BlobOutputStream(BlobImpl first, bool multisession)
            {
                first.Load();
                this.first = first;
                this.multisession = multisession;
                int size = first.size;
                while (first.next != null)
                {
                    size -= first.body.Length;
                    BlobImpl prev = first;
                    first = first.next;
                    first.Load();
                    prev.Invalidate();
                    prev.next = null;
                    pos = 0;
                }

                curr = first;
                pos = size;
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public override void Flush()
            {
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public override Int64 Seek(Int64 offset, SeekOrigin origin)
            {
                return 0;
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public override void SetLength(Int64 value)
            {
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public override Int32 Read(byte[] buffer, Int32 offset, Int32 count)
            {
                return 0;
            }

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
            public override Int64 Length
            {
                get
                {
                    return 0;
                }
            }

            //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
            public override Int64 Position
            {
                get
                {
                    return pos;
                }

                set
                {
                }
            }
        }

        public override bool RecursiveLoading
        {
            get
            {
                return false;
            }
        }

        /// <summary> Get output stream to append data to the BLOB.</summary>
        /// <returns> output srteam
        /// </returns>
        public virtual Stream GetOutputStream()
        {
            return new BlobOutputStream(this, true);
        }

        /// <summary> Get output stream to append data to the BLOB.</summary>
        /// <param name="multisession">whether BLOB allows further appends of data or closing
        /// this output streat means that BLOB will not be changed any more.
        /// </param>
        /// <returns> output srteam
        /// </returns>
        public virtual Stream GetOutputStream(bool multisession)
        {
            return new BlobOutputStream(this, multisession);
        }

        public override void Deallocate()
        {
            Load();
            if (size > 0)
            {
                BlobImpl curr = next;
                while (curr != null)
                {
                    curr.Load();
                    BlobImpl tail = curr.next;
                    curr.Deallocate();
                    curr = tail;
                }
            }

            base.Deallocate();
        }

        internal BlobImpl(Storage storage, int size) : base(storage)
        {
            body = new byte[size];
        }

        internal BlobImpl()
        {
        }
    }
}

