#if !OMIT_RTREER2
namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    public class RtreeR2 : PersistentResource, SpatialIndexR2
    {
        public virtual RectangleR2 WrappingRectangle
        {
            get
            {
                if (root != null)
                    return root.Cover();

                return null;
            }
        }

        private int height;
        private int n;
        private RtreeR2Page root;
        [NonSerialized]
        private int updateCounter;

        internal RtreeR2()
        {
        }

        internal RtreeR2(Storage storage)
            : base(storage)
        {
        }

        public virtual void Put(RectangleR2 r, IPersistent obj)
        {
            if (root == null)
            {
                root = new RtreeR2Page(Storage, obj, r);
                height = 1;
            }
            else
            {
                RtreeR2Page p = root.Insert(Storage, r, obj, height);
                if (p != null)
                {
                    root = new RtreeR2Page(Storage, root, p);
                    height += 1;
                }
            }
            n += 1;
            updateCounter += 1;
            Modify();
        }

        public virtual int Size()
        {
            return n;
        }

        public virtual void Remove(RectangleR2 r, IPersistent obj)
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
                RtreeR2Page p = (RtreeR2Page) reinsertList[i];
                for (int j = 0, n2 = p.n; j < n2; j++)
                {
                    RtreeR2Page q = root.Insert(Storage, p.b[j], p.branch.Get(j), height - reinsertLevel);
                    if (q != null)
                    {
                        // root split
                        root = new RtreeR2Page(Storage, root, q);
                        height += 1;
                    }
                }
                reinsertLevel -= 1;
                p.Deallocate();
            }

            if (root.n == 1 && height > 1)
            {
                RtreeR2Page newRoot = (RtreeR2Page) root.branch.Get(0);
                root.Deallocate();
                root = newRoot;
                height -= 1;
            }
            n -= 1;
            updateCounter += 1;
            Modify();
        }

        public virtual IPersistent[] Get(RectangleR2 r)
        {
            ArrayList result = new ArrayList();
            if (root != null)
            {
                root.Find(r, result, height);
            }
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(result, new IPersistent[result.Count]);
        }

        public virtual ArrayList GetList(RectangleR2 r)
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
            updateCounter += 1;
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
            private void InitBlock(RtreeR2 enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private RtreeR2 enclosingInstance;

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

            public RtreeR2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal RtreeIterator(RtreeR2 enclosingInstance, RectangleR2 r)
            {
                InitBlock(enclosingInstance);
                counter = Enclosing_Instance.updateCounter;
                if (Enclosing_Instance.height == 0)
                {
                    return;
                }

                this.r = r;
                pageStack = new RtreeR2Page[Enclosing_Instance.height];
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

            private bool GotoFirstItem(int sp, RtreeR2Page pg)
            {
                for (int i = 0, n = pg.n; i < n; i++)
                {
                    if (r.Intersects(pg.b[i]))
                    {
                        if (sp + 1 == Enclosing_Instance.height || GotoFirstItem(sp + 1, (RtreeR2Page) pg.branch.Get(i)))
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
                RtreeR2Page pg = pageStack[sp];
                for (int i = posStack[sp], n = pg.n; ++i < n; )
                {
                    if (r.Intersects(pg.b[i]))
                    {
                        if (sp + 1 == Enclosing_Instance.height || GotoFirstItem(sp + 1, (RtreeR2Page) pg.branch.Get(i)))
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

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal RtreeR2Page[] pageStack;
            internal int[] posStack;
            internal int counter;
            internal RectangleR2 r;
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

            internal RtreeR2Page pg;
            internal int pos;

            internal RtreeEntry(RtreeR2Page pg, int pos)
            {
                this.pg = pg;
                this.pos = pos;
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'RtreeEntryIterator' to access its enclosing instance.
        internal class RtreeEntryIterator : RtreeIterator
        {
            private void InitBlock(RtreeR2 enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private RtreeR2 enclosingInstance;
            public new RtreeR2 Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal RtreeEntryIterator(RtreeR2 enclosingInstance, RectangleR2 r)
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

        public virtual IEnumerator GetEnumerator(RectangleR2 r)
        {
            return new RtreeIterator(this, r);
        }

        public virtual IEnumerator GetEntryEnumerator(RectangleR2 r)
        {
            return new RtreeEntryIterator(this, r);
        }
    }
}
#endif

