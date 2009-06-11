namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Interface for one-to-many relation. There are two types of relations:
    /// embedded (when references to the relarted obejcts are stored in relation
    /// owner obejct itself) and stanalone (when relation is separate object, which contains
    /// the reference to the relation owner and relation members). Both kinds of relations
    /// implements Link interface. Embedded relation is created by Storage.createLink method
    /// and standalone relation is represented by Relation persistent class created by
    /// Storage.createRelation method.
    /// </summary>
    public interface Link
    {
        /// <summary> Set number of the linked objects </summary>
        /// <param name="newSize">new number of linked objects (if it is greater than original number,
        /// than extra elements will be set to null)
        /// </param>
        int Size
        {
            get;
            set;
        }

        /// <summary> Get related object by index</summary>
        /// <param name="i">index of the object in the relation
        /// </param>
        /// <returns> referenced object
        /// </returns>
        IPersistent Get(int i);

        /// <summary> Get related object by index without loading it.
        /// Returned object can be used only to get it OID or to compare with other objects using
        /// <code>Equals</code> method
        /// </summary>
        /// <param name="i">index of the object in the relation
        /// </param>
        /// <returns> stub representing referenced object
        /// </returns>
        IPersistent GetRaw(int i);

        /// <summary> Replace i-th element of the relation</summary>
        /// <param name="i">index in the relartion
        /// </param>
        /// <param name="obj">object to be included in the relation
        /// </param>
        void Set(int i, IPersistent obj);

        /// <summary> Remove object with specified index from the relation</summary>
        /// <param name="i">index in the relartion
        /// </param>
        void Remove(int i);

        /// <summary> Insert new object in the relation</summary>
        /// <param name="i">insert poistion, should be in [0,Size()]
        /// </param>
        /// <param name="obj">object inserted in the relation
        /// </param>
        void Insert(int i, IPersistent obj);

        /// <summary> Add new object to the relation</summary>
        /// <param name="obj">object inserted in the relation
        /// </param>
        void Add(IPersistent obj);

        /// <summary> Add all elements of the array to the relation</summary>
        /// <param name="arr">array of obects which should be added to the relation
        /// </param>
        void AddAll(IPersistent[] arr);

        /// <summary> Add specified elements of the array to the relation</summary>
        /// <param name="arr">array of obects which should be added to the relation
        /// </param>
        /// <param name="from">index of the first element in the array to be added to the relation
        /// </param>
        /// <param name="length">number of elements in the array to be added in the relation
        /// </param>
        void AddAll(IPersistent[] arr, int from, int length);

        /// <summary> Add all object members of the other relation to this relation</summary>
        /// <param name="link">another relation
        /// </param>
        void AddAll(Link link);

        /// <summary> Return array with relation members. Members are not loaded and
        /// size of the array can be greater than actual number of members.
        /// </summary>
        /// <returns> array of object with relation members used in implementation of Link class
        /// </returns>
        IPersistent[] ToRawArray();

        /// <summary> Get relation members as array of object</summary>
        /// <returns> array of object with relation members
        /// </returns>
        IPersistent[] ToArray();

        /// <summary> Get all relation members as array.
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
        /// <returns> array of object with relation members
        /// </returns>
        IPersistent[] ToArray(IPersistent[] arr);

        /// <summary> Checks if relation contains specified object</summary>
        /// <param name="obj">specified object
        /// </param>
        bool Contains(IPersistent obj);

        /// <summary> Check if i-th element of Link is the same as specified obj</summary>
        /// <param name="i">element index
        /// </param>
        /// <param name="obj">object to compare with
        /// </param>
        /// <returns> <code>true</code> if i-th element of Link reference the same object as "obj"
        /// </returns>
        bool ContainsElement(int i, IPersistent obj);

        /// <summary> Get index of the specified object in the relation</summary>
        /// <param name="obj">specified object
        /// </param>
        /// <returns> zero based index of the object or -1 if object is not in the relation
        /// </returns>
        int IndexOf(IPersistent obj);

        /// <summary> Remove all members from the relation</summary>
        void Clear();

        /// <summary> Get iterator through link members</summary>
        IEnumerator GetEnumerator();

        /// <summary> Replace all direct references to linked objects with stubs.
        /// This method is needed tyo avoid memory exhaustion in case when
        /// there is a large numebr of objectys in databasse, mutually
        /// refefencing each other (each object can directly or indirectly
        /// be accessed from other objects).
        /// </summary>
        void Unpin();

        /// <summary> Replace references to elements with direct references.
        /// It will impove spped of manipulations with links, but it can cause
        /// recursive loading in memory large number of objects and as a result - memory
        /// overflow, because garabge collector will not be able to collect them
        /// </summary>
        void Pin();
    }
}

