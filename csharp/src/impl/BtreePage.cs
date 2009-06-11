namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    class BtreePage
    {
        internal const int firstKeyOffs = 4;
        internal const int keySpace = Page.pageSize - firstKeyOffs;
        internal const int strKeySize = 8;
        internal const int maxItems = keySpace / 4;

        internal static int GetItemsCount(Page pg)
        {
            return Bytes.Unpack2(pg.data, 0);
        }

        internal static int GetSize(Page pg)
        {
            return Bytes.Unpack2(pg.data, 2);
        }

        internal static int GetKeyStrOid(Page pg, int index)
        {
            return Bytes.Unpack4(pg.data, firstKeyOffs + index * 8);
        }

        internal static int GetKeyStrSize(Page pg, int index)
        {
            return Bytes.Unpack2(pg.data, firstKeyOffs + index * 8 + 4);
        }

        internal static int GetKeyStrOffs(Page pg, int index)
        {
            return Bytes.Unpack2(pg.data, firstKeyOffs + index * 8 + 6);
        }

        internal static int GetReference(Page pg, int index)
        {
            return Bytes.Unpack4(pg.data, firstKeyOffs + index * 4);
        }

        internal static void SetItemsCount(Page pg, int nItems)
        {
            Bytes.Pack2(pg.data, 0, (short) nItems);
        }

        internal static void SetSize(Page pg, int size)
        {
            Bytes.Pack2(pg.data, 2, (short) size);
        }

        internal static void SetKeyStrOid(Page pg, int index, int oid)
        {
            Bytes.Pack4(pg.data, firstKeyOffs + index * 8, oid);
        }

        internal static void SetKeyStrSize(Page pg, int index, int size)
        {
            Bytes.Pack2(pg.data, firstKeyOffs + index * 8 + 4, (short) size);
        }

        internal static void SetKeyStrOffs(Page pg, int index, int offs)
        {
            Bytes.Pack2(pg.data, firstKeyOffs + index * 8 + 6, (short) offs);
        }

        internal static void SetKeyStrChars(Page pg, int offs, char[] str)
        {
            int len = str.Length;
            for (int i = 0; i < len; i++)
            {
                Bytes.Pack2(pg.data, firstKeyOffs + offs, (short) str[i]);
                offs += 2;
            }
        }

        internal static void SetKeyBytes(Page pg, int offs, byte[] bytes)
        {
            Array.Copy(bytes, 0, pg.data, firstKeyOffs + offs, bytes.Length);
        }

        internal static void SetReference(Page pg, int index, int oid)
        {
            Bytes.Pack4(pg.data, firstKeyOffs + index * 4, oid);
        }

        internal static int Compare(Key key, Page pg, int i)
        {
            long i8;
            int i4;
            float r4;
            double r8;
            switch (key.type)
            {
                case ClassDescriptor.tpBoolean:
                case ClassDescriptor.tpByte:
                    return (byte) key.ival - pg.data[BtreePage.firstKeyOffs + i];

                case ClassDescriptor.tpShort:
                    return (short) key.ival - Bytes.Unpack2(pg.data, BtreePage.firstKeyOffs + i * 2);

                case ClassDescriptor.tpChar:
                    return (char) key.ival - (char) Bytes.Unpack2(pg.data, BtreePage.firstKeyOffs + i * 2);

                case ClassDescriptor.tpObject:
                case ClassDescriptor.tpInt:
                    i4 = Bytes.Unpack4(pg.data, BtreePage.firstKeyOffs + i * 4);
                    return key.ival < i4 ? -1 : (key.ival == i4 ? 0 : 1);

                case ClassDescriptor.tpLong:
                case ClassDescriptor.tpDate:
                    i8 = Bytes.Unpack8(pg.data, BtreePage.firstKeyOffs + i * 8);
                    return key.lval < i8 ? -1 : (key.lval == i8 ? 0 : 1);

                case ClassDescriptor.tpFloat:
                    r4 = Bytes.UnpackF4(pg.data, BtreePage.firstKeyOffs + i * 4);
                    return key.dval < r4 ? -1 : (key.dval == r4 ? 0 : 1);

                case ClassDescriptor.tpDouble:
                    r8 = Bytes.UnpackF8(pg.data, BtreePage.firstKeyOffs + i * 8);
                    return key.dval < r8 ? -1 : (key.dval == r8 ? 0 : 1);
                }
            Assert.Failed("Invalid type");
            return 0;
        }

        internal static int CompareStr(Key key, Page pg, int i)
        {
            char[] chars = (char[]) key.oval;
            int alen = chars.Length;
            int blen = BtreePage.GetKeyStrSize(pg, i);
            int minlen = alen < blen ? alen : blen;
            int offs = BtreePage.GetKeyStrOffs(pg, i) + BtreePage.firstKeyOffs;
            byte[] b = pg.data;
            for (int j = 0; j < minlen; j++)
            {
                int diff = chars[j] - (char) Bytes.Unpack2(b, offs);
                if (diff != 0)
                {
                    return diff;
                }
                offs += 2;
            }
            return alen - blen;
        }

        internal static int ComparePrefix(char[] key, Page pg, int i)
        {
            int alen = key.Length;
            int blen = BtreePage.GetKeyStrSize(pg, i);
            int minlen = alen < blen ? alen : blen;
            int offs = BtreePage.GetKeyStrOffs(pg, i) + BtreePage.firstKeyOffs;
            byte[] b = pg.data;
            for (int j = 0; j < minlen; j++)
            {
                int diff = key[j] - (char) Bytes.Unpack2(b, offs);
                if (diff != 0)
                {
                    return diff;
                }
                offs += 2;
            }
            return minlen - blen;
        }

        internal static bool Find(StorageImpl db, int pageId, Key firstKey, Key lastKey, Btree tree, int height, ArrayList result)
        {
            Page pg = db.GetPage(pageId);
            int l = 0, n = GetItemsCount(pg), r = n;
            int oid;
            height -= 1;
            try
            {
                if (tree.type == ClassDescriptor.tpString)
                {
                    if (firstKey != null)
                    {
                        while (l < r)
                        {
                            int i = (l + r) >> 1;
                            if (CompareStr(firstKey, pg, i) >= firstKey.inclusion)
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
                                if (-CompareStr(lastKey, pg, l) >= lastKey.inclusion)
                                {
                                    return false;
                                }
                                oid = GetKeyStrOid(pg, l);
                                result.Add(db.LookupObject(oid, null));
                                l += 1;
                            }
                        }
                        else
                        {
                            do
                            {
                                if (!Find(db, GetKeyStrOid(pg, l), firstKey, lastKey, tree, height, result))
                                {
                                    return false;
                                }
                                if (l == n)
                                {
                                    return true;
                                }
                            }
                            while (CompareStr(lastKey, pg, l++) >= 0);
                            return false;
                        }
                    }
                    else
                    {
                        if (height == 0)
                        {
                            while (l < n)
                            {
                                oid = GetKeyStrOid(pg, l);
                                result.Add(db.LookupObject(oid, null));
                                l += 1;
                            }
                        }
                        else
                        {
                            do
                            {
                                if (!Find(db, GetKeyStrOid(pg, l), firstKey, lastKey, tree, height, result))
                                {
                                    return false;
                                }
                            }
                            while (++l <= n);
                        }
                    }
                }
                else if (tree.type == ClassDescriptor.tpArrayOfByte)
                {
                    if (firstKey != null)
                    {
                        while (l < r)
                        {
                            int i = (l + r) >> 1;
                            if (tree.CompareByteArrays(firstKey, pg, i) >= firstKey.inclusion)
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
                                if (-tree.CompareByteArrays(lastKey, pg, l) >= lastKey.inclusion)
                                {
                                    return false;
                                }
                                oid = GetKeyStrOid(pg, l);
                                result.Add(db.LookupObject(oid, null));
                                l += 1;
                            }
                        }
                        else
                        {
                            do
                            {
                                if (!Find(db, GetKeyStrOid(pg, l), firstKey, lastKey, tree, height, result))
                                {
                                    return false;
                                }
                                if (l == n)
                                {
                                    return true;
                                }
                            }
                            while (tree.CompareByteArrays(lastKey, pg, l++) >= 0);
                            return false;
                        }
                    }
                    else
                    {
                        if (height == 0)
                        {
                            while (l < n)
                            {
                                oid = GetKeyStrOid(pg, l);
                                result.Add(db.LookupObject(oid, null));
                                l += 1;
                            }
                        }
                        else
                        {
                            do
                            {
                                if (!Find(db, GetKeyStrOid(pg, l), firstKey, lastKey, tree, height, result))
                                {
                                    return false;
                                }
                            }
                            while (++l <= n);
                        }
                    }
                }
                else
                {
                    if (firstKey != null)
                    {
                        while (l < r)
                        {
                            int i = (l + r) >> 1;
                            if (Compare(firstKey, pg, i) >= firstKey.inclusion)
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
                                if (-Compare(lastKey, pg, l) >= lastKey.inclusion)
                                {
                                    return false;
                                }
                                oid = GetReference(pg, maxItems - 1 - l);
                                result.Add(db.LookupObject(oid, null));
                                l += 1;
                            }
                            return true;
                        }
                        else
                        {
                            do
                            {
                                if (!Find(db, GetReference(pg, maxItems - 1 - l), firstKey, lastKey, tree, height, result))
                                {
                                    return false;
                                }
                                if (l == n)
                                {
                                    return true;
                                }
                            }
                            while (Compare(lastKey, pg, l++) >= 0);
                            return false;
                        }
                    }
                    if (height == 0)
                    {
                        while (l < n)
                        {
                            oid = GetReference(pg, maxItems - 1 - l);
                            result.Add(db.LookupObject(oid, null));
                            l += 1;
                        }
                    }
                    else
                    {
                        do
                        {
                            if (!Find(db, GetReference(pg, maxItems - 1 - l), firstKey, lastKey, tree, height, result))
                            {
                                return false;
                            }
                        }
                        while (++l <= n);
                    }
                }
            }
            finally
            {
                db.pool.Unfix(pg);
            }
            return true;
        }

        internal static bool PrefixSearch(StorageImpl db, int pageId, char[] key, int height, ArrayList result)
        {
            Page pg = db.GetPage(pageId);
            int l = 0, n = GetItemsCount(pg), r = n;
            int oid;
            height -= 1;
            try
            {
                while (l < r)
                {
                    int i = (l + r) >> 1;
                    if (ComparePrefix(key, pg, i) > 0)
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
                        if (ComparePrefix(key, pg, l) < 0)
                        {
                            return false;
                        }
                        oid = GetKeyStrOid(pg, l);
                        result.Add(db.LookupObject(oid, null));
                        l += 1;
                    }
                }
                else
                {
                    do
                    {
                        if (!PrefixSearch(db, GetKeyStrOid(pg, l), key, height, result))
                        {
                            return false;
                        }
                        if (l == n)
                        {
                            return true;
                        }
                    }
                    while (ComparePrefix(key, pg, l++) >= 0);
                    return false;
                }
            }
            finally
            {
                db.pool.Unfix(pg);
            }
            return true;
        }

        internal static int Allocate(StorageImpl db, int root, int type, BtreeKey ins)
        {
            int pageId = db.AllocatePage();
            Page pg = db.PutPage(pageId);
            SetItemsCount(pg, 1);
            if (type == ClassDescriptor.tpString)
            {
                char[] sval = (char[]) ins.key.oval;
                int len = sval.Length;
                SetSize(pg, len * 2);
                SetKeyStrOffs(pg, 0, keySpace - len * 2);
                SetKeyStrSize(pg, 0, len);
                SetKeyStrOid(pg, 0, ins.oid);
                SetKeyStrOid(pg, 1, root);
                SetKeyStrChars(pg, keySpace - len * 2, sval);
            }
            else if (type == ClassDescriptor.tpArrayOfByte)
            {
                byte[] bval = (byte[]) ins.key.oval;
                int len = bval.Length;
                SetSize(pg, len);
                SetKeyStrOffs(pg, 0, keySpace - len);
                SetKeyStrSize(pg, 0, len);
                SetKeyStrOid(pg, 0, ins.oid);
                SetKeyStrOid(pg, 1, root);
                SetKeyBytes(pg, keySpace - len, bval);
            }
            else
            {
                ins.Pack(pg, 0);
                SetReference(pg, maxItems - 2, root);
            }
            db.pool.Unfix(pg);
            return pageId;
        }

        internal static void MemCopy(Page dst_pg, int dst_idx, Page src_pg, int src_idx, int len, int itemSize)
        {
            Array.Copy(src_pg.data, firstKeyOffs + src_idx * itemSize, dst_pg.data, firstKeyOffs + dst_idx * itemSize, len * itemSize);
        }

        internal static int Insert(StorageImpl db, int pageId, Btree tree, BtreeKey ins, int height, bool unique, bool overwrite)
        {
            Page pg = db.GetPage(pageId);
            int result;
            int l = 0, n = GetItemsCount(pg), r = n;
            try
            {
                if (tree.type == ClassDescriptor.tpString)
                {
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (CompareStr(ins.key, pg, i) > 0)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }
                    Assert.That(l == r);
                    if (--height != 0)
                    {
                        result = Insert(db, GetKeyStrOid(pg, r), tree, ins, height, unique, overwrite);
                        Assert.That(result != Btree.op_not_found);
                        if (result != Btree.op_overflow)
                        {
                            return result;
                        }
                    }
                    else if (r < n && CompareStr(ins.key, pg, r) == 0)
                    {
                        if (overwrite)
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            ins.oldOid = GetKeyStrOid(pg, r);
                            SetKeyStrOid(pg, r, ins.oid);
                            return Btree.op_overwrite;
                        }
                        else if (unique)
                        {
                            return Btree.op_duplicate;
                        }
                    }
                    db.pool.Unfix(pg);
                    pg = null;
                    pg = db.PutPage(pageId);
                    return InsertStrKey(db, pg, r, ins, height);
                }
                else if (tree.type == ClassDescriptor.tpArrayOfByte)
                {
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (tree.CompareByteArrays(ins.key, pg, i) > 0)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }
                    Assert.That(l == r);
                    if (--height != 0)
                    {
                        result = Insert(db, GetKeyStrOid(pg, r), tree, ins, height, unique, overwrite);
                        Assert.That(result != Btree.op_not_found);
                        if (result != Btree.op_overflow)
                        {
                            return result;
                        }
                    }
                    else if (r < n && tree.CompareByteArrays(ins.key, pg, r) == 0)
                    {
                        if (overwrite)
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            ins.oldOid = GetKeyStrOid(pg, r);
                            SetKeyStrOid(pg, r, ins.oid);
                            return Btree.op_overwrite;
                        }
                        else if (unique)
                        {
                            return Btree.op_duplicate;
                        }
                    }
                    db.pool.Unfix(pg);
                    pg = null;
                    pg = db.PutPage(pageId);
                    return InsertByteArrayKey(db, pg, r, ins, height);
                }
                else
                {
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (Compare(ins.key, pg, i) > 0)
                            l = i + 1;
                        else
                            r = i;
                    }
                    Assert.That(l == r);
                    /* insert before e[r] */
                    if (--height != 0)
                    {
                        result = Insert(db, GetReference(pg, maxItems - r - 1), tree, ins, height, unique, overwrite);
                        Assert.That(result != Btree.op_not_found);
                        if (result != Btree.op_overflow)
                        {
                            return result;
                        }
                        n += 1;
                    }
                    else if (r < n && Compare(ins.key, pg, r) == 0)
                    {
                        if (overwrite)
                        {
                            db.pool.Unfix(pg);
                            pg = null;
                            pg = db.PutPage(pageId);
                            ins.oldOid = GetReference(pg, maxItems - r - 1);
                            SetReference(pg, maxItems - r - 1, ins.oid);
                            return Btree.op_overwrite;
                        }
                        else if (unique)
                        {
                            return Btree.op_duplicate;
                        }
                    }
                    db.pool.Unfix(pg);
                    pg = null;
                    pg = db.PutPage(pageId);
                    int itemSize = ClassDescriptor.Sizeof[tree.type];
                    int max = keySpace / (4 + itemSize);
                    if (n < max)
                    {
                        MemCopy(pg, r + 1, pg, r, n - r, itemSize);
                        MemCopy(pg, maxItems - n - 1, pg, maxItems - n, n - r, 4);
                        ins.Pack(pg, r);
                        SetItemsCount(pg, GetItemsCount(pg) + 1);
                        return Btree.op_done;
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
                            MemCopy(b, 0, pg, 0, r, itemSize);
                            MemCopy(b, r + 1, pg, r, m - r - 1, itemSize);
                            MemCopy(pg, 0, pg, m - 1, max - m + 1, itemSize);
                            MemCopy(b, maxItems - r, pg, maxItems - r, r, 4);
                            ins.Pack(b, r);
                            MemCopy(b, maxItems - m, pg, maxItems - m + 1, m - r - 1, 4);
                            MemCopy(pg, maxItems - max + m - 1, pg, maxItems - max, max - m + 1, 4);
                        }
                        else
                        {
                            MemCopy(b, 0, pg, 0, m, itemSize);
                            MemCopy(pg, 0, pg, m, r - m, itemSize);
                            MemCopy(pg, r - m + 1, pg, r, max - r, itemSize);
                            MemCopy(b, maxItems - m, pg, maxItems - m, m, 4);
                            MemCopy(pg, maxItems - r + m, pg, maxItems - r, r - m, 4);
                            ins.Pack(pg, r - m);
                            MemCopy(pg, maxItems - max + m - 1, pg, maxItems - max, max - r, 4);
                        }
                        ins.oid = pageId;
                        ins.Extract(b, firstKeyOffs + (m - 1) * itemSize, tree.type);
                        if (height == 0)
                        {
                            SetItemsCount(pg, max - m + 1);
                            SetItemsCount(b, m);
                        }
                        else
                        {
                            SetItemsCount(pg, max - m);
                            SetItemsCount(b, m - 1);
                        }
                        db.pool.Unfix(b);
                        return Btree.op_overflow;
                    }
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

        internal static int InsertStrKey(StorageImpl db, Page pg, int r, BtreeKey ins, int height)
        {
            int nItems = GetItemsCount(pg);
            int size = GetSize(pg);
            int n = (height != 0) ? nItems + 1 : nItems;
            // insert before e[r]
            char[] sval = (char[]) ins.key.oval;
            int len = sval.Length;
            if (size + len * 2 + (n + 1) * strKeySize <= keySpace)
            {
                MemCopy(pg, r + 1, pg, r, n - r, strKeySize);
                size += len * 2;
                SetKeyStrOffs(pg, r, keySpace - size);
                SetKeyStrSize(pg, r, len);
                SetKeyStrOid(pg, r, ins.oid);
                SetKeyStrChars(pg, keySpace - size, sval);
                nItems += 1;
            }
            else
            {
                // page is full then divide page
                int pageId = db.AllocatePage();
                Page b = db.PutPage(pageId);
                int moved = 0;
                int inserted = len * 2 + strKeySize;
                int prevDelta = (1 << 31) + 1;

                for (int bn = 0, i = 0; ; bn += 1)
                {
                    int addSize, subSize;
                    int j = nItems - i - 1;
                    int keyLen = GetKeyStrSize(pg, i);
                    if (bn == r)
                    {
                        keyLen = len;
                        inserted = 0;
                        addSize = len;
                        if (height == 0)
                        {
                            subSize = 0;
                            j += 1;
                        }
                        else
                        {
                            subSize = GetKeyStrSize(pg, i);
                        }
                    }
                    else
                    {
                        addSize = subSize = keyLen;
                        if (height != 0)
                        {
                            if (i + 1 != r)
                            {
                                subSize += GetKeyStrSize(pg, i + 1);
                                j -= 1;
                            }
                            else
                            {
                                inserted = 0;
                            }
                        }
                    }
                    int delta = (moved + addSize * 2 + (bn + 1) * strKeySize) - (j * strKeySize + size - subSize * 2 + inserted);
                    if (delta >= -prevDelta)
                    {
                        if (height == 0)
                        {
                            ins.GetStr(b, bn - 1);
                        }
                        else
                        {
                            Assert.That("String fits in the B-Tree page", moved + (bn + 1) * strKeySize <= keySpace);
                            if (bn != r)
                            {
                                ins.GetStr(pg, i);
                                SetKeyStrOid(b, bn, GetKeyStrOid(pg, i));
                                size -= keyLen * 2;
                                i += 1;
                            }
                            else
                            {
                                SetKeyStrOid(b, bn, ins.oid);
                            }
                        }
                        nItems = CompactifyStrings(pg, i);
                        if (bn < r || (bn == r && height == 0))
                        {
                            MemCopy(pg, r - i + 1, pg, r - i, n - r, strKeySize);
                            size += len * 2;
                            nItems += 1;
                            Assert.That("String fits in the B-Tree page", size + (n - i + 1) * strKeySize <= keySpace);
                            SetKeyStrOffs(pg, r - i, keySpace - size);
                            SetKeyStrSize(pg, r - i, len);
                            SetKeyStrOid(pg, r - i, ins.oid);
                            SetKeyStrChars(pg, keySpace - size, sval);
                        }
                        SetItemsCount(b, bn);
                        SetSize(b, moved);
                        SetSize(pg, size);
                        SetItemsCount(pg, nItems);
                        ins.oid = pageId;
                        db.pool.Unfix(b);
                        return Btree.op_overflow;
                    }
                    moved += keyLen * 2;
                    prevDelta = delta;
                    Assert.That("String fits in the B-Tree page", moved + (bn + 1) * strKeySize <= keySpace);
                    SetKeyStrSize(b, bn, keyLen);
                    SetKeyStrOffs(b, bn, keySpace - moved);
                    if (bn == r)
                    {
                        SetKeyStrOid(b, bn, ins.oid);
                        SetKeyStrChars(b, keySpace - moved, sval);
                    }
                    else
                    {
                        SetKeyStrOid(b, bn, GetKeyStrOid(pg, i));
                        MemCopy(b, keySpace - moved, pg, GetKeyStrOffs(pg, i), keyLen * 2, 1);
                        size -= keyLen * 2;
                        i += 1;
                    }
                }
            }
            SetItemsCount(pg, nItems);
            SetSize(pg, size);
            return size + strKeySize * (nItems + 1) < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
        }

        internal static int InsertByteArrayKey(StorageImpl db, Page pg, int r, BtreeKey ins, int height)
        {
            int nItems = GetItemsCount(pg);
            int size = GetSize(pg);
            int n = (height != 0) ? nItems + 1 : nItems;
            byte[] bval = (byte[]) ins.key.oval;
            // insert before e[r]
            int len = bval.Length;
            if (size + len + (n + 1) * strKeySize <= keySpace)
            {
                MemCopy(pg, r + 1, pg, r, n - r, strKeySize);
                size += len;
                SetKeyStrOffs(pg, r, keySpace - size);
                SetKeyStrSize(pg, r, len);
                SetKeyStrOid(pg, r, ins.oid);
                SetKeyBytes(pg, keySpace - size, bval);
                nItems += 1;
            }
            else
            {
                // page is full then divide page
                int pageId = db.AllocatePage();
                Page b = db.PutPage(pageId);
                int moved = 0;
                int inserted = len + strKeySize;
                int prevDelta = (1 << 31) + 1;

                for (int bn = 0, i = 0; ; bn += 1)
                {
                    int addSize, subSize;
                    int j = nItems - i - 1;
                    int keyLen = GetKeyStrSize(pg, i);
                    if (bn == r)
                    {
                        keyLen = len;
                        inserted = 0;
                        addSize = len;
                        if (height == 0)
                        {
                            subSize = 0;
                            j += 1;
                        }
                        else
                        {
                            subSize = GetKeyStrSize(pg, i);
                        }
                    }
                    else
                    {
                        addSize = subSize = keyLen;
                        if (height != 0)
                        {
                            if (i + 1 != r)
                            {
                                subSize += GetKeyStrSize(pg, i + 1);
                                j -= 1;
                            }
                            else
                            {
                                inserted = 0;
                            }
                        }
                    }
                    int delta = (moved + addSize + (bn + 1) * strKeySize) - (j * strKeySize + size - subSize + inserted);
                    if (delta >= -prevDelta)
                    {
                        if (height == 0)
                        {
                            ins.GetByteArray(b, bn - 1);
                        }
                        else
                        {
                            Assert.That("String fits in the B-Tree page", moved + (bn + 1) * strKeySize <= keySpace);
                            if (bn != r)
                            {
                                ins.GetByteArray(pg, i);
                                SetKeyStrOid(b, bn, GetKeyStrOid(pg, i));
                                size -= keyLen;
                                i += 1;
                            }
                            else
                            {
                                SetKeyStrOid(b, bn, ins.oid);
                            }
                        }
                        nItems = CompactifyByteArrays(pg, i);
                        if (bn < r || (bn == r && height == 0))
                        {
                            MemCopy(pg, r - i + 1, pg, r - i, n - r, strKeySize);
                            size += len;
                            nItems += 1;
                            Assert.That("String fits in the B-Tree page", size + (n - i + 1) * strKeySize <= keySpace);
                            SetKeyStrOffs(pg, r - i, keySpace - size);
                            SetKeyStrSize(pg, r - i, len);
                            SetKeyStrOid(pg, r - i, ins.oid);
                            SetKeyBytes(pg, keySpace - size, bval);
                        }
                        SetItemsCount(b, bn);
                        SetSize(b, moved);
                        SetSize(pg, size);
                        SetItemsCount(pg, nItems);
                        ins.oid = pageId;
                        db.pool.Unfix(b);
                        return Btree.op_overflow;
                    }
                    moved += keyLen;
                    prevDelta = delta;
                    Assert.That("String fits in the B-Tree page", moved + (bn + 1) * strKeySize <= keySpace);
                    SetKeyStrSize(b, bn, keyLen);
                    SetKeyStrOffs(b, bn, keySpace - moved);
                    if (bn == r)
                    {
                        SetKeyStrOid(b, bn, ins.oid);
                        SetKeyBytes(b, keySpace - moved, bval);
                    }
                    else
                    {
                        SetKeyStrOid(b, bn, GetKeyStrOid(pg, i));
                        MemCopy(b, keySpace - moved, pg, GetKeyStrOffs(pg, i), keyLen, 1);
                        size -= keyLen;
                        i += 1;
                    }
                }
            }
            SetItemsCount(pg, nItems);
            SetSize(pg, size);
            return size + strKeySize * (nItems + 1) < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
        }

        internal static int CompactifyStrings(Page pg, int m)
        {
            int i, j, offs, len, n = GetItemsCount(pg);
            int[] size = new int[keySpace / 2 + 1];
            int[] index = new int[keySpace / 2 + 1];
            if (m == 0)
            {
                return n;
            }
            int nZeroLengthStrings = 0;
            if (m < 0)
            {
                m = -m;
                for (i = 0; i < n - m; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = SupportClass.URShift(GetKeyStrOffs(pg, i), 1);
                        size[offs + len] = len;
                        index[offs + len] = i;
                    }
                    else
                    {
                        nZeroLengthStrings += 1;
                    }
                }
                for (; i < n; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = SupportClass.URShift(GetKeyStrOffs(pg, i), 1);
                        size[offs + len] = len;
                        index[offs + len] = -1;
                    }
                }
            }
            else
            {
                for (i = 0; i < m; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = SupportClass.URShift(GetKeyStrOffs(pg, i), 1);
                        size[offs + len] = len;
                        index[offs + len] = -1;
                    }
                }
                for (; i < n; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = SupportClass.URShift(GetKeyStrOffs(pg, i), 1);
                        size[offs + len] = len;
                        index[offs + len] = i - m;
                    }
                    else
                    {
                        nZeroLengthStrings += 1;
                    }
                    SetKeyStrOid(pg, i - m, GetKeyStrOid(pg, i));
                    SetKeyStrSize(pg, i - m, len);
                }
                SetKeyStrOid(pg, i - m, GetKeyStrOid(pg, i));
            }
            int nItems = n -= m;
            n -= nZeroLengthStrings;
            for (offs = keySpace / 2, i = offs; n != 0; i -= len)
            {
                len = size[i];
                j = index[i];
                if (j >= 0)
                {
                    offs -= len;
                    n -= 1;
                    SetKeyStrOffs(pg, j, offs * 2);
                    if (offs != i - len)
                    {
                        MemCopy(pg, offs, pg, i - len, len, 2);
                    }
                }
            }
            return nItems;
        }

        internal static int CompactifyByteArrays(Page pg, int m)
        {
            int i, j, offs, len, n = GetItemsCount(pg);
            int[] size = new int[keySpace + 1];
            int[] index = new int[keySpace + 1];
            if (m == 0)
            {
                return n;
            }
            int nZeroLengthArrays = 0;
            if (m < 0)
            {
                m = -m;
                for (i = 0; i < n - m; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = GetKeyStrOffs(pg, i);
                        size[offs + len] = len;
                        index[offs + len] = i;
                    }
                    else
                    {
                        nZeroLengthArrays += 1;
                    }
                }
                for (; i < n; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = GetKeyStrOffs(pg, i);
                        size[offs + len] = len;
                        index[offs + len] = -1;
                    }
                }
            }
            else
            {
                for (i = 0; i < m; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = GetKeyStrOffs(pg, i);
                        size[offs + len] = len;
                        index[offs + len] = -1;
                    }
                }
                for (; i < n; i++)
                {
                    len = GetKeyStrSize(pg, i);
                    if (len != 0)
                    {
                        offs = GetKeyStrOffs(pg, i);
                        size[offs + len] = len;
                        index[offs + len] = i - m;
                    }
                    else
                    {
                        nZeroLengthArrays += 1;
                    }
                    SetKeyStrOid(pg, i - m, GetKeyStrOid(pg, i));
                    SetKeyStrSize(pg, i - m, len);
                }
                SetKeyStrOid(pg, i - m, GetKeyStrOid(pg, i));
            }
            int nItems = n -= m;
            n -= nZeroLengthArrays;
            for (offs = keySpace, i = offs; n != 0; i -= len)
            {
                len = size[i];
                j = index[i];
                if (j >= 0)
                {
                    offs -= len;
                    n -= 1;
                    SetKeyStrOffs(pg, j, offs);
                    if (offs != i - len)
                    {
                        MemCopy(pg, offs, pg, i - len, len, 1);
                    }
                }
            }
            return nItems;
        }

        internal static int RemoveStrKey(Page pg, int r)
        {
            int len = GetKeyStrSize(pg, r) * 2;
            int offs = GetKeyStrOffs(pg, r);
            int size = GetSize(pg);
            int nItems = GetItemsCount(pg);
            if ((nItems + 1) * strKeySize >= keySpace)
            {
                MemCopy(pg, r, pg, r + 1, nItems - r - 1, strKeySize);
            }
            else
            {
                MemCopy(pg, r, pg, r + 1, nItems - r, strKeySize);
            }
            if (len != 0)
            {
                MemCopy(pg, keySpace - size + len, pg, keySpace - size, size - keySpace + offs, 1);
                for (int i = nItems; --i >= 0; )
                {
                    if (GetKeyStrOffs(pg, i) < offs)
                    {
                        SetKeyStrOffs(pg, i, GetKeyStrOffs(pg, i) + len);
                    }
                }
                SetSize(pg, size -= len);
            }
            SetItemsCount(pg, nItems - 1);
            return size + strKeySize * nItems < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
        }

        internal static int RemoveByteArrayKey(Page pg, int r)
        {
            int len = GetKeyStrSize(pg, r);
            int offs = GetKeyStrOffs(pg, r);
            int size = GetSize(pg);
            int nItems = GetItemsCount(pg);
            if ((nItems + 1) * strKeySize >= keySpace)
            {
                MemCopy(pg, r, pg, r + 1, nItems - r - 1, strKeySize);
            }
            else
            {
                MemCopy(pg, r, pg, r + 1, nItems - r, strKeySize);
            }
            if (len != 0)
            {
                MemCopy(pg, keySpace - size + len, pg, keySpace - size, size - keySpace + offs, 1);
                for (int i = nItems; --i >= 0; )
                {
                    if (GetKeyStrOffs(pg, i) < offs)
                    {
                        SetKeyStrOffs(pg, i, GetKeyStrOffs(pg, i) + len);
                    }
                }
                SetSize(pg, size -= len);
            }
            SetItemsCount(pg, nItems - 1);
            return size + strKeySize * nItems < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
        }

        internal static int ReplaceStrKey(StorageImpl db, Page pg, int r, BtreeKey ins, int height)
        {
            ins.oid = GetKeyStrOid(pg, r);
            RemoveStrKey(pg, r);
            return InsertStrKey(db, pg, r, ins, height);
        }

        internal static int ReplaceByteArrayKey(StorageImpl db, Page pg, int r, BtreeKey ins, int height)
        {
            ins.oid = GetKeyStrOid(pg, r);
            RemoveByteArrayKey(pg, r);
            return InsertByteArrayKey(db, pg, r, ins, height);
        }

        internal static int HandlePageUnderflow(StorageImpl db, Page pg, int r, int type, BtreeKey rem, int height)
        {
            int nItems = GetItemsCount(pg);
            if (type == ClassDescriptor.tpString)
            {
                Page a = db.PutPage(GetKeyStrOid(pg, r));
                int an = GetItemsCount(a);
                if (r < nItems)
                {
                    // exists greater page
                    Page b = db.GetPage(GetKeyStrOid(pg, r + 1));
                    int bn = GetItemsCount(b);
                    int merged_size = (an + bn) * strKeySize + GetSize(a) + GetSize(b);
                    if (height != 1)
                    {
                        merged_size += GetKeyStrSize(pg, r) * 2 + strKeySize * 2;
                    }

                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i, j, k;
                        db.pool.Unfix(b);
                        b = db.PutPage(GetKeyStrOid(pg, r + 1));
                        int size_a = GetSize(a);
                        int size_b = GetSize(b);
                        int addSize, subSize;
                        if (height != 1)
                        {
                            addSize = GetKeyStrSize(pg, r);
                            subSize = GetKeyStrSize(b, 0);
                        }
                        else
                        {
                            addSize = subSize = GetKeyStrSize(b, 0);
                        }
                        i = 0;
                        int prevDelta = (an * strKeySize + size_a) - (bn * strKeySize + size_b);
                        while (true)
                        {
                            i += 1;
                            int delta = ((an + i) * strKeySize + size_a + addSize * 2) - ((bn - i) * strKeySize + size_b - subSize * 2);
                            if (delta >= 0)
                            {
                                if (delta >= -prevDelta)
                                {
                                    i -= 1;
                                }
                                break;
                            }
                            size_a += addSize * 2;
                            size_b -= subSize * 2;
                            prevDelta = delta;
                            if (height != 1)
                            {
                                addSize = subSize;
                                subSize = GetKeyStrSize(b, i);
                            }
                            else
                            {
                                addSize = subSize = GetKeyStrSize(b, i);
                            }
                        }
                        int result = Btree.op_done;
                        if (i > 0)
                        {
                            k = i;
                            if (height != 1)
                            {
                                int len = GetKeyStrSize(pg, r);
                                SetSize(a, GetSize(a) + len * 2);
                                SetKeyStrOffs(a, an, keySpace - GetSize(a));
                                SetKeyStrSize(a, an, len);
                                MemCopy(a, GetKeyStrOffs(a, an), pg, GetKeyStrOffs(pg, r), len * 2, 1);
                                k -= 1;
                                an += 1;
                                SetKeyStrOid(a, an + k, GetKeyStrOid(b, k));
                                SetSize(b, GetSize(b) - GetKeyStrSize(b, k) * 2);
                            }
                            for (j = 0; j < k; j++)
                            {
                                int len = GetKeyStrSize(b, j);
                                SetSize(a, GetSize(a) + len * 2);
                                SetSize(b, GetSize(b) - len * 2);
                                SetKeyStrOffs(a, an, keySpace - GetSize(a));
                                SetKeyStrSize(a, an, len);
                                SetKeyStrOid(a, an, GetKeyStrOid(b, j));
                                MemCopy(a, GetKeyStrOffs(a, an), b, GetKeyStrOffs(b, j), len * 2, 1);
                                an += 1;
                            }
                            rem.GetStr(b, i - 1);
                            result = ReplaceStrKey(db, pg, r, rem, height);
                            SetItemsCount(a, an);
                            SetItemsCount(b, CompactifyStrings(b, i));
                        }
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return result;
                    }
                    else
                    {
                        // merge page b to a
                        if (height != 1)
                        {
                            int r_len = GetKeyStrSize(pg, r);
                            SetKeyStrSize(a, an, r_len);
                            SetSize(a, GetSize(a) + r_len * 2);
                            SetKeyStrOffs(a, an, keySpace - GetSize(a));
                            MemCopy(a, GetKeyStrOffs(a, an), pg, GetKeyStrOffs(pg, r), r_len * 2, 1);
                            an += 1;
                            SetKeyStrOid(a, an + bn, GetKeyStrOid(b, bn));
                        }
                        for (int i = 0; i < bn; i++, an++)
                        {
                            SetKeyStrSize(a, an, GetKeyStrSize(b, i));
                            SetKeyStrOffs(a, an, GetKeyStrOffs(b, i) - GetSize(a));
                            SetKeyStrOid(a, an, GetKeyStrOid(b, i));
                        }
                        SetSize(a, GetSize(a) + GetSize(b));
                        SetItemsCount(a, an);
                        MemCopy(a, keySpace - GetSize(a), b, keySpace - GetSize(b), GetSize(b), 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        db.FreePage(GetKeyStrOid(pg, r + 1));
                        SetKeyStrOid(pg, r + 1, GetKeyStrOid(pg, r));
                        return RemoveStrKey(pg, r);
                    }
                }
                else
                {
                    // page b is before a
                    Page b = db.GetPage(GetKeyStrOid(pg, r - 1));
                    int bn = GetItemsCount(b);
                    int merged_size = (an + bn) * strKeySize + GetSize(a) + GetSize(b);
                    if (height != 1)
                    {
                        merged_size += GetKeyStrSize(pg, r - 1) * 2 + strKeySize * 2;
                    }
                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i, j, k, len;
                        db.pool.Unfix(b);
                        b = db.PutPage(GetKeyStrOid(pg, r - 1));
                        int size_a = GetSize(a);
                        int size_b = GetSize(b);
                        int addSize, subSize;
                        if (height != 1)
                        {
                            addSize = GetKeyStrSize(pg, r - 1);
                            subSize = GetKeyStrSize(b, bn - 1);
                        }
                        else
                        {
                            addSize = subSize = GetKeyStrSize(b, bn - 1);
                        }
                        i = 0;
                        int prevDelta = (an * strKeySize + size_a) - (bn * strKeySize + size_b);
                        while (true)
                        {
                            i += 1;
                            int delta = ((an + i) * strKeySize + size_a + addSize * 2) - ((bn - i) * strKeySize + size_b - subSize * 2);
                            if (delta >= 0)
                            {
                                if (delta >= -prevDelta)
                                {
                                    i -= 1;
                                }
                                break;
                            }
                            prevDelta = delta;
                            size_a += addSize * 2;
                            size_b -= subSize * 2;
                            if (height != 1)
                            {
                                addSize = subSize;
                                subSize = GetKeyStrSize(b, bn - i - 1);
                            }
                            else
                            {
                                addSize = subSize = GetKeyStrSize(b, bn - i - 1);
                            }
                        }
                        int result = Btree.op_done;
                        if (i > 0)
                        {
                            k = i;
                            Assert.That(i < bn);
                            if (height != 1)
                            {
                                SetSize(b, GetSize(b) - GetKeyStrSize(b, bn - k) * 2);
                                MemCopy(a, i, a, 0, an + 1, strKeySize);
                                k -= 1;
                                SetKeyStrOid(a, k, GetKeyStrOid(b, bn));
                                len = GetKeyStrSize(pg, r - 1);
                                SetKeyStrSize(a, k, len);
                                SetSize(a, GetSize(a) + len * 2);
                                SetKeyStrOffs(a, k, keySpace - GetSize(a));
                                MemCopy(a, GetKeyStrOffs(a, k), pg, GetKeyStrOffs(pg, r - 1), len * 2, 1);
                            }
                            else
                            {
                                MemCopy(a, i, a, 0, an, strKeySize);
                            }
                            for (j = 0; j < k; j++)
                            {
                                len = GetKeyStrSize(b, bn - k + j);
                                SetSize(a, GetSize(a) + len * 2);
                                SetSize(b, GetSize(b) - len * 2);
                                SetKeyStrOffs(a, j, keySpace - GetSize(a));
                                SetKeyStrSize(a, j, len);
                                SetKeyStrOid(a, j, GetKeyStrOid(b, bn - k + j));
                                MemCopy(a, GetKeyStrOffs(a, j), b, GetKeyStrOffs(b, bn - k + j), len * 2, 1);
                            }
                            an += i;
                            SetItemsCount(a, an);
                            rem.GetStr(b, bn - k - 1);
                            result = ReplaceStrKey(db, pg, r - 1, rem, height);
                            SetItemsCount(b, CompactifyStrings(b, -i));
                        }
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return result;
                    }
                    else
                    {
                        // merge page b to a
                        if (height != 1)
                        {
                            MemCopy(a, bn + 1, a, 0, an + 1, strKeySize);
                            int len = GetKeyStrSize(pg, r - 1);
                            SetKeyStrSize(a, bn, len);
                            SetSize(a, GetSize(a) + len * 2);
                            SetKeyStrOffs(a, bn, keySpace - GetSize(a));
                            SetKeyStrOid(a, bn, GetKeyStrOid(b, bn));
                            MemCopy(a, GetKeyStrOffs(a, bn), pg, GetKeyStrOffs(pg, r - 1), len * 2, 1);
                            an += 1;
                        }
                        else
                        {
                            MemCopy(a, bn, a, 0, an, strKeySize);
                        }
                        for (int i = 0; i < bn; i++)
                        {
                            SetKeyStrOid(a, i, GetKeyStrOid(b, i));
                            SetKeyStrSize(a, i, GetKeyStrSize(b, i));
                            SetKeyStrOffs(a, i, GetKeyStrOffs(b, i) - GetSize(a));
                        }
                        an += bn;
                        SetItemsCount(a, an);
                        SetSize(a, GetSize(a) + GetSize(b));
                        MemCopy(a, keySpace - GetSize(a), b, keySpace - GetSize(b), GetSize(b), 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        db.FreePage(GetKeyStrOid(pg, r - 1));
                        return RemoveStrKey(pg, r - 1);
                    }
                }
            }
            else if (type == ClassDescriptor.tpArrayOfByte)
            {
                Page a = db.PutPage(GetKeyStrOid(pg, r));
                int an = GetItemsCount(a);
                if (r < nItems)
                {
                    // exists greater page
                    Page b = db.GetPage(GetKeyStrOid(pg, r + 1));
                    int bn = GetItemsCount(b);
                    int merged_size = (an + bn) * strKeySize + GetSize(a) + GetSize(b);
                    if (height != 1)
                    {
                        merged_size += GetKeyStrSize(pg, r) + strKeySize * 2;
                    }
                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i, j, k;
                        db.pool.Unfix(b);
                        b = db.PutPage(GetKeyStrOid(pg, r + 1));
                        int size_a = GetSize(a);
                        int size_b = GetSize(b);
                        int addSize, subSize;
                        if (height != 1)
                        {
                            addSize = GetKeyStrSize(pg, r);
                            subSize = GetKeyStrSize(b, 0);
                        }
                        else
                        {
                            addSize = subSize = GetKeyStrSize(b, 0);
                        }
                        i = 0;
                        int prevDelta = (an * strKeySize + size_a) - (bn * strKeySize + size_b);
                        while (true)
                        {
                            i += 1;
                            int delta = ((an + i) * strKeySize + size_a + addSize) - ((bn - i) * strKeySize + size_b - subSize);
                            if (delta >= 0)
                            {
                                if (delta >= -prevDelta)
                                {
                                    i -= 1;
                                }
                                break;
                            }
                            size_a += addSize;
                            size_b -= subSize;
                            prevDelta = delta;
                            if (height != 1)
                            {
                                addSize = subSize;
                                subSize = GetKeyStrSize(b, i);
                            }
                            else
                            {
                                addSize = subSize = GetKeyStrSize(b, i);
                            }
                        }
                        int result = Btree.op_done;
                        if (i > 0)
                        {
                            k = i;
                            if (height != 1)
                            {
                                int len = GetKeyStrSize(pg, r);
                                SetSize(a, GetSize(a) + len);
                                SetKeyStrOffs(a, an, keySpace - GetSize(a));
                                SetKeyStrSize(a, an, len);
                                MemCopy(a, GetKeyStrOffs(a, an), pg, GetKeyStrOffs(pg, r), len, 1);
                                k -= 1;
                                an += 1;
                                SetKeyStrOid(a, an + k, GetKeyStrOid(b, k));
                                SetSize(b, GetSize(b) - GetKeyStrSize(b, k));
                            }
                            for (j = 0; j < k; j++)
                            {
                                int len = GetKeyStrSize(b, j);
                                SetSize(a, GetSize(a) + len);
                                SetSize(b, GetSize(b) - len);
                                SetKeyStrOffs(a, an, keySpace - GetSize(a));
                                SetKeyStrSize(a, an, len);
                                SetKeyStrOid(a, an, GetKeyStrOid(b, j));
                                MemCopy(a, GetKeyStrOffs(a, an), b, GetKeyStrOffs(b, j), len, 1);
                                an += 1;
                            }
                            rem.GetByteArray(b, i - 1);
                            result = ReplaceByteArrayKey(db, pg, r, rem, height);
                            SetItemsCount(a, an);
                            SetItemsCount(b, CompactifyByteArrays(b, i));
                        }
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return result;
                    }
                    else
                    {
                        // merge page b to a
                        if (height != 1)
                        {
                            int r_len = GetKeyStrSize(pg, r);
                            SetKeyStrSize(a, an, r_len);
                            SetSize(a, GetSize(a) + r_len);
                            SetKeyStrOffs(a, an, keySpace - GetSize(a));
                            MemCopy(a, GetKeyStrOffs(a, an), pg, GetKeyStrOffs(pg, r), r_len, 1);
                            an += 1;
                            SetKeyStrOid(a, an + bn, GetKeyStrOid(b, bn));
                        }
                        for (int i = 0; i < bn; i++, an++)
                        {
                            SetKeyStrSize(a, an, GetKeyStrSize(b, i));
                            SetKeyStrOffs(a, an, GetKeyStrOffs(b, i) - GetSize(a));
                            SetKeyStrOid(a, an, GetKeyStrOid(b, i));
                        }
                        SetSize(a, GetSize(a) + GetSize(b));
                        SetItemsCount(a, an);
                        MemCopy(a, keySpace - GetSize(a), b, keySpace - GetSize(b), GetSize(b), 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        db.FreePage(GetKeyStrOid(pg, r + 1));
                        SetKeyStrOid(pg, r + 1, GetKeyStrOid(pg, r));
                        return RemoveByteArrayKey(pg, r);
                    }
                }
                else
                {
                    // page b is before a
                    Page b = db.GetPage(GetKeyStrOid(pg, r - 1));
                    int bn = GetItemsCount(b);
                    int merged_size = (an + bn) * strKeySize + GetSize(a) + GetSize(b);
                    if (height != 1)
                    {
                        merged_size += GetKeyStrSize(pg, r - 1) + strKeySize * 2;
                    }
                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i, j, k, len;
                        db.pool.Unfix(b);
                        b = db.PutPage(GetKeyStrOid(pg, r - 1));
                        int size_a = GetSize(a);
                        int size_b = GetSize(b);
                        int addSize, subSize;
                        if (height != 1)
                        {
                            addSize = GetKeyStrSize(pg, r - 1);
                            subSize = GetKeyStrSize(b, bn - 1);
                        }
                        else
                        {
                            addSize = subSize = GetKeyStrSize(b, bn - 1);
                        }
                        i = 0;
                        int prevDelta = (an * strKeySize + size_a) - (bn * strKeySize + size_b);
                        while (true)
                        {
                            i += 1;
                            int delta = ((an + i) * strKeySize + size_a + addSize) - ((bn - i) * strKeySize + size_b - subSize);
                            if (delta >= 0)
                            {
                                if (delta >= -prevDelta)
                                {
                                    i -= 1;
                                }
                                break;
                            }
                            prevDelta = delta;
                            size_a += addSize;
                            size_b -= subSize;
                            if (height != 1)
                            {
                                addSize = subSize;
                                subSize = GetKeyStrSize(b, bn - i - 1);
                            }
                            else
                            {
                                addSize = subSize = GetKeyStrSize(b, bn - i - 1);
                            }
                        }
                        int result = Btree.op_done;
                        if (i > 0)
                        {
                            k = i;
                            Assert.That(i < bn);
                            if (height != 1)
                            {
                                SetSize(b, GetSize(b) - GetKeyStrSize(b, bn - k));
                                MemCopy(a, i, a, 0, an + 1, strKeySize);
                                k -= 1;
                                SetKeyStrOid(a, k, GetKeyStrOid(b, bn));
                                len = GetKeyStrSize(pg, r - 1);
                                SetKeyStrSize(a, k, len);
                                SetSize(a, GetSize(a) + len);
                                SetKeyStrOffs(a, k, keySpace - GetSize(a));
                                MemCopy(a, GetKeyStrOffs(a, k), pg, GetKeyStrOffs(pg, r - 1), len, 1);
                            }
                            else
                            {
                                MemCopy(a, i, a, 0, an, strKeySize);
                            }
                            for (j = 0; j < k; j++)
                            {
                                len = GetKeyStrSize(b, bn - k + j);
                                SetSize(a, GetSize(a) + len);
                                SetSize(b, GetSize(b) - len);
                                SetKeyStrOffs(a, j, keySpace - GetSize(a));
                                SetKeyStrSize(a, j, len);
                                SetKeyStrOid(a, j, GetKeyStrOid(b, bn - k + j));
                                MemCopy(a, GetKeyStrOffs(a, j), b, GetKeyStrOffs(b, bn - k + j), len, 1);
                            }
                            an += i;
                            SetItemsCount(a, an);
                            rem.GetByteArray(b, bn - k - 1);
                            result = ReplaceByteArrayKey(db, pg, r - 1, rem, height);
                            SetItemsCount(b, CompactifyByteArrays(b, -i));
                        }
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return result;
                    }
                    else
                    {
                        // merge page b to a
                        if (height != 1)
                        {
                            MemCopy(a, bn + 1, a, 0, an + 1, strKeySize);
                            int len = GetKeyStrSize(pg, r - 1);
                            SetKeyStrSize(a, bn, len);
                            SetSize(a, GetSize(a) + len);
                            SetKeyStrOffs(a, bn, keySpace - GetSize(a));
                            SetKeyStrOid(a, bn, GetKeyStrOid(b, bn));
                            MemCopy(a, GetKeyStrOffs(a, bn), pg, GetKeyStrOffs(pg, r - 1), len, 1);
                            an += 1;
                        }
                        else
                        {
                            MemCopy(a, bn, a, 0, an, strKeySize);
                        }
                        for (int i = 0; i < bn; i++)
                        {
                            SetKeyStrOid(a, i, GetKeyStrOid(b, i));
                            SetKeyStrSize(a, i, GetKeyStrSize(b, i));
                            SetKeyStrOffs(a, i, GetKeyStrOffs(b, i) - GetSize(a));
                        }
                        an += bn;
                        SetItemsCount(a, an);
                        SetSize(a, GetSize(a) + GetSize(b));
                        MemCopy(a, keySpace - GetSize(a), b, keySpace - GetSize(b), GetSize(b), 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        db.FreePage(GetKeyStrOid(pg, r - 1));
                        return RemoveByteArrayKey(pg, r - 1);
                    }
                }
            }
            else
            {
                // scalar types
                Page a = db.PutPage(GetReference(pg, maxItems - r - 1));
                int an = GetItemsCount(a);
                int itemSize = ClassDescriptor.Sizeof[type];
                if (r < nItems)
                {
                    // exists greater page
                    Page b = db.GetPage(GetReference(pg, maxItems - r - 2));
                    int bn = GetItemsCount(b);
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        MemCopy(a, an, pg, r, 1, itemSize);
                        an += 1;
                        bn += 1;
                    }
                    int merged_size = (an + bn) * (4 + itemSize);
                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        db.pool.Unfix(b);
                        b = db.PutPage(GetReference(pg, maxItems - r - 2));
                        MemCopy(a, an, b, 0, i, itemSize);
                        MemCopy(b, 0, b, i, bn - i, itemSize);
                        MemCopy(a, maxItems - an - i, b, maxItems - i, i, 4);
                        MemCopy(b, maxItems - bn + i, b, maxItems - bn, bn - i, 4);
                        MemCopy(pg, r, a, an + i - 1, 1, itemSize);
                        SetItemsCount(b, GetItemsCount(b) - i);
                        SetItemsCount(a, GetItemsCount(a) + i);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return Btree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, an, b, 0, bn, itemSize);
                        MemCopy(a, maxItems - an - bn, b, maxItems - bn, bn, 4);
                        db.FreePage(GetReference(pg, maxItems - r - 2));
                        MemCopy(pg, maxItems - nItems, pg, maxItems - nItems - 1, nItems - r - 1, 4);
                        MemCopy(pg, r, pg, r + 1, nItems - r - 1, itemSize);
                        SetItemsCount(a, GetItemsCount(a) + bn);
                        SetItemsCount(pg, nItems - 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return nItems * (itemSize + 4) < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
                    }
                }
                else
                {
                    // page b is before a
                    Page b = db.GetPage(GetReference(pg, maxItems - r));
                    int bn = GetItemsCount(b);
                    Assert.That(bn >= an);
                    if (height != 1)
                    {
                        an += 1;
                        bn += 1;
                    }
                    int merged_size = (an + bn) * (4 + itemSize);
                    if (merged_size > keySpace)
                    {
                        // reallocation of nodes between pages a and b
                        int i = bn - ((an + bn) >> 1);
                        db.pool.Unfix(b);
                        b = db.PutPage(GetReference(pg, maxItems - r));
                        MemCopy(a, i, a, 0, an, itemSize);
                        MemCopy(a, 0, b, bn - i, i, itemSize);
                        MemCopy(a, maxItems - an - i, a, maxItems - an, an, 4);
                        MemCopy(a, maxItems - i, b, maxItems - bn, i, 4);
                        if (height != 1)
                        {
                            MemCopy(a, i - 1, pg, r - 1, 1, itemSize);
                        }
                        MemCopy(pg, r - 1, b, bn - i - 1, 1, itemSize);
                        SetItemsCount(b, GetItemsCount(b) - i);
                        SetItemsCount(a, GetItemsCount(a) + i);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return Btree.op_done;
                    }
                    else
                    {
                        // merge page b to a
                        MemCopy(a, bn, a, 0, an, itemSize);
                        MemCopy(a, 0, b, 0, bn, itemSize);
                        MemCopy(a, maxItems - an - bn, a, maxItems - an, an, 4);
                        MemCopy(a, maxItems - bn, b, maxItems - bn, bn, 4);
                        if (height != 1)
                        {
                            MemCopy(a, bn - 1, pg, r - 1, 1, itemSize);
                        }
                        db.FreePage(GetReference(pg, maxItems - r));
                        SetReference(pg, maxItems - r, GetReference(pg, maxItems - r - 1));
                        SetItemsCount(a, GetItemsCount(a) + bn);
                        SetItemsCount(pg, nItems - 1);
                        db.pool.Unfix(a);
                        db.pool.Unfix(b);
                        return nItems * (itemSize + 4) < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
                    }
                }
            }
        }

        internal static int Remove(StorageImpl db, int pageId, Btree tree, BtreeKey rem, int height)
        {
            Page pg = db.GetPage(pageId);
            try
            {
                int i, n = GetItemsCount(pg), l = 0, r = n;

                if (tree.type == ClassDescriptor.tpString)
                {
                    while (l < r)
                    {
                        i = (l + r) >> 1;
                        if (CompareStr(rem.key, pg, i) > 0)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }
                    if (--height != 0)
                    {
                        do
                        {
                            switch (Remove(db, GetKeyStrOid(pg, r), tree, rem, height))
                            {
                                case Btree.op_underflow:
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return HandlePageUnderflow(db, pg, r, tree.type, rem, height);

                                case Btree.op_done:
                                    return Btree.op_done;

                                case Btree.op_overflow:
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return InsertStrKey(db, pg, r, rem, height);
                                }
                        }
                        while (++r <= n);
                    }
                    else
                    {
                        while (r < n)
                        {
                            if (CompareStr(rem.key, pg, r) == 0)
                            {
                                int oid = GetKeyStrOid(pg, r);
                                if (oid == rem.oid || rem.oid == 0)
                                {
                                    rem.oldOid = oid;
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return RemoveStrKey(pg, r);
                                }
                            }
                            else
                            {
                                break;
                            }
                            r += 1;
                        }
                    }
                }
                else if (tree.type == ClassDescriptor.tpArrayOfByte)
                {
                    while (l < r)
                    {
                        i = (l + r) >> 1;
                        if (tree.CompareByteArrays(rem.key, pg, i) > 0)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }
                    if (--height != 0)
                    {
                        do
                        {
                            switch (Remove(db, GetKeyStrOid(pg, r), tree, rem, height))
                            {
                                case Btree.op_underflow:
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return HandlePageUnderflow(db, pg, r, tree.type, rem, height);

                                case Btree.op_done:
                                    return Btree.op_done;

                                case Btree.op_overflow:
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return InsertByteArrayKey(db, pg, r, rem, height);
                                }
                        }
                        while (++r <= n);
                    }
                    else
                    {
                        while (r < n)
                        {
                            if (tree.CompareByteArrays(rem.key, pg, r) == 0)
                            {
                                int oid = GetKeyStrOid(pg, r);
                                if (oid == rem.oid || rem.oid == 0)
                                {
                                    rem.oldOid = oid;
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    return RemoveByteArrayKey(pg, r);
                                }
                            }
                            else
                            {
                                break;
                            }
                            r += 1;
                        }
                    }
                }
                else
                {
                    // scalar types
                    int itemSize = ClassDescriptor.Sizeof[tree.type];
                    while (l < r)
                    {
                        i = (l + r) >> 1;
                        if (Compare(rem.key, pg, i) > 0)
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
                        int oid = rem.oid;
                        while (r < n)
                        {
                            if (Compare(rem.key, pg, r) == 0)
                            {
                                if (GetReference(pg, maxItems - r - 1) == oid || oid == 0)
                                {
                                    rem.oldOid = GetReference(pg, maxItems - r - 1);
                                    db.pool.Unfix(pg);
                                    pg = null;
                                    pg = db.PutPage(pageId);
                                    MemCopy(pg, r, pg, r + 1, n - r - 1, itemSize);
                                    MemCopy(pg, maxItems - n + 1, pg, maxItems - n, n - r - 1, 4);
                                    SetItemsCount(pg, --n);
                                    return n * (itemSize + 4) < keySpace / 2 ? Btree.op_underflow : Btree.op_done;
                                }
                            }
                            else
                            {
                                break;
                            }
                            r += 1;
                        }
                        return Btree.op_not_found;
                    }
                    do
                    {
                        switch (Remove(db, GetReference(pg, maxItems - r - 1), tree, rem, height))
                        {
                            case Btree.op_underflow:
                                db.pool.Unfix(pg);
                                pg = null;
                                pg = db.PutPage(pageId);
                                return HandlePageUnderflow(db, pg, r, tree.type, rem, height);

                            case Btree.op_done:
                                return Btree.op_done;
                            }
                    }
                    while (++r <= n);
                }
                return Btree.op_not_found;
            }
            finally
            {
                if (pg != null)
                {
                    db.pool.Unfix(pg);
                }
            }
        }

        internal static void Purge(StorageImpl db, int pageId, int type, int height)
        {
            if (--height != 0)
            {
                Page pg = db.GetPage(pageId);
                int n = GetItemsCount(pg) + 1;
                if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                {
                    // page of strings
                    while (--n >= 0)
                    {
                        Purge(db, GetKeyStrOid(pg, n), type, height);
                    }
                }
                else
                {
                    while (--n >= 0)
                    {
                        Purge(db, GetReference(pg, maxItems - n - 1), type, height);
                    }
                }
                db.pool.Unfix(pg);
            }
            db.FreePage(pageId);
        }

        internal static int TraverseForward(StorageImpl db, int pageId, int type, int height, IPersistent[] result, int pos)
        {
            Page pg = db.GetPage(pageId);
            int oid;
            try
            {
                int i, n = GetItemsCount(pg);
                if (--height != 0)
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i <= n; i++)
                        {
                            pos = TraverseForward(db, GetKeyStrOid(pg, i), type, height, result, pos);
                        }
                    }
                    else
                    {
                        for (i = 0; i <= n; i++)
                        {
                            pos = TraverseForward(db, GetReference(pg, maxItems - i - 1), type, height, result, pos);
                        }
                    }
                }
                else
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i < n; i++)
                        {
                            oid = GetKeyStrOid(pg, i);
                            result[pos++] = db.LookupObject(oid, null);
                        }
                    }
                    else
                    {
                        // page of scalars
                        for (i = 0; i < n; i++)
                        {
                            oid = GetReference(pg, maxItems - 1 - i);
                            result[pos++] = db.LookupObject(oid, null);
                        }
                    }
                }
                return pos;
            }
            finally
            {
                db.pool.Unfix(pg);
            }
        }

        internal static int MarkPage(StorageImpl db, int pageId, int type, int height)
        {
            int nPages = 1;
            Page pg = db.GetGCPage(pageId);
            try
            {
                int i, n = GetItemsCount(pg);
                if (--height != 0)
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i <= n; i++)
                        {
                            nPages += MarkPage(db, GetKeyStrOid(pg, i), type, height);
                        }
                    }
                    else
                    {
                        for (i = 0; i <= n; i++)
                        {
                            nPages += MarkPage(db, GetReference(pg, maxItems - i - 1), type, height);
                        }
                    }
                }
                else
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i < n; i++)
                        {
                            db.MarkOid(GetKeyStrOid(pg, i));
                        }
                    }
                    else
                    {
                        // page of scalars
                        for (i = 0; i < n; i++)
                        {
                            db.MarkOid(GetReference(pg, maxItems - 1 - i));
                        }
                    }
                }
            }
            finally
            {
                db.pool.Unfix(pg);
            }
            return nPages;
        }

