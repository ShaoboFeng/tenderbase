namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    class BtreeMultiFieldIndex : Btree, FieldIndex
    {
        public virtual Type IndexedClass
        {
            get
            {
                return cls;
            }
        }

        public virtual FieldInfo[] KeyFields
        {
            get
            {
                return fld;
            }
        }

        internal string className;
        internal string[] fieldName;
        internal int[] types;

        [NonSerialized]
        internal Type cls;
        [NonSerialized]
        internal FieldInfo[] fld;

        internal BtreeMultiFieldIndex()
        {
        }

        internal BtreeMultiFieldIndex(Type cls, string[] fieldName, bool unique)
        {
            this.cls = cls;
            this.unique = unique;
            this.fieldName = fieldName;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
            this.className = cls.FullName;
            LocateFields();
            type = ClassDescriptor.tpArrayOfByte;
            types = new int[fieldName.Length];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = CheckType(fld[i].FieldType);
            }
        }

        private void LocateFields()
        {
            fld = new FieldInfo[fieldName.Length];
            for (int i = 0; i < fieldName.Length; i++)
            {
                Type scope = cls;
                try
                {
                    do
                    {
                        try
                        {
                            //UPGRADE_TODO: The differences in the expected value of parameters for method 'java.lang.Class.getDeclaredField' may cause compilation errors.
                            fld[i] = scope.GetField(fieldName[i], BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
                            //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                            //fld[i].setAccessible(true);
                            break;
                        }
                        catch (System.FieldAccessException)
                        {
                            scope = scope.BaseType;
                        }
                    }
                    while (scope != null);
                }
                catch (System.Exception x)
                {
                    throw new StorageError(StorageError.ACCESS_VIOLATION, className + "." + fieldName[i], x);
                }
                if (fld[i] == null)
                {
                    throw new StorageError(StorageError.INDEXED_FIELD_NOT_FOUND, className + "." + fieldName[i]);
                }
            }
        }

        public override void OnLoad()
        {
            cls = ClassDescriptor.LoadClass(Storage, className);
            LocateFields();
        }

        internal override int CompareByteArrays(byte[] key, byte[] item, int offs, int lengtn)
        {
            int o1 = 0;
            int o2 = offs;
            byte[] a1 = key;
            byte[] a2 = item;
            for (int i = 0; i < fld.Length && o1 < key.Length; i++)
            {
                int diff = 0;
                switch (types[i])
                {
                    case ClassDescriptor.tpBoolean:
                    case ClassDescriptor.tpByte:
                        diff = a1[o1++] - a2[o2++];
                        break;

                    case ClassDescriptor.tpShort:
                        diff = Bytes.Unpack2(a1, o1) - Bytes.Unpack2(a2, o2);
                        o1 += 2;
                        o2 += 2;
                        break;

                    case ClassDescriptor.tpChar:
                        diff = (char) Bytes.Unpack2(a1, o1) - (char) Bytes.Unpack2(a2, o2);
                        o1 += 2;
                        o2 += 2;
                        break;

                    case ClassDescriptor.tpInt:
                    case ClassDescriptor.tpObject:
                    {
                        int i1 = Bytes.Unpack4(a1, o1);
                        int i2 = Bytes.Unpack4(a2, o2);
                        diff = i1 < i2 ? -1 : (i1 == i2 ? 0 : 1);
                        o1 += 4;
                        o2 += 4;
                        break;
                    }

                    case ClassDescriptor.tpLong:
                    case ClassDescriptor.tpDate:
                    {
                        long l1 = Bytes.Unpack8(a1, o1);
                        long l2 = Bytes.Unpack8(a2, o2);
                        diff = l1 < l2 ? -1 : (l1 == l2 ? 0 : 1);
                        o1 += 8;
                        o2 += 8;
                        break;
                    }

                    case ClassDescriptor.tpFloat:
                    {
                        float f1 = Bytes.UnpackF4(a1, o1);
                        float f2 = Bytes.UnpackF4(a2, o2);
                        diff = f1 < f2 ? -1 : (f1 == f2 ? 0 : 1);
                        o1 += 4;
                        o2 += 4;
                        break;
                    }

                    case ClassDescriptor.tpDouble:
                    {
                        double d1 = Bytes.UnpackF8(a1, o1);
                        double d2 = Bytes.UnpackF8(a2, o2);
                        diff = d1 < d2 ? -1 : (d1 == d2 ? 0 : 1);
                        o1 += 8;
                        o2 += 8;
                        break;
                    }

                    case ClassDescriptor.tpString:
                    {
                        int len1 = Bytes.Unpack4(a1, o1);
                        int len2 = Bytes.Unpack4(a2, o2);
                        o1 += 4;
                        o2 += 4;
                        int len = len1 < len2 ? len1 : len2;
                        while (--len >= 0)
                        {
                            diff = (char) Bytes.Unpack2(a1, o1) - (char) Bytes.Unpack2(a2, o2);
                            if (diff != 0)
                            {
                                return diff;
                            }
                            o1 += 2;
                            o2 += 2;
                        }
                        diff = len1 - len2;
                        break;
                    }

                    case ClassDescriptor.tpArrayOfByte:
                    {
                        int len1 = Bytes.Unpack4(a1, o1);
                        int len2 = Bytes.Unpack4(a2, o2);
                        o1 += 4;
                        o2 += 4;
                        int len = len1 < len2 ? len1 : len2;
                        while (--len >= 0)
                        {
                            diff = a1[o1++] - a2[o2++];
                            if (diff != 0)
                            {
                                return diff;
                            }
                        }
                        diff = len1 - len2;
                        break;
                    }

                    default:
                        Assert.Failed("Invalid type");
                        break;
                }
                if (diff != 0)
                {
                    return diff;
                }
            }
            return 0;
        }

        internal override object UnpackByteArrayKey(Page pg, int pos)
        {
            int offs = BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, pos);
            byte[] data = pg.data;
            object[] values = new object[fld.Length];

            for (int i = 0; i < fld.Length; i++)
            {
                object v = null;
                switch (types[i])
                {
                    case ClassDescriptor.tpBoolean:
                        v = Convert.ToBoolean(data[offs++]);
                        break;

                    case ClassDescriptor.tpByte:
                        v = (byte) data[offs++];
                        break;

                    case ClassDescriptor.tpShort:
                        v = (short) Bytes.Unpack2(data, offs);
                        offs += 2;
                        break;

                    case ClassDescriptor.tpChar:
                        v = (char) Bytes.Unpack2(data, offs);
                        offs += 2;
                        break;

                    case ClassDescriptor.tpInt:
                        v = (Int32) Bytes.Unpack4(data, offs);
                        offs += 4;
                        break;

                    case ClassDescriptor.tpObject:
                    {
                        int oid = Bytes.Unpack4(data, offs);
                        v = oid == 0 ? null : ((StorageImpl) Storage).LookupObject(oid, null);
                        offs += 4;
                        break;
                    }

                    case ClassDescriptor.tpLong:
                        v = (long) Bytes.Unpack8(data, offs);
                        offs += 8;
                        break;

                    case ClassDescriptor.tpDate:
                    {
                        long msec = Bytes.Unpack8(data, offs);
                        //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                        //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                        if (msec == -1)
                            v = new DateTime();
                        else
                            v = new DateTime(msec);
                        offs += 8;
                        break;
                    }

                    case ClassDescriptor.tpFloat:
                        v = Bytes.UnpackF4(data, offs);
                        offs += 4;
                        break;

                    case ClassDescriptor.tpDouble:
                        v = Bytes.UnpackF8(data, offs);
                        offs += 8;
                        break;

                    case ClassDescriptor.tpString:
                    {
                        int len = Bytes.Unpack4(data, offs);
                        offs += 4;
                        char[] sval = new char[len];
                        for (int j = 0; j < len; j++)
                        {
                            sval[j] = (char) Bytes.Unpack2(data, offs);
                            offs += 2;
                        }
                        v = new string(sval);
                        break;
                    }

                    case ClassDescriptor.tpArrayOfByte:
                    {
                        int len = Bytes.Unpack4(data, offs);
                        offs += 4;
                        byte[] bval = new byte[len];
                        Array.Copy(data, offs, bval, 0, len);
                        offs += len;
                        break;
                    }

                    default:
                        Assert.Failed("Invalid type");
                        break;
                }
                values[i] = v;
            }
            return values;
        }

        private Key ExtractKey(IPersistent obj)
        {
            try
            {
                ByteBuffer buf = new ByteBuffer();
                int dst = 0;
                for (int i = 0; i < fld.Length; i++)
                {
                    FieldInfo f = (FieldInfo) fld[i];
                    switch (types[i])
                    {
                        case ClassDescriptor.tpBoolean:
                            buf.Extend(dst + 1);
                            buf.arr[dst++] = (byte) ((bool) f.GetValue(obj) ? 1 : 0);
                            break;

                        case ClassDescriptor.tpByte:
                            buf.Extend(dst + 1);
                            buf.arr[dst++] = (byte) f.GetValue(obj);
                            break;

                        case ClassDescriptor.tpShort:
                            buf.Extend(dst + 2);
                            Bytes.Pack2(buf.arr, dst, (short) f.GetValue(obj));
                            dst += 2;
                            break;

                        case ClassDescriptor.tpChar:
                            buf.Extend(dst + 2);
                            Bytes.Pack2(buf.arr, dst, (short) f.GetValue(obj));
                            dst += 2;
                            break;

                        case ClassDescriptor.tpInt:
                            buf.Extend(dst + 4);
                            Bytes.Pack4(buf.arr, dst, (int) f.GetValue(obj));
                            dst += 4;
                            break;

                        case ClassDescriptor.tpObject:
                        {
                            IPersistent p = (IPersistent) f.GetValue(obj);
                            int oid = p == null ? 0 : p.Oid;
                            buf.Extend(dst + 4);
                            Bytes.Pack4(buf.arr, dst, oid);
                            dst += 4;
                            break;
                        }

                        case ClassDescriptor.tpLong:
                            buf.Extend(dst + 8);
                            Bytes.Pack8(buf.arr, dst, (long) f.GetValue(obj));
                            dst += 8;
                            break;

                        case ClassDescriptor.tpDate:
                        {
                            DateTime d = (DateTime) f.GetValue(obj);
                            buf.Extend(dst + 8);
                            //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                            //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                            Bytes.Pack8(buf.arr, dst, d == null ? -1 : d.Ticks);
                            dst += 8;
                            break;
                        }

                        case ClassDescriptor.tpFloat:
                            buf.Extend(dst + 4);
                            Bytes.PackF4(buf.arr, dst, (float) f.GetValue(obj));
                            dst += 4;
                            break;

                        case ClassDescriptor.tpDouble:
                            buf.Extend(dst + 8);
                            Bytes.PackF8(buf.arr, dst, (double) f.GetValue(obj));
                            dst += 8;
                            break;

                        case ClassDescriptor.tpString:
                        {
                            buf.Extend(dst + 4);
                            string str = (string) f.GetValue(obj);
                            if (str != null)
                            {
                                int len = str.Length;
                                Bytes.Pack4(buf.arr, dst, len);
                                dst += 4;
                                buf.Extend(dst + len * 2);
                                for (int j = 0; j < len; j++)
                                {
                                    Bytes.Pack2(buf.arr, dst, (short) str[j]);
                                    dst += 2;
                                }
                            }
                            else
                            {
                                Bytes.Pack4(buf.arr, dst, 0);
                                dst += 4;
                            }
                            break;
                        }

                        case ClassDescriptor.tpArrayOfByte:
                        {
                            buf.Extend(dst + 4);
                            byte[] arr = (byte[]) f.GetValue(obj);
                            if (arr != null)
                            {
                                int len = arr.Length;
                                Bytes.Pack4(buf.arr, dst, len);
                                dst += 4;
                                buf.Extend(dst + len);
                                Array.Copy(arr, 0, buf.arr, dst, len);
                                dst += len;
                            }
                            else
                            {
                                Bytes.Pack4(buf.arr, dst, 0);
                                dst += 4;
                            }
                            break;
                        }

                        default:
                            Assert.Failed("Invalid type");
                            break;
                    }
                }
                return new Key(buf.ToArray());
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.ACCESS_VIOLATION, x);
            }
        }

        private Key ConvertKey(Key key)
        {
            if (key == null)
            {
                return null;
            }

            if (key.type != ClassDescriptor.tpArrayOfObject)
            {
                throw new StorageError(StorageError.INCOMPATIBLE_KEY_TYPE);
            }

            object[] values = (object[]) key.oval;
            ByteBuffer buf = new ByteBuffer();
            int dst = 0;
            for (int i = 0; i < values.Length; i++)
            {
                object v = values[i];
                switch (types[i])
                {
                    case ClassDescriptor.tpBoolean:
                        buf.Extend(dst + 1);
                        buf.arr[dst++] = (byte) (((bool) v) ? 1 : 0);
                        break;

                    case ClassDescriptor.tpByte:
                        buf.Extend(dst + 1);
                        buf.arr[dst++] = (byte) Convert.ToSByte(((System.ValueType) v));
                        break;

                    case ClassDescriptor.tpShort:
                        buf.Extend(dst + 2);
                        Bytes.Pack2(buf.arr, dst, Convert.ToInt16(((System.ValueType) v)));
                        dst += 2;
                        break;

                    case ClassDescriptor.tpChar:
                        buf.Extend(dst + 2);
                        Bytes.Pack2(buf.arr, dst, (v is System.ValueType) ? Convert.ToInt16(((System.ValueType) v)) : (short) ((System.Char) v));
                        dst += 2;
                        break;

                    case ClassDescriptor.tpInt:
                        buf.Extend(dst + 4);
                        Bytes.Pack4(buf.arr, dst, Convert.ToInt32(((System.ValueType) v)));
                        dst += 4;
                        break;

                    case ClassDescriptor.tpObject:
                        buf.Extend(dst + 4);
                        Bytes.Pack4(buf.arr, dst, v == null ? 0 : ((IPersistent) v).Oid);
                        dst += 4;
                        break;

                    case ClassDescriptor.tpLong:
                        buf.Extend(dst + 8);
                        Bytes.Pack8(buf.arr, dst, Convert.ToInt64(((System.ValueType) v)));
                        dst += 8;
                        break;

                    case ClassDescriptor.tpDate:
                        buf.Extend(dst + 8);
                        //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                        Bytes.Pack8(buf.arr, dst, v == null ? -1 : ((DateTime) v).Ticks);
                        dst += 8;
                        break;

                    case ClassDescriptor.tpFloat:
                        buf.Extend(dst + 4);
                        float f = (float)v;
                        Bytes.PackF4(buf.arr, dst, f);
                        dst += 4;
                        break;

                    case ClassDescriptor.tpDouble:
                        buf.Extend(dst + 8);
                        double d = (double)v; // TODOPORT: Convert.ToDouble(((System.ValueType) v) ?
                        Bytes.PackF8(buf.arr, dst, d);
                        dst += 8;
                        break;

                    case ClassDescriptor.tpString:
                    {
                        buf.Extend(dst + 4);
                        if (v != null)
                        {
                            string str = (string) v;
                            int len = str.Length;
                            Bytes.Pack4(buf.arr, dst, len);
                            dst += 4;
                            buf.Extend(dst + len * 2);
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack2(buf.arr, dst, (short) str[j]);
                                dst += 2;
                            }
                        }
                        else
                        {
                            Bytes.Pack4(buf.arr, dst, 0);
                            dst += 4;
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfByte:
                    {
                        buf.Extend(dst + 4);
                        if (v != null)
                        {
                            byte[] arr = (byte[]) v;
                            int len = arr.Length;
                            Bytes.Pack4(buf.arr, dst, len);
                            dst += 4;
                            buf.Extend(dst + len);
                            Array.Copy(arr, 0, buf.arr, dst, len);
                            dst += len;
                        }
                        else
                        {
                            Bytes.Pack4(buf.arr, dst, 0);
                            dst += 4;
                        }
                        break;
                    }

                    default:
                        Assert.Failed("Invalid type");
                        break;
                }
            }
            return new Key(buf.ToArray(), key.inclusion != 0);
        }

        public virtual bool Put(IPersistent obj)
        {
            return base.Put(ExtractKey(obj), obj);
        }

        public virtual IPersistent Set(IPersistent obj)
        {
            return base.Set(ExtractKey(obj), obj);
        }

        public virtual void Remove(IPersistent obj)
        {
            base.Remove(new BtreeKey(ExtractKey(obj), obj.Oid));
        }

        public override IPersistent Remove(Key key)
        {
            return base.Remove(ConvertKey(key));
        }

        public virtual bool Contains(IPersistent obj)
        {
            Key key = ExtractKey(obj);
            if (unique)
            {
                return base.Get(key) != null;
            }
            else
            {
                IPersistent[] mbrs = Get(key, key);
                for (int i = 0; i < mbrs.Length; i++)
                {
                    if (mbrs[i] == obj)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public virtual void Append(IPersistent obj)
        {
            throw new StorageError(StorageError.UNSUPPORTED_INDEX_TYPE);
        }

        public override IPersistent[] Get(Key from, Key till)
        {
            ArrayList list = new ArrayList();
            if (root != 0)
            {
                BtreePage.Find((StorageImpl) Storage, root, ConvertKey(from), ConvertKey(till), this, height, list);
            }
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, (object[]) System.Array.CreateInstance(cls, list.Count));
        }

        public override IPersistent[] ToPersistentArray()
        {
            IPersistent[] arr = (IPersistent[]) System.Array.CreateInstance(cls, nElems);
            if (root != 0)
            {
                BtreePage.TraverseForward((StorageImpl) Storage, root, type, height, arr, 0);
            }
            return arr;
        }

        public override IPersistent Get(Key key)
        {
            return base.Get(ConvertKey(key));
        }

        public override IEnumerator GetEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return base.GetEnumerator(ConvertKey(from), ConvertKey(till), order);
        }

        public override IEnumerator GetEntryEnumerator(Key from, Key till, IndexSortOrder order)
        {
            return base.GetEntryEnumerator(ConvertKey(from), ConvertKey(till), order);
        }
    }
}

