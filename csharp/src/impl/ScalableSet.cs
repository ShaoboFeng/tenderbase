namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    class ScalableSet : PersistentResource, IPersistentSet
    {
        public virtual int Count
        {
            get
            {
                if (link != null)
                    return link.Size;
                else
                    return Set.Count;
            }
        }

        internal Link link;
        internal IPersistentSet Set;

        internal const int BTREE_THRESHOLD = 128;

        internal ScalableSet(StorageImpl storage, int initialSize)
            : base(storage)
        {
            if (initialSize <= BTREE_THRESHOLD)
            {
                link = storage.CreateLink(initialSize);
            }
            else
            {
                Set = storage.CreateSet();
            }
        }

        internal ScalableSet()
        {
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.isEmpty' is not an override method.
        public virtual bool IsEmpty()
        {
            return Count != 0;
        }

        public virtual void Clear()
        {
            if (link != null)
            {
                link.Clear();
                Modify();
            }
            else
            {
                Set.Clear();
            }
        }

        public virtual bool Contains(object o)
        {
            if (o is IPersistent)
            {
                IPersistent p = (IPersistent) o;
                if (link != null)
                    return link.Contains(p);
                else
                    return Set.Contains(p);
            }
            return false;
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.toArray' is not an override method.
        public virtual object[] ToArray()
        {
            if (link != null)
                return (object[])link.ToArray();
            else
                return Set.ToArray();
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.toArray' is not an override method.
        public virtual object[] ToArray(object[] a)
        {
            if (link != null)
                return (object[]) link.ToArray((IPersistent[]) a);
            else
                return Set.ToArray(a);
        }

        //UPGRADE_ISSUE: The equivalent in .NET for method 'java.util.Set.iterator' returns a different type.
        public virtual IEnumerator GetEnumerator()
        {
            if (link != null)
                return link.GetEnumerator();
            else
                return Set.GetEnumerator();
        }

        public virtual bool Add(object o)
        {
            if (link != null)
            {
                IPersistent obj = (IPersistent) o;
                if (link.IndexOf(obj) >= 0)
                {
                    return false;
                }
                if (link.Size == BTREE_THRESHOLD)
                {
                    Set = Storage.CreateSet();
                    for (int i = 0, n = link.Size; i < n; i++)
                    {
                        Set.Add(link.GetRaw(i));
                    }
                    link = null;
                    Modify();
                    Set.Add(obj);
                }
                else
                {
                    Modify();
                    link.Add(obj);
                }
                return true;
            }
            else
            {
                return Set.Add(o);
            }
        }

        //UPGRADE_ISSUE: The equivalent in .NET for method 'java.util.Set.remove' returns a different type.
        public virtual bool Remove(object o)
        {
            if (link != null)
            {
                IPersistent obj = (IPersistent) o;
                int i = link.IndexOf(obj);
                if (i < 0)
                {
                    return false;
                }
                link.Remove(i);
                Modify();
                return true;
            }
            else
            {
                bool tempBoolean;
                tempBoolean = Set.Contains(o);
                Set.Remove(o);
                return tempBoolean;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.containsAll' is not an override method.
        public virtual bool ContainsAll(ICollection c)
        {
            IEnumerator i = c.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (i.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                if (!Contains(i.Current))
                    return false;
            }
            return true;
        }

        public virtual bool AddAll(ICollection c)
        {
            bool modified = false;
            IEnumerator i = c.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (i.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                modified |= Add(i.Current);
            }
            return modified;
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.retainAll' is not an override method.
        public virtual bool RetainAll(ICollection c)
        {
            ArrayList toBeRemoved = new ArrayList();
            IEnumerator i = GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (i.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                object o = i.Current;
                if (!SupportClass.ICollectionSupport.Contains(c, o))
                {
                    toBeRemoved.Add(o);
                }
            }
            int n = toBeRemoved.Count;
            for (int j = 0; j < n; j++)
            {
                bool tempBoolean;
                tempBoolean = Contains(toBeRemoved[j]);
                Remove(toBeRemoved[j]);
                bool generatedAux2 = tempBoolean;
            }
            return n != 0;
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.removeAll' is not an override method.
        public virtual bool RemoveAll(ICollection c)
        {
            bool modified = false;
            IEnumerator i = c.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (i.MoveNext())
            {
                bool tempBoolean;
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                tempBoolean = Contains(i.Current);
                Remove(i.Current);
                modified |= tempBoolean;
            }
            return modified;
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (!(o is SupportClass.SetSupport))
            {
                return false;
            }
            ICollection c = (ICollection) o;
            if (c.Count != Count)
            {
                return false;
            }
            return ContainsAll(c);
        }

        public override int GetHashCode()
        {
            int h = 0;
            IEnumerator i = GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (i.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                h += ((IPersistent) i.Current).Oid;
            }
            return h;
        }

        public override void Deallocate()
        {
            if (Set != null)
                Set.Deallocate();

            base.Deallocate();
        }
    }
}

