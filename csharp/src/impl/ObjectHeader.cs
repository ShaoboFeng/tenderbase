namespace TenderBaseImpl
{
    using System;

    class ObjectHeader
    {
        internal const int Sizeof = 8;

        internal static int GetSize(byte[] arr, int offs)
        {
            return Bytes.Unpack4(arr, offs);
        }

        internal static void SetSize(byte[] arr, int offs, int size)
        {
            Bytes.Pack4(arr, offs, size);
        }

        internal static int GetType(byte[] arr, int offs)
        {
            return Bytes.Unpack4(arr, offs + 4);
        }

        internal static void SetType(byte[] arr, int offs, int type)
        {
            Bytes.Pack4(arr, offs + 4, type);
        }
    }
}

