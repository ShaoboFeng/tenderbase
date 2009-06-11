namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Double linked list.</summary>
    [Serializable]
    public class L2List : L2ListElem, ICollection
    {
        public virtual int Count
        {
            get
            {
                return nElems;
            }
        }

        private int nElems;
        private int updateCounter;

        /// <summary> Get list head element</summary>
        /// <returns> list head element or null if list is empty
        /// </returns>
        public virtual L2ListElem Head()
        {
            lock (this)
            {
                return next != this ? next : null;
            }
        }

        /// <summary> Get list tail element</summary>
        /// <returns> list tail element or null if list is empty
        /// </returns>
        public virtual L2ListElem Tail()
        {
            lock (this)
            {
                return prev != this ? prev : null;
            }
        }

        /// <summary> Make list empty. </summary>
        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.clear' is not an override method.
        public virtual void Clear()
        {
            lock (this)
            {
                Modify();
                next = prev = null;
                nElems = 0;
                updateCounter += 1;
            }
        }

        /// <summary> Insert element at the beginning of the list</summary>
        public virtual void Prepend(L2ListElem elem)
        {
            lock (this)
            {
                Modify();
                next.Modify();
                elem.Modify();
                elem.next = next;
                elem.prev = this;
                next.prev = elem;
                next = elem;
                nElems += 1;
                updateCounter += 1;
            }
        }

        /// <summary> Insert element at the end of the list</summary>
        public virtual void Append(L2ListElem elem)
        {
            lock (this)
            {
                Modify();
                prev.Modify();
                elem.Modify();
                elem.next = this;
                elem.prev = prev;
                prev.next = elem;
                prev = elem;
                nElems += 1;
                updateCounter += 1;
            }
        }

        /// <summary> Remove element from the list</summary>
        public virtual void Remove(L2ListElem elem)
        {
            lock (this)
            {
                Modify();
                elem.prev.Modify();
                elem.next.Modify();
                elem.next.prev = elem.prev;
                elem.prev.next = elem.next;
                nElems -= 1;
                updateCounter += 1;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.isEmpty' is not an override method.
        public virtual bool IsEmpty()
        {
            lock (this)
            {
                return next == this;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.add' is not an override method.
        public virtual bool Add(object obj)
        {
            lock (this)
            {
                Append((L2ListElem) obj);
                return true;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.remove' is not an override method.
        public virtual bool Remove(object o)
        {
            lock (this)
            {
                Remove((L2ListElem) o);
                return true;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.contains' is not an override method.
        public virtual bool Contains(object o)
        {
            lock (this)
            {
                for (L2ListElem e = next; e != this; e = e.next)
                {
                    if (e.Equals(o))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'L2ListIterator' to access its enclosing instance.
        internal class L2ListIterator : IEnumerator
        {
            private void InitBlock(L2List enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private L2List enclosingInstance;

            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    curr = curr.next;
                    return curr;
                }
            }

            public L2List Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            private L2ListElem curr;
            private int counter;

            internal L2ListIterator(L2List enclosingInstance)
            {
                InitBlock(enclosingInstance);
                curr = Enclosing_Instance;
            }

            public virtual bool MoveNext()
            {
                if (counter != Enclosing_Instance.updateCounter)
                {
                    throw new System.SystemException();
                }
                return curr.next != Enclosing_Instance;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                if (counter != Enclosing_Instance.updateCounter || curr == Enclosing_Instance)
                {
                    throw new System.SystemException();
                }
                Enclosing_Instance.Remove(curr);
                counter = Enclosing_Instance.updateCounter;
                curr = curr.prev;
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        //UPGRADE_ISSUE: The equivalent in .NET for method 'java.util.Collection.iterator' returns a different type.
        public virtual IEnumerator GetEnumerator()
        {
            return new L2ListIterator(this);
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.toArray' is not an override method.
        public virtual object[] ToArray()
        {
            lock (this)
            {
                L2ListElem[] arr = new L2ListElem[nElems];
                L2ListElem e = this;
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = e = e.next;
                }
                return arr;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.toArray' is not an override method.
        public virtual object[] ToArray(object[] a)
        {
            lock (this)
            {
                int size = nElems;
                if (a.Length < size)
                {
                    a = (object[]) System.Array.CreateInstance(a.GetType().GetElementType(), size);
                }
                L2ListElem e = this;
                for (int i = 0; i < size; i++)
                {
                    a[i] = e = e.next;
                }
                if (a.Length > size)
                {
                    a[size] = null;
                }
                return a;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.containsAll' is not an override method.
        public virtual bool ContainsAll(ICollection c)
        {
            lock (this)
            {
                IEnumerator e = c.GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (e.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    if (!Contains(e.Current))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.addAll' is not an override method.
        public virtual bool AddAll(ICollection c)
        {
            lock (this)
            {
                IEnumerator e = c.GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (e.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    Add(e.Current);
                }
                return true;
            }
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.removeAll' is not an override method.
        public virtual bool RemoveAll(ICollection c)
        {
            // TODOPORT:
            /*lock (this)
            {
                bool modified = false;
                IEnumerator e = GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (e.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    if (SupportClass.ICollectionSupport.Contains(c, e.Current))
                    {
                        //UPGRADE_ISSUE: Method 'java.util.Iterator.remove' was not converted.
                        e.Remove();
                        modified = true;
                    }
                }
                return modified;
            } */
            throw new NotImplementedException();
        }

        //UPGRADE_NOTE: The equivalent of method 'java.util.Collection.retainAll' is not an override method.
        public virtual bool RetainAll(ICollection c)
        {
            // TODOPORT:
            /*
            lock (this)
            {
                bool modified = false;
                IEnumerator e = GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (e.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    if (!SupportClass.ICollectionSupport.Contains(c, e.Current))
                    {
                        //UPGRADE_ISSUE: Method 'java.util.Iterator.remove' was not converted.
                        e.Remove();
                        modified = true;
                    }
                }
                return modified;
            }
            */
            throw new System.NotImplementedException();
        }

        //UPGRADE_NOTE: The following method implementation was automatically added to preserve functionality.
        public virtual void CopyTo(System.Array array, Int32 index)
        {
            int i = 0;
            int length = array.Length;
            object[] objects = new object[length];

            foreach (object o in this)
                objects.SetValue(o, i++);

            for (i = index; i < length; i++)
                array.SetValue(objects[i], i);
        }

        //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
        public virtual object SyncRoot
        {
            get
            {
                return null;
            }
        }

        //UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic.
        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }
    }
}

