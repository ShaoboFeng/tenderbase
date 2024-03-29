package org.garret.perst.impl;

import org.garret.perst.StorageError;
import java.io.UnsupportedEncodingException;

//
// Class for packing/unpacking data
//
public class Bytes {
    public static short unpack2(byte[] arr, int offs) { 
        return (short)((arr[offs] << 8) | (arr[offs+1] & 0xFF));
    }
    public static int unpack4(byte[] arr, int offs) { 
        return (arr[offs] << 24) | ((arr[offs+1] & 0xFF) << 16)
            | ((arr[offs+2] & 0xFF) << 8) | (arr[offs+3] & 0xFF);
    }
    public static long unpack8(byte[] arr, int offs) { 
        return ((long)unpack4(arr, offs) << 32)
            | (unpack4(arr, offs+4) & 0xFFFFFFFFL);
    }
    public static float unpackF4(byte[] arr, int offs) { 
        return Float.intBitsToFloat(Bytes.unpack4(arr, offs));
    }
    public static double unpackF8(byte[] arr, int offs) { 
        return Double.longBitsToDouble(Bytes.unpack8(arr, offs));
    }
    public static String unpackStr(byte[] arr, int offs, String encoding) { 
        int len = unpack4(arr, offs);        
        if (len >= 0) { 
            char[] chars = new char[len];
            offs += 4;
            for (int i = 0; i < len; i++) { 
                chars[i] = (char)unpack2(arr, offs);
                offs += 2;
            }
            return new String(chars);
        } else if (len < -1) { 
            if (encoding != null) { 
                try { 
                    return new String(arr, offs, -len-2, encoding);
                } catch (UnsupportedEncodingException x) { 
                    throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
                }
            } else { 
                return new String(arr, offs, -len-2);
            }
        }
        return null;
    }

    public static void pack2(byte[] arr, int offs, short val) { 
        arr[offs] = (byte)(val >> 8);
        arr[offs+1] = (byte)val;
    }
    public static void pack4(byte[] arr, int offs, int val) { 
        arr[offs] = (byte)(val >> 24);
        arr[offs+1] = (byte)(val >> 16);
        arr[offs+2] = (byte)(val >> 8);
        arr[offs+3] = (byte)val;
    }
    public static void pack8(byte[] arr, int offs, long val) { 
        pack4(arr, offs, (int)(val >> 32));
        pack4(arr, offs+4, (int)val);
    }
    public static void packF4(byte[] arr, int offs, float val) { 
        pack4(arr, offs,  Float.floatToIntBits(val));
    }
    public static void packF8(byte[] arr, int offs, double val) { 
        pack8(arr, offs, Double.doubleToLongBits(val));
    }
    public static int packStr(byte[] arr, int offs, String str, String encoding) { 
        if (str == null) { 
            Bytes.pack4(arr, offs, -1);
            offs += 4;
        } else if (encoding == null) { 
            int n = str.length();
            Bytes.pack4(arr, offs, n);
            offs += 4;
            for (int i = 0; i < n; i++) {
                Bytes.pack2(arr, offs, (short)str.charAt(i));
                offs += 2;
            }
        } else {
            try { 
                byte[] bytes = str.getBytes(encoding);
                pack4(arr, offs, -2-bytes.length);
                System.arraycopy(bytes, 0, arr, offs+4, bytes.length);
                offs += 4 + bytes.length;
            } catch (UnsupportedEncodingException x) { 
                throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
            }
        }
        return offs;
    }
    public static int sizeof(String str, String encoding) { 
        try {
            if (str == null)
                return 4;
            if (encoding == null)
                return 4 + str.length()*2;
            return 4 + new String(str).getBytes(encoding).length;
        } catch (UnsupportedEncodingException x) { 
            throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
        }
    }
    public static int sizeof(byte[] arr, int offs) { 
        int len = unpack4(arr, offs);        
        if (len >= 0) { 
            return 4 + len*2;
        } else if (len < -1) { 
            return 4-2-len;
        } else { 
            return 4;
        }
    }
}

