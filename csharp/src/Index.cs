namespace TenderBase
{
    using System;
    
    /// <summary> Interface of object index.
    /// Index is used to provide fast access to the object by key.
    /// Object in the index are stored ordered by key value.
    /// It is possible to select object using exact value of the key or
    /// select set of objects which key belongs to the specified interval
    /// (each boundary can be specified or unspecified and can be inclusive or exclusive)
    /// Key should be of scalar, String, java.util.Date or peristent object type.
    /// </summary>
    public interface Index : GenericIndex
    {
        /// <summary> Put new object in the index. </summary>
        /// <param name="key">object key
        /// </param>
        /// <param name="obj">object associated with this key. Object can be not yet peristent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        /// <returns> <code>true</code> if object is successfully inserted in the index,
        /// <code>false</code> if index was declared as unique and there is already object with such value
        /// of the key in the index.
        /// </returns>
        bool Put(Key key, IPersistent obj);

        /// <summary> Associate new value with the key. If there is already object with such key in the index,
        /// then it will be removed from the index and new value associated with this key.
        /// </summary>
        /// <param name="key">object key
        /// </param>
        /// <param name="obj">object associated with this key. Object can be not yet peristent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        /// <returns> object previously associated with this key, <code>null</code> if there was no such object
        /// </returns>
        IPersistent Set(Key key, IPersistent obj);

        /// <summary> Remove object with specified key from the index</summary>
        /// <param name="key">value of the key of removed object
        /// </param>
        /// <param name="obj">object removed from the index
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the index
        /// </exception>
        void Remove(Key key, IPersistent obj);

        /// <summary> Remove key from the unique index.</summary>
        /// <param name="key">value of removed key
        /// </param>
        /// <returns> removed object
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the index,
        /// or StorageError(StorageError.KEY_NOT_UNIQUE) if index is not unique.
        /// </exception>
        IPersistent Remove(Key key);

        /// <summary> Put new object in the string index. </summary>
        /// <param name="key">string key
        /// </param>
        /// <param name="obj">object associated with this key. Object can be not yet peristent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        /// <returns> <code>true</code> if object is successfully inserted in the index,
        /// <code>false</code> if index was declared as unique and there is already object with such value
        /// of the key in the index.
        /// </returns>
        bool Put(string key, IPersistent obj);

        /// <summary> Associate new value with string key. If there is already object with such key in the index,
        /// then it will be removed from the index and new value associated with this key.
        /// </summary>
        /// <param name="key">string key
        /// </param>
        /// <param name="obj">object associated with this key. Object can be not yet peristent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        /// <returns> object previously associated with this key, <code>null</code> if there was no such object
        /// </returns>
        IPersistent Set(string key, IPersistent obj);

        /// <summary> Remove object with specified string key from the index</summary>
        /// <param name="key">value of the key of removed object
        /// </param>
        /// <param name="obj">object removed from the index
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the index
        /// </exception>
        void Remove(string key, IPersistent obj);

        /// <summary> Remove key from the unique string index.</summary>
        /// <param name="key">value of removed key
        /// </param>
        /// <returns> removed object
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the index,
        /// or StorageError(StorageError.KEY_NOT_UNIQUE) if index is not unique.
        /// </exception>
        IPersistent Remove(string key);
    }
}

