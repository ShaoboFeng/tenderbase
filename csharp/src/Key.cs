namespace TenderBase
{
    using System;
    using ClassDescriptor = TenderBaseImpl.ClassDescriptor;
    
    /// <summary> Class for specifying key value (needed to access obejct by key usig index)</summary>
    public class Key
    {
        public int type;

        public int ival;
        public long lval;
        public double dval;
        public object oval;

        public int inclusion;

        /// <summary> Constructor of boolean key (boundary is inclusive)</summary>
        public Key(bool v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of byte key (boundary is inclusive)</summary>
        public Key(byte v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of char key (boundary is inclusive)</summary>
        public Key(char v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of short key (boundary is inclusive)</summary>
        public Key(short v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of int key (boundary is inclusive)</summary>
        public Key(int v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of long key (boundary is inclusive)</summary>
        public Key(long v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of float key (boundary is inclusive)</summary>
        public Key(float v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of double key (boundary is inclusive)</summary>
        public Key(double v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of date key (boundary is inclusive)</summary>
        //UPGRADE_NOTE: ref keyword was added to struct-type parameters.
        public Key(ref DateTime v)
            : this(ref v, true)
        {
        }

        /// <summary> Constructor of string key (boundary is inclusive)</summary>
        public Key(string v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of array of char key (boundary is inclusive)</summary>
        public Key(char[] v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of array of byte key (boundary is inclusive)</summary>
        public Key(byte[] v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of key of user defined type (boundary is inclusive)</summary>
        /// <param name="v">user defined value
        /// </param>
        public Key(IComparable v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of compound key (boundary is inclusive)</summary>
        /// <param name="v">array of compound key values
        /// </param>
        public Key(object[] v)
            : this(v, true)
        {
        }

        /// <summary> Constructor of compound key with two values (boundary is inclusive)</summary>
        /// <param name="v1">first value of compound key
        /// </param>
        /// <param name="v2">second value of compound key
        /// </param>
        public Key(object v1, object v2)
            : this(new object[] { v1, v2 }, true)
        {
        }

        /// <summary> Constructor of key with persistent object reference (boundary is inclusive)</summary>
        public Key(IPersistent v)
            : this(v, true)
        {
        }

        private Key(int type, long lval, double dval, object oval, bool inclusive)
        {
            this.type = type;
            this.ival = (int) lval;
            this.lval = lval;
            this.dval = dval;
            this.oval = oval;
            this.inclusion = inclusive ? 1 : 0;
        }

        /// <summary> Constructor of boolean key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(bool v, bool inclusive)
            : this(ClassDescriptor.tpBoolean, v ? 1 : 0, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of byte key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(byte v, bool inclusive)
            : this(ClassDescriptor.tpByte, v, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of char key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(char v, bool inclusive)
            : this(ClassDescriptor.tpChar, v, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of short key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(short v, bool inclusive)
            : this(ClassDescriptor.tpShort, v, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of int key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(int v, bool inclusive)
            : this(ClassDescriptor.tpInt, v, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of long key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(long v, bool inclusive)
            : this(ClassDescriptor.tpLong, v, 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of float key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(float v, bool inclusive)
            : this(ClassDescriptor.tpFloat, 0, v, null, inclusive)
        {
        }

        /// <summary> Constructor of double key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(double v, bool inclusive)
            : this(ClassDescriptor.tpDouble, 0, v, null, inclusive)
        {
        }

        /// <summary> Constructor of date key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
        //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
        //UPGRADE_NOTE: ref keyword was added to struct-type parameters.
        public Key(ref DateTime v, bool inclusive)
            : this(ClassDescriptor.tpDate, v.ToBinary(), 0.0, null, inclusive)
        {
        }

        /// <summary> Constructor of string key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(string v, bool inclusive)
            : this(ClassDescriptor.tpString, 0, 0.0, v, inclusive)
        {
        }

        /// <summary> Constructor of array of char key</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(char[] v, bool inclusive)
            : this(ClassDescriptor.tpString, 0, 0.0, v, inclusive)
        {
        }

        /// <summary> Constructor of key with persistent object reference</summary>
        /// <param name="v">key value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(IPersistent v, bool inclusive)
            : this(ClassDescriptor.tpObject, v == null ? 0 : v.Oid, 0.0, v, inclusive)
        {
        }

        /// <summary> Constructor of compound key</summary>
        /// <param name="v">array of key values
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(object[] v, bool inclusive)
            : this(ClassDescriptor.tpArrayOfObject, 0, 0.0, v, inclusive)
        {
        }

        /// <summary> Constructor of key of used defined type</summary>
        /// <param name="v">array of key values
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(System.IComparable v, bool inclusive)
            : this(ClassDescriptor.tpRaw, 0, 0.0, v, inclusive)
        {
        }

        /// <summary> Constructor of compound key with two values</summary>
        /// <param name="v1">first value of compound key
        /// </param>
        /// <param name="v2">second value of compound key
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(object v1, object v2, bool inclusive)
            : this(new object[] { v1, v2 }, inclusive)
        {
        }

        /// <summary> Constructor of byte array key</summary>
        /// <param name="v">byte array value
        /// </param>
        /// <param name="inclusive">whether boundary is inclusive or exclusive
        /// </param>
        public Key(byte[] v, bool inclusive)
            : this(ClassDescriptor.tpArrayOfByte, 0, 0.0, v, inclusive)
        {
        }
    }
}

