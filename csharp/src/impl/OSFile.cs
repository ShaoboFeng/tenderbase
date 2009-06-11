namespace TenderBaseImpl
{
    using System;
    using System.IO;
    using TenderBase;
    
    public class OSFile : IFile
    {
        public virtual void Write(long pos, byte[] buf)
        {
            try
            {
                file.Seek(pos, System.IO.SeekOrigin.Begin);
                file.Write(buf, 0, buf.Length);
            }
            catch (IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual int Read(long pos, byte[] buf)
        {
            try
            {
                file.Seek(pos, SeekOrigin.Begin);
                int read = file.Read(buf, 0, buf.Length);
                return read;
            }
            catch (System.IO.IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual void Sync()
        {
            if (noFlush)
                return;

            try
            {
                file.Flush();
            }
            catch (System.IO.IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual void Close()
        {
            try
            {
                file.Close();
            }
            catch (System.IO.IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual bool Lock()
        {
            try
            {
                file.Lock(0, file.Length);
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }

        internal static readonly long MAX_FILE_SIZE = Int64.MaxValue - 2;

        public OSFile(string filePath, bool readOnly, bool noFlush)
        {
            this.noFlush = noFlush;
            try
            {
                if (readOnly)
                    file = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read);
                else
                    file = new System.IO.FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch (System.IO.IOException x)
            {
                throw new StorageError(StorageError.FILE_ACCESS_ERROR, x);
            }
        }

        public virtual long Length()
        {
            try
            {
                return file.Length;
            }
            catch (System.IO.IOException)
            {
                return -1;
            }
        }

        protected internal FileStream file;
        protected internal bool noFlush;
    }
}

