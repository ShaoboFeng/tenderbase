namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    class AltPersistentSet : AltBtree, IPersistentSet
    {
        public virtual int Count
        {
            get
            {
                return nElems;
            }
        }

        internal AltPersistentSet()
        {
            type = ClassDescriptor.tpObject;
            unique = true;
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.isEmpty' is not an override method.
        public virtual bool IsEmpty()
        {
            return nElems == 0;
        }

        public virtual bool Contains(object o)
        {
            if (o is IPersistent)
            {
                Key key = new Key((IPersistent) o);
                IEnumerator i = GetEnumerator(key, key, IndexSortOrder.Ascent);
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                return i.MoveNext();
            }

            return false;
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.toArray' is not an override method.
        public virtual object[] ToArray()
        {
            return ToPersistentArray();
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Set.toArray' is not an override method.
        public virtual object[] ToArray(object[] a)
        {
            return ToPersistentArray((IPersistent[]) a);
        }

        public virtual bool Add(object o)
        {
            IPersistent obj = (IPersistent) o;
            return Put(new Key(obj), obj);
        }

        //UPGRADE_ISSUE: The equivalent in .NET for method 'java.util.Set.remove' returns a different type.
        public virtual bool Remove(object o)
        {
            IPersistent obj = (IPersistent) o;
            try
            {
                Remove(new Key(obj), obj);
            }
            catch (StorageError x)
            {
                if (x.ErrorCode == StorageError.KEY_NOT_FOUND)
                {
                    return false;
                }

                throw x;
            }

            return true;
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
                modified |= this.Add(i.Current);
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
            if (c.Count != Size())
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
    }
}

