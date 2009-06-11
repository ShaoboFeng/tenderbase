namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    public class LinkImpl : Link
    {
        public virtual int Size
        {
            get
            {
                return used;
            }

            set
            {
                if (value < used)
                {
                    for (int i = used; --i >= value; arr[i] = null)
                        ;
                }
                else
                {
                    ReserveSpace(value - used);
                }
                used = value;
                Modify();
            }
        }

        private void Modify()
        {
            if (owner != null)
            {
                owner.Modify();
            }
        }

        public virtual IPersistent Get(int i)
        {
            if (i < 0 || i >= used)
            {
                throw new System.IndexOutOfRangeException();
            }
            return LoadElem(i);
        }

        public virtual IPersistent GetRaw(int i)
        {
            if (i < 0 || i >= used)
            {
                throw new System.IndexOutOfRangeException();
            }
            return arr[i];
        }

        public virtual void Pin()
        {
            for (int i = 0, n = used; i < n; i++)
            {
                arr[i] = LoadElem(i);
            }
        }

        public virtual void Unpin()
        {
            for (int i = 0, n = used; i < n; i++)
            {
                IPersistent elem = arr[i];
                if (elem != null && !elem.IsRaw() && elem.IsPersistent())
                {
                    arr[i] = new PersistentStub(elem.Storage, elem.Oid);
                }
            }
        }

        public virtual void Set(int i, IPersistent obj)
        {
            if (i < 0 || i >= used)
            {
                throw new System.IndexOutOfRangeException();
            }
            arr[i] = obj;
            Modify();
        }

        public virtual void Remove(int i)
        {
            if (i < 0 || i >= used)
            {
                throw new System.IndexOutOfRangeException();
            }
            used -= 1;
            Array.Copy(arr, i + 1, arr, i, used - i);
            arr[used] = null;
            Modify();
        }

        internal virtual void ReserveSpace(int len)
        {
            if (used + len > arr.Length)
            {
                IPersistent[] newArr = new IPersistent[used + len > arr.Length * 2 ? used + len : arr.Length * 2];
                Array.Copy(arr, 0, newArr, 0, used);
                arr = newArr;
            }
            Modify();
        }

        public virtual void Insert(int i, IPersistent obj)
        {
            if (i < 0 || i > used)
            {
                throw new System.IndexOutOfRangeException();
            }
            ReserveSpace(1);
            Array.Copy(arr, i, arr, i + 1, used - i);
            arr[i] = obj;
            used += 1;
        }

        public virtual void Add(IPersistent obj)
        {
            ReserveSpace(1);
            arr[used++] = obj;
        }

        public virtual void AddAll(IPersistent[] a)
        {
            AddAll(a, 0, a.Length);
        }

        public virtual void AddAll(IPersistent[] a, int from, int length)
        {
            ReserveSpace(length);
            Array.Copy(a, from, arr, used, length);
            used += length;
        }

        public virtual void AddAll(Link link)
        {
            int n = link.Size;
            ReserveSpace(n);
            for (int i = 0, j = used; i < n; i++, j++)
            {
                arr[j] = link.GetRaw(i);
            }
            used += n;
        }

        public virtual IPersistent[] ToArray()
        {
            IPersistent[] a = new IPersistent[used];
            for (int i = used; --i >= 0; )
            {
                a[i] = LoadElem(i);
            }
            return a;
        }

        public virtual IPersistent[] ToRawArray()
        {
            return arr;
        }

        public virtual IPersistent[] ToArray(IPersistent[] arr)
        {
            if (arr.Length < used)
            {
                arr = (IPersistent[]) System.Array.CreateInstance(arr.GetType().GetElementType(), used);
            }
            for (int i = used; --i >= 0; )
            {
                arr[i] = LoadElem(i);
            }
            if (arr.Length > used)
            {
                arr[used] = null;
            }
            return arr;
        }

        public virtual bool Contains(IPersistent obj)
        {
            return IndexOf(obj) >= 0;
        }

        public virtual int IndexOf(IPersistent obj)
        {
            int oid;
            if (obj != null && (oid = ((IPersistent) obj).Oid) != 0)
            {
                for (int i = used; --i >= 0; )
                {
                    IPersistent elem = arr[i];
                    if (elem != null && elem.Oid == oid)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = used; --i >= 0; )
                {
                    if (arr[i] == obj)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public virtual bool ContainsElement(int i, IPersistent obj)
        {
            IPersistent elem = arr[i];
            return elem == obj || (elem != null && elem.Oid != 0 && elem.Oid == obj.Oid);
        }

        public virtual void Clear()
        {
            for (int i = used; --i >= 0; )
            {
                arr[i] = null;
            }
            used = 0;
            Modify();
        }

        internal class LinkIterator : IEnumerator
        {
            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    return link.Get(i++);
                }
            }

            private Link link;
            private int i;

            internal LinkIterator(Link link)
            {
                this.link = link;
            }

            public virtual bool MoveNext()
            {
                return i < link.Size;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                link.Remove(i);
            }

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new LinkIterator(this);
        }

        private IPersistent LoadElem(int i)
        {
            IPersistent elem = arr[i];
            if (elem != null && elem.IsRaw())
            {
                StorageImpl si = elem.Storage as StorageImpl;
                // arr[i] = elem = si.LookupObject(elem.getOid(), null);
                elem = si.LookupObject(elem.Oid, null);
            }
            return elem;
        }

        internal LinkImpl()
        {
        }

        internal LinkImpl(int initSize)
        {
            this.arr = new IPersistent[initSize];
        }

        internal LinkImpl(IPersistent[] arr, IPersistent owner)
        {
            this.arr = arr;
            this.owner = owner;
            used = arr.Length;
        }

        internal IPersistent[] arr;
        internal int used;
        [NonSerialized]
        internal IPersistent owner;
    }
}

