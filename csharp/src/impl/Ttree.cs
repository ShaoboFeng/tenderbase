namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    public class Ttree : PersistentResource, SortedCollection
    {
        /// <summary> Get comparator used in this collection</summary>
        /// <returns> collection comparator
        /// </returns>
        public virtual PersistentComparator Comparator
        {
            get
            {
                return comparator;
            }
        }

        private PersistentComparator comparator;
        private bool unique;
        private TtreePage root;
        private int nMembers;

        private Ttree()
        {
        }

        internal Ttree(PersistentComparator comparator, bool unique)
        {
            this.comparator = comparator;
            this.unique = unique;
        }

        public override bool RecursiveLoading
        {
            get
            {
                return false;
            }
        }

        /*
        * Get member with specified key.
        * @param key specified key. It should match with type of the index and should be inclusive.
        * @return object with this value of the key or <code>null</code> if key not found
        * @exception StorageError(StorageError.KEY_NOT_UNIQUE) exception if there are more than
        * one objects in the collection with specified value of the key.
        */
        public virtual IPersistent Get(object key)
        {
            if (root != null)
            {
                ArrayList list = new ArrayList();
                root.Find(comparator, key, 1, key, 1, list);
                if (list.Count > 1)
                {
                    throw new StorageError(StorageError.KEY_NOT_UNIQUE);
                }
                else if (list.Count == 0)
                {
                    return null;
                }
                else
                {
                    return (IPersistent) list[0];
                }
            }
            return null;
        }

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
        internal static readonly IPersistent[] emptySelection = new IPersistent[0];

        public virtual IPersistent[] Get(object from, object till)
        {
            return Get(from, true, till, true);
        }

        public virtual IPersistent[] Get(object from, bool fromInclusive, object till, bool tillInclusive)
        {
            if (root != null)
            {
                ArrayList list = new ArrayList();
                root.Find(comparator, from, fromInclusive ? 1 : 0, till, tillInclusive ? 1 : 0, list);
                if (list.Count != 0)
                {
                    return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
                }
            }
            return emptySelection;
        }

        /// <summary> Add new member to collection</summary>
        /// <param name="obj">new member
        /// </param>
        /// <returns> <code>true</code> if object is successfully added in the index,
        /// <code>false</code> if collection was declared as unique and there is already member with such value
        /// of the key in the collection.
        /// </returns>
        public virtual bool Add(IPersistent obj)
        {
            TtreePage newRoot;
            if (root == null)
            {
                newRoot = new TtreePage(obj);
            }
            else
            {
                TtreePage.PageReference ref_Renamed = new TtreePage.PageReference(root);
                if (root.Insert(comparator, obj, unique, ref_Renamed) == TtreePage.NOT_UNIQUE)
                {
                    return false;
                }
                newRoot = ref_Renamed.pg;
            }
            root = newRoot;
            nMembers += 1;
            Modify();
            return true;
        }

        /// <summary> Check if collections contains specified member</summary>
        /// <returns> <code>true</code> if specified member belongs to the collection
        /// </returns>
        public virtual bool Contains(IPersistent member)
        {
            return (root != null) ? root.Contains(comparator, member) : false;
        }

        /// <summary> Remove member from collection</summary>
        /// <param name="obj">member to be removed
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the collection
        /// </exception>
        public virtual void Remove(IPersistent obj)
        {
            if (root == null)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            TtreePage.PageReference ref_Renamed = new TtreePage.PageReference(root);
            if (root.Remove(comparator, obj, ref_Renamed) == TtreePage.NOT_FOUND)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            root = ref_Renamed.pg;
            nMembers -= 1;
            Modify();
        }

        /// <summary> Get number of objects in the collection</summary>
        /// <returns> number of objects in the collection
        /// </returns>
        public virtual int Size()
        {
            return nMembers;
        }

        /// <summary> Remove all objects from the collection</summary>
        public virtual void Clear()
        {
            if (root != null)
            {
                root.Prune();
                root = null;
                nMembers = 0;
                Modify();
            }
        }

        /// <summary> T-Tree destructor</summary>
        public override void Deallocate()
        {
            if (root != null)
            {
                root.Prune();
            }
            base.Deallocate();
        }

        /// <summary> Get all objects in the index as array ordered by index key.</summary>
        /// <returns> array of objects in the index ordered by key value
        /// </returns>
        public virtual IPersistent[] ToPersistentArray()
        {
            if (root == null)
            {
                return emptySelection;
            }
            IPersistent[] arr = new IPersistent[nMembers];
            root.ToArray(arr, 0);
            return arr;
        }

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
        public virtual IPersistent[] ToPersistentArray(IPersistent[] arr)
        {
            if (arr.Length < nMembers)
            {
                arr = (IPersistent[]) System.Array.CreateInstance(arr.GetType().GetElementType(), nMembers);
            }
            if (root != null)
            {
                root.ToArray(arr, 0);
            }
            if (arr.Length > nMembers)
            {
                arr[nMembers] = null;
            }
            return arr;
        }

        internal class TtreeIterator : IEnumerator
        {
            public virtual object Current
            {
                get
                {
                    if (i + 1 >= list.Count)
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    removed = false;
                    return list[++i];
                }
            }

            internal int i;
            internal ArrayList list;
            internal bool removed;
            internal Ttree tree;

            internal TtreeIterator(Ttree tree, ArrayList list)
            {
                this.tree = tree;
                this.list = list;
                i = -1;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                if (removed || i < 0 || i >= list.Count)
                {
                    throw new System.SystemException();
                }
                tree.Remove((IPersistent) list[i]);
                list.RemoveAt(i--);
                removed = true;
            }

            public virtual bool MoveNext()
            {
                return i + 1 < list.Count;
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        /// <summary> Get iterator for traversing all collection members.
        /// You should not update/remove or add members to the index during iteration
        /// </summary>
        /// <returns> collection iterator
        /// </returns>
        public virtual IEnumerator GetEnumerator()
        {
            return GetEnumerator((object)null, (object)null);
        }

        public virtual IEnumerator GetEnumerator(object from, object till)
        {
            return GetEnumerator(from, true, till, true);
        }

        public virtual IEnumerator GetEnumerator(object from, bool fromInclusive, object till, bool tillInclusive)
        {
            ArrayList list = new ArrayList();
            if (root != null)
            {
                root.Find(comparator, from, fromInclusive ? 1 : 0, till, tillInclusive ? 1 : 0, list);
            }
            return new TtreeIterator(this, list);
        }
    }
}

