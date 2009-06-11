namespace TenderBaseImpl
{
    using System;

    public interface FastSerializable
    {
        int Pack(ByteBuffer buf, int offs, string encoding);
        int Unpack(byte[] buf, int offs, string encoding);
    }
}

