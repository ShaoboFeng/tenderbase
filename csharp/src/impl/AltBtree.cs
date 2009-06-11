namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    class AltBtree : PersistentResource, Index
    {
        public virtual Type KeyType
        {
            get
            {
                switch (type)
                {
                    case ClassDescriptor.tpBoolean:
                        return typeof(bool);

                    case ClassDescriptor.tpByte:
                        return typeof(byte);

                    case ClassDescriptor.tpChar:
                        return typeof(char);

                    case ClassDescriptor.tpShort:
                        return typeof(short);

                    case ClassDescriptor.tpInt:
                        return typeof(int);

                    case ClassDescriptor.tpLong:
                        return typeof(long);

                    case ClassDescriptor.tpFloat:
                        return typeof(float);

                    case ClassDescriptor.tpDouble:
                        return typeof(double);

                    case ClassDescriptor.tpString:
                        return typeof(string);

                    case ClassDescriptor.tpDate:
                        return typeof(System.DateTime);

                    case ClassDescriptor.tpObject:
                        return typeof(IPersistent);

                    case ClassDescriptor.tpRaw:
                        return typeof(System.IComparable);

                    default:
                        return null;
                }
            }
        }

        internal int height;
        internal int type;
        internal int nElems;
        internal bool unique;
        internal BtreePage root;

        [NonSerialized]
        internal int updateCounter;

        internal AltBtree()
        {
        }

        internal class BtreeKey
        {
            internal Key key;
            internal IPersistent node;
            internal IPersistent oldNode;

            internal BtreeKey(Key key, IPersistent node)
            {
                this.key = key;
                this.node = node;
            }
        }

        //UPGRADE_NOTE: The access modifier for this class or class field has been changed in order to prevent compilation errors due to the visibility level.
        [Serializable]
        protected internal abstract class BtreePage : Persistent
        {
            internal abstract object Data { get; }
            internal int nItems;
            internal Link items;

            internal const int BTREE_PAGE_SIZE = Page.pageSize - ObjectHeader.Sizeof - 4 * 3;
            internal abstract object GetKeyValue(int i);
            internal abstract Key GetKey(int i);
            internal abstract int Compare(Key key, int i);
            internal abstract void Insert(BtreeKey key, int i);
            internal abstract BtreePage ClonePage();

            internal virtual void ClearKeyValue(int i)
            {
            }

            internal virtual bool Find(Key firstKey, Key lastKey, int height, ArrayList result)
            {
                int l = 0, n = nItems, r = n;
                height -= 1;
                if (firstKey != null)
                {
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (Compare(firstKey, i) >= firstKey.inclusion)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }

                    Assert.That(r == l);
                }

                if (lastKey != null)
                {
                    if (height == 0)
                    {
                        while (l < n)
                        {
                            if (-Compare(lastKey, l) >= lastKey.inclusion)
                            {
                                return false;
                            }

                            result.Add(items.Get(l));
                            l += 1;
                        }
                        return true;
                    }
                    else
                    {
                        do
                        {
                            if (!((BtreePage) items.Get(l)).Find(firstKey, lastKey, height, result))
                            {
                                return false;
                            }

                            if (l == n)
                            {
                                return true;
                            }
                        }
                        while (Compare(lastKey, l++) >= 0);
                        return false;
                    }
                }
                if (height == 0)
                {
                    while (l < n)
                    {
                        result.Add(items.Get(l));
                        l += 1;
                    }
                }
                else
                {
                    do
                    {
                        if (!((BtreePage) items.Get(l)).Find(firstKey, lastKey, height, result))
                        {
                            return false;
                        }
                    }
                    while (++l <= n);
                }
                return true;
            }

            internal static void MemCopyData(BtreePage dst_pg, int dst_idx, BtreePage src_pg, int src_idx, int len)
            {
                throw new System.NotImplementedException();
                /* TODOPORT:
                Array.Copy(src_pg.Data, src_idx, dst_pg.Data, dst_idx, len);
                */
            }

            internal static void MemCopyItems(BtreePage dst_pg, int dst_idx, BtreePage src_pg, int src_idx, int len)
            {
                Array.Copy(src_pg.items.ToRawArray(), src_idx, dst_pg.items.ToRawArray(), dst_idx, len);
            }

            internal static void MemCopy(BtreePage dst_pg, int dst_idx, BtreePage src_pg, int src_idx, int len)
            {
                MemCopyData(dst_pg, dst_idx, src_pg, src_idx, len);
                MemCopyItems(dst_pg, dst_idx, src_pg, src_idx, len);
            }

            internal virtual void MemSet(int i, int len)
            {
                while (--len >= 0)
                {
                    items.Set(i++, null);
                }
            }

            internal virtual int Insert(BtreeKey ins, int height, bool unique, bool overwrite)
            {
                int result;
                int l = 0, n = nItems, r = n;
                while (l < r)
                {
                    int i = (l + r) >> 1;
                    if (Compare(ins.key, i) > 0)
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
                if (--height != 0)
                {
                    result = ((BtreePage) items.Get(r)).Insert(ins, height, unique, overwrite);
                    Assert.That(result != TenderBaseImpl.AltBtree.op_not_found);
                    if (result != TenderBaseImpl.AltBtree.op_overflow)
                    {
                        return result;
                    }
                    n += 1;
                }
                else if (r < n && Compare(ins.key, r) == 0)
                {
                    if (overwrite)
                    {
                        ins.oldNode = items.Get(r);
                        Modify();
                        items.Set(r, ins.node);
                        return TenderBaseImpl.AltBtree.op_overwrite;
                    }
                    else if (unique)
                    {
                        ins.oldNode = items.Get(r);
                        return TenderBaseImpl.AltBtree.op_duplicate;
                    }
                }
                int max = items.Size;
                Modify();
                if (n < max)
                {
                    MemCopy(this, r + 1, this, r, n - r);
                    Insert(ins, r);
                    nItems += 1;
                    return TenderBaseImpl.AltBtree.op_done;
                }
                else
                {
                    /* page is full then divide page */
                    BtreePage b = ClonePage();
                    Assert.That(n == max);
                    int m = max / 2;
                    if (r < m)
                    {
                        MemCopy(b, 0, this, 0, r);
                        MemCopy(b, r + 1, this, r, m - r - 1);
                        MemCopy(this, 0, this, m - 1, max - m + 1);
                        b.Insert(ins, r);
                    }
                    else
                    {
                        MemCopy(b, 0, this, 0, m);
                        MemCopy(this, 0, this, m, r - m);
                        MemCopy(this, r - m + 1, this, r, max - r);
                        Insert(ins, r - m);
                    }

                    MemSet(max - m + 1, m - 1);
                    ins.node = b;
                    ins.key = b.GetKey(m - 1);
                    if (height == 0)
                    {
                        nItems = max - m + 1;
                        b.nItems = m;
                    }
                    else
                    {
                        b.ClearKeyValue(m - 1);
                        nItems = max - m;
                        b.nItems = m - 1;
                    }
                    return TenderBaseImpl.AltBtree.op_overflow;
                }
            }

            internal virtual int HandlePageUnderflow(int r, BtreeKey rem, int height)
            {
                BtreePage a = (BtreePage) items.Get(r);
                a.Modify();
                Modify();
                int an = a.nItems;
                if (r < nItems)
                {
                    // exists greater page
                    BtreePage b = (BtreePage) items.Get(r + 1);
                    int bn = b.nItems;
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        MemCopyData(a, an, this, r, 1);
                        an += 1;
                        bn += 1;
                    }

                    if (an + bn > items.Size)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        b.Modify();
                        MemCopy(a, an, b, 0, i);
                        MemCopy(b, 0, b, i, bn - i);
                        MemCopyData(this, r, a, an + i - 1, 1);
                        if (height != 1)
                        {
                            a.ClearKeyValue(an + i - 1);
                        }
                        b.MemSet(bn - i, i);
                        b.nItems -= i;
                        a.nItems += i;
                        return TenderBaseImpl.AltBtree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, an, b, 0, bn);
                        b.Deallocate();
                        MemCopyData(this, r, this, r + 1, nItems - r - 1);
                        MemCopyItems(this, r + 1, this, r + 2, nItems - r - 1);
                        items.Set(nItems, null);
                        a.nItems += bn;
                        nItems -= 1;
                        return nItems < (items.Size >> 1) ? TenderBaseImpl.AltBtree.op_underflow : TenderBaseImpl.AltBtree.op_done;
                    }
                }
                else
                {
                    // page b is before a
                    BtreePage b = (BtreePage) items.Get(r - 1);
                    int bn = b.nItems;
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        an += 1;
                        bn += 1;
                    }
                    if (an + bn > items.Size)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        b.Modify();
                        MemCopy(a, i, a, 0, an);
                        MemCopy(a, 0, b, bn - i, i);
                        if (height != 1)
                        {
                            MemCopyData(a, i - 1, this, r - 1, 1);
                        }
                        MemCopyData(this, r - 1, b, bn - i - 1, 1);
                        if (height != 1)
                        {
                            b.ClearKeyValue(bn - i - 1);
                        }
                        b.MemSet(bn - i, i);
                        b.nItems -= i;
                        a.nItems += i;
                        return TenderBaseImpl.AltBtree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, bn, a, 0, an);
                        MemCopy(a, 0, b, 0, bn);
                        if (height != 1)
                        {
                            MemCopyData(a, bn - 1, this, r - 1, 1);
                        }
                        b.Deallocate();
                        items.Set(r - 1, a);
                        items.Set(nItems, null);
                        a.nItems += bn;
                        nItems -= 1;
                        return nItems < (items.Size >> 1) ? TenderBaseImpl.AltBtree.op_underflow : TenderBaseImpl.AltBtree.op_done;
                    }
                }
            }

            internal virtual int Remove(BtreeKey rem, int height)
            {
                int i, n = nItems, l = 0, r = n;

                while (l < r)
                {
                    i = (l + r) >> 1;
                    if (Compare(rem.key, i) > 0)
                    {
                        l = i + 1;
                    }
                    else
                    {
                        r = i;
                    }
                }
                if (--height == 0)
                {
                    IPersistent node = rem.node;
                    while (r < n)
                    {
                        if (Compare(rem.key, r) == 0)
                        {
                            if (node == null || items.ContainsElement(r, node))
                            {
                                rem.oldNode = items.Get(r);
                                Modify();
                                MemCopy(this, r, this, r + 1, n - r - 1);
                                nItems = --n;
                                MemSet(n, 1);
                                return n < (items.Size >> 1) ? TenderBaseImpl.AltBtree.op_underflow : TenderBaseImpl.AltBtree.op_done;
                            }
                        }
                        else
                        {
                            break;
                        }
                        r += 1;
                    }
                    return TenderBaseImpl.AltBtree.op_not_found;
                }
                do
                {
                    switch (((BtreePage) items.Get(r)).Remove(rem, height))
                    {
                        case TenderBaseImpl.AltBtree.op_underflow:
                            return HandlePageUnderflow(r, rem, height);

                        case TenderBaseImpl.AltBtree.op_done:
                            return TenderBaseImpl.AltBtree.op_done;
                        }
                }
                while (++r <= n);

                return TenderBaseImpl.AltBtree.op_not_found;
            }

            internal virtual void Purge(int height)
            {
                if (--height != 0)
                {
                    int n = nItems;
                    do
                    {
                        ((BtreePage) items.Get(n)).Purge(height);
                    }
                    while (--n >= 0);
                }
                base.Deallocate();
            }

            internal virtual int TraverseForward(int height, IPersistent[] result, int pos)
            {
                int i, n = nItems;
                if (--height != 0)
                {
                    for (i = 0; i <= n; i++)
                    {
                        pos = ((BtreePage) items.Get(i)).TraverseForward(height, result, pos);
                    }
                }
                else
                {
                    for (i = 0; i < n; i++)
                    {
                        result[pos++] = items.Get(i);
                    }
                }
                return pos;
            }

            internal BtreePage(Storage s, int n)
                : base(s)
            {
                items = s.CreateLink(n);
                items.Size = n;
            }

            internal BtreePage()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfByte : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal byte[] data;

            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 1);

            internal override object GetKeyValue(int i)
            {
                return (byte) data[i];
            }

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfByte(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return (byte) key.ival - data[i];
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = (byte) key.key.ival;
            }

            internal BtreePageOfByte(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new byte[MAX_ITEMS];
            }

            internal BtreePageOfByte()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfBoolean : BtreePageOfByte
        {
            internal override Key GetKey(int i)
            {
                return new Key(data[i] != 0);
            }

            internal override object GetKeyValue(int i)
            {
                return Convert.ToBoolean(i);
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfBoolean(Storage);
            }

            internal BtreePageOfBoolean()
            {
            }

            internal BtreePageOfBoolean(Storage s)
                : base(s)
            {
            }
        }

        [Serializable]
        internal class BtreePageOfShort : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal short[] data;
            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 2);

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return (short) data[i];
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfShort(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return (short) key.ival - data[i];
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = (short) key.key.ival;
            }

            internal BtreePageOfShort(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new short[MAX_ITEMS];
            }

            internal BtreePageOfShort()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfInt : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }
            internal int[] data;

            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 4);

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return (Int32) data[i];
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfInt(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return key.ival - data[i];
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = key.key.ival;
            }

            internal BtreePageOfInt(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new int[MAX_ITEMS];
            }

            internal BtreePageOfInt()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfLong : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal long[] data;
            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 8);

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return (long) data[i];
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfLong(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return key.lval < data[i] ? -1 : (key.lval == data[i] ? 0 : 1);
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = key.key.lval;
            }

            internal BtreePageOfLong(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new long[MAX_ITEMS];
            }

            internal BtreePageOfLong()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfFloat : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal float[] data;
            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 4);

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return (float) data[i];
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfFloat(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                return (float) key.dval < data[i] ? -1 : ((float) key.dval == data[i] ? 0 : 1);
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                data[i] = (float) key.key.dval;
            }

            internal BtreePageOfFloat(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new float[MAX_ITEMS];
            }

            internal BtreePageOfFloat()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfDouble : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal double[] data;
            internal const int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 8);

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return (double) data[i];
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfDouble(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return key.dval < data[i] ? -1 : (key.dval == data[i] ? 0 : 1);
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = key.key.dval;
            }

            internal BtreePageOfDouble(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new double[MAX_ITEMS];
            }

            internal BtreePageOfDouble()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfObject : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data.ToRawArray();
                }
            }

            internal Link data;
            internal static readonly int MAX_ITEMS = BTREE_PAGE_SIZE / (4 + 4);

            internal override Key GetKey(int i)
            {
                return new Key(data.GetRaw(i));
            }

            internal override object GetKeyValue(int i)
            {
                return data.Get(i);
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfObject(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return key.ival - data.Get(i).Oid;
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data.Set(i, (IPersistent) key.key.oval);
            }

            internal BtreePageOfObject(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = s.CreateLink(MAX_ITEMS);
                data.Size = MAX_ITEMS;
            }

            internal BtreePageOfObject()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfString : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal string[] data;
            internal const int MAX_ITEMS = 100;

            internal override Key GetKey(int i)
            {
                return new Key(data[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return data[i];
            }

            internal override void ClearKeyValue(int i)
            {
                data[i] = null;
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfString(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return String.CompareOrdinal(((string) key.oval), data[i]);
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                data[i] = (string) key.key.oval;
            }

            internal override void MemSet(int i, int len)
            {
                while (--len >= 0)
                {
                    items.Set(i, null);
                    data[i] = null;
                    i += 1;
                }
            }

            internal virtual bool PrefixSearch(string key, int height, ArrayList result)
            {
                int l = 0, n = nItems, r = n;
                height -= 1;
                while (l < r)
                {
                    int i = (l + r) >> 1;
                    if (!key.StartsWith(data[i]) && String.CompareOrdinal(key, data[i]) > 0)
                    {
                        l = i + 1;
                    }
                    else
                    {
                        r = i;
                    }
                }
                Assert.That(r == l);
                if (height == 0)
                {
                    while (l < n)
                    {
                        if (String.CompareOrdinal(key, data[l]) < 0)
                        {
                            return false;
                        }
                        result.Add(items.Get(l));
                        l += 1;
                    }
                }
                else
                {
                    do
                    {
                        if (!((BtreePageOfString) items.Get(l)).PrefixSearch(key, height, result))
                        {
                            return false;
                        }
                        if (l == n)
                        {
                            return true;
                        }
                    }
                    while (String.CompareOrdinal(key, data[l++]) >= 0);
                    return false;
                }
                return true;
            }

            internal BtreePageOfString(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new string[MAX_ITEMS];
            }

            internal BtreePageOfString()
            {
            }
        }

        [Serializable]
        internal class BtreePageOfRaw : BtreePage
        {
            internal override object Data
            {
                get
                {
                    return data;
                }
            }

            internal object data;
            internal const int MAX_ITEMS = 100;

            internal override Key GetKey(int i)
            {
                return new Key((System.IComparable) ((object[]) data)[i]);
            }

            internal override object GetKeyValue(int i)
            {
                return ((object[]) data)[i];
            }

            internal override void ClearKeyValue(int i)
            {
                ((object[]) data)[i] = null;
            }

            internal override BtreePage ClonePage()
            {
                return new BtreePageOfRaw(Storage);
            }

            internal override int Compare(Key key, int i)
            {
                return ((System.IComparable) key.oval).CompareTo(((object[]) data)[i]);
            }

            internal override void Insert(BtreeKey key, int i)
            {
                items.Set(i, key.node);
                ((object[]) data)[i] = key.key.oval;
            }

            internal BtreePageOfRaw(Storage s)
                : base(s, MAX_ITEMS)
            {
                data = new object[MAX_ITEMS];
            }

            internal BtreePageOfRaw()
            {
            }
        }

        internal static int CheckType(Type c)
        {
            int elemType = ClassDescriptor.GetTypeCode(c);
            if (elemType > ClassDescriptor.tpObject && elemType != ClassDescriptor.tpRaw)
            {
                throw new StorageError(StorageError.UNSUPPORTED_INDEX_TYPE, c);
            }
            return elemType;
        }

        internal AltBtree(Type cls, bool unique)
        {
            this.unique = unique;
            type = CheckType(cls);
        }

        internal AltBtree(int type, bool unique)
        {
            this.type = type;
            this.unique = unique;
        }

        internal const int op_done = 0;
        internal const int op_overflow = 1;
        internal const int op_underflow = 2;
        internal const int op_not_found = 3;
        internal const int op_duplicate = 4;
        internal const int op_overwrite = 5;

        internal virtual Key CheckKey(Key key)
        {
            if (key != null)
            {
                if (key.type != type)
                {
                    throw new StorageError(StorageError.INCOMPATIBLE_KEY_TYPE);
                }
                if (type == ClassDescriptor.tpObject && key.ival == 0 && key.oval != null)
                {
                    throw new StorageError(StorageError.INVALID_OID);
                }
                if (key.oval is char[])
                {
                    key = new Key(new string((char[]) key.oval), key.inclusion != 0);
                }
            }
            return key;
        }

        public virtual IPersistent Get(Key key)
        {
            key = CheckKey(key);
            if (root != null)
            {
                ArrayList list = new ArrayList();
                root.Find(key, key, height, list);
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

        internal static readonly IPersistent[] emptySelection = new IPersistent[0];

        public virtual IPersistent[] PrefixSearch(string key)
        {
            if (ClassDescriptor.tpString != type)
            {
                throw new StorageError(StorageError.INCOMPATIBLE_KEY_TYPE);
            }
            if (root != null)
            {
                ArrayList list = new ArrayList();
                ((BtreePageOfString) root).PrefixSearch(key, height, list);
                if (list.Count != 0)
                {
                    return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
                }
            }
            return emptySelection;
        }

        public virtual IPersistent[] Get(Key from, Key till)
        {
            if (root != null)
            {
                ArrayList list = new ArrayList();
                root.Find(CheckKey(from), CheckKey(till), height, list);
                if (list.Count != 0)
                {
                    return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
                }
            }
            return emptySelection;
        }

        public virtual bool Put(Key key, IPersistent obj)
        {
            return Insert(key, obj, false) == null;
        }

        public virtual IPersistent Set(Key key, IPersistent obj)
        {
            return Insert(key, obj, true);
        }

        internal void AllocateRootPage(BtreeKey ins)
        {
            Storage s = Storage;
            BtreePage newRoot = null;
            switch (type)
            {
                case ClassDescriptor.tpByte:
                    newRoot = new BtreePageOfByte(s);
                    break;

                case ClassDescriptor.tpShort:
                    newRoot = new BtreePageOfShort(s);
                    break;

                case ClassDescriptor.tpBoolean:
                    newRoot = new BtreePageOfBoolean(s);
                    break;

                case ClassDescriptor.tpInt:
                    newRoot = new BtreePageOfInt(s);
                    break;

                case ClassDescriptor.tpLong:
                    newRoot = new BtreePageOfLong(s);
                    break;

                case ClassDescriptor.tpFloat:
                    newRoot = new BtreePageOfFloat(s);
                    break;

                case ClassDescriptor.tpDouble:
                    newRoot = new BtreePageOfDouble(s);
                    break;

                case ClassDescriptor.tpObject:
                    newRoot = new BtreePageOfObject(s);
                    break;

                case ClassDescriptor.tpString:
                    newRoot = new BtreePageOfString(s);
                    break;

                case ClassDescriptor.tpRaw:
                    newRoot = new BtreePageOfRaw(s);
                    break;

                default:
                    Assert.Failed("Invalid type");
                    break;
            }
            newRoot.Insert(ins, 0);
            newRoot.items.Set(1, root);
            newRoot.nItems = 1;
            root = newRoot;
        }

        internal IPersistent Insert(Key key, IPersistent obj, bool overwrite)
        {
            BtreeKey ins = new BtreeKey(CheckKey(key), obj);
            if (root == null)
            {
                AllocateRootPage(ins);
                height = 1;
            }
            else
            {
                int result = root.Insert(ins, height, unique, overwrite);
                if (result == op_overflow)
                {
                    AllocateRootPage(ins);
                    height += 1;
                }
                else if (result == op_duplicate || result == op_overwrite)
                {
                    return ins.oldNode;
                }
            }
            updateCounter += 1;
            nElems += 1;
            Modify();
            return null;
        }

        public virtual void Remove(Key key, IPersistent obj)
        {
            Remove(new BtreeKey(CheckKey(key), obj));
        }

        internal virtual void Remove(BtreeKey rem)
        {
            if (root == null)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            int result = root.Remove(rem, height);
            if (result == op_not_found)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }
            nElems -= 1;
            if (result == op_underflow)
            {
                if (root.nItems == 0)
                {
                    BtreePage newRoot = null;
                    if (height != 1)
                    {
                        newRoot = (BtreePage) root.items.Get(0);
                    }
                    root.Deallocate();
                    root = newRoot;
                    height -= 1;
                }
            }
            updateCounter += 1;
            Modify();
        }

        public virtual IPersistent Remove(Key key)
        {
            if (!unique)
            {
                throw new StorageError(StorageError.KEY_NOT_UNIQUE);
            }
            BtreeKey rk = new BtreeKey(CheckKey(key), null);
            Remove(rk);
            return rk.oldNode;
        }

        public virtual IPersistent Get(string key)
        {
            return Get(new Key(key, true));
        }

        public virtual IPersistent[] GetPrefix(string prefix)
        {
            return Get(new Key(prefix, true), new Key(prefix + System.Char.MaxValue, false));
        }

        public virtual bool Put(string key, IPersistent obj)
        {
            return Put(new Key(key, true), obj);
        }

        public virtual IPersistent Set(string key, IPersistent obj)
        {
            return Set(new Key(key, true), obj);
        }

        public virtual void Remove(string key, IPersistent obj)
        {
            Remove(new Key(key, true), obj);
        }

        public virtual IPersistent Remove(string key)
        {
            return Remove(new Key(key, true));
        }

        public virtual int Size()
        {
            return nElems;
        }

        public virtual void Clear()
        {
            if (root != null)
            {
                root.Purge(height);
                root = null;
                nElems = 0;
                height = 0;
                updateCounter += 1;
                Modify();
            }
        }

        public virtual IPersistent[] ToPersistentArray()
        {
            IPersistent[] arr = new IPersistent[nElems];
            if (root != null)
            {
                root.TraverseForward(height, arr, 0);
            }
            return arr;
        }

        public virtual IPersistent[] ToPersistentArray(IPersistent[] arr)
        {
            if (arr.Length < nElems)
            {
                arr = (IPersistent[]) System.Array.CreateInstance(arr.GetType().GetElementType(), nElems);
            }
            if (root != null)
            {
                root.TraverseForward(height, arr, 0);
            }
            if (arr.Length > nElems)
            {
                arr[nElems] = null;
            }
            return arr;
        }

        public override void Deallocate()
        {
            if (root != null)
            {
                root.Purge(height);
            }
            base.Deallocate();
        }

        internal struct BtreeEntry
        {
            public object Key
            {
                get
                {
                    return pg.GetKeyValue(pos);
                }
            }

            public object Value
            {
                get
                {
                    return pg.items.Get(pos);
                }

                set
                {
                    throw new System.NotSupportedException();
                }
            }

            public override bool Equals(object o)
            {
                if (!(o is System.Collections.DictionaryEntry))
                {
                    return false;
                }
                System.Collections.DictionaryEntry e = (System.Collections.DictionaryEntry) o;
                return (Key == null ? e.Key == null : Key.Equals(e.Key)) && (Value == null ? Value == null : Value.Equals(e.Value));
            }

            internal BtreeEntry(BtreePage pg, int pos)
            {
                this.pg = pg;
                this.pos = pos;
            }

            private BtreePage pg;
            private int pos;
            //UPGRADE_NOTE: The following method implementation was automatically added to preserve functionality.
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeIterator' to access its enclosing instance.
        internal class BtreeIterator : IEnumerator
        {
            private void InitBlock(AltBtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private AltBtree enclosingInstance;

            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    int pos = posStack[sp - 1];
                    BtreePage pg = pageStack[sp - 1];
                    object curr = GetCurrent(pg, pos);
                    if (++pos == end)
                    {
                        while (--sp != 0)
                        {
                            pos = posStack[sp - 1];
                            pg = pageStack[sp - 1];
                            if (++pos <= pg.nItems)
                            {
                                posStack[sp - 1] = pos;
                                do
                                {
                                    pg = (BtreePage) pg.items.Get(pos);
                                    end = pg.nItems;
                                    pageStack[sp] = pg;
                                    posStack[sp] = pos = 0;
                                }
                                while (++sp < pageStack.Length);
                                break;
                            }
                        }
                    }
                    else
                    {
                        posStack[sp - 1] = pos;
                    }
                    return curr;
                }
            }

            public AltBtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeIterator(AltBtree enclosingInstance)
            {
                InitBlock(enclosingInstance);
                BtreePage page = Enclosing_Instance.root;
                int h = Enclosing_Instance.height;
                counter = Enclosing_Instance.updateCounter;
                pageStack = new BtreePage[h];
                posStack = new int[h];
                sp = 0;
                if (h > 0)
                {
                    while (--h > 0)
                    {
                        posStack[sp] = 0;
                        pageStack[sp] = page;
                        page = (BtreePage) page.items.Get(0);
                        sp += 1;
                    }
                    posStack[sp] = 0;
                    pageStack[sp] = page;
                    end = page.nItems;
                    sp += 1;
                }
            }

            public virtual bool MoveNext()
            {
                if (counter != Enclosing_Instance.updateCounter)
                {
                    throw new System.Exception();
                }
                return sp > 0 && posStack[sp - 1] < end;
            }

            internal virtual object GetCurrent(BtreePage pg, int pos)
            {
                return pg.items.Get(pos);
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal BtreePage[] pageStack;
            internal int[] posStack;
            internal int sp;
            internal int end;
            internal int counter;
            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeEntryIterator' to access its enclosing instance.
        internal class BtreeEntryIterator : BtreeIterator
        {
            public BtreeEntryIterator(AltBtree enclosingInstance)
                : base(enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }

            private void InitBlock(AltBtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private AltBtree enclosingInstance;
            public new AltBtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal override object GetCurrent(BtreePage pg, int pos)
            {
                return new BtreeEntry(pg, pos);
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new BtreeIterator(this);
        }

        public virtual IEnumerator GetEntryEnumerator()
        {
            return new BtreeEntryIterator(this);
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeSelectionIterator' to access its enclosing instance.
        internal class BtreeSelectionIterator : IEnumerator
        {
            private void InitBlock(AltBtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private AltBtree enclosingInstance;
            public virtual object Current
            {
                get
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                    if (!MoveNext())
                    {
                        throw new System.ArgumentOutOfRangeException();
                    }
                    int pos = posStack[sp - 1];
                    BtreePage pg = pageStack[sp - 1];
                    object curr = GetCurrent(pg, pos);
                    GotoNextItem(pg, pos);
                    return curr;
                }
            }

            public AltBtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeSelectionIterator(AltBtree enclosingInstance, Key from, Key till, IndexSortOrder order)
            {
                InitBlock(enclosingInstance);
                int i, l, r;

                sp = 0;
                counter = Enclosing_Instance.updateCounter;
                if (Enclosing_Instance.height == 0)
                {
                    return;
                }

                BtreePage page = Enclosing_Instance.root;
                int h = Enclosing_Instance.height;
                this.from = from;
                this.till = till;
                this.order = order;

                pageStack = new BtreePage[h];
                posStack = new int[h];

                if (order == TenderBase.IndexSortOrder.Ascent)
                {
                    if (from == null)
                    {
                        while (--h > 0)
                        {
                            posStack[sp] = 0;
                            pageStack[sp] = page;
                            page = (BtreePage) page.items.Get(0);
                            sp += 1;
                        }

                        posStack[sp] = 0;
                        pageStack[sp] = page;
                        end = page.nItems;
                        sp += 1;
                    }
                    else
                    {
                        while (--h > 0)
                        {
                            pageStack[sp] = page;
                            l = 0;
                            r = page.nItems;
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (page.Compare(from, i) >= from.inclusion)
                                {
                                    l = i + 1;
                                }
                                else
                                {
                                    r = i;
                                }
                            }
                            Assert.That(r == l);
                            posStack[sp] = r;
                            page = (BtreePage) page.items.Get(r);
                            sp += 1;
                        }
                        pageStack[sp] = page;
                        l = 0;
                        r = end = page.nItems;
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (page.Compare(from, i) >= from.inclusion)
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }
                        Assert.That(r == l);
                        if (r == end)
                        {
                            sp += 1;
                            GotoNextItem(page, r - 1);
                        }
                        else
                        {
                            posStack[sp++] = r;
                        }
                    }
                    if (sp != 0 && till != null)
                    {
                        page = pageStack[sp - 1];
                        if (-page.Compare(till, posStack[sp - 1]) >= till.inclusion)
                        {
                            sp = 0;
                        }
                    }
                }
                else
                {
                    // descent order
                    if (till == null)
                    {
                        while (--h > 0)
                        {
                            pageStack[sp] = page;
                            posStack[sp] = page.nItems;
                            page = (BtreePage) page.items.Get(page.nItems);
                            sp += 1;
                        }
                        pageStack[sp] = page;
                        posStack[sp++] = page.nItems - 1;
                    }
                    else
                    {
                        while (--h > 0)
                        {
                            pageStack[sp] = page;
                            l = 0;
                            r = page.nItems;
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (page.Compare(till, i) >= 1 - till.inclusion)
                                {
                                    l = i + 1;
                                }
                                else
                                {
                                    r = i;
                                }
                            }
                            Assert.That(r == l);
                            posStack[sp] = r;
                            page = (BtreePage) page.items.Get(r);
                            sp += 1;
                        }
                        pageStack[sp] = page;
                        l = 0;
                        r = page.nItems;
                        while (l < r)
                        {
                            i = (l + r) >> 1;
                            if (page.Compare(till, i) >= 1 - till.inclusion)
                            {
                                l = i + 1;
                            }
                            else
                            {
                                r = i;
                            }
                        }
                        Assert.That(r == l);
                        if (r == 0)
                        {
                            sp += 1;
                            GotoNextItem(page, r);
                        }
                        else
                        {
                            posStack[sp++] = r - 1;
                        }
                    }
                    if (sp != 0 && from != null)
                    {
                        page = pageStack[sp - 1];
                        if (page.Compare(from, posStack[sp - 1]) >= from.inclusion)
                        {
                            sp = 0;
                        }
                    }
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

            protected internal virtual object GetCurrent(BtreePage pg, int pos)
            {
                return pg.items.Get(pos);
            }

            protected internal void GotoNextItem(BtreePage pg, int pos)
            {
                if (order == TenderBase.IndexSortOrder.Ascent)
                {
                    if (++pos == end)
                    {
                        while (--sp != 0)
                        {
                            pos = posStack[sp - 1];
                            pg = pageStack[sp - 1];
                            if (++pos <= pg.nItems)
                            {
                                posStack[sp - 1] = pos;
                                do
                                {
                                    pg = (BtreePage) pg.items.Get(pos);
                                    end = pg.nItems;
                                    pageStack[sp] = pg;
                                    posStack[sp] = pos = 0;
                                }
                                while (++sp < pageStack.Length);
                                break;
                            }
                        }
                    }
                    else
                    {
                        posStack[sp - 1] = pos;
                    }
                    if (sp != 0 && till != null && -pg.Compare(till, pos) >= till.inclusion)
                    {
                        sp = 0;
                    }
                }
                else
                {
                    // descent order
                    if (--pos < 0)
                    {
                        while (--sp != 0)
                        {
                            pos = posStack[sp - 1];
                            pg = pageStack[sp - 1];
                            if (--pos >= 0)
                            {
                                posStack[sp - 1] = pos;
                                do
                                {
                                    pg = (BtreePage) pg.items.Get(pos);
                                    pageStack[sp] = pg;
                                    posStack[sp] = pos = pg.nItems;
                                }
                                while (++sp < pageStack.Length);
                                posStack[sp - 1] = --pos;
                                break;
                            }
                        }
                    }
                    else
                    {
                        posStack[sp - 1] = pos;
                    }
                    if (sp != 0 && from != null && pg.Compare(from, pos) >= from.inclusion)
                    {
                        sp = 0;
                    }
                }
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal BtreePage[] pageStack;
            internal int[] posStack;
            internal int sp;
            internal int end;
            internal Key from;
            internal Key till;
            internal IndexSortOrder order;
            internal int counter;

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeSelectionEntryIterator' to access its enclosing instance.
        internal class BtreeSelectionEntryIterator : BtreeSelectionIterator
        {
            private void InitBlock(AltBtree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private AltBtree enclosingInstance;
            public new AltBtree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeSelectionEntryIterator(AltBtree enclosingInstance, Key from, Key till, IndexSortOrder order)
                : base(enclosingInstance, from, till, order)
            {
                InitBlock(enclosingInstance);
            }

            protected internal override object GetCurrent(BtreePage pg, int pos)
            {
                return new BtreeEntry(pg, pos);
            }
        }

        public virtual IEnumerator GetEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new BtreeSelectionIterator(this, CheckKey(from), CheckKey(till), order);
        }

        public virtual IEnumerator PrefixIterator(string prefix)
        {
            return GetEnumerator(new Key(prefix), new Key(prefix + System.Char.MaxValue, false), IndexSortOrder.Ascent);
        }

        public virtual IEnumerator GetEntryEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new BtreeSelectionEntryIterator(this, CheckKey(from), CheckKey(till), order);
        }
    }
}
