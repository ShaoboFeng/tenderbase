namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    class PagePool
    {
        internal LRU lru;
        internal Page freePages;
        internal Page[] hashTable;
        internal int poolSize;
        internal bool autoExtended;
        internal IFile file;

        internal int nDirtyPages;
        internal Page[] dirtyPages;

        internal bool flushing;

        internal const int INFINITE_POOL_INITIAL_SIZE = 8;

        internal PagePool(int poolSize)
        {
            if (poolSize == 0)
            {
                autoExtended = true;
                poolSize = INFINITE_POOL_INITIAL_SIZE;
            }
            this.poolSize = poolSize;
        }

        internal Page Find(long addr, int state)
        {
            //Assert.That((addr & (Page.pageSize-1)) == 0);
            Page pg;
            int pageNo = (int) (SupportClass.URShift(addr, Page.pageBits));
            int hashCode = pageNo % poolSize;

            lock (this)
            {
                for (pg = hashTable[hashCode]; pg != null; pg = pg.collisionChain)
                {
                    if (pg.offs == addr)
                    {
                        if (pg.accessCount++ == 0)
                        {
                            pg.Unlink();
                        }
                        break;
                    }
                }

                if (pg == null)
                {
                    pg = freePages;
                    if (pg != null)
                    {
                        freePages = (Page) pg.next;
                    }
                    else if (autoExtended)
                    {
                        if (pageNo >= poolSize)
                        {
                            int newPoolSize = pageNo >= poolSize * 2 ? pageNo + 1 : poolSize * 2;
                            Page[] newHashTable = new Page[newPoolSize];
                            Array.Copy(hashTable, 0, newHashTable, 0, hashTable.Length);
                            hashTable = newHashTable;
                            poolSize = newPoolSize;
                        }
                        pg = new Page();
                        hashCode = pageNo;
                    }
                    else
                    {
                        Assert.That("unfixed page available", lru.prev != lru);
                        pg = (Page) lru.prev;
                        pg.Unlink();
                        lock (pg)
                        {
                            if ((pg.state & Page.psDirty) != 0)
                            {
                                pg.state = 0;
                                file.Write(pg.offs, pg.data);
                                if (!flushing)
                                {
                                    dirtyPages[pg.writeQueueIndex] = dirtyPages[--nDirtyPages];
                                    dirtyPages[pg.writeQueueIndex].writeQueueIndex = pg.writeQueueIndex;
                                }
                            }
                        }
                        int h = (int) (pg.offs >> Page.pageBits) % poolSize;
                        Page curr = hashTable[h], prev = null;
                        while (curr != pg)
                        {
                            prev = curr;
                            curr = curr.collisionChain;
                        }
                        if (prev == null)
                        {
                            hashTable[h] = pg.collisionChain;
                        }
                        else
                        {
                            prev.collisionChain = pg.collisionChain;
                        }
                    }
                    pg.accessCount = 1;
                    pg.offs = addr;
                    pg.state = Page.psRaw;
                    pg.collisionChain = hashTable[hashCode];
                    hashTable[hashCode] = pg;
                }

                if ((pg.state & Page.psDirty) == 0 && (state & Page.psDirty) != 0)
                {
                    Assert.That(!flushing);
                    if (nDirtyPages >= dirtyPages.Length)
                    {
                        Page[] newDirtyPages = new Page[nDirtyPages * 2];
                        Array.Copy(dirtyPages, 0, newDirtyPages, 0, dirtyPages.Length);
                        dirtyPages = newDirtyPages;
                    }
                    dirtyPages[nDirtyPages] = pg;
                    pg.writeQueueIndex = nDirtyPages++;
                    pg.state |= Page.psDirty;
                }

                if ((pg.state & Page.psRaw) != 0)
                {
                    if (file.Read(pg.offs, pg.data) < Page.pageSize)
                    {
                        for (int i = 0; i < Page.pageSize; i++)
                        {
                            pg.data[i] = 0;
                        }
                    }
                    pg.state &= ~ Page.psRaw;
                }
            }
            return pg;
        }

        internal void Copy(long dst, long src, long size)
        {
            int dstOffs = (int) dst & (Page.pageSize - 1);
            int srcOffs = (int) src & (Page.pageSize - 1);
            dst -= dstOffs;
            src -= srcOffs;
            Page dstPage = Find(dst, Page.psDirty);
            Page srcPage = Find(src, 0);
            do
            {
                if (dstOffs == Page.pageSize)
                {
                    Unfix(dstPage);
                    dst += Page.pageSize;
                    dstPage = Find(dst, Page.psDirty);
                    dstOffs = 0;
                }

                if (srcOffs == Page.pageSize)
                {
                    Unfix(srcPage);
                    src += Page.pageSize;
                    srcPage = Find(src, 0);
                    srcOffs = 0;
                }

                long len = size;
                if (len > Page.pageSize - srcOffs)
                {
                    len = Page.pageSize - srcOffs;
                }

                if (len > Page.pageSize - dstOffs)
                {
                    len = Page.pageSize - dstOffs;
                }

                Array.Copy(srcPage.data, srcOffs, dstPage.data, dstOffs, (int) len);
                srcOffs = (int) (srcOffs + len);
                dstOffs = (int) (dstOffs + len);
                size -= len;
            }
            while (size != 0);

            Unfix(dstPage);
            Unfix(srcPage);
        }

        internal void Write(long dstPos, byte[] src)
        {
            Assert.That((dstPos & (Page.pageSize - 1)) == 0);
            Assert.That((src.Length & (Page.pageSize - 1)) == 0);
            for (int i = 0; i < src.Length; )
            {
                Page pg = Find(dstPos, Page.psDirty);
                byte[] dst = pg.data;
                for (int j = 0; j < Page.pageSize; j++)
                {
                    dst[j] = src[i++];
                }
                Unfix(pg);
                dstPos += Page.pageSize;
            }
        }

        internal void Open(IFile f)
        {
            file = f;
            hashTable = new Page[poolSize];
            dirtyPages = new Page[poolSize];
            nDirtyPages = 0;
            lru = new LRU();
            freePages = null;
            if (!autoExtended)
            {
                for (int i = poolSize; --i >= 0; )
                {
                    Page pg = new Page();
                    pg.next = freePages;
                    freePages = pg;
                }
            }
        }

        internal void Close()
        {
            lock (this)
            {
                file.Close();
                hashTable = null;
                dirtyPages = null;
                lru = null;
                freePages = null;
            }
        }

        internal void Unfix(Page pg)
        {
            lock (this)
            {
                Assert.That(pg.accessCount > 0);
                if (--pg.accessCount == 0)
                {
                    lru.Link(pg);
                }
            }
        }

        internal void Modify(Page pg)
        {
            lock (this)
            {
                Assert.That(pg.accessCount > 0);
                if ((pg.state & Page.psDirty) == 0)
                {
                    Assert.That(!flushing);
                    pg.state |= Page.psDirty;
                    if (nDirtyPages >= dirtyPages.Length)
                    {
                        Page[] newDirtyPages = new Page[nDirtyPages * 2];
                        Array.Copy(dirtyPages, 0, newDirtyPages, 0, dirtyPages.Length);
                        dirtyPages = newDirtyPages;
                    }
                    dirtyPages[nDirtyPages] = pg;
                    pg.writeQueueIndex = nDirtyPages++;
                }
            }
        }

        internal Page GetPage(long addr)
        {
            return Find(addr, 0);
        }

        internal Page PutPage(long addr)
        {
            return Find(addr, Page.psDirty);
        }

        internal byte[] Get(long pos)
        {
            Assert.That(pos != 0);
            int offs = (int) pos & (Page.pageSize - 1);
            Page pg = Find(pos - offs, 0);
            int size = ObjectHeader.GetSize(pg.data, offs);
            Assert.That(size >= ObjectHeader.Sizeof);
            byte[] obj = new byte[size];
            int dst = 0;
            while (size > Page.pageSize - offs)
            {
                Array.Copy(pg.data, offs, obj, dst, Page.pageSize - offs);
                Unfix(pg);
                size -= (Page.pageSize - offs);
                pos += Page.pageSize - offs;
                dst += Page.pageSize - offs;
                pg = Find(pos, 0);
                offs = 0;
            }
            Array.Copy(pg.data, offs, obj, dst, size);
            Unfix(pg);
            return obj;
        }

        internal void Put(long pos, byte[] obj)
        {
            Put(pos, obj, obj.Length);
        }

        internal void Put(long pos, byte[] obj, int size)
        {
            int offs = (int) pos & (Page.pageSize - 1);
            Page pg = Find(pos - offs, Page.psDirty);
            int src = 0;
            while (size > Page.pageSize - offs)
            {
                Array.Copy(obj, src, pg.data, offs, Page.pageSize - offs);
                Unfix(pg);
                size -= (Page.pageSize - offs);
                pos += Page.pageSize - offs;
                src += Page.pageSize - offs;
                pg = Find(pos, Page.psDirty);
                offs = 0;
            }
            Array.Copy(obj, src, pg.data, offs, size);
            Unfix(pg);
        }

        internal virtual void Flush()
        {
            lock (this)
            {
                flushing = true;
                System.Array.Sort(dirtyPages, 0, nDirtyPages - 0);
            }
            for (int i = 0; i < nDirtyPages; i++)
            {
                Page pg = dirtyPages[i];
                lock (pg)
                {
                    if ((pg.state & Page.psDirty) != 0)
                    {
                        file.Write(pg.offs, pg.data);
                        pg.state &= ~ Page.psDirty;
                    }
                }
            }
            file.Sync();
            nDirtyPages = 0;
            flushing = false;
        }
    }
}

