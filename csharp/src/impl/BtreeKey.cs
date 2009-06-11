namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    class BtreeKey
    {
        internal Key key;
        internal int oid;
        internal int oldOid;

        internal BtreeKey(Key key, int oid)
        {
            this.key = key;
            this.oid = oid;
        }

        internal void GetStr(Page pg, int i)
        {
            int len = BtreePage.GetKeyStrSize(pg, i);
            int offs = BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, i);
            char[] sval = new char[len];
            for (int j = 0; j < len; j++)
            {
                sval[j] = (char) Bytes.Unpack2(pg.data, offs);
                offs += 2;
            }
            key = new Key(sval);
        }

        internal void GetByteArray(Page pg, int i)
        {
            int len = BtreePage.GetKeyStrSize(pg, i);
            int offs = BtreePage.firstKeyOffs + BtreePage.GetKeyStrOffs(pg, i);
            byte[] bval = new byte[len];
            Array.Copy(pg.data, offs, bval, 0, len);
            key = new Key(bval);
        }

        internal void Extract(Page pg, int offs, int type)
        {
            byte[] data = pg.data;

            switch (type)
            {
                case ClassDescriptor.tpBoolean:
                    key = new Key(data[offs] != 0);
                    break;

                case ClassDescriptor.tpByte:
                    key = new Key(data[offs]);
                    break;

                case ClassDescriptor.tpShort:
                    key = new Key(Bytes.Unpack2(data, offs));
                    break;

                case ClassDescriptor.tpChar:
                    key = new Key((char) Bytes.Unpack2(data, offs));
                    break;

                case ClassDescriptor.tpInt:
                case ClassDescriptor.tpObject:
                    key = new Key(Bytes.Unpack4(data, offs));
                    break;

                case ClassDescriptor.tpLong:
                case ClassDescriptor.tpDate:
                    key = new Key(Bytes.Unpack8(data, offs));
                    break;

                case ClassDescriptor.tpFloat:
                    key = new Key(Bytes.UnpackF4(data, offs));
                    break;

                case ClassDescriptor.tpDouble:
                    key = new Key(Bytes.UnpackF8(data, offs));
                    break;

                default:
                    Assert.Failed("Invalid type");
                    break;
            }
        }

        internal void Pack(Page pg, int i)
        {
            byte[] dst = pg.data;
            switch (key.type)
            {
                case ClassDescriptor.tpBoolean:
                case ClassDescriptor.tpByte:
                    dst[BtreePage.firstKeyOffs + i] = (byte) key.ival;
                    break;

                case ClassDescriptor.tpShort:
                case ClassDescriptor.tpChar:
                    Bytes.Pack2(dst, BtreePage.firstKeyOffs + i * 2, (short) key.ival);
                    break;

                case ClassDescriptor.tpInt:
                case ClassDescriptor.tpObject:
                    Bytes.Pack4(dst, BtreePage.firstKeyOffs + i * 4, key.ival);
                    break;

                case ClassDescriptor.tpLong:
                case ClassDescriptor.tpDate:
                    Bytes.Pack8(dst, BtreePage.firstKeyOffs + i * 8, key.lval);
                    break;

                case ClassDescriptor.tpFloat:
                    Bytes.PackF4(dst, BtreePage.firstKeyOffs + i * 4, (float)key.dval);
                    break;

                case ClassDescriptor.tpDouble:
                    Bytes.PackF8(dst, BtreePage.firstKeyOffs + i * 8, key.dval);
                    break;

                default:
                    Assert.Failed("Invalid type");
                    break;
            }
            Bytes.Pack4(dst, BtreePage.firstKeyOffs + (BtreePage.maxItems - i - 1) * 4, oid);
        }
    }
}
