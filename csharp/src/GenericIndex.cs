namespace TenderBase
{
    using System;
    using System.Collections;

    public enum IndexSortOrder
    {
        Ascent = 0,
        Descent = 1
    }

    /// <summary> Interface of object index.
    /// This is base interface for Index and FieldIndex, allowing to write generic algorithms
    /// working with both itype of indices.
    /// </summary>
    public interface GenericIndex : IPersistent, IResource
    {
        //UPGRADE_NOTE: Members of interface 'GenericIndex' were extracted into structure 'GenericIndex_Fields'.
        /// <summary> Gets type of index key</summary>
        /// <returns> type of index key
        /// </returns>
        Type KeyType
        {
            get;
        }

        /// <summary> Get object by key (exact match)     </summary>
        /// <param name="key">specified key. It should match with type of the index and should be inclusive.
        /// </param>
        /// <returns> object with this value of the key or <code>null</code> if key not found
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_UNIQUE)">exception if there are more than
        /// one objects in the index with specified value of the key.
        /// </exception>
        IPersistent Get(Key key);

        /// <summary> Get objects which key value belongs to the specified range.
        /// Either from boundary, either till boundary either both of them can be <code>null</code>.
        /// In last case the method returns all objects from the index.
        /// </summary>
        /// <param name="from">low boundary. If <code>null</code> then low boundary is not specified.
        /// Low boundary can be inclusive or exclusive.
        /// </param>
        /// <param name="till">high boundary. If <code>null</code> then high boundary is not specified.
        /// High boundary can be inclusive or exclusive.
        /// </param>
        /// <returns> array of objects which keys belongs to the specified interval, ordered by key value
        /// </returns>
        IPersistent[] Get(Key from, Key till);

        /// <summary> Get object by string key (exact match)     </summary>
        /// <param name="key">string key
        /// </param>
        /// <returns> object with this value of the key or <code>null</code> if key not[ found
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_UNIQUE)">exception if there are more than
        /// one objects in the index with specified value of the key.
        /// </exception>
        IPersistent Get(string key);

        /// <summary> Get objects with string key prefix </summary>
        /// <param name="prefix">string key prefix
        /// </param>
        /// <returns> array of objects which key starts with this prefix
        /// </returns>
        IPersistent[] GetPrefix(string prefix);

        /// <summary> Locate all objects which key is prefix of specified word.</summary>
        /// <param name="word">string which prefixes are located in index
        /// </param>
        /// <returns> array of objects which key is prefix of specified word, ordered by key value
        /// </returns>
        IPersistent[] PrefixSearch(string word);

        /// <summary> Get number of objects in the index</summary>
        /// <returns> number of objects in the index
        /// </returns>
        int Size();

        /// <summary> Remove all objects from the index</summary>
        void Clear();

        /// <summary> Get all objects in the index as array ordered by index key.</summary>
        /// <returns> array of objects in the index ordered by key value
        /// </returns>
        IPersistent[] ToPersistentArray();

        /// <summary> Get all objects in the index as array ordered by index key.
        /// The runtime type of the returned array is that of the specified array.
        /// If the index fits in the specified array, it is returned therein.
        /// Otherwise, a new array is allocated with the runtime type of the
        /// specified array and the size of this index.<p>
        ///
        /// If this index fits in the specified array with room to spare
        /// (i.e., the array has more elements than this index), the element
        /// in the array immediately following the end of the index is set to
        /// <tt>null</tt>. This is useful in determining the length of this
        /// index <i>only</i> if the caller knows that this index does
        /// not contain any <tt>null</tt> elements.)<p>
        /// </summary>
        /// <param name="arr">specified array
        /// </param>
        /// <returns> array of all objects in the index
        /// </returns>
        IPersistent[] ToPersistentArray(IPersistent[] arr);

        /// <summary> Get iterator for traversing all objects in the index.
        /// Objects are iterated in the ascent key order.
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <returns> index iterator
        /// </returns>
        IEnumerator GetEnumerator();

        /// <summary> Get iterator for traversing all entries in the index.
        /// Iterator next() method returns object implementing <code>Map.Entry</code> interface
        /// which allows to get entry key and value.
        /// Objects are iterated in the ascent key order.
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <returns> index iterator
        /// </returns>
        IEnumerator GetEntryEnumerator();

        /// <summary> Get iterator for traversing objects in the index with key belonging to the specified range.
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <param name="from">low boundary. If <code>null</code> then low boundary is not specified.
        /// Low boundary can be inclusive or exclusive.
        /// </param>
        /// <param name="till">high boundary. If <code>null</code> then high boundary is not specified.
        /// High boundary can be inclusive or exclusive.
        /// </param>
        /// <param name="order"><code>IndexSortOrder.Ascent</code> or <code>IndexSortOrder.Descent</code>
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator GetEnumerator(Key from, Key till, IndexSortOrder order);

        /// <summary> Get iterator for traversing index entries with key belonging to the specified range.
        /// Iterator next() method returns object implementing <code>Map.Entry</code> interface
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <param name="from">low boundary. If <code>null</code> then low boundary is not specified.
        /// Low boundary can be inclusive or exclusive.
        /// </param>
        /// <param name="till">high boundary. If <code>null</code> then high boundary is not specified.
        /// High boundary can be inclusive or exclusive.
        /// </param>
        /// <param name="order"><code>IndexSortOrder.Ascent</code> or <code>IndexSortOrder.Descent</code>
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator GetEntryEnumerator(Key from, Key till, IndexSortOrder order);

        /// <summary> Get iterator for records which keys started with specified prefix
        /// Objects are iterated in the ascent key order.
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <param name="prefix">key prefix
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator PrefixIterator(string prefix);
    }
}

