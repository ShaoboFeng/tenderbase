namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    class Btree : PersistentResource, Index
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

                    case ClassDescriptor.tpArrayOfByte:
                        return typeof(byte[]);

                    default:
                        return null;
                }
            }
        }

        internal int root;
        internal int height;
        internal int type;
        internal int nElems;
        internal bool unique;

        [NonSerialized]
        internal int updateCounter;

        internal const int Sizeof = ObjectHeader.Sizeof + 4 * 4 + 1;

        internal Btree()
        {
        }

        internal static int CheckType(Type c)
        {
            int elemType = ClassDescriptor.GetTypeCode(c);
            if (elemType > ClassDescriptor.tpObject && elemType != ClassDescriptor.tpArrayOfByte)
                throw new StorageError(StorageError.UNSUPPORTED_INDEX_TYPE, c);
            return elemType;
        }

        internal virtual int CompareByteArrays(byte[] key, byte[] item, int offs, int length)
        {
            int n = key.Length >= length ? length : key.Length;
            for (int i = 0; i < n; i++)
            {
                int diff = key[i] - item[i + offs];
                if (diff != 0)
                    return diff;
            }
            return key.Length - length;
        }

        internal Btree(Type cls, bool unique)
        {
            this.unique = unique;
            type = CheckType(cls);
        }

        internal Btree(int type, bool unique)
        {
            this.type = type;
            this.unique = unique;
        }

        internal Btree(byte[] obj, int offs)
        {
            root = Bytes.Unpack4(obj, offs);
            offs += 4;
            height = Bytes.Unpack4(obj, offs);
            offs += 4;
            type = Bytes.Unpack4(obj, offs);
            offs += 4;
            nElems = Bytes.Unpack4(obj, offs);
            offs += 4;
            unique = obj[offs] != 0;
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
                    throw new StorageError(StorageError.INCOMPATIBLE_KEY_TYPE);

                if (key.oval is string)
                    key = new Key(((string) key.oval).ToCharArray(), key.inclusion != 0);
            }
            return key;
        }

        public virtual IPersistent Get(Key key)
        {
            key = CheckKey(key);
            if (root != 0)
            {
                ArrayList list = new ArrayList();
                BtreePage.Find((StorageImpl) Storage, root, key, key, this, height, list);
                if (list.Count > 1)
                    throw new StorageError(StorageError.KEY_NOT_UNIQUE);
                else if (list.Count == 0)
                    return null;
                else
                    return (IPersistent) list[0];
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

            if (root != 0)
            {
                ArrayList list = new ArrayList();
                BtreePage.PrefixSearch((StorageImpl) Storage, root, key.ToCharArray(), height, list);
                if (list.Count != 0)
                {
                    return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
                }
            }
            return emptySelection;
        }

        public virtual IPersistent[] Get(Key from, Key till)
        {
            if (root != 0)
            {
                ArrayList list = new ArrayList();
                BtreePage.Find((StorageImpl) Storage, root, CheckKey(from), CheckKey(till), this, height, list);
                if (list.Count != 0)
                {
                    return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, new IPersistent[list.Count]);
                }
            }
            return emptySelection;
        }

        public virtual bool Put(Key key, IPersistent obj)
        {
            return Insert(key, obj, false) >= 0;
        }

        public virtual IPersistent Set(Key key, IPersistent obj)
        {
            int oid = Insert(key, obj, true);
            return (oid != 0) ? ((StorageImpl) Storage).LookupObject(oid, null) : null;
        }

        internal int Insert(Key key, IPersistent obj, bool overwrite)
        {
            StorageImpl db = (StorageImpl) Storage;
            if (db == null)
            {
                throw new StorageError(StorageError.DELETED_OBJECT);
            }

            key = CheckKey(key);
            if (!obj.IsPersistent())
            {
                db.MakePersistent(obj);
            }

            BtreeKey ins = new BtreeKey(key, obj.Oid);
            if (root == 0)
            {
                root = BtreePage.Allocate(db, 0, type, ins);
                height = 1;
            }
            else
            {
                int result = BtreePage.Insert(db, root, this, ins, height, unique, overwrite);
                if (result == op_overflow)
                {
                    root = BtreePage.Allocate(db, root, type, ins);
                    height += 1;
                }
                else if (result == op_duplicate)
                {
                    return -1;
                }
                else if (result == op_overwrite)
                {
                    return ins.oldOid;
                }
            }
            updateCounter += 1;
            nElems += 1;
            Modify();
            return 0;
        }

        public virtual void Remove(Key key, IPersistent obj)
        {
            Remove(new BtreeKey(CheckKey(key), obj.Oid));
        }

        internal virtual void Remove(BtreeKey rem)
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

            int result = BtreePage.Remove(db, root, this, rem, height);
            if (result == op_not_found)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND);
            }

            nElems -= 1;
            if (result == op_underflow)
            {
                Page pg = db.GetPage(root);
                if (BtreePage.GetItemsCount(pg) == 0)
                {
                    int newRoot = 0;
                    if (height != 1)
                    {
                        newRoot = (type == ClassDescriptor.tpString || type == ClassDescriptor.tpArrayOfByte) ? BtreePage.GetKeyStrOid(pg, 0) : BtreePage.GetReference(pg, BtreePage.maxItems - 1);
                    }
                    db.FreePage(root);
                    root = newRoot;
                    height -= 1;
                }
                db.pool.Unfix(pg);
            }
            else if (result == op_overflow)
            {
                root = BtreePage.Allocate(db, root, type, rem);
                height += 1;
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
            BtreeKey rk = new BtreeKey(CheckKey(key), 0);
            StorageImpl db = (StorageImpl) Storage;
            Remove(rk);
            return db.LookupObject(rk.oldOid, null);
        }

        public virtual IPersistent Get(string key)
        {
            return Get(new Key(key.ToCharArray(), true));
        }

        public virtual IPersistent[] GetPrefix(string prefix)
        {
            return Get(new Key(prefix.ToCharArray(), true), new Key((prefix + System.Char.MaxValue).ToCharArray(), false));
        }

        public virtual bool Put(string key, IPersistent obj)
        {
            return Put(new Key(key.ToCharArray(), true), obj);
        }

        public virtual IPersistent Set(string key, IPersistent obj)
        {
            return Set(new Key(key.ToCharArray(), true), obj);
        }

        public virtual void Remove(string key, IPersistent obj)
        {
            Remove(new Key(key.ToCharArray(), true), obj);
        }

        public virtual IPersistent Remove(string key)
        {
            return Remove(new Key(key.ToCharArray(), true));
        }

        public virtual int Size()
        {
            return nElems;
        }

        public virtual void Clear()
        {
            if (root != 0)
            {
                BtreePage.Purge((StorageImpl) Storage, root, type, height);
                root = 0;
                nElems = 0;
                height = 0;
                updateCounter += 1;
                Modify();
            }
        }

        public virtual IPersistent[] ToPersistentArray()
        {
            IPersistent[] arr = new IPersistent[nElems];
            if (root != 0)
            {
                BtreePage.TraverseForward((StorageImpl) Storage, root, type, height, arr, 0);
            }
            return arr;
        }

        public virtual IPersistent[] ToPersistentArray(IPersistent[] arr)
        {
            if (arr.Length < nElems)
            {
                arr = (IPersistent[]) System.Array.CreateInstance(arr.GetType().GetElementType(), nElems);
            }
            if (root != 0)
            {
                BtreePage.TraverseForward((StorageImpl) Storage, root, type, height, arr, 0);
            }
            if (arr.Length > nElems)
            {
                arr[nElems] = null;
            }
            return arr;
        }

        public override void Deallocate()
        {
            if (root != 0)
            {
                BtreePage.Purge((StorageImpl) Storage, root, type, height);
            }
            base.Deallocate();
        }

        public virtual int MarkTree()
        {
            if (root != 0)
            {
                return BtreePage.MarkPage((StorageImpl) Storage, root, type, height);
            }
            return 0;
        }

