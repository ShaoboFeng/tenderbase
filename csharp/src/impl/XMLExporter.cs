#if !OMIT_XML
namespace TenderBaseImpl
{
    using System;
    using Assert = TenderBase.Assert;
    
    public class XMLExporter
    {
        //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Writer' and 'System.IO.StreamWriter' may cause compilation errors.
        public XMLExporter(StorageImpl storage, System.IO.StreamWriter writer)
        {
            this.storage = storage;
            this.writer = writer;
        }

        public virtual void ExportDatabase(int rootOid)
        {
            writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            writer.Write("<database root=\"" + rootOid + "\">\n");
            exportedBitmap = new int[(storage.currIndexSize + 31) / 32];
            markedBitmap = new int[(storage.currIndexSize + 31) / 32];
            markedBitmap[rootOid >> 5] |= 1 << (rootOid & 31);
            int nExportedObjects;
            do
            {
                nExportedObjects = 0;
                for (int i = 0; i < markedBitmap.Length; i++)
                {
                    int mask = markedBitmap[i];
                    if (mask != 0)
                    {
                        for (int j = 0, bit = 1; j < 32; j++, bit <<= 1)
                        {
                            if ((mask & bit) != 0)
                            {
                                int oid = (i << 5) + j;
                                exportedBitmap[i] |= bit;
                                markedBitmap[i] &= ~ bit;
                                byte[] obj = storage.Get(oid);
                                int typeOid = ObjectHeader.GetType(obj, 0);
                                ClassDescriptor desc = storage.FindClassDescriptor(typeOid);
                                if (desc.cls == typeof(Btree))
                                {
                                    ExportIndex(oid, obj, "TenderBaseImpl.Btree");
                                }
                                else if (desc.cls == typeof(BitIndexImpl))
                                {
                                    ExportIndex(oid, obj, "TenderBaseImpl.BitIndexImpl");
                                }
                                else if (desc.cls == typeof(PersistentSet))
                                {
                                    ExportSet(oid, obj);
                                }
                                else if (desc.cls == typeof(BtreeFieldIndex))
                                {
                                    ExportFieldIndex(oid, obj);
                                }
                                else if (desc.cls == typeof(BtreeMultiFieldIndex))
                                {
                                    ExportMultiFieldIndex(oid, obj);
                                }
                                else
                                {
                                    string className = ExportIdentifier(desc.name);
                                    writer.Write(" <" + className + " id=\"" + oid + "\">\n");
                                    ExportObject(desc, obj, ObjectHeader.Sizeof, 2);
                                    writer.Write(" </" + className + ">\n");
                                }
                                nExportedObjects += 1;
                            }
                        }
                    }
                }
            }
            while (nExportedObjects != 0);
            writer.Write("</database>\n");
            writer.Flush(); // writer should be closed by calling code
        }

        internal string ExportIdentifier(string name)
        {
            return name.Replace('$', '-');
        }

        internal void ExportSet(int oid, byte[] data)
        {
            Btree btree = new Btree(data, ObjectHeader.Sizeof);
            storage.AssignOid(btree, oid);
            writer.Write(" <TenderBaseImpl.PersistentSet id=\"" + oid + "\">\n");
            btree.Export(this);
            writer.Write(" </TenderBaseImpl.PersistentSet>\n");
        }

        internal void ExportIndex(int oid, byte[] data, string name)
        {
            Btree btree = new Btree(data, ObjectHeader.Sizeof);
            storage.AssignOid(btree, oid);
            writer.Write(" <" + name + " id=\"" + oid + "\" unique=\"" + (btree.unique ? '1' : '0') + "\" type=\"" + ClassDescriptor.signature[btree.type] + "\">\n");
            btree.Export(this);
            writer.Write(" </" + name + ">\n");
        }

        internal void ExportFieldIndex(int oid, byte[] data)
        {
            Btree btree = new Btree(data, ObjectHeader.Sizeof);
            storage.AssignOid(btree, oid);
            writer.Write(" <TenderBaseImpl.BtreeFieldIndex id=\"" + oid + "\" unique=\"" + (btree.unique ? '1' : '0') + "\" class=");
            int offs = ExportString(data, Btree.Sizeof);
            writer.Write(" field=");
            offs = ExportString(data, offs);
            writer.Write(" autoinc=\"" + Bytes.Unpack8(data, offs) + "\">\n");
            btree.Export(this);
            writer.Write(" </TenderBaseImpl.BtreeFieldIndex>\n");
        }

