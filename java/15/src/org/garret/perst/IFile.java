package org.garret.perst;

/**
 * Interface of file.
 * Programmer can provide its own implementation of this interface, adding such features
 * as support for flash cards, encrypted files, etc.
 * Implementation of this interface should throw StorageError exception in case of failure
 */
public interface IFile { 
    /**
     * Write data to a file
     * @param pos offset in the file
     * @param buf array with data to be writer (size is always equal to database page size)
     */
    void write(long pos, byte[] buf);

    /**
     * Read data from the file
     * @param pos offset in the file
     * @param buf array to receive read data (size is always equal to database page size)
     * @return number of bytes actually read
     */
    int read(long pos, byte[] buf);

    /**
     * Flush all file changes to the disk
     */
    void sync();
        
    /**
     * Lock file
     * @return <code>true</code> if file was successfully locked or locking in not implemented,
     * <code>false</code> if file is locked by some other application     
     */
    boolean lock();

    /**
     * Close file
     */
    void close();

    /**
     * Length of the file
     * @return length of file in bytes
     */
    long length();
}
