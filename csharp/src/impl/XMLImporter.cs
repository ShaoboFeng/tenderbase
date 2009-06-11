#if !OMIT_XML
namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class XMLImporter
    {
        //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Reader' and 'System.IO.StreamReader' may cause compilation errors.
        public XMLImporter(StorageImpl storage, System.IO.StreamReader reader)
        {
            this.storage = storage;
            scanner = new XMLScanner(reader);
        }

        public virtual void ImportDatabase()
        {
            if (scanner.Scan() != XMLScanner.XML_LT || scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals("database"))
            {
                ThrowException("No root element");
            }

            if (scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals("root") || scanner.Scan() != XMLScanner.XML_EQ || scanner.Scan() != XMLScanner.XML_SCONST || scanner.Scan() != XMLScanner.XML_GT)
            {
                ThrowException("Database element should have \"root\" attribute");
            }

            int rootId = 0;
            try
            {
                rootId = Int32.Parse(scanner.String);
            }
            catch (FormatException)
            {
                ThrowException("Incorrect root object specification");
            }
            idMap = new int[rootId * 2];
            idMap[rootId] = storage.AllocateId();
            storage.header.root[1 - storage.currIndex].rootObject = idMap[rootId];

            //XMLElement elem;
            int tkn;
            while ((tkn = scanner.Scan()) == XMLScanner.XML_LT)
            {
                if (scanner.Scan() != XMLScanner.XML_IDENT)
                {
                    ThrowException("Element name expected");
                }

                string elemName = scanner.Identifier;
                if (elemName.Equals("TenderBaseImpl.Btree") || elemName.Equals("TenderBaseImpl.BitIndexImpl") || elemName.Equals("TenderBaseImpl.PersistentSet") || elemName.Equals("TenderBaseImpl.BtreeFieldIndex") || elemName.Equals("TenderBaseImpl.BtreeMultiFieldIndex"))
                {
                    CreateIndex(elemName);
                }
                else
                {
                    CreateObject(ReadElement(elemName));
                }
            }

            if (tkn != XMLScanner.XML_LTS || scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals("database") || scanner.Scan() != XMLScanner.XML_GT)
            {
                ThrowException("Root element is not closed");
            }
        }

        internal class XMLElement
        {
            internal virtual XMLElement NextSibling
            {
                get
                {
                    return next;
                }
            }

            internal virtual int Counter
            {
                get
                {
                    return counter;
                }
            }

            private XMLElement next;
            private XMLElement prev;
            internal string name;
            //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
            private System.Collections.Hashtable siblings;
            //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
            private System.Collections.Hashtable attributes;
            private string svalue;
            private long ivalue;
            private double rvalue;
            private int valueType;
            private int counter;

            internal const int NO_VALUE = 0;
            internal const int STRING_VALUE = 1;
            internal const int INT_VALUE = 2;
            internal const int REAL_VALUE = 3;
            internal const int NULL_VALUE = 4;

            internal XMLElement(string name)
            {
                this.name = name;
                valueType = NO_VALUE;
            }

            internal void AddSibling(XMLElement elem)
            {
                if (siblings == null)
                {
                    //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
                    siblings = new System.Collections.Hashtable();
                }
                //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                XMLElement prev = (XMLElement) siblings[elem.name];
                if (prev != null)
                {
                    elem.next = null;
                    elem.prev = prev.prev;
                    elem.prev.next = elem;
                    prev.prev = elem;
                    prev.counter += 1;
                }
                else
                {
                    siblings[elem.name] = elem;
                    elem.prev = elem;
                    elem.counter = 1;
                }
            }

            internal void AddAttribute(string name, string val)
            {
                if (attributes == null)
                {
                    //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
                    attributes = new System.Collections.Hashtable();
                }
                attributes[name] = val;
            }

            internal XMLElement GetSibling(string name)
            {
                if (siblings != null)
                {
                    //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                    return (XMLElement) siblings[name];
                }
                return null;
            }

            internal string GetAttribute(string name)
            {
                //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                return attributes != null ? (string) attributes[name] : null;
            }

            internal void SetIntValue(long val)
            {
                ivalue = val;
                valueType = INT_VALUE;
            }

            internal void SetRealValue(double val)
            {
                rvalue = val;
                valueType = REAL_VALUE;
            }

            internal void SetStringValue(string val)
            {
                svalue = val;
                valueType = STRING_VALUE;
            }

            internal void SetNullValue()
            {
                valueType = NULL_VALUE;
            }

            internal string GetStringValue()
            {
                return svalue;
            }

            internal long GetIntValue()
            {
                return ivalue;
            }

            internal double GetRealValue()
            {
                return rvalue;
            }

            internal bool IsIntValue()
            {
                return valueType == INT_VALUE;
            }

            internal bool IsRealValue()
            {
                return valueType == REAL_VALUE;
            }

            internal bool IsStringValue()
            {
                return valueType == STRING_VALUE;
            }

            internal bool IsNullValue()
            {
                return valueType == NULL_VALUE;
            }
        }

        internal string GetAttribute(XMLElement elem, string name)
        {
            string val = elem.GetAttribute(name);
            if (val == null)
            {
                ThrowException("Attribute " + name + " is not set");
            }
            return val;
        }

        internal int GetIntAttribute(XMLElement elem, string name)
        {
            string val = elem.GetAttribute(name);
            if (val == null)
            {
                ThrowException("Attribute " + name + " is not set");
            }
            try
            {
                return Int32.Parse(val);
            }
            catch (FormatException)
            {
                ThrowException("Attribute " + name + " should has integer value");
            }
            return -1;
        }

        internal int MapId(int id)
        {
            int oid = 0;
            if (id != 0)
            {
                if (id >= idMap.Length)
                {
                    int[] newMap = new int[id * 2];
                    Array.Copy(idMap, 0, newMap, 0, idMap.Length);
                    idMap = newMap;
                    idMap[id] = oid = storage.AllocateId();
                }
                else
                {
                    oid = idMap[id];
                    if (oid == 0)
                    {
                        idMap[id] = oid = storage.AllocateId();
                    }
                }
            }
            return oid;
        }

        internal int MapType(string signature)
        {
            for (int i = 0; i < ClassDescriptor.signature.Length; i++)
            {
                if (ClassDescriptor.signature[i].Equals(signature))
                {
                    return i;
                }
            }
            ThrowException("Bad type");
            return -1;
        }

        internal Key CreateCompoundKey(int[] types, string[] values)
        {
            ByteBuffer buf = new ByteBuffer();
            int dst = 0;

            try
            {
                for (int i = 0; i < types.Length; i++)
                {
                    string val = values[i];
                    switch (types[i])
                    {
                        case ClassDescriptor.tpBoolean:
                            buf.Extend(dst + 1);
                            buf.arr[dst++] = (byte) (Int32.Parse(val) != 0 ? 1 : 0);
                            break;

                        case ClassDescriptor.tpByte:
                            buf.Extend(dst + 1);
                            buf.arr[dst++] = (byte) System.SByte.Parse(val);
                            break;

                        case ClassDescriptor.tpChar:
                            buf.Extend(dst + 2);
                            Bytes.Pack2(buf.arr, dst, (short) Int32.Parse(val));
                            dst += 2;
                            break;

                        case ClassDescriptor.tpShort:
                            buf.Extend(dst + 2);
                            Bytes.Pack2(buf.arr, dst, System.Int16.Parse(val));
                            dst += 2;
                            break;

                        case ClassDescriptor.tpInt:
                            buf.Extend(dst + 4);
                            Bytes.Pack4(buf.arr, dst, Int32.Parse(val));
                            dst += 4;
                            break;

                        case ClassDescriptor.tpObject:
                            buf.Extend(dst + 4);
                            Bytes.Pack4(buf.arr, dst, MapId(Int32.Parse(val)));
                            dst += 4;
                            break;

                        case ClassDescriptor.tpLong:
                        case ClassDescriptor.tpDate:
                            buf.Extend(dst + 8);
                            Bytes.Pack8(buf.arr, dst, Int64.Parse(val));
                            dst += 8;
                            break;

                        case ClassDescriptor.tpFloat:
                            buf.Extend(dst + 4);
                            Bytes.PackF4(buf.arr, dst, Single.Parse(val));
                            dst += 4;
                            break;

                        case ClassDescriptor.tpDouble:
                            buf.Extend(dst + 8);
                            Bytes.PackF8(buf.arr, dst, Double.Parse(val));
                            dst += 8;
                            break;

                        case ClassDescriptor.tpString:
                            dst = buf.PackString(dst, val, storage.encoding);
                            break;

                        case ClassDescriptor.tpArrayOfByte:
                            buf.Extend(dst + 4 + (SupportClass.URShift(val.Length, 1)));
                            Bytes.Pack4(buf.arr, dst, SupportClass.URShift(val.Length, 1));
                            dst += 4;
                            for (int j = 0, n = val.Length; j < n; j += 2)
                            {
                                buf.arr[dst++] = (byte) ((GetHexValue(val[j]) << 4) | GetHexValue(val[j + 1]));
                            }
                            break;

                        default:
                            ThrowException("Bad key type");
                            break;
                    }
                }
            }
            catch (FormatException)
            {
                ThrowException("Failed to convert key value");
            }
            return new Key(buf.ToArray());
        }

        internal Key CreateKey(int type, string val)
        {
            try
            {
                /* TODOPORT: DateTime date; */
                switch (type)
                {
                    case ClassDescriptor.tpBoolean:
                        return new Key(Int32.Parse(val) != 0);

                    case ClassDescriptor.tpByte:
                        return new Key((byte) Byte.Parse(val));

                    case ClassDescriptor.tpChar:
                        return new Key((char) Int32.Parse(val));

                    case ClassDescriptor.tpShort:
                        return new Key(Int16.Parse(val));

                    case ClassDescriptor.tpInt:
                        return new Key(Int32.Parse(val));

                    case ClassDescriptor.tpObject:
                        return new Key(new PersistentStub(storage, MapId(Int32.Parse(val))));

                    case ClassDescriptor.tpLong:
                        return new Key(Int64.Parse(val));

                    case ClassDescriptor.tpFloat:
                        return new Key(Single.Parse(val));

                    case ClassDescriptor.tpDouble:
                        return new Key(Double.Parse(val));

                    case ClassDescriptor.tpString:
                        return new Key(val);

                    case ClassDescriptor.tpArrayOfByte:
                    {
                        byte[] buf = new byte[val.Length >> 1];
                        for (int i = 0; i < buf.Length; i++)
                        {
                            buf[i] = (byte) ((GetHexValue(val[i * 2]) << 4) | GetHexValue(val[i * 2 + 1]));
                        }
                        return new Key(buf);
                    }

                    case ClassDescriptor.tpDate:
                        /* TODOPORT:
                        if (val.Equals("null"))
                        {
                            //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                            date = null;
                        }
                        else
                        {
                            //UPGRADE_ISSUE: Method 'java.text.DateFormat.parse' was not converted.
                            date = httpFormatter.parse(val, 0);
                            //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                            if (date == null)
                            {
                                ThrowException("Invalid date");
                            }
                        }

                        //UPGRADE_NOTE: ref keyword was added to struct-type parameters.
                        return new Key(ref date);
                        */
                        ThrowException("Not implemented");
                        return null;

                    default:
                        ThrowException("Bad key type");
                        break;
                }
            }
            catch (FormatException)
            {
                ThrowException("Failed to convert key value");
            }
            return null;
        }

        internal int ParseInt(string str)
        {
            try
            {
                // TODO: for perf it could use TryParse()
                return Int32.Parse(str);
            }
            catch (FormatException)
            {
                ThrowException("Bad integer constant");
            }
            return -1;
        }

        internal void CreateIndex(string indexType)
        {
            Btree btree = null;
            int tkn;
            int oid = 0;
            bool unique = false;
            string className = null;
            string fieldName = null;
            string[] fieldNames = null;
            long autoinc = 0;
            string type = null;
            while ((tkn = scanner.Scan()) == XMLScanner.XML_IDENT)
            {
                string attrName = scanner.Identifier;
                if (scanner.Scan() != XMLScanner.XML_EQ || scanner.Scan() != XMLScanner.XML_SCONST)
                {
                    ThrowException("Attribute value expected");
                }
                string attrValue = scanner.String;
                if (attrName.Equals("id"))
                {
                    oid = MapId(ParseInt(attrValue));
                }
                else if (attrName.Equals("unique"))
                {
                    unique = ParseInt(attrValue) != 0;
                }
                else if (attrName.Equals("class"))
                {
                    className = attrValue;
                }
                else if (attrName.Equals("type"))
                {
                    type = attrValue;
                }
                else if (attrName.Equals("autoinc"))
                {
                    autoinc = ParseInt(attrValue);
                }
                else if (attrName.StartsWith("field"))
                {
                    int len = attrName.Length;
                    if (len == 5)
                    {
                        fieldName = attrValue;
                    }
                    else
                    {
                        try
                        {
                            int fieldNo = Int32.Parse(attrName.Substring(5));
                            if (fieldNames == null || fieldNames.Length <= fieldNo)
                            {
                                string[] newFieldNames = new string[fieldNo + 1];
                                if (fieldNames != null)
                                {
                                    Array.Copy(fieldNames, 0, newFieldNames, 0, fieldNames.Length);
                                }
                                fieldNames = newFieldNames;
                            }
                            fieldNames[fieldNo] = attrValue;
                        }
                        catch (FormatException)
                        {
                            ThrowException("Invalid field index");
                        }
                    }
                }
            }

            if (tkn != XMLScanner.XML_GT)
            {
                ThrowException("Unclosed element tag");
            }

            if (oid == 0)
            {
                ThrowException("ID is not specified or index");
            }

            if (className != null)
            {
                Type cls = ClassDescriptor.LoadClass(storage, className);
                if (fieldName != null)
                {
                    btree = new BtreeFieldIndex(cls, fieldName, unique, autoinc);
                }
                else if (fieldNames != null)
                {
                    btree = new BtreeMultiFieldIndex(cls, fieldNames, unique);
                }
                else
                {
                    ThrowException("Field name is not specified for field index");
                }
            }
            else
            {
                if (type == null)
                {
                    if (indexType.Equals("TenderBaseImpl.PersistentSet"))
                    {
                        btree = new PersistentSet();
                    }
                    else
                    {
                        ThrowException("Key type is not specified for index");
                    }
                }
                else
                {
                    if (indexType.Equals("TenderBaseImpl.BitIndexImpl"))
                    {
                        btree = new BitIndexImpl();
                    }
                    else
                    {
                        btree = new Btree(MapType(type), unique);
                    }
                }
            }
            storage.AssignOid(btree, oid);

            while ((tkn = scanner.Scan()) == XMLScanner.XML_LT)
            {
                if (scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals("ref"))
                {
                    ThrowException("<ref> element expected");
                }
                XMLElement ref_Renamed = ReadElement("ref");
                Key key = null;
                int mask = 0;
                if (fieldNames != null)
                {
                    string[] values = new string[fieldNames.Length];
                    int[] types = ((BtreeMultiFieldIndex) btree).types;
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = GetAttribute(ref_Renamed, "key" + i);
                    }
                    key = CreateCompoundKey(types, values);
                }
                else
                {
                    if (btree is BitIndex)
                    {
                        mask = GetIntAttribute(ref_Renamed, "key");
                    }
                    else
                    {
                        key = CreateKey(btree.type, GetAttribute(ref_Renamed, "key"));
                    }
                }
                IPersistent obj = new PersistentStub(storage, MapId(GetIntAttribute(ref_Renamed, "id")));
                if (btree is BitIndex)
                {
                    ((BitIndex) btree).Put(obj, mask);
                }
                else
                {
                    btree.Insert(key, obj, false);
                }
            }
            if (tkn != XMLScanner.XML_LTS || scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals(indexType) || scanner.Scan() != XMLScanner.XML_GT)
            {
                ThrowException("Element is not closed");
            }
            byte[] data = storage.PackObject(btree);
            int size = ObjectHeader.GetSize(data, 0);
            long pos = storage.Allocate(size, 0);
            storage.SetPos(oid, pos | StorageImpl.dbModifiedFlag);

            storage.pool.Put(pos & ~ StorageImpl.dbFlagsMask, data, size);
        }

        internal void CreateObject(XMLElement elem)
        {
            Type cls = ClassDescriptor.LoadClass(storage, elem.name);
            ClassDescriptor desc = storage.GetClassDescriptor(cls);
            int oid = MapId(GetIntAttribute(elem, "id"));
            ByteBuffer buf = new ByteBuffer();
            int offs = ObjectHeader.Sizeof;
            buf.Extend(offs);

            offs = PackObject(elem, desc, offs, buf);

            ObjectHeader.SetSize(buf.arr, 0, offs);
            ObjectHeader.SetType(buf.arr, 0, desc.Oid);

            long pos = storage.Allocate(offs, 0);
            storage.SetPos(oid, pos | StorageImpl.dbModifiedFlag);
            storage.pool.Put(pos, buf.arr, offs);
        }

        internal int GetHexValue(char ch)
        {
            if (ch >= '0' && ch <= '9')
            {
                return ch - '0';
            }
            else if (ch >= 'A' && ch <= 'F')
            {
                return ch - 'A' + 10;
            }
            else if (ch >= 'a' && ch <= 'f')
            {
                return ch - 'a' + 10;
            }
            else
            {
                ThrowException("Bad hexadecimal constant");
            }
            return -1;
        }

        internal int ImportBinary(XMLElement elem, int offs, ByteBuffer buf, string fieldName)
        {
            if (elem == null || elem.IsNullValue())
            {
                buf.Extend(offs + 4);
                Bytes.Pack4(buf.arr, offs, -1);
                offs += 4;
            }
            else if (elem.IsStringValue())
            {
                string hexStr = elem.GetStringValue();
                int len = hexStr.Length;
                if (hexStr.StartsWith("#"))
                {
                    buf.Extend(offs + 4 + len / 2 - 1);
                    Bytes.Pack4(buf.arr, offs, -2 - GetHexValue(hexStr[1]));
                    offs += 4;
                    for (int j = 2; j < len; j += 2)
                    {
                        buf.arr[offs++] = (byte) ((GetHexValue(hexStr[j]) << 4) | GetHexValue(hexStr[j + 1]));
                    }
                }
                else
                {
                    buf.Extend(offs + 4 + len / 2);
                    Bytes.Pack4(buf.arr, offs, len / 2);
                    offs += 4;
                    for (int j = 0; j < len; j += 2)
                    {
                        buf.arr[offs++] = (byte) ((GetHexValue(hexStr[j]) << 4) | GetHexValue(hexStr[j + 1]));
                    }
                }
            }
            else
            {
                XMLElement ref_Renamed = elem.GetSibling("ref");
                if (ref_Renamed != null)
                {
                    buf.Extend(offs + 4);
                    Bytes.Pack4(buf.arr, offs, MapId(GetIntAttribute(ref_Renamed, "id")));
                    offs += 4;
                }
                else
                {
                    XMLElement item = elem.GetSibling("element");
                    int len = (item == null) ? 0 : item.Counter;
                    buf.Extend(offs + 4 + len);
                    Bytes.Pack4(buf.arr, offs, len);
                    offs += 4;
                    while (--len >= 0)
                    {
                        if (item.IsIntValue())
                        {
                            buf.arr[offs] = (byte) item.GetIntValue();
                        }
                        else if (item.IsRealValue())
                        {
                            //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                            buf.arr[offs] = (byte) item.GetRealValue();
                        }
                        else
                        {
                            ThrowException("Conversion for field " + fieldName + " is not possible");
                        }
                        item = item.NextSibling;
                        offs += 1;
                    }
                }
            }
            return offs;
        }

        internal int PackObject(XMLElement objElem, ClassDescriptor desc, int offs, ByteBuffer buf)
        {
            ClassDescriptor.FieldDescriptor[] flds = desc.allFields;
            for (int i = 0, n = flds.Length; i < n; i++)
            {
                ClassDescriptor.FieldDescriptor fd = flds[i];
                string fieldName = fd.fieldName;
                XMLElement elem = (objElem != null) ? objElem.GetSibling(fieldName) : null;

                switch (fd.type)
                {
                    case ClassDescriptor.tpByte:
                        buf.Extend(offs + 1);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                buf.arr[offs] = (byte) elem.GetIntValue();
                            }
                            else if (elem.IsRealValue())
                            {
                                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                buf.arr[offs] = (byte) elem.GetRealValue();
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 1;
                        continue;

                    case ClassDescriptor.tpBoolean:
                        buf.Extend(offs + 1);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                buf.arr[offs] = (byte) (elem.GetIntValue() != 0 ? 1 : 0);
                            }
                            else if (elem.IsRealValue())
                            {
                                buf.arr[offs] = (byte) (elem.GetRealValue() != 0.0 ? 1 : 0);
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 1;
                        continue;

                    case ClassDescriptor.tpShort:
                    case ClassDescriptor.tpChar:
                        buf.Extend(offs + 2);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                Bytes.Pack2(buf.arr, offs, (short) elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                Bytes.Pack2(buf.arr, offs, (short) elem.GetRealValue());
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 2;
                        continue;

                    case ClassDescriptor.tpInt:
                        buf.Extend(offs + 4);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                Bytes.Pack4(buf.arr, offs, (int) elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                Bytes.Pack4(buf.arr, offs, (int) elem.GetRealValue());
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpLong:
                        buf.Extend(offs + 8);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                Bytes.Pack8(buf.arr, offs, elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                Bytes.Pack8(buf.arr, offs, (long) elem.GetRealValue());
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpFloat:
                        buf.Extend(offs + 4);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                Bytes.PackF4(buf.arr, offs, (float) elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                Bytes.PackF4(buf.arr, offs, (float) elem.GetRealValue());
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpDouble:
                        buf.Extend(offs + 8);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                Bytes.PackF8(buf.arr, offs, (double) elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                Bytes.PackF8(buf.arr, offs, (double) elem.GetRealValue());
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpDate:
                        buf.Extend(offs + 8);
                        if (elem != null)
                        {
                            if (elem.IsIntValue())
                            {
                                Bytes.Pack8(buf.arr, offs, elem.GetIntValue());
                            }
                            else if (elem.IsNullValue())
                            {
                                Bytes.Pack8(buf.arr, offs, -1);
                            }
                            else if (elem.IsStringValue())
                            {
                                /* TODOPORT:
                                //UPGRADE_ISSUE: Method 'java.text.DateFormat.parse' was not converted.
                                DateTime date = httpFormatter.parse(elem.GetStringValue(), 0);
                                //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                                if (date == null)
                                {
                                    ThrowException("Invalid date");
                                }
                                //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                                Bytes.Pack8(buf.arr, offs, date.Ticks);
                                */
                                ThrowException("Not implemented");
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                        }
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpString:
                        if (elem != null)
                        {
                            string val = null;
                            if (elem.IsIntValue())
                            {
                                val = Convert.ToString(elem.GetIntValue());
                            }
                            else if (elem.IsRealValue())
                            {
                                val = elem.GetRealValue().ToString();
                            }
                            else if (elem.IsStringValue())
                            {
                                val = elem.GetStringValue();
                            }
                            else if (elem.IsNullValue())
                            {
                                val = null;
                            }
                            else
                            {
                                ThrowException("Conversion for field " + fieldName + " is not possible");
                            }
                            offs = buf.PackString(offs, val, storage.encoding);
                            continue;
                        }

                        buf.Extend(offs + 4);
                        Bytes.Pack4(buf.arr, offs, -1);
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpObject:
                    {
                        int oid = 0;
                        if (elem != null)
                        {
                            XMLElement ref_Renamed = elem.GetSibling("ref");
                            if (ref_Renamed == null)
                            {
                                ThrowException("<ref> element expected");
                            }
                            oid = MapId(GetIntAttribute(ref_Renamed, "id"));
                        }
                        buf.Extend(offs + 4);
                        Bytes.Pack4(buf.arr, offs, oid);
                        offs += 4;
                        continue;
                    }

                    case ClassDescriptor.tpValue:
                        offs = PackObject(elem, fd.valueDesc, offs, buf);
                        continue;

                    case ClassDescriptor.tpRaw:
                    case ClassDescriptor.tpArrayOfByte:
                        offs = ImportBinary(elem, offs, buf, fieldName);
                        continue;

                    case ClassDescriptor.tpArrayOfBoolean:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    buf.arr[offs] = (byte) (item.GetIntValue() != 0 ? 1 : 0);
                                }
                                else if (item.IsRealValue())
                                {
                                    buf.arr[offs] = (byte) (item.GetRealValue() != 0.0 ? 1 : 0);
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 1;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfChar:
                    case ClassDescriptor.tpArrayOfShort:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 2);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    Bytes.Pack2(buf.arr, offs, (short) item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                    Bytes.Pack2(buf.arr, offs, (short) item.GetRealValue());
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 2;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfInt:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    Bytes.Pack4(buf.arr, offs, (int) item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                    Bytes.Pack4(buf.arr, offs, (int) item.GetRealValue());
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 4;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfLong:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    Bytes.Pack8(buf.arr, offs, item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                    Bytes.Pack8(buf.arr, offs, (long) item.GetRealValue());
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 8;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfFloat:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    Bytes.PackF4(buf.arr, offs, (float) item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    Bytes.PackF4(buf.arr, offs, (float) item.GetRealValue());
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 4;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfDouble:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsIntValue())
                                {
                                    //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
                                    Bytes.PackF8(buf.arr, offs, (double) item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    Bytes.PackF8(buf.arr, offs, (double) item.GetRealValue());
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 8;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfDate:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                if (item.IsNullValue())
                                {
                                    Bytes.Pack8(buf.arr, offs, -1);
                                }
                                else if (item.IsStringValue())
                                {
                                    /* TODOPORT:
                                    //UPGRADE_ISSUE: Method 'java.text.DateFormat.parse' was not converted.
                                    DateTime date = httpFormatter.parse(item.GetStringValue(), 0);
                                    //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                                    if (date == null)
                                    {
                                        ThrowException("Invalid date");
                                    }
                                    //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                                    Bytes.Pack8(buf.arr, offs, date.Ticks);
                                    */
                                    ThrowException("Not implemented");
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }
                                item = item.NextSibling;
                                offs += 8;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfString:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                string val = null;
                                if (item.IsIntValue())
                                {
                                    val = Convert.ToString(item.GetIntValue());
                                }
                                else if (item.IsRealValue())
                                {
                                    val = item.GetRealValue().ToString();
                                }
                                else if (item.IsStringValue())
                                {
                                    val = item.GetStringValue();
                                }
                                else if (elem.IsNullValue())
                                {
                                    val = null;
                                }
                                else
                                {
                                    ThrowException("Conversion for field " + fieldName + " is not possible");
                                }

                                offs = buf.PackString(offs, val, storage.encoding);
                                item = item.NextSibling;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfObject:
                    case ClassDescriptor.tpLink:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                XMLElement ref_Renamed = item.GetSibling("ref");
                                if (ref_Renamed == null)
                                {
                                    ThrowException("<ref> element expected");
                                }
                                int oid = MapId(GetIntAttribute(ref_Renamed, "id"));
                                Bytes.Pack4(buf.arr, offs, oid);
                                item = item.NextSibling;
                                offs += 4;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfValue:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            ClassDescriptor elemDesc = fd.valueDesc;
                            while (--len >= 0)
                            {
                                offs = PackObject(item, elemDesc, offs, buf);
                                item = item.NextSibling;
                            }
                        }
                        continue;

                    case ClassDescriptor.tpArrayOfRaw:
                        if (elem == null || elem.IsNullValue())
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            XMLElement item = elem.GetSibling("element");
                            int len = (item == null) ? 0 : item.Counter;
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            while (--len >= 0)
                            {
                                offs = ImportBinary(item, offs, buf, fieldName);
                                item = item.NextSibling;
                            }
                        }
                        continue;
                    }
            }
            return offs;
        }

        internal XMLElement ReadElement(string name)
        {
            XMLElement elem = new XMLElement(name);
            string attribute;
            int tkn;
            while (true)
            {
                switch (scanner.Scan())
                {
                    case XMLScanner.XML_GTS:
                        return elem;

                    case XMLScanner.XML_GT:
                        while ((tkn = scanner.Scan()) == XMLScanner.XML_LT)
                        {
                            if (scanner.Scan() != XMLScanner.XML_IDENT)
                            {
                                ThrowException("Element name expected");
                            }
                            string siblingName = scanner.Identifier;
                            XMLElement sibling = ReadElement(siblingName);
                            elem.AddSibling(sibling);
                        }
                        switch (tkn)
                        {
                            case XMLScanner.XML_SCONST:
                                elem.SetStringValue(scanner.String);
                                tkn = scanner.Scan();
                                break;

                            case XMLScanner.XML_ICONST:
                                elem.SetIntValue(scanner.Int);
                                tkn = scanner.Scan();
                                break;

                            case XMLScanner.XML_FCONST:
                                elem.SetRealValue(scanner.Real);
                                tkn = scanner.Scan();
                                break;

                            case XMLScanner.XML_IDENT:
                                if (scanner.Identifier.Equals("null"))
                                {
                                    elem.SetNullValue();
                                }
                                else
                                {
                                    elem.SetStringValue(scanner.Identifier);
                                }
                                tkn = scanner.Scan();
                                break;
                            }
                        if (tkn != XMLScanner.XML_LTS || scanner.Scan() != XMLScanner.XML_IDENT || !scanner.Identifier.Equals(name) || scanner.Scan() != XMLScanner.XML_GT)
                        {
                            ThrowException("Element is not closed");
                        }
                        return elem;

                    case XMLScanner.XML_IDENT:
                        attribute = scanner.Identifier;
                        if (scanner.Scan() != XMLScanner.XML_EQ || scanner.Scan() != XMLScanner.XML_SCONST)
                        {
                            ThrowException("Attribute value expected");
                        }
                        elem.AddAttribute(attribute, scanner.String);
                        continue;

                    default:
                        ThrowException("Unexpected token");
                        break;
                }
            }
        }

        internal void ThrowException(string message)
        {
            throw new XMLImportException(scanner.Line, scanner.Column, message);
        }

        internal StorageImpl storage;
        internal XMLScanner scanner;
        internal int[] idMap;

        internal const string dateFormat = "EEE, d MMM yyyy kk:mm:ss z";
        //UPGRADE_ISSUE: Constructor 'java.text.SimpleDateFormat.SimpleDateFormat' was not converted.
        // TODOPORT: internal static readonly System.Globalization.DateTimeFormatInfo httpFormatter = new SimpleDateFormat(dateFormat, new System.Globalization.CultureInfo("en"));
        internal class XMLScanner
        {
            internal virtual string Identifier
            {
                get
                {
                    return ident;
                }
            }

            internal virtual string String
            {
                get
                {
                    return new string(sconst, 0, slen);
                }
            }

            internal virtual long Int
            {
                get
                {
                    return iconst;
                }
            }

            internal virtual double Real
            {
                get
                {
                    return fconst;
                }
            }

            internal virtual int Line
            {
                get
                {
                    return line;
                }
            }

            internal virtual int Column
            {
                get
                {
                    return column;
                }
            }

            internal const int XML_IDENT = 0;
            internal const int XML_SCONST = 1;
            internal const int XML_ICONST = 2;
            internal const int XML_FCONST = 3;
            internal const int XML_LT = 4;
            internal const int XML_GT = 5;
            internal const int XML_LTS = 6;
            internal const int XML_GTS = 7;
            internal const int XML_EQ = 8;
            internal const int XML_EOF = 9;

            //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Reader' and 'System.IO.StreamReader' may cause compilation errors.
            internal System.IO.StreamReader reader;
            internal int line;
            internal int column;
            internal char[] sconst;
            internal long iconst;
            internal double fconst;
            internal int slen;
            internal string ident;
            internal int size;
            internal int ungetChar;
            internal bool hasUngetChar;

            //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Reader' and 'System.IO.StreamReader' may cause compilation errors.
            internal XMLScanner(System.IO.StreamReader streamIn)
            {
                reader = streamIn;
                sconst = new char[size = 1024];
                line = 1;
                column = 0;
                hasUngetChar = false;
            }

            internal int Get()
            {
                if (hasUngetChar)
                {
                    hasUngetChar = false;
                    return ungetChar;
                }
                try
                {
                    //UPGRADE_TODO: Method 'java.io.Reader.read' was converted to 'System.IO.StreamReader.Read' which has a different behavior.
                    int ch = reader.Read();
                    if (ch == '\n')
                    {
                        line += 1;
                        column = 0;
                    }
                    else if (ch == '\t')
                    {
                        column += ((column + 8) & ~ 7);
                    }
                    else
                    {
                        column += 1;
                    }
                    return ch;
                }
                catch (System.IO.IOException x)
                {
                    //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.getMessage' may return a different value.
                    throw new XMLImportException(line, column, x.Message);
                }
            }

            internal void Unget(int ch)
            {
                if (ch == '\n')
                {
                    line -= 1;
                }
                else
                {
                    column -= 1;
                }
                ungetChar = ch;
                hasUngetChar = true;
            }

            internal int Scan()
            {
                int i, ch;
                bool floatingPoint;

                while (true)
                {
                    do
                    {
                        if ((ch = Get()) < 0)
                        {
                            return XML_EOF;
                        }
                    }
                    while (ch <= ' ');

                    switch (ch)
                    {
                        case '<':
                            ch = Get();
                            if (ch == '?')
                            {
                                while ((ch = Get()) != '?')
                                {
                                    if (ch < 0)
                                    {
                                        throw new XMLImportException(line, column, "Bad XML file format");
                                    }
                                }
                                if ((ch = Get()) != '>')
                                {
                                    throw new XMLImportException(line, column, "Bad XML file format");
                                }
                                continue;
                            }
                            if (ch != '/')
                            {
                                Unget(ch);
                                return XML_LT;
                            }
                            return XML_LTS;

                        case '>':
                            return XML_GT;

                        case '/':
                            ch = Get();
                            if (ch != '>')
                            {
                                Unget(ch);
                                throw new XMLImportException(line, column, "Bad XML file format");
                            }
                            return XML_GTS;

                        case '=':
                            return XML_EQ;

                        case '"':
                            i = 0;
                            while (true)
                            {
                                ch = Get();
                                if (ch < 0)
                                {
                                    throw new XMLImportException(line, column, "Bad XML file format");
                                }
                                else if (ch == '&')
                                {
                                    switch (Get())
                                    {
                                        case 'a':
                                            if (Get() != 'm' || Get() != 'p' || Get() != ';')
                                            {
                                                throw new XMLImportException(line, column, "Bad XML file format");
                                            }
                                            ch = '&';
                                            break;

                                        case 'l':
                                            if (Get() != 't' || Get() != ';')
                                            {
                                                throw new XMLImportException(line, column, "Bad XML file format");
                                            }
                                            ch = '<';
                                            break;

                                        case 'g':
                                            if (Get() != 't' || Get() != ';')
                                            {
                                                throw new XMLImportException(line, column, "Bad XML file format");
                                            }
                                            ch = '>';
                                            break;

                                        case 'q':
                                            if (Get() != 'u' || Get() != 'o' || Get() != 't' || Get() != ';')
                                            {
                                                throw new XMLImportException(line, column, "Bad XML file format");
                                            }
                                            ch = '"';
                                            break;

                                        default:
                                            throw new XMLImportException(line, column, "Bad XML file format");
                                    }
                                }
                                else if (ch == '"')
                                {
                                    slen = i;
                                    return XML_SCONST;
                                }
                                if (i == size)
                                {
                                    char[] newBuf = new char[size *= 2];
                                    Array.Copy(sconst, 0, newBuf, 0, i);
                                    sconst = newBuf;
                                }
                                sconst[i++] = (char) ch;
                            }

                        case '-':
                        case '+':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            i = 0;
                            floatingPoint = false;
                            while (true)
                            {
                                if (!System.Char.IsDigit((char) ch) && ch != '-' && ch != '+' && ch != '.' && ch != 'E')
                                {
                                    Unget(ch);
                                    try
                                    {
                                        if (floatingPoint)
                                        {
                                            fconst = Double.Parse(new string(sconst, 0, i));
                                            return XML_FCONST;
                                        }
                                        else
                                        {
                                            iconst = Int64.Parse(new string(sconst, 0, i));
                                            return XML_ICONST;
                                        }
                                    }
                                    catch (FormatException)
                                    {
                                        throw new XMLImportException(line, column, "Bad XML file format");
                                    }
                                }
                                if (i == size)
                                {
                                    throw new XMLImportException(line, column, "Bad XML file format");
                                }
                                sconst[i++] = (char) ch;
                                if (ch == '.')
                                {
                                    floatingPoint = true;
                                }
                                ch = Get();
                            }

                        default:
                            i = 0;
                            while (System.Char.IsLetterOrDigit((char) ch) || ch == '-' || ch == ':' || ch == '_' || ch == '.')
                            {
                                if (i == size)
                                {
                                    throw new XMLImportException(line, column, "Bad XML file format");
                                }
                                if (ch == '-')
                                {
                                    ch = '$';
                                }
                                sconst[i++] = (char) ch;
                                ch = Get();
                            }
                            Unget(ch);
                            if (i == 0)
                            {
                                throw new XMLImportException(line, column, "Bad XML file format");
                            }
                            ident = new string(sconst, 0, i);
                            return XML_IDENT;
                    }
                }
            }
        }
    }
}
#endif

