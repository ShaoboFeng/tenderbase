namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class Rc4File : IFile
    {
        private void InitBlock()
        {
            cipherBuf = new byte[Page.pageSize];
        }

        private byte[] Key
        {
            set
            {
                for (int counter = 0; counter < 256; ++counter)
                {
                    initState[counter] = (byte) counter;
                }
                int index1 = 0;
                int index2 = 0;
                for (int counter = 0; counter < 256; ++counter)
                {
                    index2 = (value[index1] + initState[counter] + index2) & 0xff;
                    byte temp = initState[counter];
                    initState[counter] = initState[index2];
                    initState[index2] = temp;
                    index1 = (index1 + 1) % value.Length;
                }
            }
        }

        public virtual void Write(long pos, byte[] buf)
        {
            if (pos > length_Renamed_Field)
            {
                if (zeroPage == null)
                {
                    zeroPage = new byte[Page.pageSize];
                    Encrypt(zeroPage, 0, zeroPage, 0, Page.pageSize);
                }
                do
                {
                    file.Write(length_Renamed_Field, zeroPage);
                }
                while ((length_Renamed_Field += Page.pageSize) < pos);
            }
            if (pos == length_Renamed_Field)
            {
                length_Renamed_Field += Page.pageSize;
            }
            Encrypt(buf, 0, cipherBuf, 0, buf.Length);
            file.Write(pos, cipherBuf);
        }

        public virtual int Read(long pos, byte[] buf)
        {
            if (pos < length_Renamed_Field)
            {
                int rc = file.Read(pos, buf);
                Decrypt(buf, 0, buf, 0, rc);
                return rc;
            }
            return 0;
        }

        public Rc4File(string filePath, bool readOnly, bool noFlush, string key)
        {
            InitBlock();
            file = new OSFile(filePath, readOnly, noFlush);
            length_Renamed_Field = file.Length() & ~ (Page.pageSize - 1);
            Key = SupportClass.ToByteArray(key);
        }

        public Rc4File(IFile file, string key)
        {
            InitBlock();
            this.file = file;
            length_Renamed_Field = file.Length() & ~ (Page.pageSize - 1);
            Key = SupportClass.ToByteArray(key);
        }

        private void Encrypt(byte[] clearText, int clearOff, byte[] cipherText, int cipherOff, int len)
        {
            x = y = 0;
            Array.Copy(initState, 0, state, 0, state.Length);
            for (int i = 0; i < len; i++)
            {
                cipherText[cipherOff + i] = (byte) (clearText[clearOff + i] ^ state[NextState()]);
            }
        }

        private void Decrypt(byte[] cipherText, int cipherOff, byte[] clearText, int clearOff, int len)
        {
            x = y = 0;
            Array.Copy(initState, 0, state, 0, state.Length);
            for (int i = 0; i < len; i++)
            {
                clearText[clearOff + i] = (byte) (cipherText[cipherOff + i] ^ state[NextState()]);
            }
        }

        private int NextState()
        {
            x = (x + 1) & 0xff;
            y = (y + state[x]) & 0xff;
            byte temp = state[x];
            state[x] = state[y];
            state[y] = temp;
            return (state[x] + state[y]) & 0xff;
        }

        public virtual void Close()
        {
            file.Close();
        }

        public virtual bool Lock()
        {
            return file.Lock();
        }

        public virtual void Sync()
        {
            file.Sync();
        }

        public virtual long Length()
        {
            return file.Length();
        }

        private IFile file;
        //UPGRADE_NOTE: The initialization of 'cipherBuf' was moved to method 'InitBlock'.
        private byte[] cipherBuf;
        private byte[] initState = new byte[256];
        private byte[] state = new byte[256];
        private int x, y;
        private long length_Renamed_Field;
        private byte[] zeroPage;
    }
}