#if !OMIT_XML
        internal static void ExportPage(StorageImpl db, XMLExporter exporter, int pageId, int type, int height)
        {
            Page pg = db.GetPage(pageId);
            try
            {
                int i, n = GetItemsCount(pg);
                if (--height != 0)
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i <= n; i++)
                        {
                            ExportPage(db, exporter, GetKeyStrOid(pg, i), type, height);
                        }
                    }
                    else
                    {
                        for (i = 0; i <= n; i++)
                        {
                            ExportPage(db, exporter, GetReference(pg, maxItems - i - 1), type, height);
                        }
                    }
                }
                else
                {
                    if (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte)
                    {
                        // page of strings
                        for (i = 0; i < n; i++)
                        {
                            exporter.ExportAssoc(GetKeyStrOid(pg, i), pg.data, BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, i), BtreePage.GetKeyStrSize(pg, i), type);
                        }
                    }
                    else
                    {
                        for (i = 0; i < n; i++)
                        {
                            exporter.ExportAssoc(GetReference(pg, maxItems - 1 - i), pg.data, BtreePage.firstKeyOffs + i * ClassDescriptor.Sizeof[type], ClassDescriptor.Sizeof[type], type);
                        }
                    }
                }
            }
            finally
            {
                db.pool.Unfix(pg);
            }
        }
#endif
    }
}