        internal void ExportMultiFieldIndex(int oid, byte[] data)
        {
            Btree btree = new Btree(data, ObjectHeader.Sizeof);
            storage.AssignOid(btree, oid);
            writer.Write(" <TenderBaseImpl.BtreeMultiFieldIndex id=\"" + oid + "\" unique=\"" + (btree.unique ? '1' : '0') + "\" class=");
            int offs = ExportString(data, Btree.Sizeof);
            int nFields = Bytes.Unpack4(data, offs);
            offs += 4;
            for (int i = 0; i < nFields; i++)
            {
                writer.Write(" field" + i + "=");
                offs = ExportString(data, offs);
            }
            writer.Write(">\n");
            int nTypes = Bytes.Unpack4(data, offs);
            offs += 4;
            compoundKeyTypes = new int[nTypes];
            for (int i = 0; i < nTypes; i++)
            {
                compoundKeyTypes[i] = Bytes.Unpack4(data, offs);
                offs += 4;
            }
            btree.Export(this);
            compoundKeyTypes = null;
            writer.Write(" </TenderBaseImpl.BtreeMultiFieldIndex>\n");
        }

        internal int ExportKey(byte[] body, int offs, int size, int type)
        {
            switch (type)
            {
                case ClassDescriptor.tpBoolean:
                    writer.Write(body[offs++] != 0 ? "1" : "0");
                    break;

                case ClassDescriptor.tpByte:
                    writer.Write(Convert.ToString(body[offs++]));
                    break;

                case ClassDescriptor.tpChar:
                    writer.Write(Convert.ToString((char) Bytes.Unpack2(body, offs)));
                    offs += 2;
                    break;

                case ClassDescriptor.tpShort:
                    writer.Write(Convert.ToString(Bytes.Unpack2(body, offs)));
                    offs += 2;
                    break;

                case ClassDescriptor.tpInt:
                case ClassDescriptor.tpObject:
                    writer.Write(Convert.ToString(Bytes.Unpack4(body, offs)));
                    offs += 4;
                    break;

                case ClassDescriptor.tpLong:
                    writer.Write(Convert.ToString(Bytes.Unpack8(body, offs)));
                    offs += 8;
                    break;

                case ClassDescriptor.tpFloat:
                    float flt = Bytes.UnpackF4(body, offs);
                    writer.Write(flt.ToString());
                    offs += 4;
                    break;

                case ClassDescriptor.tpDouble:
                    double dbl = Bytes.UnpackF8(body, offs);
                    writer.Write(dbl.ToString());
                    offs += 8;
                    break;

                case ClassDescriptor.tpString:
                    for (int i = 0; i < size; i++)
                    {
                        ExportChar((char) Bytes.Unpack2(body, offs));
                        offs += 2;
                    }
                    break;

                case ClassDescriptor.tpArrayOfByte:
                    for (int i = 0; i < size; i++)
                    {
                        byte b = body[offs++];
                        writer.Write(hexDigit[(SupportClass.URShift(b, 4)) & 0xF]);
                        writer.Write(hexDigit[b & 0xF]);
                    }
                    break;

                case ClassDescriptor.tpDate:
                {
                    long msec = Bytes.Unpack8(body, offs);
                    offs += 8;
                    if (msec >= 0)
                    {
                        //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                        // TODOPORT: writer.Write(SupportClass.FormatDateTime(XMLImporter.httpFormatter, new System.DateTime(msec)));
                        throw new NotImplementedException();
                    }
                    else
                    {
                        writer.Write("null");
                    }
                    break;
                }

                default:
                    Assert.That(false);
                    break;
            }

            return offs;
        }

        internal void ExportCompoundKey(byte[] body, int offs, int size, int type)
        {
            Assert.That(type == ClassDescriptor.tpArrayOfByte);
            int end = offs + size;
            for (int i = 0; i < compoundKeyTypes.Length; i++)
            {
                type = compoundKeyTypes[i];
                if (type == ClassDescriptor.tpArrayOfByte || type == ClassDescriptor.tpString)
                {
                    size = Bytes.Unpack4(body, offs);
                    offs += 4;
                }
                writer.Write(" key" + i + "=\"");
                offs = ExportKey(body, offs, size, type);
                writer.Write("\"");
            }
            Assert.That(offs == end);
        }

        internal void ExportAssoc(int oid, byte[] body, int offs, int size, int type)
        {
            writer.Write(" <ref id=\"" + oid + "\"");
            if ((exportedBitmap[oid >> 5] & (1 << (oid & 31))) == 0)
            {
                markedBitmap[oid >> 5] |= 1 << (oid & 31);
            }
            if (compoundKeyTypes != null)
            {
                ExportCompoundKey(body, offs, size, type);
            }
            else
            {
                writer.Write(" key=\"");
                ExportKey(body, offs, size, type);
                writer.Write("\"");
            }
            writer.Write("/>\n");
        }