#if !OMIT_XML
        public virtual void Export(XMLExporter exporter)
        {
            if (root != 0)
            {
                BtreePage.ExportPage((StorageImpl) Storage, exporter, root, type, height);
            }
        }
#endif

        internal struct BtreeEntry
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
                    return db.LookupObject(oid, null);
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

            internal BtreeEntry(StorageImpl db, object key, int oid)
            {
                this.db = db;
                this.key = key;
                this.oid = oid;
            }

            private object key;
            private StorageImpl db;
            private int oid;

            //UPGRADE_NOTE: The following method implementation was automatically added to preserve functionality.
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        internal virtual object UnpackKey(StorageImpl db, Page pg, int pos)
        {
            byte[] data = pg.data;
            int offs = BtreePage.firstKeyOffs + pos * ClassDescriptor.Sizeof[type];
            switch (type)
            {
                case ClassDescriptor.tpBoolean:
                    return Convert.ToBoolean(data[offs]);

                case ClassDescriptor.tpByte:
                    return (byte) data[offs];

                case ClassDescriptor.tpShort:
                    return (short) Bytes.Unpack2(data, offs);

                case ClassDescriptor.tpChar:
                    return (char) Bytes.Unpack2(data, offs);

                case ClassDescriptor.tpInt:
                    return (Int32) Bytes.Unpack4(data, offs);

                case ClassDescriptor.tpObject:
                    return db.LookupObject(Bytes.Unpack4(data, offs), null);

                case ClassDescriptor.tpLong:
                    return (long) Bytes.Unpack8(data, offs);

                case ClassDescriptor.tpDate:
                    //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                    return new DateTime(Bytes.Unpack8(data, offs));

                case ClassDescriptor.tpFloat:
                    return Bytes.UnpackF4(data, offs);

                case ClassDescriptor.tpDouble:
                    return Bytes.UnpackF8(data, offs);

                case ClassDescriptor.tpString:
                    return UnpackStrKey(pg, pos);

                case ClassDescriptor.tpArrayOfByte:
                    return UnpackByteArrayKey(pg, pos);

                default:
                    Assert.Failed("Invalid type");
                    break;
            }
            return null;
        }

        internal static string UnpackStrKey(Page pg, int pos)
        {
            int len = BtreePage.GetKeyStrSize(pg, pos);
            int offs = BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, pos);
            byte[] data = pg.data;
            char[] sval = new char[len];
            for (int j = 0; j < len; j++)
            {
                sval[j] = (char) Bytes.Unpack2(data, offs);
                offs += 2;
            }
            return new string(sval);
        }

        internal virtual object UnpackByteArrayKey(Page pg, int pos)
        {
            int len = BtreePage.GetKeyStrSize(pg, pos);
            int offs = BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, pos);
            byte[] val = new byte[len];
            Array.Copy(pg.data, offs, val, 0, len);
            return val;
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeIterator' to access its enclosing instance.
        internal class BtreeIterator : IEnumerator
        {
            private void InitBlock(Btree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private Btree enclosingInstance;

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
                    object curr = GetCurrent(pg, pos);
                    if (++pos == end)
                    {
                        while (--sp != 0)
                        {
                            db.pool.Unfix(pg);
                            pos = posStack[sp - 1];
                            pg = db.GetPage(pageStack[sp - 1]);
                            if (++pos <= BtreePage.GetItemsCount(pg))
                            {
                                posStack[sp - 1] = pos;
                                do
                                {
                                    int pageId = GetReference(pg, pos);
                                    db.pool.Unfix(pg);
                                    pg = db.GetPage(pageId);
                                    end = BtreePage.GetItemsCount(pg);
                                    pageStack[sp] = pageId;
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
                    db.pool.Unfix(pg);
                    return curr;
                }
            }

            public Btree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeIterator(Btree enclosingInstance)
            {
                InitBlock(enclosingInstance);
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                if (db == null)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }
                int pageId = Enclosing_Instance.root;
                int h = Enclosing_Instance.height;
                counter = Enclosing_Instance.updateCounter;
                pageStack = new int[h];
                posStack = new int[h];
                sp = 0;
                while (--h >= 0)
                {
                    posStack[sp] = 0;
                    pageStack[sp] = pageId;
                    Page pg = db.GetPage(pageId);
                    pageId = GetReference(pg, 0);
                    end = BtreePage.GetItemsCount(pg);
                    db.pool.Unfix(pg);
                    sp += 1;
                }
            }

            protected internal virtual int GetReference(Page pg, int pos)
            {
                return (Enclosing_Instance.type == ClassDescriptor.tpString || Enclosing_Instance.type == ClassDescriptor.tpArrayOfByte) ? BtreePage.GetKeyStrOid(pg, pos) : BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos);
            }

            public virtual bool MoveNext()
            {
                if (counter != Enclosing_Instance.updateCounter)
                {
                    throw new System.Exception();
                }
                return sp > 0 && posStack[sp - 1] < end;
            }

            protected internal virtual object GetCurrent(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                return db.LookupObject(GetReference(pg, pos), null);
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            internal int[] pageStack;
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
            public BtreeEntryIterator(Btree enclosingInstance)
                : base(enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }

            private void InitBlock(Btree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private Btree enclosingInstance;

            public new Btree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            protected internal override object GetCurrent(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                if (db == null)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }

                switch (Enclosing_Instance.type)
                {
                    case ClassDescriptor.tpString:
                        return new BtreeEntry(db, TenderBaseImpl.Btree.UnpackStrKey(pg, pos), BtreePage.GetKeyStrOid(pg, pos));

                    case ClassDescriptor.tpArrayOfByte:
                        return new BtreeEntry(db, Enclosing_Instance.UnpackByteArrayKey(pg, pos), BtreePage.GetKeyStrOid(pg, pos));

                    default:
                        return new BtreeEntry(db, Enclosing_Instance.UnpackKey(db, pg, pos), BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos));
                }
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

        internal int CompareByteArrays(Key key, Page pg, int i)
        {
            return CompareByteArrays((byte[]) key.oval, pg.data, BtreePage.GetKeyStrOffs(pg, i) + BtreePage.firstKeyOffs, BtreePage.GetKeyStrSize(pg, i));
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'BtreeSelectionIterator' to access its enclosing instance.
        internal class BtreeSelectionIterator : IEnumerator
        {
            private void InitBlock(Btree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private Btree enclosingInstance;

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
                    object curr = GetCurrent(pg, pos);
                    GotoNextItem(pg, pos);
                    return curr;
                }
            }

            public Btree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeSelectionIterator(Btree enclosingInstance, Key from, Key till, IndexSortOrder order)
            {
                InitBlock(enclosingInstance);
                int i, l, r;

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
                this.from = from;
                this.till = till;
                this.order = order;

                pageStack = new int[h];
                posStack = new int[h];

                if (Enclosing_Instance.type == ClassDescriptor.tpString)
                {
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (from == null)
                        {
                            while (--h >= 0)
                            {
                                posStack[sp] = 0;
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                pageId = BtreePage.GetKeyStrOid(pg, 0);
                                end = BtreePage.GetItemsCount(pg);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (BtreePage.CompareStr(from, pg, i) >= from.inclusion)
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
                                pageId = BtreePage.GetKeyStrOid(pg, r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg2 = db.GetPage(pageId);
                            l = 0;
                            end = r = BtreePage.GetItemsCount(pg2);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (BtreePage.CompareStr(from, pg2, i) >= from.inclusion)
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
                                GotoNextItem(pg2, r - 1);
                            }
                            else
                            {
                                posStack[sp++] = r;
                                db.pool.Unfix(pg2);
                            }
                        }
                        if (sp != 0 && till != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (-BtreePage.CompareStr(till, pg, posStack[sp - 1]) >= till.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
                        }
                    }
                    else
                    {
                        // descent order
                        if (till == null)
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                posStack[sp] = BtreePage.GetItemsCount(pg);
                                pageId = BtreePage.GetKeyStrOid(pg, posStack[sp]);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg3 = db.GetPage(pageId);
                            posStack[sp++] = BtreePage.GetItemsCount(pg3) - 1;
                            db.pool.Unfix(pg3);
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (BtreePage.CompareStr(till, pg, i) >= 1 - till.inclusion)
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
                                pageId = BtreePage.GetKeyStrOid(pg, r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg4 = db.GetPage(pageId);
                            l = 0;
                            r = BtreePage.GetItemsCount(pg4);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (BtreePage.CompareStr(till, pg4, i) >= 1 - till.inclusion)
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
                                GotoNextItem(pg4, r);
                            }
                            else
                            {
                                posStack[sp++] = r - 1;
                                db.pool.Unfix(pg4);
                            }
                        }
                        if (sp != 0 && from != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (BtreePage.CompareStr(from, pg, posStack[sp - 1]) >= from.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
                        }
                    }
                }
                else if (Enclosing_Instance.type == ClassDescriptor.tpArrayOfByte)
                {
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (from == null)
                        {
                            while (--h >= 0)
                            {
                                posStack[sp] = 0;
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                pageId = BtreePage.GetKeyStrOid(pg, 0);
                                end = BtreePage.GetItemsCount(pg);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (Enclosing_Instance.CompareByteArrays(from, pg, i) >= from.inclusion)
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
                                pageId = BtreePage.GetKeyStrOid(pg, r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg5 = db.GetPage(pageId);
                            l = 0;
                            end = r = BtreePage.GetItemsCount(pg5);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (Enclosing_Instance.CompareByteArrays(from, pg5, i) >= from.inclusion)
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
                                GotoNextItem(pg5, r - 1);
                            }
                            else
                            {
                                posStack[sp++] = r;
                                db.pool.Unfix(pg5);
                            }
                        }
                        if (sp != 0 && till != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (-Enclosing_Instance.CompareByteArrays(till, pg, posStack[sp - 1]) >= till.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
                        }
                    }
                    else
                    {
                        // descent order
                        if (till == null)
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                posStack[sp] = BtreePage.GetItemsCount(pg);
                                pageId = BtreePage.GetKeyStrOid(pg, posStack[sp]);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg6 = db.GetPage(pageId);
                            posStack[sp++] = BtreePage.GetItemsCount(pg6) - 1;
                            db.pool.Unfix(pg6);
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (Enclosing_Instance.CompareByteArrays(till, pg, i) >= 1 - till.inclusion)
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
                                pageId = BtreePage.GetKeyStrOid(pg, r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg7 = db.GetPage(pageId);
                            l = 0;
                            r = BtreePage.GetItemsCount(pg7);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (Enclosing_Instance.CompareByteArrays(till, pg7, i) >= 1 - till.inclusion)
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
                                GotoNextItem(pg7, r);
                            }
                            else
                            {
                                posStack[sp++] = r - 1;
                                db.pool.Unfix(pg7);
                            }
                        }
                        if (sp != 0 && from != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (Enclosing_Instance.CompareByteArrays(from, pg, posStack[sp - 1]) >= from.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
                        }
                    }
                }
                else
                {
                    // scalar type
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (from == null)
                        {
                            while (--h >= 0)
                            {
                                posStack[sp] = 0;
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1);
                                end = BtreePage.GetItemsCount(pg);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (BtreePage.Compare(from, pg, i) >= from.inclusion)
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
                                pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1 - r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg8 = db.GetPage(pageId);
                            l = 0;
                            r = end = BtreePage.GetItemsCount(pg8);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (BtreePage.Compare(from, pg8, i) >= from.inclusion)
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
                                GotoNextItem(pg8, r - 1);
                            }
                            else
                            {
                                posStack[sp++] = r;
                                db.pool.Unfix(pg8);
                            }
                        }
                        if (sp != 0 && till != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (-BtreePage.Compare(till, pg, posStack[sp - 1]) >= till.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
                        }
                    }
                    else
                    {
                        // descent order
                        if (till == null)
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                posStack[sp] = BtreePage.GetItemsCount(pg);
                                pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1 - posStack[sp]);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg9 = db.GetPage(pageId);
                            posStack[sp++] = BtreePage.GetItemsCount(pg9) - 1;
                            db.pool.Unfix(pg9);
                        }
                        else
                        {
                            while (--h > 0)
                            {
                                pageStack[sp] = pageId;
                                Page pg = db.GetPage(pageId);
                                l = 0;
                                r = BtreePage.GetItemsCount(pg);
                                while (l < r)
                                {
                                    i = (l + r) >> 1;
                                    if (BtreePage.Compare(till, pg, i) >= 1 - till.inclusion)
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
                                pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1 - r);
                                db.pool.Unfix(pg);
                                sp += 1;
                            }
                            pageStack[sp] = pageId;
                            Page pg10 = db.GetPage(pageId);
                            l = 0;
                            r = BtreePage.GetItemsCount(pg10);
                            while (l < r)
                            {
                                i = (l + r) >> 1;
                                if (BtreePage.Compare(till, pg10, i) >= 1 - till.inclusion)
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
                                GotoNextItem(pg10, r);
                            }
                            else
                            {
                                posStack[sp++] = r - 1;
                                db.pool.Unfix(pg10);
                            }
                        }
                        if (sp != 0 && from != null)
                        {
                            Page pg = db.GetPage(pageStack[sp - 1]);
                            if (BtreePage.Compare(from, pg, posStack[sp - 1]) >= from.inclusion)
                            {
                                sp = 0;
                            }
                            db.pool.Unfix(pg);
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

            protected internal virtual object GetCurrent(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                return db.LookupObject((Enclosing_Instance.type == ClassDescriptor.tpString || Enclosing_Instance.type == ClassDescriptor.tpArrayOfByte) ? BtreePage.GetKeyStrOid(pg, pos) : BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos), null);
            }

            protected internal void GotoNextItem(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                if (Enclosing_Instance.type == ClassDescriptor.tpString)
                {
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (++pos == end)
                        {
                            while (--sp != 0)
                            {
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (++pos <= BtreePage.GetItemsCount(pg))
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetKeyStrOid(pg, pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        end = BtreePage.GetItemsCount(pg);
                                        pageStack[sp] = pageId;
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
                        if (sp != 0 && till != null && -BtreePage.CompareStr(till, pg, pos) >= till.inclusion)
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
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (--pos >= 0)
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetKeyStrOid(pg, pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        pageStack[sp] = pageId;
                                        posStack[sp] = pos = BtreePage.GetItemsCount(pg);
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
                        if (sp != 0 && from != null && BtreePage.CompareStr(from, pg, pos) >= from.inclusion)
                        {
                            sp = 0;
                        }
                    }
                }
                else if (Enclosing_Instance.type == ClassDescriptor.tpArrayOfByte)
                {
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (++pos == end)
                        {
                            while (--sp != 0)
                            {
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (++pos <= BtreePage.GetItemsCount(pg))
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetKeyStrOid(pg, pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        end = BtreePage.GetItemsCount(pg);
                                        pageStack[sp] = pageId;
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
                        if (sp != 0 && till != null && -Enclosing_Instance.CompareByteArrays(till, pg, pos) >= till.inclusion)
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
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (--pos >= 0)
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetKeyStrOid(pg, pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        pageStack[sp] = pageId;
                                        posStack[sp] = pos = BtreePage.GetItemsCount(pg);
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
                        if (sp != 0 && from != null && Enclosing_Instance.CompareByteArrays(from, pg, pos) >= from.inclusion)
                        {
                            sp = 0;
                        }
                    }
                }
                else
                {
                    // scalar type
                    if (order == TenderBase.IndexSortOrder.Ascent)
                    {
                        if (++pos == end)
                        {
                            while (--sp != 0)
                            {
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (++pos <= BtreePage.GetItemsCount(pg))
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        end = BtreePage.GetItemsCount(pg);
                                        pageStack[sp] = pageId;
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
                        if (sp != 0 && till != null && -BtreePage.Compare(till, pg, pos) >= till.inclusion)
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
                                db.pool.Unfix(pg);
                                pos = posStack[sp - 1];
                                pg = db.GetPage(pageStack[sp - 1]);
                                if (--pos >= 0)
                                {
                                    posStack[sp - 1] = pos;
                                    do
                                    {
                                        int pageId = BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos);
                                        db.pool.Unfix(pg);
                                        pg = db.GetPage(pageId);
                                        pageStack[sp] = pageId;
                                        posStack[sp] = pos = BtreePage.GetItemsCount(pg);
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
                        if (sp != 0 && from != null && BtreePage.Compare(from, pg, pos) >= from.inclusion)
                        {
                            sp = 0;
                        }
                    }
                }
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
        internal class BtreeSelectionEntryIterator
            : BtreeSelectionIterator
        {
            private void InitBlock(Btree enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private Btree enclosingInstance;

            public new Btree Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal BtreeSelectionEntryIterator(Btree enclosingInstance, Key from, Key till, IndexSortOrder order)
                : base(enclosingInstance, from, till, order)
            {
                InitBlock(enclosingInstance);
            }

            protected internal override object GetCurrent(Page pg, int pos)
            {
                StorageImpl db = (StorageImpl) Enclosing_Instance.Storage;
                switch (Enclosing_Instance.type)
                {
                    case ClassDescriptor.tpString:
                        return new BtreeEntry(db, TenderBaseImpl.Btree.UnpackStrKey(pg, pos), BtreePage.GetKeyStrOid(pg, pos));

                    case ClassDescriptor.tpArrayOfByte:
                        return new BtreeEntry(db, Enclosing_Instance.UnpackByteArrayKey(pg, pos), BtreePage.GetKeyStrOid(pg, pos));

                    default:
                        return new BtreeEntry(db, Enclosing_Instance.UnpackKey(db, pg, pos), BtreePage.GetReference(pg, BtreePage.maxItems - 1 - pos));
                }
            }
        }

        public virtual IEnumerator GetEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new BtreeSelectionIterator(this, CheckKey(from), CheckKey(till), order);
        }

        public virtual IEnumerator PrefixIterator(string prefix)
        {
            return GetEnumerator(new Key(prefix.ToCharArray()), new Key((prefix + System.Char.MaxValue).ToCharArray(), false), TenderBase.IndexSortOrder.Ascent);
        }

        public virtual IEnumerator GetEntryEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return new BtreeSelectionEntryIterator(this, CheckKey(from), CheckKey(till), order);
        }
    }
}

