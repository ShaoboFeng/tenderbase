package org.garret.perst;
import  org.garret.perst.impl.ClassDescriptor;

/**
 * Class for specifying key value (needed to access object by key usig index)
 */
public class Key { 
    public final int     type;

    public final int     ival;
    public final long    lval;
    public final double  dval;
    public final Object  oval;

    public final int     inclusion;

    /**
     * Constructor of boolean key (boundary is inclusive)
     */
    public Key(boolean v) { 
        this(v, true);
    }

    /**
     * Constructor of byte key (boundary is inclusive)
     */
    public Key(byte v) { 
        this(v, true);
    }

    /**
     * Constructor of char key (boundary is inclusive)
     */
    public Key(char v) { 
        this(v, true);
    }

    /**
     * Constructor of short key (boundary is inclusive)
     */
    public Key(short v) { 
        this(v, true);
    }

    /**
     * Constructor of int key (boundary is inclusive)
     */
    public Key(int v) { 
        this(v, true);
    }

    /**
     * Constructor of long key (boundary is inclusive)
     */
    public Key(long v) { 
        this(v, true);
    }

    /**
     * Constructor of float key (boundary is inclusive)
     */
    public Key(float v) { 
        this(v, true);
    }

    /**
     * Constructor of double key (boundary is inclusive)
     */
    public Key(double v) { 
        this(v, true);
    }

    /**
     * Constructor of date key (boundary is inclusive)
     */
    public Key(java.util.Date v) { 
         this(v, true);
    }

    /**
     * Constructor of string key (boundary is inclusive)
     */
    public Key(String v) { 
        this(v, true);
    }

    /**
     * Constructor of array of char key (boundary is inclusive)
     */
    public Key(char[] v) { 
        this(v, true);
    }

    /**
     * Constructor of array of byte key (boundary is inclusive)
     */
    public Key(byte[] v) { 
        this(v, true);
    }

    /**
     * Constructor of key of user defined type (boundary is inclusive)
     * @param v user defined value
     */
    public Key(Comparable v) { 
        this(v, true);
    }    
    /**
     * Constructor of compound key (boundary is inclusive)
     * @param v array of compound key values
     */
    public Key(Object[] v) { 
        this(v, true);
    }    

    /**
     * Constructor of compound key with two values (boundary is inclusive)
     * @param v1 first value of compund key
     * @param v2 second value of compund key
     */
    public Key(Object v1, Object v2) { 
        this(new Object[]{v1, v2}, true);
    }    

    /**
     * Constructor of key with persistent object reference (boundary is inclusive)
     */
    public Key(IPersistent v) { 
        this(v, true);
    }

    private Key(int type, long lval, double dval, Object oval, boolean inclusive) { 
        this.type = type;
        this.ival = (int)lval;
        this.lval = lval;
        this.dval = dval;
        this.oval = oval;
        this.inclusion = inclusive ? 1 : 0;
    }

    /**
     * Constructor of boolean key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
     public Key(boolean v, boolean inclusive) { 
        this(ClassDescriptor.tpBoolean, v ? 1 : 0, 0.0, null, inclusive);
    }

    /**
     * Constructor of byte key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(byte v, boolean inclusive) { 
        this(ClassDescriptor.tpByte, v, 0.0, null, inclusive);
    }

    /**
     * Constructor of char key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(char v, boolean inclusive) { 
        this(ClassDescriptor.tpChar, v, 0.0, null, inclusive);
    }

    /**
     * Constructor of short key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(short v, boolean inclusive) { 
        this(ClassDescriptor.tpShort, v, 0.0, null, inclusive);
    }

    /**
     * Constructor of int key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(int v, boolean inclusive) { 
        this(ClassDescriptor.tpInt, v, 0.0, null, inclusive);
    }

    /**
     * Constructor of long key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(long v, boolean inclusive) { 
        this(ClassDescriptor.tpLong, v, 0.0, null, inclusive);
    }

    /**
     * Constructor of float key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(float v, boolean inclusive) { 
        this(ClassDescriptor.tpFloat, 0, v, null, inclusive);
    }

    /**
     * Constructor of double key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(double v, boolean inclusive) { 
        this(ClassDescriptor.tpDouble, 0, v, null, inclusive);
    }

    /**
     * Constructor of date key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(java.util.Date v, boolean inclusive) { 
        this(ClassDescriptor.tpDate, v == null ? -1 : v.getTime(), 0.0, null, inclusive);
    }

    /**
     * Constructor of string key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(String v, boolean inclusive) { 
        this(ClassDescriptor.tpString, 0, 0.0, v, inclusive);
    }

    /**
     * Constructor of array of char key
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(char[] v, boolean inclusive) { 
        this(ClassDescriptor.tpString, 0, 0.0, v, inclusive);
    }

    /**
     * Constructor of key with persistent object reference
     * @param v key value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(IPersistent v, boolean inclusive) { 
        this(ClassDescriptor.tpObject, v == null ? 0 : v.getOid(), 0.0, v, inclusive);
    }

    /**
     * Constructor of compound key
     * @param v array of key values
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(Object[] v, boolean inclusive) { 
        this(ClassDescriptor.tpArrayOfObject, 0, 0.0, v, inclusive);        
    }    

    /**
     * Constructor of key of used defined type
     * @param v array of key values
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(Comparable v, boolean inclusive) { 
        this(ClassDescriptor.tpRaw, 0, 0.0, v, inclusive);        
    }    

    /**
     * Constructor of compound key with two values
     * @param v1 first value of compund key
     * @param v2 second value of compund key
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(Object v1, Object v2, boolean inclusive) { 
        this(new Object[]{v1, v2}, inclusive);
    }

    /**
     * Constructor of byte array key
     * @param v byte array value
     * @param inclusive whether boundary is inclusive or exclusive
     */
    public Key(byte[] v, boolean inclusive) { 
        this(ClassDescriptor.tpArrayOfByte, 0, 0.0, v, inclusive);        
    }
}


