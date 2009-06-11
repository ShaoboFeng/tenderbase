namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    class AltBtreeFieldIndex : AltBtree, FieldIndex
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
                return new FieldInfo[] { fld };
            }
        }

        internal string className;
        internal string fieldName;
        internal long autoincCount;
        [NonSerialized]
        internal Type cls;
        [NonSerialized]
        internal FieldInfo fld;

        internal AltBtreeFieldIndex()
        {
        }

        private void LocateField()
        {
            Type scope = cls;
            try
            {
                do
                {
                    try
                    {
                        //UPGRADE_TODO: The differences in the expected value of parameters for method 'java.lang.Class.getDeclaredField' may cause compilation errors.
                        fld = scope.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
                        //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                        //fld.setAccessible(true);
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
                throw new StorageError(StorageError.ACCESS_VIOLATION, className + "." + fieldName, x);
            }

            if (fld == null)
                throw new StorageError(StorageError.INDEXED_FIELD_NOT_FOUND, className + "." + fieldName);
        }

        public override void OnLoad()
        {
            cls = ClassDescriptor.LoadClass(Storage, className);
            LocateField();
        }

        internal AltBtreeFieldIndex(Type cls, string fieldName, bool unique)
        {
            this.cls = cls;
            this.unique = unique;
            this.fieldName = fieldName;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
            this.className = cls.FullName;
            LocateField();
            type = CheckType(fld.FieldType);
        }

        private Key ExtractKey(IPersistent obj)
        {
            try
            {
                FieldInfo f = fld;
                Key key = null;
                switch (type)
                {
                    case ClassDescriptor.tpBoolean:
                        key = new Key((bool) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpByte:
                        key = new Key((byte) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpShort:
                        key = new Key((short) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpChar:
                        key = new Key((char) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpInt:
                        key = new Key((int) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpObject:
                        key = new Key((IPersistent) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpLong:
                        key = new Key((long) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpDate:
                        //UPGRADE_NOTE: ref keyword was added to struct-type parameters.
                        key = new Key(ref new DateTime[] { (DateTime) f.GetValue(obj) } [0]);
                        break;

                    case ClassDescriptor.tpFloat:
                        key = new Key((float) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpDouble:
                        key = new Key((double) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpString:
                        key = new Key((string) f.GetValue(obj));
                        break;

                    case ClassDescriptor.tpRaw:
                        key = new Key((System.IComparable) f.GetValue(obj));
                        break;

                    default:
                        Assert.Failed("Invalid type");
                        break;
                }

                return key;
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.ACCESS_VIOLATION, x);
            }
        }

        public virtual bool Put(IPersistent obj)
        {
            return base.Insert(ExtractKey(obj), obj, false) == null;
        }

        public virtual IPersistent Set(IPersistent obj)
        {
            return base.Set(ExtractKey(obj), obj);
        }

        public virtual void Remove(IPersistent obj)
        {
            base.Remove(new BtreeKey(ExtractKey(obj), obj));
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
            lock (this)
            {
                Key key;
                try
                {
                    switch (type)
                    {
                        case ClassDescriptor.tpInt:
                            key = new Key((int) autoincCount);
                            fld.SetValue(obj, (int) autoincCount);
                            break;

                        case ClassDescriptor.tpLong:
                            key = new Key(autoincCount);
                            fld.SetValue(obj, autoincCount);
                            break;

                        default:
                            throw new StorageError(StorageError.UNSUPPORTED_INDEX_TYPE, fld.FieldType);
                    }
                }
                catch (System.Exception x)
                {
                    throw new StorageError(StorageError.ACCESS_VIOLATION, x);
                }

                autoincCount += 1;
                obj.Modify();
                base.Insert(key, obj, false);
            }
        }

        public override IPersistent[] Get(Key from, Key till)
        {
            ArrayList list = new ArrayList();
            if (root != null)
            {
                root.Find(CheckKey(from), CheckKey(till), height, list);
            }

            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(list, (object[]) System.Array.CreateInstance(cls, list.Count));
        }

        public override IPersistent[] ToPersistentArray()
        {
            IPersistent[] arr = (IPersistent[]) System.Array.CreateInstance(cls, nElems);
            if (root != null)
            {
                root.TraverseForward(height, arr, 0);
            }

            return arr;
        }
    }
}

