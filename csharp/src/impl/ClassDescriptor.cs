namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    public sealed class ClassDescriptor : Persistent
    {
        internal static ReflectionProvider ReflectionProvider
        {
            get
            {
                if (reflectionProvider == null)
                {
                    reflectionProvider = new StandardReflectionProvider();
                }
                return reflectionProvider;
            }
        }

        internal ClassDescriptor next;
        internal string name;
        internal bool hasReferences;
        internal FieldDescriptor[] allFields;

        [Serializable]
        internal class FieldDescriptor : Persistent
        {
            internal string fieldName;
            internal string className;
            internal int type;
            internal ClassDescriptor valueDesc;
            [NonSerialized]
            internal FieldInfo field;

            public bool Equals(FieldDescriptor fd)
            {
                return fieldName.Equals(fd.fieldName) && className.Equals(fd.className) && valueDesc == fd.valueDesc && type == fd.type;
            }
        }

        [NonSerialized]
        internal Type cls;
        [NonSerialized]
        internal ConstructorInfo loadConstructor;
        [NonSerialized]
        internal object[] constructorParams;
        [NonSerialized]
        internal bool hasSubclasses;
        [NonSerialized]
        internal bool resolved;

        internal static ReflectionProvider reflectionProvider;

        public const int tpBoolean = 0;
        public const int tpByte = 1;
        public const int tpChar = 2;
        public const int tpShort = 3;
        public const int tpInt = 4;
        public const int tpLong = 5;
        public const int tpFloat = 6;
        public const int tpDouble = 7;
        public const int tpString = 8;
        public const int tpDate = 9;
        public const int tpObject = 10;
        public const int tpValue = 11;
        public const int tpRaw = 12;
        public const int tpLink = 13;
        public const int tpArrayOfBoolean = 20;
        public const int tpArrayOfByte = 21;
        public const int tpArrayOfChar = 22;
        public const int tpArrayOfShort = 23;
        public const int tpArrayOfInt = 24;
        public const int tpArrayOfLong = 25;
        public const int tpArrayOfFloat = 26;
        public const int tpArrayOfDouble = 27;
        public const int tpArrayOfString = 28;
        public const int tpArrayOfDate = 29;
        public const int tpArrayOfObject = 30;
        public const int tpArrayOfValue = 31;
        public const int tpArrayOfRaw = 32;

        internal static readonly string[] signature = new string[] {
            "boolean",
            "byte",
            "char",
            "short",
            "int",
            "long",
            "float",
            "double",
            "String",
            "Date",
            "Object",
            "Value",
            "Raw",
            "Link",
            "", "", "", "", "", "",
            "ArrayOfBoolean",
            "ArrayOfByte",
            "ArrayOfChar",
            "ArrayOfShort",
            "ArrayOfInt",
            "ArrayOfLong",
            "ArrayOfFloat",
            "ArrayOfDouble",
            "ArrayOfString",
            "ArrayOfDate",
            "ArrayOfObject",
            "ArrayOfValue",
            "ArrayOfRaw"
        };

        internal static readonly int[] Sizeof = new int[] { 1, 1, 2, 2, 4, 8, 4, 8, 0, 8, 4 };

        internal static readonly Type[] perstConstructorProfile = new Type[] { typeof(ClassDescriptor) };

        public bool Equals(ClassDescriptor cd)
        {
            if (cd == null || allFields.Length != cd.allFields.Length)
            {
                return false;
            }

            for (int i = 0; i < allFields.Length; i++)
            {
                if (!allFields[i].Equals(cd.allFields[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal object NewInstance()
        {
            try
            {
                return loadConstructor.Invoke(constructorParams);
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.CONSTRUCTOR_FAILURE, cls, x);
            }
        }

        internal void BuildFieldList(StorageImpl storage, Type cls, List<FieldDescriptor> list)
        {
            Type superclass = cls.BaseType;
            if (superclass != null)
            {
                BuildFieldList(storage, superclass, list);
            }

            FieldInfo[] flds = cls.GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public | BindingFlags.DeclaredOnly |
                BindingFlags.Static);

            foreach (FieldInfo f in flds)
            {
                //UPGRADE_ISSUE: Method 'java.lang.reflect.Field.getModifiers' was not converted.
                //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.TRANSIENT' was not converted.
                //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.STATIC' was not converted.
                //if ((f.getModifiers() & (Modifier.TRANSIENT | Modifier.STATIC)) == 0)
                if (f.IsStatic || f.IsNotSerialized)
                    continue;

                //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                //f.setAccessible(true);
                FieldDescriptor fd = new FieldDescriptor();
                fd.field = f;
                fd.fieldName = f.Name;
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
                fd.className = cls.FullName;
                int type = GetTypeCode(f.FieldType);
                switch (type)
                {
                    case tpObject:
                    case tpLink:
                    case tpArrayOfObject:
                        hasReferences = true;
                        break;

                    case tpValue:
                        fd.valueDesc = storage.GetClassDescriptor(f.FieldType);
                        hasReferences |= fd.valueDesc.hasReferences;
                        break;

                    case tpArrayOfValue:
                        fd.valueDesc = storage.GetClassDescriptor(f.FieldType.GetElementType());
                        hasReferences |= fd.valueDesc.hasReferences;
                        break;
                }
                fd.type = type;
                list.Add(fd);
            }
        }

        public static int GetTypeCode(Type c)
        {
            int type;
            if (c.Equals(typeof(byte)))
            {
                type = tpByte;
            }
            else if (c.Equals(typeof(short)))
            {
                type = tpShort;
            }
            else if (c.Equals(typeof(char)))
            {
                type = tpChar;
            }
            else if (c.Equals(typeof(int)))
            {
                type = tpInt;
            }
            else if (c.Equals(typeof(long)))
            {
                type = tpLong;
            }
            else if (c.Equals(typeof(float)))
            {
                type = tpFloat;
            }
            else if (c.Equals(typeof(double)))
            {
                type = tpDouble;
            }
            else if (c.Equals(typeof(string)))
            {
                type = tpString;
            }
            else if (c.Equals(typeof(bool)))
            {
                type = tpBoolean;
            }
            else if (c.Equals(typeof(System.DateTime)))
            {
                type = tpDate;
            }
            else if (typeof(IPersistent).IsAssignableFrom(c))
            {
                type = tpObject;
            }
            else if (typeof(IValue).IsAssignableFrom(c))
            {
                type = tpValue;
            }
            else if (c.Equals(typeof(Link)))
            {
                type = tpLink;
            }
            else if (c.IsArray)
            {
                type = GetTypeCode(c.GetElementType());
                if (type >= tpLink)
                {
                    throw new StorageError(StorageError.UNSUPPORTED_TYPE, c);
                }
                type += tpArrayOfBoolean;
            }
            else if (c.Equals(typeof(object)) || c.Equals(typeof(System.IComparable)))
            {
                type = tpRaw;
            }
            else if (serializeNonPersistentObjects)
            {
                type = tpRaw;
            }
            else if (treateAnyNonPersistentObjectAsValue)
            {
                if (c.Equals(typeof(object)))
                {
                    throw new StorageError(StorageError.EMPTY_VALUE);
                }
                type = tpValue;
            }
            else
            {
                throw new StorageError(StorageError.UNSUPPORTED_TYPE, c);
            }
            return type;
        }

        //UPGRADE_ISSUE: Method 'java.lang.Boolean.getBoolean' was not converted.
        //internal static bool treateAnyNonPersistentObjectAsValue = Boolean.getBoolean("perst.implicit.values");
        //UPGRADE_ISSUE: Method 'java.lang.Boolean.getBoolean' was not converted.
        //internal static bool serializeNonPersistentObjects = Boolean.getBoolean("perst.serialize.transient.objects");
        internal static bool treateAnyNonPersistentObjectAsValue = true;
        internal static bool serializeNonPersistentObjects = false;

        internal ClassDescriptor() { }

        private void LocateConstructor()
        {
            // TODOPORT: should this be inside try/catch?
            //loadConstructor = cls.GetConstructor(perstConstructorProfile);
            //BindingFlags flags = BindingFlags.DeclaredOnly;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            loadConstructor = cls.GetConstructor(flags, null, perstConstructorProfile, null);

            if (loadConstructor != null)
            {
                constructorParams = new object[] { this };
                return;
            }

            // TODOPORT: should this be inside try/catch?
            loadConstructor = ReflectionProvider.GetDefaultConstructor(cls);
            if (loadConstructor != null)
            {
                constructorParams = null;
                return;
            }

            throw new StorageError(StorageError.DESCRIPTOR_FAILURE, cls);
        }

        internal ClassDescriptor(StorageImpl storage, Type cls)
        {
            this.cls = cls;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
            name = cls.FullName;
            List<FieldDescriptor> list = new List<FieldDescriptor>();
            BuildFieldList(storage, cls, list);
            allFields = list.ToArray();
            LocateConstructor();
            resolved = true;
        }

        internal static Type LoadClass(Storage storage, string name)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            Assembly[] assems = currentDomain.GetAssemblies();
            foreach (var ass in assems)
            {
                Type tp = ass.GetType(name);
                if (tp != null)
                    return tp;
            }
            throw new StorageError(StorageError.CLASS_NOT_FOUND, name);
        }

        public override void OnLoad()
        {
            cls = LoadClass(Storage, name);
            Type scope = cls;
            int n = allFields.Length;
            for (int i = n; --i >= 0; )
            {
                FieldDescriptor fd = allFields[i];
                fd.Load();
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
                if (!fd.className.Equals(scope.FullName))
                {
                    for (scope = cls; scope != null; scope = scope.BaseType)
                    {
                        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
                        if (fd.className.Equals(scope.FullName))
                        {
                            break;
                        }
                    }
                }
                if (scope != null)
                {
                    try
                    {
                        //UPGRADE_TODO: The differences in the expected value of parameters for method 'java.lang.Class.getDeclaredField' may cause compilation errors.
                        FieldInfo f = scope.GetField(fd.fieldName,
                            BindingFlags.Instance | BindingFlags.NonPublic |
                            BindingFlags.Public | BindingFlags.DeclaredOnly |
                            BindingFlags.Static);
                        //UPGRADE_ISSUE: Method 'java.lang.reflect.Field.getModifiers' was not converted.
                        //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.TRANSIENT' was not converted.
                        //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.STATIC' was not converted.
                        //if ((f.getModifiers() & (Modifier.TRANSIENT | Modifier.STATIC)) == 0)
                        if (!f.IsStatic && !f.IsNotSerialized)
                        {
                            //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                            //f.setAccessible(true);
                            fd.field = f;
                        }
                    }
                    catch (System.FieldAccessException)
                    {
                    }
                }
                else
                {
                    scope = cls;
                }
            }

            for (int i = n; --i >= 0; )
            {
                FieldDescriptor fd = allFields[i];
                if (fd.field == null)
                {
                    //UPGRADE_NOTE: Label 'hierarchyLoop' was moved.
                    for (scope = cls; scope != null; scope = scope.BaseType)
                    {
                        try
                        {
                            //UPGRADE_TODO: The differences in the expected value of parameters for method 'java.lang.Class.getDeclaredField' may cause compilation errors.
                            FieldInfo f = scope.GetField(fd.fieldName,
                                BindingFlags.Instance | BindingFlags.NonPublic |
                                BindingFlags.Public | BindingFlags.DeclaredOnly |
                                BindingFlags.Static);
                            //UPGRADE_ISSUE: Method 'java.lang.reflect.Field.getModifiers' was not converted.
                            //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.TRANSIENT' was not converted.
                            //UPGRADE_ISSUE: Field 'java.lang.reflect.Modifier.STATIC' was not converted.
                            //if ((f.getModifiers() & (Modifier.TRANSIENT | Modifier.STATIC)) == 0)
                            if (!f.IsStatic && !f.IsNotSerialized)
                            {
                                for (int j = 0; j < n; j++)
                                {
                                    if (allFields[j].field == f)
                                    {
                                        //UPGRADE_NOTE: Labeled continue statement was changed to a goto statement.
                                        goto hierarchyLoop;
                                    }
                                }
                                //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                                //f.setAccessible(true);
                                fd.field = f;
                                break;
                            }
                        }
                        catch (System.FieldAccessException)
                        {
                        }
                        //UPGRADE_NOTE: Label 'hierarchyLoop' was moved.
hierarchyLoop: ;
                    }
                }
            }
            LocateConstructor();
            StorageImpl s = (StorageImpl) Storage;
            //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
            if (s.classDescMap[cls] == null)
            {
                s.classDescMap[cls] = this;
            }
        }

        internal void Resolve()
        {
            if (resolved)
                return;

            StorageImpl classStorage = (StorageImpl) Storage;
            ClassDescriptor desc = new ClassDescriptor(classStorage, cls);
            resolved = true;
            if (!desc.Equals(this))
                classStorage.RegisterClassDescriptor(desc);
        }

        public override bool RecursiveLoading
        {
            get
            {
                return false;
            }
        }
    }
}

