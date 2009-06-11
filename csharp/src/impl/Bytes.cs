namespace TenderBaseImpl
{
    using System;
    using System.Text;
    using StorageError = TenderBase.StorageError;
    
    // Class for packing/unpacking data
    public class Bytes
    {
        public static short Unpack2(byte[] arr, int offs)
        {
            return BitConverter.ToInt16(arr, offs);
        }

        public static int Unpack4(byte[] arr, int offs)
        {
            return BitConverter.ToInt32(arr, offs);
        }

        public static long Unpack8(byte[] arr, int offs)
        {
            return BitConverter.ToInt64(arr, offs);
        }

        public static float UnpackF4(byte[] arr, int offs)
        {
            return BitConverter.ToSingle(arr, offs);
        }

        public static double UnpackF8(byte[] arr, int offs)
        {
            return BitConverter.ToDouble(arr, offs);
        }

        public static String UnpackStr(byte[] arr, int offs, String encoding)
        {
            int len = Unpack4(arr, offs);
            if (len >= 0)
            {
                char[] chars = new char[len];
                offs += 4;
                for (int i = 0; i < len; i++)
                {
                    chars[i] = (char) Unpack2(arr, offs);
                    offs += 2;
                }
                return new String(chars);
            }
            else if (len < -1)
            {
                if (encoding != null)
                {
                    try
                    {
                        Encoding enc = Encoding.GetEncoding(encoding);
                        String str = enc.GetString(arr);
                        //Debug.Assert(str.Length == (-len - 2));
                        return str;
                    }
                    catch (System.IO.IOException)
                    {
                        throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
                    }
                }
                else
                {
                    return new string(SupportClass.ToCharArray(arr), offs, -len - 2);
                }
            }
            return null;
        }

        public static void Pack2(byte[] arr, int offs, short val)
        {
            byte[] tmp = BitConverter.GetBytes(val);
            arr[offs] = tmp[0];
            arr[offs + 1] = tmp[1];
        }

        public static void Pack4(byte[] arr, int offs, int val)
        {
            byte[] tmp = BitConverter.GetBytes(val);
            arr[offs]     = tmp[0];
            arr[offs + 1] = tmp[1];
            arr[offs + 2] = tmp[2];
            arr[offs + 3] = tmp[3];
        }

        public static void Pack8(byte[] arr, int offs, long val)
        {
            byte[] tmp = BitConverter.GetBytes(val);
            Array.Copy(tmp, 0, arr, offs, 8);
        }

        public static void PackF4(byte[] arr, int offs, float val)
        {
            byte[] tmp = BitConverter.GetBytes(val);
            Array.Copy(tmp, 0, arr, offs, 4);
        }

        public static void PackF8(byte[] arr, int offs, double val)
        {
            byte[] tmp = BitConverter.GetBytes(val);
            Array.Copy(tmp, 0, arr, offs, 8);
        }

        public static int PackStr(byte[] arr, int offs, String str, String encoding)
        {
            if (str == null)
            {
                Bytes.Pack4(arr, offs, -1);
                offs += 4;
            }
            else if (encoding == null)
            {
                int n = str.Length;
                Bytes.Pack4(arr, offs, n);
                offs += 4;
                for (int i = 0; i < n; i++)
                {
                    Bytes.Pack2(arr, offs, (short) str[i]);
                    offs += 2;
                }
            }
            else
            {
                try
                {
                    Encoding enc = Encoding.GetEncoding(encoding);
                    byte[] bytes = enc.GetBytes(str);
                    Pack4(arr, offs, -2 - bytes.Length);
                    Array.Copy(bytes, 0, arr, offs + 4, bytes.Length);
                    offs += 4 + bytes.Length;
                }
                catch (System.IO.IOException)
                {
                    throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
                }
            }
            return offs;
        }

        // TODOPORT: a better name
        public static int Sizeof(String str, String encoding)
        {
            try
            {
                if (str == null)
                    return 4;
                if (encoding == null)
                    return 4 + str.Length * 2;
                Encoding enc = Encoding.GetEncoding(encoding);
                return 4 + enc.GetByteCount(str);
            }
            catch (System.IO.IOException)
            {
                throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
            }
        }

        // TODOPORT: a better name
        public static int Sizeof(byte[] arr, int offs)
        {
            int len = Unpack4(arr, offs);
            if (len >= 0)
            {
                return 4 + len * 2;
            }
            else if (len < -1)
            {
                return 4 - 2 - len;
            }
            else
            {
                return 4;
            }
        }
    }
}

