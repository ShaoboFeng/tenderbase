namespace TenderBase
{
    using System;
    
    /// <summary> Interface of file.
    /// Programmer can provide its own impleentation of this interface, adding such features
    /// as support of flash cards, encrypted files,...
    /// Implentation of this interface should throw StorageError exception in case of failure
    /// </summary>
    public interface IFile
    {
        /// <summary> Write data to the file</summary>
        /// <param name="pos">offset in the file
        /// </param>
        /// <param name="buf">array with data to be writter (size is always equal to database page size)
        /// </param>
        void Write(long pos, byte[] buf);

        /// <summary> Reade data from the file</summary>
        /// <param name="pos">offset in the file
        /// </param>
        /// <param name="buf">array to receive readen data (size is always equal to database page size)
        /// </param>
        /// <returns> number of bytes actually readen
        /// </returns>
        int Read(long pos, byte[] buf);

        /// <summary> Flush all fiels changes to the disk</summary>
        void Sync();

        /// <summary> Lock file</summary>
        /// <returns> <code>true</code> if file was successfully locked or locking in not implemented,
        /// <code>false</code> if file is locked by some other applciation
        /// </returns>
        bool Lock();

        /// <summary> Close file</summary>
        void Close();

        /// <summary> Length of the file</summary>
        long Length();
    }
}

