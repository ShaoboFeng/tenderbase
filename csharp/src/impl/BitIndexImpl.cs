namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    class BitIndexImpl : Btree, BitIndex
    {
        internal BitIndexImpl() : base(ClassDescriptor.tpInt, true)
        {
        }

        internal class Key
        {
            internal int key;
            internal int oid;

            internal Key(int key, int oid)
            {
                this.key = key;
                this.oid = oid;
            }
        }

        public virtual int Get(IPersistent obj)
        {
            StorageImpl db = (StorageImpl) Storage;
            if (root == 0)
                throw new StorageError(StorageError.KEY_NOT_FOUND);

            return BitIndexPage.Find(db, root, obj.Oid, height);
        }

        public virtual void Put(IPersistent obj, int mask)
        {
            StorageImpl db = (StorageImpl) Storage;
            if (db == null)
            {
                throw new StorageError(StorageError.DELETED_OBJECT);
            }

            if (!obj.IsPersistent())
            {
                db.MakePersistent(obj);
            }

            Key ins = new Key(mask, obj.Oid);
            if (root == 0)
            {
                root = BitIndexPage.Allocate(db, 0, ins);
                height = 1;
            }
            else
            {
                int result = BitIndexPage.Insert(db, root, ins, height);
                if (result == op_overflow)
                {
                    root = BitIndexPage.Allocate(db, root, ins);
                    height += 1;
                }
            }

            updateCounter += 1;
            nElems += 1;
            Modify();
        }

        public virtual void Remove(IPersistent obj)
        {
            StorageImpl db = (StorageImpl) Storage;
            if (db == null)
            {
                throw new StorageError(StorageError.DELETED_OBJECT);
            }
            if (root == 0)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            int result = BitIndexPage.Remove(db, root, obj.Oid, height);
            if (result == op_not_found)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            nElems -= 1;
            if (result == op_underflow)
            {
                Page pg = db.GetPage(root);
                if (BitIndexPage.GetItemsCount(pg) == 0)
                {
                    int newRoot = 0;
                    if (height != 1)
                    {
                        newRoot = BitIndexPage.GetItem(pg, BitIndexPage.maxItems - 1);
                    }
                    db.FreePage(root);
                    root = newRoot;
                    height -= 1;
                }
                db.pool.Unfix(pg);
            }

            updateCounter += 1;
            Modify();
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BitIndexIterator' to access its enclosing instance.
        internal class BitIndexIterator : IEnumerator
        {
            private void InitBlock(BitIndexImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private BitIndexImpl enclosingInstance;

            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }

                    StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                    int pos = posStack[sp - 1];
                    Page pg = db.GetPage(pageStack[sp - 1]);
                    object curr = db.LookupObject(BitIndexPage.GetItem(pg, TenderBaseImpl.BitIndexImpl.BitIndexPage.maxItems - 1 - pos), null);
                    GotoNextItem(pg, pos + 1);
                    return curr;
                }
            }

            public BitIndexImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BitIndexIterator(BitIndexImpl enclosingInstance, int set_Renamed, int clear)
            {
                InitBlock(enclosingInstance);
                sp = 0;
                counter = Enclosing_Instance.updateCounter;
                if (Enclosing_Instance.height == 0)
                {
                    return;
                }
                int pageId = Enclosing_Instance.root;
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                if (db == null)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }
                int h = Enclosing_Instance.height;
                this.set_Renamed = set_Renamed;
                this.clear = clear;

                pageStack = new int[h];
                posStack = new int[h];

                while (true)
                {
                    pageStack[sp] = pageId;
                    Page pg = db.GetPage(pageId);
                    sp += 1;
                    if (--h == 0)
                    {
                        GotoNextItem(pg, 0);
                        break;
                    }

                    pageId = BitIndexPage.GetItem(pg, TenderBaseImpl.BitIndexImpl.BitIndexPage.maxItems - 1);
                    db.pool.Unfix(pg);
                }
            }

            public virtual bool MoveNext()
            {
                if (counter != Enclosing_Instance.updateCounter)
                {
                    throw new System.Exception();
                }

                return sp != 0;
            }

            private void GotoNextItem(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;

                do
                {
                    int end = BitIndexPage.GetItemsCount(pg);
                    while (pos < end)
                    {
                        int mask = BitIndexPage.GetItem(pg, pos);
                        if ((set_Renamed & mask) == set_Renamed && (clear & mask) == 0)
                        {
                            posStack[sp - 1] = pos;
                            db.pool.Unfix(pg);
                            return;
                        }
                        pos += 1;
                    }
                    while (--sp != 0)
                    {
                        db.pool.Unfix(pg);
                        pos = posStack[sp - 1];
                        pg = db.GetPage(pageStack[sp - 1]);
                        if (++pos <= BitIndexPage.GetItemsCount(pg))
                        {
                            posStack[sp - 1] = pos;
                            do
                            {
                                int pageId = BitIndexPage.GetItem(pg, TenderBaseImpl.BitIndexImpl.BitIndexPage.maxItems - 1 - pos);
                                db.pool.Unfix(pg);
                                pg = db.GetPage(pageId);
                                pageStack[sp] = pageId;
                                posStack[sp] = pos = 0;
                            }
                            while (++sp < pageStack.Length);
                            break;
                        }
                    }
                }
                while (sp != 0);

                db.pool.Unfix(pg);
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal int[] pageStack;
            internal int[] posStack;
            internal int sp;
            internal int set_Renamed;
            internal int clear;
            internal int counter;
            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        public override IEnumerator GetEnumerator()
        {
            return GetEnumerator(0, 0);
        }

        public virtual IEnumerator GetEnumerator(int set_Renamed, int clear)
        {
            return new BitIndexIterator(this, set_Renamed, clear);
        }

        internal class BitIndexPage : BtreePage
        {
            internal static readonly int max = keySpace / 8;

            internal static int GetItem(Page pg, int index)
            {
                return Bytes.Unpack4(pg.data, firstKeyOffs + index * 4);
            }

            internal static void SetItem(Page pg, int index, int mask)
            {
                Bytes.Pack4(pg.data, firstKeyOffs + index * 4, mask);
            }

            internal static int Allocate(StorageImpl db, int root, Key ins)
            {
                int pageId = db.AllocatePage();
                Page pg = db.PutPage(pageId);
                SetItemsCount(pg, 1);
                SetItem(pg, 0, ins.key);
                SetItem(pg, maxItems - 1, ins.oid);
                SetItem(pg, maxItems - 2, root);
                db.pool.Unfix(pg);
                return pageId;
            }

            internal static void MemCopy(Page dst_pg, int dst_idx, Page src_pg, int src_idx, int len)
            {
                Array.Copy(src_pg.data, firstKeyOffs + src_idx * 4, dst_pg.data, firstKeyOffs + dst_idx * 4, len * 4);
            }

            internal static int Find(StorageImpl db, int pageId, int oid, int height)
            {
                Page pg = db.GetPage(pageId);
                try
                {
                    int i, n = GetItemsCount(pg), l = 0, r = n;
                    if (--height == 0)
                    {
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (oid > GetItem(pg, maxItems - 1 - i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }
                        if (r < n && GetItem(pg, maxItems - r - 1) == oid)
                        {
                            return GetItem(pg, r);
                        }
                        throw new StorageError(StorageError.KEY_NOT_FOUND);
                    }
                    else
                    {
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (oid > GetItem(pg, i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }
                        return Find(db, GetItem(pg, maxItems - r - 1), oid, height);
                    }
                }
                finally
                {
                    if (pg != null)
                    {
                        db.pool.Unfix(pg);
                    }
                }
            }

            internal static int Insert(StorageImpl db, int pageId, Key ins, int height)
            {
                Page pg = db.GetPage(pageId);
                int l = 0, n = GetItemsCount(pg), r = n;
                int oid = ins.oid;
                try
                {
                    if (--height != 0)
                    {
                        while (l < r)
                        {
                            int i = (l + r) >> 1;
                            if (oid > GetItem(pg, i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }
                        Assert.That(l == r);
                        /* insert before e[r] */
                        int result = Insert(db, GetItem(pg, maxItems - r - 1), ins, height);
                        Assert.That(result != TenderBaseImpl.Btree.op_not_found);
                        if (result != TenderBaseImpl.Btree.op_overflow)
                        {
                            return result;
                        }
                        n += 1;
                    }
                    else
                    {
                        while (l < r)
                        {
                            int i = (l + r) >> 1;
                            if (oid > GetItem(pg, maxItems - 1 - i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }

                        if (r < n && oid == GetItem(pg, maxItems - 1 - r))
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            SetItem(pg, r, ins.key);
                            return TenderBaseImpl.Btree.op_overwrite;
                        }
                    }
                    db.pool.Unfix(pg);
                    pg = null;
                    pg = db.PutPage(pageId);
                    if (n < max)
                    {
                        MemCopy(pg, r + 1, pg, r, n - r);
                        MemCopy(pg, maxItems - n - 1, pg, maxItems - n, n - r);
                        SetItem(pg, r, ins.key);
                        SetItem(pg, maxItems - 1 - r, ins.oid);
                        SetItemsCount(pg, GetItemsCount(pg) + 1);
                        return TenderBaseImpl.Btree.op_done;
                    }
                    else
                    {
                        /* page is full then divide page */
                        pageId = db.AllocatePage();
                        Page b = db.PutPage(pageId);
                        Assert.That(n == max);
                        int m = max / 2;
                        if (r < m)
                        {
                            MemCopy(b, 0, pg, 0, r);
                            MemCopy(b, r + 1, pg, r, m - r - 1);
                            MemCopy(pg, 0, pg, m - 1, max - m + 1);
                            MemCopy(b, maxItems - r, pg, maxItems - r, r);
                            SetItem(b, r, ins.key);
                            SetItem(b, maxItems - 1 - r, ins.oid);
                            MemCopy(b, maxItems - m, pg, maxItems - m + 1, m - r - 1);
                            MemCopy(pg, maxItems - max + m - 1, pg, maxItems - max, max - m + 1);
                        }
                        else
                        {
                            MemCopy(b, 0, pg, 0, m);
                            MemCopy(pg, 0, pg, m, r - m);
                            MemCopy(pg, r - m + 1, pg, r, max - r);
                            MemCopy(b, maxItems - m, pg, maxItems - m, m);
                            MemCopy(pg, maxItems - r + m, pg, maxItems - r, r - m);
                            SetItem(pg, r - m, ins.key);
                            SetItem(pg, maxItems - 1 - r + m, ins.oid);
                            MemCopy(pg, maxItems - max + m - 1, pg, maxItems - max, max - r);
                        }

                        ins.oid = pageId;
                        if (height == 0)
                        {
                            ins.key = GetItem(b, maxItems - m);
                            SetItemsCount(pg, max - m + 1);
                            SetItemsCount(b, m);
                        }
                        else
                        {
                            ins.key = GetItem(b, m - 1);
                            SetItemsCount(pg, max - m);
                            SetItemsCount(b, m - 1);
                        }

                        db.pool.Unfix(b);
                        return TenderBaseImpl.Btree.op_overflow;
                    }
                }
                finally
                {
                    if (pg != null)
                    {
                        db.pool.Unfix(pg);
                    }
                }
            }

            internal static int HandlePageUnderflow(StorageImpl db, Page pg, int r, int height)
            {
                int nItems = GetItemsCount(pg);
                Page a = db.PutPage(GetItem(pg, maxItems - r - 1));
                int an = GetItemsCount(a);
                if (r < nItems)
                {
                    // exists greater page
                    Page b = db.GetPage(GetItem(pg, maxItems - r - 2));
                    int bn = GetItemsCount(b);
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        MemCopy(a, an, pg, r, 1);
                        an += 1;
                        bn += 1;
                    }

                    if (an + bn > max)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        db.pool.Unfix(b);
                        b = db.PutPage(GetItem(pg, maxItems - r - 2));
                        MemCopy(a, an, b, 0, i);
                        MemCopy(b, 0, b, i, bn - i);
                        MemCopy(a, maxItems - an - i, b, maxItems - i, i);
                        MemCopy(b, maxItems - bn + i, b, maxItems - bn, bn - i);
                        if (height != 1)
                        {
                            MemCopy(pg, r, a, an + i - 1, 1);
                        }
                        else
                        {
                            MemCopy(pg, r, a, maxItems - an - i, 1);
                        }

                        SetItemsCount(b, GetItemsCount(b) - i);
                        SetItemsCount(a, GetItemsCount(a) + i);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return TenderBaseImpl.Btree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, an, b, 0, bn);
                        MemCopy(a, maxItems - an - bn, b, maxItems - bn, bn);
                        db.FreePage(GetItem(pg, maxItems - r - 2));
                        MemCopy(pg, maxItems - nItems, pg, maxItems - nItems - 1, nItems - r - 1);
                        MemCopy(pg, r, pg, r + 1, nItems - r - 1);
                        SetItemsCount(a, GetItemsCount(a) + bn);
                        SetItemsCount(pg, nItems - 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        if (nItems < max / 2)
                            return TenderBaseImpl.Btree.op_underflow;
                        else
                            return TenderBaseImpl.Btree.op_done;
                    }
                }
                else
                {
                    // page b is before a
                    Page b = db.GetPage(GetItem(pg, maxItems - r));
                    int bn = GetItemsCount(b);
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        an += 1;
                        bn += 1;
                    }

                    if (an + bn > max)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        db.pool.Unfix(b);
                        b = db.PutPage(GetItem(pg, maxItems - r));
                        MemCopy(a, i, a, 0, an);
                        MemCopy(a, 0, b, bn - i, i);
                        MemCopy(a, maxItems - an - i, a, maxItems - an, an);
                        MemCopy(a, maxItems - i, b, maxItems - bn, i);
                        if (height != 1)
                        {
                            MemCopy(a, i - 1, pg, r - 1, 1);
                            MemCopy(pg, r - 1, b, bn - i - 1, 1);
                        }
                        else
                        {
                            MemCopy(pg, r - 1, b, maxItems - bn + i, 1);
                        }

                        SetItemsCount(b, GetItemsCount(b) - i);
                        SetItemsCount(a, GetItemsCount(a) + i);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return TenderBaseImpl.Btree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, bn, a, 0, an);
                        MemCopy(a, 0, b, 0, bn);
                        MemCopy(a, maxItems - an - bn, a, maxItems - an, an);
                        MemCopy(a, maxItems - bn, b, maxItems - bn, bn);
                        if (height != 1)
                        {
                            MemCopy(a, bn - 1, pg, r - 1, 1);
                        }
                        db.FreePage(GetItem(pg, maxItems - r));
                        SetItem(pg, maxItems - r, GetItem(pg, maxItems - r - 1));
                        SetItemsCount(a, GetItemsCount(a) + bn);
                        SetItemsCount(pg, nItems - 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return nItems < max / 2 ? TenderBaseImpl.Btree.op_underflow : TenderBaseImpl.Btree.op_done;
                    }
                }
            }

            internal static int Remove(StorageImpl db, int pageId, int oid, int height)
            {
                Page pg = db.GetPage(pageId);
                try
                {
                    int i, n = GetItemsCount(pg), l = 0, r = n;
                    if (--height == 0)
                    {
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (oid > GetItem(pg, maxItems - 1 - i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }

                        if (r < n && GetItem(pg, maxItems - r - 1) == oid)
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            MemCopy(pg, r, pg, r + 1, n - r - 1);
                            MemCopy(pg, maxItems - n + 1, pg, maxItems - n, n - r - 1);
                            SetItemsCount(pg, --n);
                            return n < max / 2 ? TenderBaseImpl.Btree.op_underflow : TenderBaseImpl.Btree.op_done;
                        }

                        return TenderBaseImpl.Btree.op_not_found;
                    }
                    else
                    {
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (oid > GetItem(pg, i))
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }

                        int result = Remove(db, GetItem(pg, maxItems - r - 1), oid, height);
                        if (result == TenderBaseImpl.Btree.op_underflow)
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            return HandlePageUnderflow(db, pg, r, height);
                        }

                        return result;
                    }
                }
                finally
                {
                    if (pg != null)
                    {
                        db.pool.Unfix(pg);
                    }
                }
            }
        }
    }
}
