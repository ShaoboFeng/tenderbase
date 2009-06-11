namespace TenderBaseImpl
{
    using System;
    using StorageError = TenderBase.StorageError;
    
    public class ByteBuffer
    {
        public const int INITIAL_SIZE = 64;

        public void Extend(int size)
        {
            if (size > arr.Length)
            {
                int newLen = size > arr.Length * 2 ? size : arr.Length * 2;
                byte[] newArr = new byte[newLen];
                Array.Copy(arr, 0, newArr, 0, used);
                arr = newArr;
            }
            used = size;
        }

        internal byte[] ToArray()
        {
            byte[] result = new byte[used];
            Array.Copy(arr, 0, result, 0, used);
            return result;
        }

        internal int PackString(int dst, string val, string encoding)
        {
            if (val == null)
            {
                Extend(dst + 4);
                Bytes.Pack4(arr, dst, -1);
                dst += 4;
            }
            else
            {
                int length = val.Length;
                if (encoding == null)
                {
                    Extend(dst + 4 + 2 * length);
                    Bytes.Pack4(arr, dst, length);
                    dst += 4;
                    for (int i = 0; i < length; i++)
                    {
                        Bytes.Pack2(arr, dst, (short) val[i]);
                        dst += 2;
                    }
                }
                else
                {
                    try
                    {
                        byte[] bytes = System.Text.Encoding.GetEncoding(encoding).GetBytes(val);
                        Extend(dst + 4 + bytes.Length);
                        Bytes.Pack4(arr, dst, -2 - bytes.Length);
                        Array.Copy(bytes, 0, arr, dst + 4, bytes.Length);
                        dst += 4 + bytes.Length;
                    }
                    catch (System.IO.IOException)
                    {
                        throw new StorageError(StorageError.UNSUPPORTED_ENCODING);
                    }
                }
            }
            return dst;
        }

        internal ByteBuffer()
        {
            arr = new byte[INITIAL_SIZE];
        }

        public byte[] arr;
        public int used;
    }
}

