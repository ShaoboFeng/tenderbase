namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    class ThickIndex : PersistentResource, Index
    {
        public virtual Type KeyType
        {
            get
            {
                return index.KeyType;
            }
        }

        private Index index;
        private int nElems;

        internal const int BTREE_THRESHOLD = 128;

        internal ThickIndex(Type keyType, StorageImpl db)
            : base(db)
        {
            index = db.CreateIndex(keyType, true);
        }

        internal ThickIndex()
        {
        }

        public virtual IPersistent Get(Key key)
        {
            IPersistent s = index.Get(key);
            if (s == null)
                return null;

            if (s is Relation)
            {
                Relation r = (Relation) s;
                if (r.Size == 1)
                    return r.Get(0);
            }

            throw new StorageError(StorageError.KEY_NOT_UNIQUE);
        }

        public virtual IPersistent[] Get(Key from, Key till)
        {
            return Extend(index.Get(from, till));
        }

        private IPersistent[] Extend(IPersistent[] s)
        {
            ArrayList list = new ArrayList();
            for (int i = 0; i < s.Length; i++)
            {
                IPersistent p = s[i];
                IEnumerator iterator = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (iterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    list.Add(iterator.Current);
                }
            }

            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
        }

        public virtual IPersistent Get(string key)
        {
            return Get(new Key(key));
        }

        public virtual IPersistent[] GetPrefix(string prefix)
        {
            return Extend(index.GetPrefix(prefix));
        }

        public virtual IPersistent[] PrefixSearch(string word)
        {
            return Extend(index.PrefixSearch(word));
        }

        public virtual int Size()
        {
            return nElems;
        }

        public virtual void Clear()
        {
            IEnumerator iterator = index.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (iterator.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                ((IPersistent) iterator.Current).Deallocate();
            }
            index.Clear();
            nElems = 0;
            Modify();
        }

        public virtual IPersistent[] ToPersistentArray()
        {
            return Extend(index.ToPersistentArray());
        }

        public virtual IPersistent[] ToPersistentArray(IPersistent[] arr)
        {
            IPersistent[] s = index.ToPersistentArray();
            ArrayList list = new ArrayList();
            for (int i = 0; i < s.Length; i++)
            {
                IPersistent p = s[i];
                IEnumerator iterator = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (iterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    list.Add(iterator.Current);
                }
            }
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, arr);
        }

        internal class ExtendIterator : IEnumerator
        {
            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    object obj = inner.Current;
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!inner.MoveNext())
                    {
                        //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                        if (outer.MoveNext())
                        {
                            //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                            object p = outer.Current;
                            inner = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                        }
                        else
                        {
                            inner = null;
                        }
                    }
                    return obj;
                }
            }

            public virtual bool MoveNext()
            {
                return inner != null;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal ExtendIterator(IEnumerator iterator)
            {
                outer = iterator;
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                if (iterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    object p = iterator.Current;
                    inner = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                }
            }

            private IEnumerator outer;
            private IEnumerator inner;

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        internal struct ExtendEntry
        {
            public object Key
            {
                get
                {
                    return key;
                }
            }

            public object Value
            {
                get
                {
                    return val;
                }

                set
                {
                    throw new System.NotSupportedException();
                }
            }

            internal ExtendEntry(object key, object val)
            {
                this.key = key;
                this.val = val;
            }

            private object key;
            private object val;
        }

        internal class ExtendEntryIterator : IEnumerator
        {
            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    ExtendEntry curr = new ExtendEntry(key, inner.Current);
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!inner.MoveNext())
                    {
                        //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                        if (outer.MoveNext())
                        {
                            //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                            ExtendEntry entry = (ExtendEntry) outer.Current;
                            key = entry.Key;
                            object p = entry.Value;
                            inner = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                        }
                        else
                        {
                            inner = null;
                        }
                    }
                    return curr;
                }
            }

            public virtual bool MoveNext()
            {
                return inner != null;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal ExtendEntryIterator(IEnumerator iterator)
            {
                outer = iterator;
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                if (iterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) iterator.Current;
                    key = entry.Key;
                    object p = entry.Value;
                    inner = (p is Relation) ? ((Relation)p).GetEnumerator() : ((IPersistentSet)p).GetEnumerator();
                }
            }

            private IEnumerator outer;
            private IEnumerator inner;
            private object key;

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new ExtendIterator(index.GetEnumerator());
        }

        public virtual IEnumerator GetEntryEnumerator()
        {
            return new ExtendEntryIterator(index.GetEntryEnumerator());
        }

        public virtual IEnumerator GetEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new ExtendIterator(index.GetEnumerator(from, till, order));
        }

        public virtual IEnumerator GetEntryEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new ExtendEntryIterator(index.GetEntryEnumerator(from, till, order));
        }

        public virtual IEnumerator PrefixIterator(string prefix)
        {
            return new ExtendIterator(index.PrefixIterator(prefix));
        }

        public virtual bool Put(Key key, IPersistent obj)
        {
            IPersistent s = index.Get(key);
            if (s == null)
            {
                Relation r = Storage.CreateRelation(null);
                r.Add(obj);
                index.Put(key, r);
            }
            else if (s is Relation)
            {
                Relation r = (Relation) s;
                if (r.Size == BTREE_THRESHOLD)
                {
                    IPersistentSet ps = Storage.CreateSet();
                    for (int i = 0; i < BTREE_THRESHOLD; i++)
                    {
                        ps.Add(r.GetRaw(i));
                    }
                    ps.Add(obj);
                    index.Set(key, ps);
                    r.Deallocate();
                }
                else
                {
                    r.Add(obj);
                }
            }
            else
            {
                ((IPersistentSet) s).Add(obj);
            }
            nElems += 1;
            Modify();
            return true;
        }

        public virtual IPersistent Set(Key key, IPersistent obj)
        {
            IPersistent s = index.Get(key);
            if (s == null)
            {
                Relation r = Storage.CreateRelation(null);
                r.Add(obj);
                index.Put(key, r);
                nElems += 1;
                Modify();
                return null;
            }
            else if (s is Relation)
            {
                Relation r = (Relation) s;
                if (r.Size == 1)
                {
                    IPersistent prev = r.Get(0);
                    r.Set(0, obj);
                    return prev;
                }
            }
            throw new StorageError(StorageError.KEY_NOT_UNIQUE);
        }

        public virtual void Remove(Key key, IPersistent obj)
        {
            IPersistent s = index.Get(key);
            if (s is Relation)
            {
                Relation r = (Relation) s;
                int i = r.IndexOf(obj);
                if (i >= 0)
                {
                    r.Remove(i);
                    if (r.Size == 0)
                    {
                        index.Remove(key, r);
                        r.Deallocate();
                    }
                    nElems -= 1;
                    Modify();
                    return;
                }
            }
            else if (s is IPersistentSet)
            {
                IPersistentSet ps = (IPersistentSet) s;
                bool tempBoolean;
                tempBoolean = ps.Contains(obj);
                ps.Remove(obj);
                if (tempBoolean)
                {
                    if (ps.Count == 0)
                    {
                        index.Remove(key, ps);
                        ps.Deallocate();
                    }
                    nElems -= 1;
                    Modify();
                    return;
                }
            }

            throw new StorageError(StorageError.KEY_NOT_FOUND);
        }

        public virtual IPersistent Remove(Key key)
        {
            throw new StorageError(StorageError.KEY_NOT_UNIQUE);
        }

        public virtual bool Put(string key, IPersistent obj)
        {
            return Put(new Key(key), obj);
        }

        public virtual IPersistent Set(string key, IPersistent obj)
        {
            return Set(new Key(key), obj);
        }

        public virtual void Remove(string key, IPersistent obj)
        {
            Remove(new Key(key), obj);
        }

        public virtual IPersistent Remove(string key)
        {
            throw new StorageError(StorageError.KEY_NOT_UNIQUE);
        }

        public override void Deallocate()
        {
            Clear();
            index.Deallocate();
            base.Deallocate();
        }
    }
}

