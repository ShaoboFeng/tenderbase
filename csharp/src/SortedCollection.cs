namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Interface of sorted collection.
    /// Sorted collections keeps in members in order specified by comparator.
    /// Members in the collections can be located using key or range of keys.
    /// The SortedCollection is efficient container of objects for in-memory databases.
    /// For databases which size is significatly larger than size of page pool, operation with SortedList
    /// can cause trashing and so very bad performance. Unlike other index structures SortedCollection
    /// doesn't store values of keys and so search in the collection requires fetching of its members.
    /// </summary>
    public interface SortedCollection : IPersistent, IResource
    {
        /// <summary> Get comparator used in this collection</summary>
        /// <returns> collection comparator
        /// </returns>
        PersistentComparator Comparator
        {
            get;
        }

        /// <summary> Get member with specified key.</summary>
        /// <param name="key">specified key. It should match with type of the index and should be inclusive.
        /// </param>
        /// <returns> object with this value of the key or <code>null</code> if key not found
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_UNIQUE)">exception if there are more than
        /// one objects in the collection with specified value of the key.
        /// </exception>
        IPersistent Get(object key);

        /// <summary> Get members which key value belongs to the specified range.
        /// Either from boundary, either till boundary either both of them can be <code>null</code>.
        /// In last case the method returns all objects from the collection.
        /// </summary>
        /// <param name="from">inclusive low boundary. If <code>null</code> then low boundary is not specified.
        /// </param>
        /// <param name="till">inclusive high boundary. If <code>null</code> then high boundary is not specified.
        /// </param>
        /// <returns> array of objects which keys belongs to the specified interval, ordered by key value
        /// </returns>
        IPersistent[] Get(object from, object till);

        /// <summary> Get members which key value belongs to the specified range.
        /// Either from boundary, either till boundary either both of them can be <code>null</code>.
        /// In last case the method returns all objects from the collection.
        /// </summary>
        /// <param name="from">inclusive low boundary. If <code>null</code> then low boundary is not specified.
        /// </param>
        /// <param name="fromInclusive">specifies whether from boundary is inclusive or exclusive
        /// </param>
        /// <param name="till">inclusive high boundary. If <code>null</code> then high boundary is not specified.
        /// </param>
        /// <param name="tillInclusive">specifies whether till boundary is inclusive or exclusive
        /// </param>
        /// <returns> array of objects which keys belongs to the specified interval, ordered by key value
        /// </returns>
        IPersistent[] Get(object from, bool fromInclusive, object till, bool tillInclusive);

        /// <summary> Add new member to collection</summary>
        /// <param name="obj">new member
        /// </param>
        /// <returns> <code>true</code> if object is successfully added in the index,
        /// <code>false</code> if collection was declared as unique and there is already member with such value
        /// of the key in the collection.
        /// </returns>
        bool Add(IPersistent obj);

        /// <summary> Check if collections contains specified member</summary>
        /// <returns> <code>true</code> if specified member belongs to the collection
        /// </returns>
        bool Contains(IPersistent member);

        /// <summary> Remove member from collection</summary>
        /// <param name="obj">member to be removed
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the collection
        /// </exception>
        void Remove(IPersistent obj);

        /// <summary> Get number of objects in the collection</summary>
        /// <returns> number of objects in the collection
        /// </returns>
        int Size();

        /// <summary> Remove all objects from the collection</summary>
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
        /// <returns> array of objects in the index ordered by key value
        /// </returns>
        IPersistent[] ToPersistentArray(IPersistent[] arr);

        /// <summary> Get iterator for traversing all collection members.</summary>
        /// <returns> collection iterator
        /// </returns>
        IEnumerator GetEnumerator();

        /// <summary> Get iterator for traversing collection members with key belonging to the specified range. </summary>
        /// <param name="from">inclusive low boundary. If <code>null</code> then low boundary is not specified.
        /// </param>
        /// <param name="till">inclusive high boundary. If <code>null</code> then high boundary is not specified.
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator GetEnumerator(object from, object till);

        /// <summary> Get iterator for traversing collection members with key belonging to the specified range. </summary>
        /// <param name="from">inclusive low boundary. If <code>null</code> then low boundary is not specified.
        /// </param>
        /// <param name="fromInclusive">specifies whether from boundary is inclusive or exclusive
        /// </param>
        /// <param name="till">inclusive high boundary. If <code>null</code> then high boundary is not specified.
        /// </param>
        /// <param name="tillInclusive">specifies whether till boundary is inclusive or exclusive
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator GetEnumerator(object from, bool fromInclusive, object till, bool tillInclusive);
    }
}