        internal void Indentation(int indent)
        {
            while (--indent >= 0)
            {
                writer.Write(' ');
            }
        }

        internal void ExportChar(char ch)
        {
            switch (ch)
            {
                case '<':
                    writer.Write("&lt;");
                    break;

                case '>':
                    writer.Write("&gt;");
                    break;

                case '&':
                    writer.Write("&amp;");
                    break;

                case '"':
                    writer.Write("&quot;");
                    break;

                default:
                    writer.Write(ch);
                    break;
            }
        }

        internal int ExportString(byte[] body, int offs)
        {
            int len = Bytes.Unpack4(body, offs);
            offs += 4;
            if (len >= 0)
            {
                writer.Write("\"");
                while (--len >= 0)
                {
                    ExportChar((char) Bytes.Unpack2(body, offs));
                    offs += 2;
                }
                writer.Write("\"");
            }
            else if (len < -1)
            {
                writer.Write("\"");
                string s;
                if (storage.encoding != null)
                {
                    string tempStr;
                    //UPGRADE_TODO: The differences in the Format of parameters for constructor 'java.lang.String.String' may cause compilation errors.
                    tempStr = System.Text.Encoding.GetEncoding(storage.encoding).GetString(body);
                    s = new string(tempStr.ToCharArray(), offs, -len - 2);
                }
                else
                {
                    s = new string(SupportClass.ToCharArray(body), offs, -len - 2);
                }
                offs -= (len + 2);
                for (int i = 0, n = s.Length; i < n; i++)
                {
                    ExportChar(s[i]);
                }
            }
            else
            {
                writer.Write("null");
            }
            return offs;
        }

        internal static readonly char[] hexDigit = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        internal int ExportBinary(byte[] body, int offs)
        {
            int len = Bytes.Unpack4(body, offs);
            offs += 4;
            if (len < 0)
            {
                if (len == -2 - ClassDescriptor.tpObject)
                {
                    ExportRef(Bytes.Unpack4(body, offs));
                    offs += 4;
                }
                else if (len < -1)
                {
                    writer.Write("\"#");
                    writer.Write(hexDigit[-2 - len]);
                    len = ClassDescriptor.Sizeof[-2 - len];
                    while (--len >= 0)
                    {
                        byte b = body[offs++];
                        writer.Write(hexDigit[(SupportClass.URShift(b, 4)) & 0xF]);
                        writer.Write(hexDigit[b & 0xF]);
                    }
                    writer.Write('\"');
                }
                else
                {
                    writer.Write("null");
                }
            }
            else
            {
                writer.Write('\"');
                while (--len >= 0)
                {
                    byte b = body[offs++];
                    writer.Write(hexDigit[(SupportClass.URShift(b, 4)) & 0xF]);
                    writer.Write(hexDigit[b & 0xF]);
                }
                writer.Write('\"');
            }
            return offs;
        }

        internal void ExportRef(int oid)
        {
            writer.Write("<ref id=\"" + oid + "\"/>");
            if (oid != 0 && (exportedBitmap[oid >> 5] & (1 << (oid & 31))) == 0)
            {
                markedBitmap[oid >> 5] |= 1 << (oid & 31);
            }
        }

