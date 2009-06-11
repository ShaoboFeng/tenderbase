#if !OMIT_PATRICIA_TRIE
namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Convert different type of keys to 64-bit long value used in PATRICIA trie
    /// (Practical Algorithm To Retrieve Information Coded In Alphanumeric)
    /// </summary>
    public class PatriciaTrieKey
    {
        /// <summary> Bit mask representing bit vector.
        /// The last digit of the key is the right most bit of the mask
        /// </summary>
        public long mask;

        /// <summary> Length of bit vector (can not be larger than 64)</summary>
        public int length;

        public PatriciaTrieKey(long mask, int length)
        {
            this.mask = mask;
            this.length = length;
        }

        public static PatriciaTrieKey FromIpAddress(System.Net.IPAddress addr)
        {
            byte[] bytes = addr.GetAddressBytes();
            UInt64 mask = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                mask = (mask << 8) | bytes[i];
            }
            return new PatriciaTrieKey((long)mask, bytes.Length * 8);
        }

        public static PatriciaTrieKey FromIpAddress(string addr)
        {
            long mask = 0;
            int pos = 0;
            int len = 0;
            do
            {
                //UPGRADE_WARNING: Method 'java.lang.String.indexOf' was converted to 'string.IndexOf' which may throw an exception.
                int dot = addr.IndexOf('.', pos);
                string part = dot < 0 ? addr.Substring(pos) : addr.Substring(pos, (dot) - (pos));
                pos = dot + 1;
                //UPGRADE_TODO: Method 'java.lang.Integer.ParseInt' was converted to 'Convert.ToInt32' which has a different behavior.
                int b = Convert.ToInt32(part, 10);
                mask = (mask << 8) | (byte)(b & 0xFF);
                len += 8;
            }
            while (pos > 0);
            return new PatriciaTrieKey(mask, len);
        }

        public static PatriciaTrieKey FromDecimalDigits(string digits)
        {
            long mask = 0;
            int n = digits.Length;
            Assert.That(n <= 16);
            for (int i = 0; i < n; i++)
            {
                char ch = digits[i];
                Assert.That(ch >= '0' && ch <= '9');
                mask = (mask << 4) | (byte)(ch - '0');
            }
            return new PatriciaTrieKey(mask, n * 4);
        }

        public static PatriciaTrieKey From7bitString(string str)
        {
            long mask = 0;
            int n = str.Length;
            Assert.That(n * 7 <= 64);
            for (int i = 0; i < n; i++)
            {
                char ch = str[i];
                mask = (mask << 7) | (byte)(ch & 0x7F);
            }
            return new PatriciaTrieKey(mask, n * 7);
        }

        public static PatriciaTrieKey From8bitString(string str)
        {
            long mask = 0;
            int n = str.Length;
            Assert.That(n <= 8);
            for (int i = 0; i < n; i++)
            {
                char ch = str[i];
                mask = (mask << 8) | (byte)(ch & 0xFF);
            }
            return new PatriciaTrieKey(mask, n * 8);
        }

        public static PatriciaTrieKey FromByteArray(byte[] arr)
        {
            long mask = 0;
            int n = arr.Length;
            Assert.That(n <= 8);
            for (int i = 0; i < n; i++)
            {
                mask = (mask << 8) | arr[i];
            }
            return new PatriciaTrieKey(mask, n * 8);
        }
    }
}
#endif

