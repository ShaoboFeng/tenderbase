#if !OMIT_RTREE
namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    public class Rtree : PersistentResource, SpatialIndex
    {
        public virtual Rectangle WrappingRectangle
        {
            get
            {
                if (root != null)
                {
                    return root.Cover();
                }
                return null;
            }
        }

        private int height;
        private int n;
        private RtreePage root;
        [NonSerialized]
        private int updateCounter;

        internal Rtree()
        {
        }

        public virtual void Put(Rectangle r, IPersistent obj)
        {
            if (root == null)
            {
                root = new RtreePage(Storage, obj, r);
                height = 1;
            }
            else
            {
                RtreePage p = root.Insert(Storage, r, obj, height);
                if (p != null)
                {
                    root = new RtreePage(Storage, root, p);
                    height += 1;
                }
            }
            updateCounter += 1;
            n += 1;
            Modify();
        }

        public virtual int Size()
        {
            return n;
        }

        public virtual void Remove(Rectangle r, IPersistent obj)
        {
            if (root == null)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            ArrayList reinsertList = new ArrayList();
            int reinsertLevel = root.Remove(r, obj, height, reinsertList);
            if (reinsertLevel < 0)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }

            for (int i = reinsertList.Count; --i >= 0; )
            {
                RtreePage p = (RtreePage) reinsertList[i];
                for (int j = 0, n2 = p.n; j < n2; j++)
                {
                    RtreePage q = root.Insert(Storage, p.b[j], p.branch.Get(j), height - reinsertLevel);
                    if (q != null)
                    {
                        // root split
                        root = new RtreePage(Storage, root, q);
                        height += 1;
                    }
                }
                reinsertLevel -= 1;
                p.Deallocate();
            }

            if (root.n == 1 && height > 1)
            {
                RtreePage newRoot = (RtreePage) root.branch.Get(0);
                root.Deallocate();
                root = newRoot;
                height -= 1;
            }
            n -= 1;
            updateCounter += 1;
            Modify();
        }

        public virtual IPersistent[] Get(Rectangle r)
        {
            ArrayList result = GetList(r);
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(result, new IPersistent[result.Count]);
        }

        public virtual ArrayList GetList(Rectangle r)
        {
            ArrayList result = new ArrayList();
            if (root != null)
            {
                root.Find(r, result, height);
            }
            return result;
        }

        public virtual IPersistent[] ToArray()
        {
            return Get(WrappingRectangle);
        }

        public virtual IPersistent[] ToArray(IPersistent[] arr)
        {
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(GetList(WrappingRectangle), arr);
        }

        public virtual void Clear()
        {
            if (root != null)
            {
                root.Purge(height);
                root = null;
            }
            height = 0;
            n = 0;
            Modify();
        }

        public override void Deallocate()
        {
            Clear();
            base.Deallocate();
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'RtreeIterator' to access its enclosing instance.
        internal class RtreeIterator : IEnumerator
        {
            private void InitBlock(Rtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private Rtree enclosingInstance;
            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    object curr = current(Enclosing_Instance.height - 1);
                    if (!GotoNextItem(Enclosing_Instance.height - 1))
                    {
                        pageStack = null;
                        posStack = null;
                    }
                    return curr;
                }
            }

            public Rtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal RtreeIterator(Rtree enclosingInstance, Rectangle r)
            {
                InitBlock(enclosingInstance);
                counter = Enclosing_Instance.updateCounter;
                if (Enclosing_Instance.height == 0)
                {
                    return;
                }
                this.r = r;
                pageStack = new RtreePage[Enclosing_Instance.height];
                posStack = new int[Enclosing_Instance.height];

                if (!GotoFirstItem(0, Enclosing_Instance.root))
                {
                    pageStack = null;
                    posStack = null;
                }
            }

            public virtual bool MoveNext()
            {
                if (counter != Enclosing_Instance.updateCounter)
                {
                    throw new System.Exception();
                }
                return pageStack != null;
            }

            protected internal virtual object current(int sp)
            {
                return pageStack[sp].branch.Get(posStack[sp]);
            }

            private bool GotoFirstItem(int sp, RtreePage pg)
            {
                for (int i = 0, n = pg.n; i < n; i++)
                {
                    if (r.Intersects(pg.b[i]))
                    {
                        if (sp + 1 == Enclosing_Instance.height || GotoFirstItem(sp + 1, (RtreePage) pg.branch.Get(i)))
                        {
                            pageStack[sp] = pg;
                            posStack[sp] = i;
                            return true;
                        }
                    }
                }
                return false;
            }

            private bool GotoNextItem(int sp)
            {
                RtreePage pg = pageStack[sp];
                for (int i = posStack[sp], n = pg.n; ++i < n; )
                {
                    if (r.Intersects(pg.b[i]))
                    {
                        if (sp + 1 == Enclosing_Instance.height || GotoFirstItem(sp + 1, (RtreePage) pg.branch.Get(i)))
                        {
                            pageStack[sp] = pg;
                            posStack[sp] = i;
                            return true;
                        }
                    }
                }
                pageStack[sp] = null;
                return (sp > 0) ? GotoNextItem(sp - 1) : false;
            }

            internal RtreePage[] pageStack;
            internal int[] posStack;
            internal int counter;
            internal Rectangle r;
            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        internal struct RtreeEntry
        {
            public object Key
            {
                get
                {
                    return pg.b[pos];
                }
            }

            public object Value
            {
                get
                {
                    return pg.branch.Get(pos);
                }

                set
                {
                    throw new System.NotSupportedException();
                }
            }

            internal RtreePage pg;
            internal int pos;

            internal RtreeEntry(RtreePage pg, int pos)
            {
                this.pg = pg;
                this.pos = pos;
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'RtreeEntryIterator' to access its enclosing instance.
        internal class RtreeEntryIterator : RtreeIterator
        {
            private void InitBlock(Rtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private Rtree enclosingInstance;

            public new Rtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal RtreeEntryIterator(Rtree enclosingInstance, Rectangle r)
                : base(enclosingInstance, r)
            {
                InitBlock(enclosingInstance);
            }

            protected internal override object current(int sp)
            {
                return new RtreeEntry(pageStack[sp], posStack[sp]);
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return GetEnumerator(WrappingRectangle);
        }

        public virtual IEnumerator GetEntryEnumerator()
        {
            return GetEntryEnumerator(WrappingRectangle);
        }

        public virtual IEnumerator GetEnumerator(Rectangle r)
        {
            return new RtreeIterator(this, r);
        }

        public virtual IEnumerator GetEntryEnumerator(Rectangle r)
        {
            return new RtreeEntryIterator(this, r);
        }
    }
}
#endif