        internal int ExportObject(ClassDescriptor desc, byte[] body, int offs, int indent)
        {
            ClassDescriptor.FieldDescriptor[] all = desc.allFields;

            for (int i = 0, n = all.Length; i < n; i++)
            {
                ClassDescriptor.FieldDescriptor fd = all[i];
                Indentation(indent);
                string fieldName = ExportIdentifier(fd.fieldName);
                writer.Write("<" + fieldName + ">");
                switch (fd.type)
                {
                    case ClassDescriptor.tpBoolean:
                        writer.Write(body[offs++] != 0 ? "1" : "0");
                        break;

                    case ClassDescriptor.tpByte:
                        writer.Write(Convert.ToString((byte) body[offs++]));
                        break;

                    case ClassDescriptor.tpChar:
                        writer.Write(Convert.ToString((char) Bytes.Unpack2(body, offs)));
                        offs += 2;
                        break;

                    case ClassDescriptor.tpShort:
                        writer.Write(Convert.ToString(Bytes.Unpack2(body, offs)));
                        offs += 2;
                        break;

                    case ClassDescriptor.tpInt:
                        writer.Write(Convert.ToString(Bytes.Unpack4(body, offs)));
                        offs += 4;
                        break;

                    case ClassDescriptor.tpLong:
                        writer.Write(Convert.ToString(Bytes.Unpack8(body, offs)));
                        offs += 8;
                        break;

                    case ClassDescriptor.tpFloat:
                        float flt = Bytes.UnpackF4(body, offs);
                        writer.Write(flt.ToString());
                        offs += 4;
                        break;

                    case ClassDescriptor.tpDouble:
                        double dbl = Bytes.UnpackF8(body, offs);
                        writer.Write(dbl.ToString());
                        offs += 8;
                        break;

                    case ClassDescriptor.tpString:
                        offs = ExportString(body, offs);
                        break;

                    case ClassDescriptor.tpDate:
                    {
                        long msec = Bytes.Unpack8(body, offs);
                        offs += 8;
                        if (msec >= 0)
                        {
                            //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                            // TODOPORT: writer.Write("\"" + SupportClass.FormatDateTime(XMLImporter.httpFormatter, new DateTime(msec)) + "\"");
                            throw new NotImplementedException();
                        }
                        else
                        {
                            writer.Write("null");
                        }
                        break;
                    }

                    case ClassDescriptor.tpObject:
                        ExportRef(Bytes.Unpack4(body, offs));
                        offs += 4;
                        break;

                    case ClassDescriptor.tpValue:
                        writer.Write('\n');
                        offs = ExportObject(fd.valueDesc, body, offs, indent + 1);
                        Indentation(indent);
                        break;

                    case ClassDescriptor.tpRaw:
                    case ClassDescriptor.tpArrayOfByte:
                        offs = ExportBinary(body, offs);
                        break;

                    case ClassDescriptor.tpArrayOfBoolean:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + (body[offs++] != 0 ? "1" : "0") + "</element>\n");
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfChar:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + (Bytes.Unpack2(body, offs) & 0xFFFF) + "</element>\n");
                                offs += 2;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfShort:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + Bytes.Unpack2(body, offs) + "</element>\n");
                                offs += 2;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfInt:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + Bytes.Unpack4(body, offs) + "</element>\n");
                                offs += 4;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfLong:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + Bytes.Unpack8(body, offs) + "</element>\n");
                                offs += 8;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfFloat:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + Bytes.UnpackF4(body, offs) + "</element>\n");
                                offs += 4;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfDouble:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>" + Bytes.UnpackF8(body, offs) + "</element>\n");
                                offs += 8;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfDate:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                long msec = Bytes.Unpack8(body, offs);
                                offs += 8;
                                if (msec >= 0)
                                {
                                    writer.Write("<element>\"");
                                    //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                                    // TODOPORT: writer.Write(SupportClass.FormatDateTime(XMLImporter.httpFormatter, new DateTime(msec)));
                                    writer.Write("\"</element>\n");
                                    throw new NotImplementedException();
                                }
                                else
                                {
                                    writer.Write("<element>null</element>\n");
                                }
                            }
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfString:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>");
                                offs = ExportString(body, offs);
                                writer.Write("</element>\n");
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpLink:
                    case ClassDescriptor.tpArrayOfObject:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                int oid = Bytes.Unpack4(body, offs);
                                if (oid != 0 && (exportedBitmap[oid >> 5] & (1 << (oid & 31))) == 0)
                                {
                                    markedBitmap[oid >> 5] |= 1 << (oid & 31);
                                }
                                writer.Write("<element><ref id=\"" + oid + "\"/></element>\n");
                                offs += 4;
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfValue:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>\n");
                                offs = ExportObject(fd.valueDesc, body, offs, indent + 2);
                                Indentation(indent + 1);
                                writer.Write("</element>\n");
                            }
                            Indentation(indent);
                        }
                        break;
                    }

                    case ClassDescriptor.tpArrayOfRaw:
                    {
                        int len = Bytes.Unpack4(body, offs);
                        offs += 4;
                        if (len < 0)
                        {
                            writer.Write("null");
                        }
                        else
                        {
                            writer.Write('\n');
                            while (--len >= 0)
                            {
                                Indentation(indent + 1);
                                writer.Write("<element>");
                                offs = ExportBinary(body, offs);
                                writer.Write("</element>\n");
                            }
                            Indentation(indent);
                        }
                        break;
                    }
                    }
                writer.Write("</" + fieldName + ">\n");
            }
            return offs;
        }


        private StorageImpl storage;
        //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Writer' and 'System.IO.StreamWriter' may cause compilation errors.
        private System.IO.StreamWriter writer;
        private int[] markedBitmap;
        private int[] exportedBitmap;
        private int[] compoundKeyTypes;
    }
}
#endif

