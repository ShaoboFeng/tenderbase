namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    class AltBtreeMultiFieldIndex : AltBtree, FieldIndex
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

        [NonSerialized]
        internal Type cls;
        [NonSerialized]
        internal FieldInfo[] fld;

        internal AltBtreeMultiFieldIndex()
        {
        }

        internal AltBtreeMultiFieldIndex(Type cls, string[] fieldName, bool unique)
        {
            this.cls = cls;
            this.unique = unique;
            this.fieldName = fieldName;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
            this.className = cls.FullName;
            LocateFields();
            type = ClassDescriptor.tpRaw;
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

        [Serializable]
        internal class CompoundKey : System.IComparable
        {
            internal object[] keys;

            public virtual int CompareTo(object o)
            {
                CompoundKey c = (CompoundKey) o;
                int n = keys.Length < c.keys.Length ? keys.Length : c.keys.Length;
                for (int i = 0; i < n; i++)
                {
                    int diff = ((System.IComparable) keys[i]).CompareTo(c.keys[i]);
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                return keys.Length - c.keys.Length;
            }

            internal CompoundKey(object[] keys)
            {
                this.keys = keys;
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

            return new Key(new CompoundKey((object[]) key.oval), key.inclusion != 0);
        }

        private Key ExtractKey(IPersistent obj)
        {
            object[] keys = new object[fld.Length];
            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i] = fld[i].GetValue(obj);
                }
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.ACCESS_VIOLATION, x);
            }

            return new Key(new CompoundKey(keys));
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
            base.Remove(new BtreeKey(ExtractKey(obj), obj));
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
            if (root != null)
            {
                root.Find(ConvertKey(from), ConvertKey(till), height, list);
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

