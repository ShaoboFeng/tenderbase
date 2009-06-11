namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using TenderBase;
    
    public class StorageImpl : Storage
    {
        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'AnonymousClassGenericSortArray' to access its enclosing instance.
        private class AnonymousClassGenericSortArray : GenericSortArray
        {
            public AnonymousClassGenericSortArray(int nObjects, long[] index, int[] oids, StorageImpl enclosingInstance)
            {
                InitBlock(nObjects, index, oids, enclosingInstance);
            }

            private void InitBlock(int nObjects, long[] index, int[] oids, StorageImpl enclosingInstance)
            {
                this.nObjects = nObjects;
                this.index = index;
                this.oids = oids;
                this.enclosingInstance = enclosingInstance;
            }

            //UPGRADE_NOTE: Final variable nObjects was copied into class AnonymousClassGenericSortArray.
            private int nObjects;
            //UPGRADE_NOTE: Final variable index was copied into class AnonymousClassGenericSortArray.
            private long[] index;
            //UPGRADE_NOTE: Final variable oids was copied into class AnonymousClassGenericSortArray.
            private int[] oids;
            private StorageImpl enclosingInstance;

            public StorageImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            public virtual int Size()
            {
                return nObjects;
            }

            public virtual int Compare(int i, int j)
            {
                return index[i] < index[j] ? -1 : (index[i] == index[j] ? 0 : 1);
            }

            public virtual void Swap(int i, int j)
            {
                long t1 = index[i];
                index[i] = index[j];
                index[j] = t1;
                int t2 = oids[i];
                oids[i] = oids[j];
                oids[j] = t2;
            }
        }

        public virtual long UsedSize
        {
            get
            {
                return usedSize;
            }
        }

        public virtual long DatabaseSize
        {
            get
            {
                return header.root[1 - currIndex].size;
            }
        }

        public virtual bool Opened
        {
            get
            {
                return opened;
            }
        }

        public virtual long GcThreshold
        {
            set
            {
                gcThreshold = value;
            }
        }

        [ThreadStatic]
        internal static ThreadTransactionContext TransactionContext = new ThreadTransactionContext();

        //UPGRADE_ISSUE: Class hierarchy differences between 'java.util.Properties' and 'System.Collections.Specialized.NameValueCollection' may cause compilation errors.
        public virtual NameValueCollection Properties
        {
            set
            {
                string val;
                if ((val = value.Get("perst.implicit.values")) != null)
                {
                    ClassDescriptor.treateAnyNonPersistentObjectAsValue = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.serialize.transient.objects")) != null)
                {
                    ClassDescriptor.serializeNonPersistentObjects = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.object.cache.init.size")) != null)
                {
                    objectCacheInitSize = (int) GetIntegerValue(val);
                }
                if ((val = value.Get("perst.object.cache.kind")) != null)
                {
                    cacheKind = val;
                }
                if ((val = value.Get("perst.object.index.init.size")) != null)
                {
                    initIndexSize = (int) GetIntegerValue(val);
                }
                if ((val = value.Get("perst.extension.quantum")) != null)
                {
                    extensionQuantum = GetIntegerValue(val);
                }
                if ((val = value.Get("perst.gc.threshold")) != null)
                {
                    gcThreshold = GetIntegerValue(val);
                }
                if ((val = value.Get("perst.file.readonly")) != null)
                {
                    readOnly = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.file.noflush")) != null)
                {
                    noFlush = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.alternative.btree")) != null)
                {
                    alternativeBtree = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.background.gc")) != null)
                {
                    backgroundGc = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.string.encoding")) != null)
                {
                    encoding = val;
                }
                if ((val = value.Get("perst.lock.file")) != null)
                {
                    lockFile = GetBooleanValue(val);
                }
                if ((val = value.Get("perst.replication.ack")) != null)
                {
                    replicationAck = GetBooleanValue(val);
                }
            }
        }

        /// <summary> Initialial database index size - increasing it reduce number of inde reallocation but increase
        /// initial database size. Should be set before openning connection.
        /// </summary>
        private const int dbDefaultInitIndexSize = 1024;

        /// <summary> Initial capacity of object hash</summary>
        private const int dbDefaultObjectCacheInitSize = 1319;

        /// <summary> Database extension quantum. Memory is allocate by scanning bitmap. If there is no
        /// large enough hole, then database is extended by the value of dbDefaultExtensionQuantum
        /// This parameter should not be smaller than dbFirstUserId
        /// </summary>
        private const long dbDefaultExtensionQuantum = 1024 * 1024;

        private const int dbDatabaseOffsetBits = 32; // up to 4 gigabyte
        private const int dbLargeDatabaseOffsetBits = 40; // up to 1 terabyte

        private const int dbAllocationQuantumBits = 5;
        private const int dbAllocationQuantum = 1 << dbAllocationQuantumBits;
        private const int dbBitmapSegmentBits = Page.pageBits + 3 + dbAllocationQuantumBits;
        //private static readonly int dbBitmapSegmentSize = 1 << dbBitmapSegmentBits;
        private const int dbBitmapPages = 1 << (dbDatabaseOffsetBits - dbBitmapSegmentBits);
        private const int dbLargeBitmapPages = 1 << (dbLargeDatabaseOffsetBits - dbBitmapSegmentBits);
        private const int dbHandlesPerPageBits = Page.pageBits - 3;
        private const int dbHandlesPerPage = 1 << dbHandlesPerPageBits;
        private const int dbDirtyPageBitmapSize = 1 << (32 - dbHandlesPerPageBits - 3);

        private const int dbInvalidId = 0;
        private const int dbBitmapId = 1;
        private const int dbFirstUserId = dbBitmapId + dbBitmapPages;

        internal const int dbPageObjectFlag = 1;
        internal const int dbModifiedFlag = 2;
        internal const int dbFreeHandleFlag = 4;
        internal const int dbFlagsMask = 7;
        internal const int dbFlagsBits = 3;

        internal int GetBitmapPageId(int i)
        {
            return i < dbBitmapPages ? dbBitmapId + i : header.root[1 - currIndex].bitmapExtent + i;
        }

        internal long GetPos(int oid)
        {
            lock (objectCache)
            {
                if (oid == 0 || oid >= currIndexSize)
                {
                    throw new StorageError(StorageError.INVALID_OID);
                }
                Page pg = pool.GetPage(header.root[1 - currIndex].index + ((long) (SupportClass.URShift(oid, dbHandlesPerPageBits)) << Page.pageBits));
                long pos = Bytes.Unpack8(pg.data, (oid & (dbHandlesPerPage - 1)) << 3);
                pool.Unfix(pg);
                return pos;
            }
        }

        internal void SetPos(int oid, long pos)
        {
            lock (objectCache)
            {
                dirtyPagesMap[SupportClass.URShift(oid, (dbHandlesPerPageBits + 5))] |= 1 << ((SupportClass.URShift(oid, dbHandlesPerPageBits)) & 31);
                Page pg = pool.PutPage(header.root[1 - currIndex].index + ((long) (SupportClass.URShift(oid, dbHandlesPerPageBits)) << Page.pageBits));
                Bytes.Pack8(pg.data, (oid & (dbHandlesPerPage - 1)) << 3, pos);
                pool.Unfix(pg);
            }
        }

        internal byte[] Get(int oid)
        {
            long pos = GetPos(oid);
            if ((pos & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
            {
                throw new StorageError(StorageError.INVALID_OID);
            }
            return pool.Get(pos & ~ dbFlagsMask);
        }

        internal Page GetPage(int oid)
        {
            long pos = GetPos(oid);
            if ((pos & (dbFreeHandleFlag | dbPageObjectFlag)) != dbPageObjectFlag)
            {
                throw new StorageError(StorageError.DELETED_OBJECT);
            }
            return pool.GetPage(pos & ~ dbFlagsMask);
        }

        internal Page PutPage(int oid)
        {
            lock (objectCache)
            {
                long pos = GetPos(oid);
                if ((pos & (dbFreeHandleFlag | dbPageObjectFlag)) != dbPageObjectFlag)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }
                if ((pos & dbModifiedFlag) == 0)
                {
                    dirtyPagesMap[SupportClass.URShift(oid, (dbHandlesPerPageBits + 5))] |= 1 << ((SupportClass.URShift(oid, dbHandlesPerPageBits)) & 31);
                    Allocate(Page.pageSize, oid);
                    CloneBitmap(pos & ~ dbFlagsMask, Page.pageSize);
                    pos = GetPos(oid);
                }
                modified = true;
                return pool.PutPage(pos & ~ dbFlagsMask);
            }
        }

        internal virtual int AllocatePage()
        {
            int oid = AllocateId();
            SetPos(oid, Allocate(Page.pageSize, 0) | dbPageObjectFlag | dbModifiedFlag);
            return oid;
        }

        public virtual void DeallocateObject(IPersistent obj)
        {
            lock (this)
            {
                lock (objectCache)
                {
                    int oid = obj.Oid;
                    if (oid == 0)
                    {
                        return;
                    }
                    long pos = GetPos(oid);
                    objectCache.Remove(oid);
                    int offs = (int) pos & (Page.pageSize - 1);
                    if ((offs & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
                    {
                        throw new StorageError(StorageError.DELETED_OBJECT);
                    }
                    Page pg = pool.GetPage(pos - offs);
                    offs &= ~ dbFlagsMask;
                    int size = ObjectHeader.GetSize(pg.data, offs);
                    pool.Unfix(pg);
                    FreeId(oid);
                    if ((pos & dbModifiedFlag) != 0)
                    {
                        Free(pos & ~ dbFlagsMask, size);
                    }
                    else
                    {
                        CloneBitmap(pos, size);
                    }
                    obj.AssignOid(this, 0, false);
                }
            }
        }

        internal void FreePage(int oid)
        {
            long pos = GetPos(oid);
            Assert.That((pos & (dbFreeHandleFlag | dbPageObjectFlag)) == dbPageObjectFlag);
            if ((pos & dbModifiedFlag) != 0)
                Free(pos & ~ dbFlagsMask, Page.pageSize);
            else
                CloneBitmap(pos & ~ dbFlagsMask, Page.pageSize);
            FreeId(oid);
        }

        internal virtual int AllocateId()
        {
            lock (objectCache)
            {
                int oid;
                int curr = 1 - currIndex;
                SetDirty();
                if ((oid = header.root[curr].freeList) != 0)
                {
                    header.root[curr].freeList = (int) (GetPos(oid) >> dbFlagsBits);
                    Assert.That(header.root[curr].freeList >= 0);
                    dirtyPagesMap[SupportClass.URShift(oid, (dbHandlesPerPageBits + 5))] |= 1 << ((SupportClass.URShift(oid, dbHandlesPerPageBits)) & 31);
                    return oid;
                }

                if (currIndexSize >= header.root[curr].indexSize)
                {
                    int oldIndexSize = header.root[curr].indexSize;
                    int newIndexSize = oldIndexSize << 1;
                    if (newIndexSize < oldIndexSize)
                    {
                        newIndexSize = Int32.MaxValue & ~ (dbHandlesPerPage - 1);
                        if (newIndexSize <= oldIndexSize)
                        {
                            throw new StorageError(StorageError.NOT_ENOUGH_SPACE);
                        }
                    }
                    long newIndex = Allocate(newIndexSize * 8L, 0);
                    if (currIndexSize >= header.root[curr].indexSize)
                    {
                        long oldIndex = header.root[curr].index;
                        pool.Copy(newIndex, oldIndex, currIndexSize * 8L);
                        header.root[curr].index = newIndex;
                        header.root[curr].indexSize = newIndexSize;
                        Free(oldIndex, oldIndexSize * 8L);
                    }
                    else
                    {
                        // index was already reallocated
                        Free(newIndex, newIndexSize * 8L);
                    }
                }
                oid = currIndexSize;
                header.root[curr].indexUsed = ++currIndexSize;
                return oid;
            }
        }

        internal virtual void FreeId(int oid)
        {
            lock (objectCache)
            {
                SetPos(oid, ((long) (header.root[1 - currIndex].freeList) << dbFlagsBits) | dbFreeHandleFlag);
                header.root[1 - currIndex].freeList = oid;
            }
        }

        static readonly byte[] firstHoleSize = new byte[] {
            8,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            5,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            6,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            5,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            7,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            5,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            6,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,
            5,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0,4,0,1,0,2,0,1,0,3,0,1,0,2,0,1,0
        };

        static readonly byte[] lastHoleSize = new byte[] {
            8,7,6,6,5,5,5,5,4,4,4,4,4,4,4,4,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
        };

        static readonly byte[] maxHoleSize = new byte[] {
            8,7,6,6,5,5,5,5,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            5,4,3,3,2,2,2,2,3,2,2,2,2,2,2,2,4,3,2,2,2,2,2,2,3,2,2,2,2,2,2,2,
            6,5,4,4,3,3,3,3,3,2,2,2,2,2,2,2,4,3,2,2,2,1,1,1,3,2,1,1,2,1,1,1,
            5,4,3,3,2,2,2,2,3,2,1,1,2,1,1,1,4,3,2,2,2,1,1,1,3,2,1,1,2,1,1,1,
            7,6,5,5,4,4,4,4,3,3,3,3,3,3,3,3,4,3,2,2,2,2,2,2,3,2,2,2,2,2,2,2,
            5,4,3,3,2,2,2,2,3,2,1,1,2,1,1,1,4,3,2,2,2,1,1,1,3,2,1,1,2,1,1,1,
            6,5,4,4,3,3,3,3,3,2,2,2,2,2,2,2,4,3,2,2,2,1,1,1,3,2,1,1,2,1,1,1,
            5,4,3,3,2,2,2,2,3,2,1,1,2,1,1,1,4,3,2,2,2,1,1,1,3,2,1,1,2,1,1,0
        };

        static readonly byte[] maxHoleOffset = new byte[] {
            0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,0,1,5,5,5,5,5,5,0,5,5,5,5,5,5,5,
            0,1,2,2,0,3,3,3,0,1,6,6,0,6,6,6,0,1,2,2,0,6,6,6,0,1,6,6,0,6,6,6,
            0,1,2,2,3,3,3,3,0,1,4,4,0,4,4,4,0,1,2,2,0,1,0,3,0,1,0,2,0,1,0,5,
            0,1,2,2,0,3,3,3,0,1,0,2,0,1,0,4,0,1,2,2,0,1,0,3,0,1,0,2,0,1,0,7,
            0,1,2,2,3,3,3,3,0,4,4,4,4,4,4,4,0,1,2,2,0,5,5,5,0,1,5,5,0,5,5,5,
            0,1,2,2,0,3,3,3,0,1,0,2,0,1,0,4,0,1,2,2,0,1,0,3,0,1,0,2,0,1,0,6,
            0,1,2,2,3,3,3,3,0,1,4,4,0,4,4,4,0,1,2,2,0,1,0,3,0,1,0,2,0,1,0,5,
            0,1,2,2,0,3,3,3,0,1,0,2,0,1,0,4,0,1,2,2,0,1,0,3,0,1,0,2,0,1,0,0
        };

        internal const int pageBits = Page.pageSize * 8;
        internal const int inc = Page.pageSize / dbAllocationQuantum / 8;

        internal static void MemSet(Page pg, int offs, int pattern, int len)
        {
            byte[] arr = pg.data;
            byte pat = (byte) pattern;
            while (--len >= 0)
            {
                arr[offs++] = pat;
            }
        }

        internal void Extend(long size)
        {
            if (size > header.root[1 - currIndex].size)
            {
                header.root[1 - currIndex].size = size;
            }
        }

        internal class Location
        {
            internal long pos;
            internal long size;
            internal Location next;
        }

        internal bool WasReserved(long pos, long size)
        {
            for (Location location = reservedChain; location != null; location = location.next)
            {
                if ((pos >= location.pos && pos - location.pos < location.size) || (pos <= location.pos && location.pos - pos < size))
                {
                    return true;
                }
            }
            return false;
        }

        internal void ReserveLocation(long pos, long size)
        {
            Location location = new Location();
            location.pos = pos;
            location.size = size;
            location.next = reservedChain;
            reservedChain = location;
        }

        internal void CommitLocation()
        {
            reservedChain = reservedChain.next;
        }

        internal void SetDirty()
        {
            modified = true;
            if (!header.dirty)
            {
                header.dirty = true;
                Page pg = pool.PutPage(0);
                header.Pack(pg.data);
                pool.Flush();
                pool.Unfix(pg);
            }
        }

        internal Page PutBitmapPage(int i)
        {
            return PutPage(GetBitmapPageId(i));
        }

        internal Page GetBitmapPage(int i)
        {
            return GetPage(GetBitmapPageId(i));
        }

        internal long Allocate(long size, int oid)
        {
            lock (objectCache)
            {
                SetDirty();
                size = (size + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1);
                Assert.That(size != 0);
                allocatedDelta += size;
                if (allocatedDelta > gcThreshold)
                {
                    Gc0();
                }

                int objBitSize = (int) (size >> dbAllocationQuantumBits);
                Assert.That(objBitSize == (size >> dbAllocationQuantumBits));
                long pos;
                int holeBitSize = 0;
                int alignment = (int) size & (Page.pageSize - 1);
                int offs, firstPage, lastPage, i, j;
                int holeBeforeFreePage = 0;
                int freeBitmapPage = 0;
                int curr = 1 - currIndex;
                Page pg;

                lastPage = header.root[curr].bitmapEnd - dbBitmapId;
                usedSize += size;

                if (alignment == 0)
                {
                    firstPage = currPBitmapPage;
                    offs = (currPBitmapOffs + inc - 1) & ~ (inc - 1);
                }
                else
                {
                    firstPage = currRBitmapPage;
                    offs = currRBitmapOffs;
                }

                while (true)
                {
                    if (alignment == 0)
                    {
                        // allocate page object
                        for (i = firstPage; i < lastPage; i++)
                        {
                            int spaceNeeded = objBitSize - holeBitSize < pageBits ? objBitSize - holeBitSize : pageBits;
                            if (bitmapPageAvailableSpace[i] <= spaceNeeded)
                            {
                                holeBitSize = 0;
                                offs = 0;
                                continue;
                            }
                            pg = GetBitmapPage(i);
                            int startOffs = offs;
                            while (offs < Page.pageSize)
                            {
                                if (pg.data[offs++] != 0)
                                {
                                    offs = (offs + inc - 1) & ~ (inc - 1);
                                    holeBitSize = 0;
                                }
                                else if ((holeBitSize += 8) == objBitSize)
                                {
                                    pos = (((long) i * Page.pageSize + offs) * 8 - holeBitSize) << dbAllocationQuantumBits;
                                    if (WasReserved(pos, size))
                                    {
                                        offs += (objBitSize >> 3);
                                        startOffs = offs = (offs + inc - 1) & ~ (inc - 1);
                                        holeBitSize = 0;
                                        continue;
                                    }
                                    ReserveLocation(pos, size);
                                    currPBitmapPage = i;
                                    currPBitmapOffs = offs;
                                    Extend(pos + size);
                                    if (oid != 0)
                                    {
                                        long prev = GetPos(oid);
                                        uint marker = (uint) prev & dbFlagsMask;
                                        pool.Copy(pos, prev - marker, size);
                                        SetPos(oid, pos | marker | dbModifiedFlag);
                                    }
                                    pool.Unfix(pg);
                                    pg = PutBitmapPage(i);
                                    int holeBytes = holeBitSize >> 3;
                                    if (holeBytes > offs)
                                    {
                                        MemSet(pg, 0, 0xFF, offs);
                                        holeBytes -= offs;
                                        pool.Unfix(pg);
                                        pg = PutBitmapPage(--i);
                                        offs = Page.pageSize;
                                    }
                                    while (holeBytes > Page.pageSize)
                                    {
                                        MemSet(pg, 0, 0xFF, Page.pageSize);
                                        holeBytes -= Page.pageSize;
                                        bitmapPageAvailableSpace[i] = 0;
                                        pool.Unfix(pg);
                                        pg = PutBitmapPage(--i);
                                    }
                                    MemSet(pg, offs - holeBytes, 0xFF, holeBytes);
                                    CommitLocation();
                                    pool.Unfix(pg);
                                    return pos;
                                }
                            }
                            if (startOffs == 0 && holeBitSize == 0 && spaceNeeded < bitmapPageAvailableSpace[i])
                            {
                                bitmapPageAvailableSpace[i] = spaceNeeded;
                            }
                            offs = 0;
                            pool.Unfix(pg);
                        }
                    }
                    else
                    {
                        for (i = firstPage; i < lastPage; i++)
                        {
                            int spaceNeeded = objBitSize - holeBitSize < pageBits ? objBitSize - holeBitSize : pageBits;
                            if (bitmapPageAvailableSpace[i] <= spaceNeeded)
                            {
                                holeBitSize = 0;
                                offs = 0;
                                continue;
                            }
                            pg = GetBitmapPage(i);
                            int startOffs = offs;
                            while (offs < Page.pageSize)
                            {
                                int mask = pg.data[offs] & 0xFF;
                                if (holeBitSize + firstHoleSize[mask] >= objBitSize)
                                {
                                    pos = (((long) i * Page.pageSize + offs) * 8 - holeBitSize) << dbAllocationQuantumBits;
                                    if (WasReserved(pos, size))
                                    {
                                        startOffs = offs += ((objBitSize + 7) >> 3);
                                        holeBitSize = 0;
                                        continue;
                                    }
                                    ReserveLocation(pos, size);
                                    currRBitmapPage = i;
                                    currRBitmapOffs = offs;
                                    Extend(pos + size);
                                    if (oid != 0)
                                    {
                                        long prev = GetPos(oid);
                                        uint marker = (uint) prev & dbFlagsMask;
                                        pool.Copy(pos, prev - marker, size);
                                        SetPos(oid, pos | marker | dbModifiedFlag);
                                    }
                                    pool.Unfix(pg);
                                    pg = PutBitmapPage(i);
                                    pg.data[offs] |= (byte) ((1 << (objBitSize - holeBitSize)) - 1);
                                    if (holeBitSize != 0)
                                    {
                                        if (holeBitSize > offs * 8)
                                        {
                                            MemSet(pg, 0, 0xFF, offs);
                                            holeBitSize -= offs * 8;
                                            pool.Unfix(pg);
                                            pg = PutBitmapPage(--i);
                                            offs = Page.pageSize;
                                        }
                                        while (holeBitSize > pageBits)
                                        {
                                            MemSet(pg, 0, 0xFF, Page.pageSize);
                                            holeBitSize -= pageBits;
                                            bitmapPageAvailableSpace[i] = 0;
                                            pool.Unfix(pg);
                                            pg = PutBitmapPage(--i);
                                        }
                                        while ((holeBitSize -= 8) > 0)
                                        {
                                            pg.data[--offs] = (byte) 0xFF;
                                        }
                                        pg.data[offs - 1] |= (byte) ~ ((1 << -holeBitSize) - 1);
                                    }
                                    pool.Unfix(pg);
                                    CommitLocation();
                                    return pos;
                                }
                                else if (maxHoleSize[mask] >= objBitSize)
                                {
                                    int holeBitOffset = maxHoleOffset[mask];
                                    pos = (((long) i * Page.pageSize + offs) * 8 + holeBitOffset) << dbAllocationQuantumBits;
                                    if (WasReserved(pos, size))
                                    {
                                        startOffs = offs += ((objBitSize + 7) >> 3);
                                        holeBitSize = 0;
                                        continue;
                                    }
                                    ReserveLocation(pos, size);
                                    currRBitmapPage = i;
                                    currRBitmapOffs = offs;
                                    Extend(pos + size);
                                    if (oid != 0)
                                    {
                                        long prev = GetPos(oid);
                                        uint marker = (uint) prev & dbFlagsMask;
                                        pool.Copy(pos, prev - marker, size);
                                        SetPos(oid, pos | marker | dbModifiedFlag);
                                    }
                                    pool.Unfix(pg);
                                    pg = PutBitmapPage(i);
                                    pg.data[offs] |= (byte) (((1 << objBitSize) - 1) << holeBitOffset);
                                    pool.Unfix(pg);
                                    CommitLocation();
                                    return pos;
                                }
                                offs += 1;
                                if (lastHoleSize[mask] == 8)
                                {
                                    holeBitSize += 8;
                                }
                                else
                                {
                                    holeBitSize = lastHoleSize[mask];
                                }
                            }
                            if (startOffs == 0 && holeBitSize == 0 && spaceNeeded < bitmapPageAvailableSpace[i])
                            {
                                bitmapPageAvailableSpace[i] = spaceNeeded;
                            }
                            offs = 0;
                            pool.Unfix(pg);
                        }
                    }

                    if (firstPage == 0)
                    {
                        if (freeBitmapPage > i)
                        {
                            i = freeBitmapPage;
                            holeBitSize = holeBeforeFreePage;
                        }

                        objBitSize -= holeBitSize;
                        // number of bits reserved for the object and aligned on page boundary
                        int skip = (objBitSize + Page.pageSize / dbAllocationQuantum - 1) & ~ (Page.pageSize / dbAllocationQuantum - 1);
                        // page aligned position after allocated object
                        pos = ((long) i << dbBitmapSegmentBits) + ((long) skip << dbAllocationQuantumBits);

                        long extension = (size > extensionQuantum) ? size : extensionQuantum;
                        int oldIndexSize = 0;
                        long oldIndex = 0;
                        int morePages = (int) ((extension + Page.pageSize * (dbAllocationQuantum * 8 - 1) - 1) / (Page.pageSize * (dbAllocationQuantum * 8 - 1)));
                        if (i + morePages > dbLargeBitmapPages)
                        {
                            throw new StorageError(StorageError.NOT_ENOUGH_SPACE);
                        }

                        if (i <= dbBitmapPages && i + morePages > dbBitmapPages)
                        {
                            // We are out of space mapped by memory default allocation bitmap
                            oldIndexSize = header.root[curr].indexSize;
                            if (oldIndexSize <= currIndexSize + dbLargeBitmapPages - dbBitmapPages)
                            {
                                int newIndexSize = oldIndexSize;
                                oldIndex = header.root[curr].index;
                                do
                                {
                                    newIndexSize <<= 1;
                                    if (newIndexSize < 0)
                                    {
                                        newIndexSize = Int32.MaxValue & ~ (dbHandlesPerPage - 1);
                                        if (newIndexSize < currIndexSize + dbLargeBitmapPages - dbBitmapPages)
                                        {
                                            throw new StorageError(StorageError.NOT_ENOUGH_SPACE);
                                        }
                                        break;
                                    }
                                }
                                while (newIndexSize <= currIndexSize + dbLargeBitmapPages - dbBitmapPages);

                                if (size + newIndexSize * 8L > extensionQuantum)
                                {
                                    extension = size + newIndexSize * 8L;
                                    morePages = (int) ((extension + Page.pageSize * (dbAllocationQuantum * 8 - 1) - 1) / (Page.pageSize * (dbAllocationQuantum * 8 - 1)));
                                }
                                Extend(pos + (long) morePages * Page.pageSize + newIndexSize * 8L);
                                long newIndex = pos + (long) morePages * Page.pageSize;
                                FillBitmap(pos + (skip >> 3) + (long) morePages * (Page.pageSize / dbAllocationQuantum / 8), SupportClass.URShift(newIndexSize, dbAllocationQuantumBits));
                                pool.Copy(newIndex, oldIndex, oldIndexSize * 8L);
                                header.root[curr].index = newIndex;
                                header.root[curr].indexSize = newIndexSize;
                            }
                            int[] newBitmapPageAvailableSpace = new int[dbLargeBitmapPages];
                            Array.Copy(bitmapPageAvailableSpace, 0, newBitmapPageAvailableSpace, 0, dbBitmapPages);
                            for (j = dbBitmapPages; j < dbLargeBitmapPages; j++)
                            {
                                newBitmapPageAvailableSpace[j] = Int32.MaxValue;
                            }
                            bitmapPageAvailableSpace = newBitmapPageAvailableSpace;

                            for (j = 0; j < dbLargeBitmapPages - dbBitmapPages; j++)
                            {
                                SetPos(currIndexSize + j, dbFreeHandleFlag);
                            }

                            header.root[curr].bitmapExtent = currIndexSize;
                            header.root[curr].indexUsed = currIndexSize += dbLargeBitmapPages - dbBitmapPages;
                        }
                        Extend(pos + (long) morePages * Page.pageSize);
                        long adr = pos;
                        int len = objBitSize >> 3;
                        // fill bitmap pages used for allocation of object space with 0xFF
                        while (len >= Page.pageSize)
                        {
                            pg = pool.PutPage(adr);
                            MemSet(pg, 0, 0xFF, Page.pageSize);
                            pool.Unfix(pg);
                            adr += Page.pageSize;
                            len -= Page.pageSize;
                        }

                        // fill part of last page responsible for allocation of object space
                        pg = pool.PutPage(adr);
                        MemSet(pg, 0, 0xFF, len);
                        pg.data[len] = (byte) ((1 << (objBitSize & 7)) - 1);
                        pool.Unfix(pg);

                        // mark in bitmap newly allocated object
                        FillBitmap(pos + (skip >> 3), morePages * (Page.pageSize / dbAllocationQuantum / 8));

                        j = i;
                        while (--morePages >= 0)
                        {
                            SetPos(GetBitmapPageId(j++), pos | dbPageObjectFlag | dbModifiedFlag);
                            pos += Page.pageSize;
                        }

                        header.root[curr].bitmapEnd = j + dbBitmapId;
                        j = i + objBitSize / pageBits;
                        if (alignment != 0)
                        {
                            currRBitmapPage = j;
                            currRBitmapOffs = 0;
                        }
                        else
                        {
                            currPBitmapPage = j;
                            currPBitmapOffs = 0;
                        }

                        while (j > i)
                        {
                            bitmapPageAvailableSpace[--j] = 0;
                        }

                        pos = ((long) i * Page.pageSize * 8 - holeBitSize) << dbAllocationQuantumBits;
                        if (oid != 0)
                        {
                            long prev = GetPos(oid);
                            uint marker = (uint) prev & dbFlagsMask;
                            pool.Copy(pos, prev - marker, size);
                            SetPos(oid, pos | marker | dbModifiedFlag);
                        }

                        if (holeBitSize != 0)
                        {
                            ReserveLocation(pos, size);
                            while (holeBitSize > pageBits)
                            {
                                holeBitSize -= pageBits;
                                pg = PutBitmapPage(--i);
                                MemSet(pg, 0, 0xFF, Page.pageSize);
                                bitmapPageAvailableSpace[i] = 0;
                                pool.Unfix(pg);
                            }
                            pg = PutBitmapPage(--i);
                            offs = Page.pageSize;
                            while ((holeBitSize -= 8) > 0)
                            {
                                pg.data[--offs] = (byte) 0xFF;
                            }
                            pg.data[offs - 1] |= (byte) ~ ((1 << -holeBitSize) - 1);
                            pool.Unfix(pg);
                            CommitLocation();
                        }

                        if (oldIndex != 0)
                        {
                            Free(oldIndex, oldIndexSize * 8L);
                        }
                        return pos;
                    }

                    if (gcThreshold != Int64.MaxValue && !gcDone && !gcActive)
                    {
                        allocatedDelta -= size;
                        usedSize -= size;
                        Gc0();
                        currRBitmapPage = currPBitmapPage = 0;
                        currRBitmapOffs = currPBitmapOffs = 0;
                        return Allocate(size, oid);
                    }

                    freeBitmapPage = i;
                    holeBeforeFreePage = holeBitSize;
                    holeBitSize = 0;
                    lastPage = firstPage + 1;
                    firstPage = 0;
                    offs = 0;
                }
            }
        }

        internal void FillBitmap(long adr, int len)
        {
            while (true)
            {
                int off = (int) adr & (Page.pageSize - 1);
                Page pg = pool.PutPage(adr - off);
                if (Page.pageSize - off >= len)
                {
                    MemSet(pg, off, 0xFF, len);
                    pool.Unfix(pg);
                    break;
                }
                else
                {
                    MemSet(pg, off, 0xFF, Page.pageSize - off);
                    pool.Unfix(pg);
                    adr += Page.pageSize - off;
                    len -= (Page.pageSize - off);
                }
            }
        }

        internal void Free(long pos, long size)
        {
            lock (objectCache)
            {
                Assert.That(pos != 0 && (pos & (dbAllocationQuantum - 1)) == 0);
                long quantNo = SupportClass.URShift(pos, dbAllocationQuantumBits);
                int objBitSize = (int) (SupportClass.URShift((size + dbAllocationQuantum - 1), dbAllocationQuantumBits));
                int pageId = (int) (SupportClass.URShift(quantNo, (Page.pageBits + 3)));
                int offs = (int) (quantNo & (Page.pageSize * 8 - 1)) >> 3;
                Page pg = PutBitmapPage(pageId);
                int bitOffs = (int) quantNo & 7;

                allocatedDelta -= ((long) objBitSize << dbAllocationQuantumBits);
                usedSize -= ((long) objBitSize << dbAllocationQuantumBits);

                if ((pos & (Page.pageSize - 1)) == 0 && size >= Page.pageSize)
                {
                    if (pageId == currPBitmapPage && offs < currPBitmapOffs)
                    {
                        currPBitmapOffs = offs;
                    }
                }

                if (pageId == currRBitmapPage && offs < currRBitmapOffs)
                {
                    currRBitmapOffs = offs;
                }
                bitmapPageAvailableSpace[pageId] = Int32.MaxValue;

                if (objBitSize > 8 - bitOffs)
                {
                    objBitSize -= (8 - bitOffs);
                    pg.data[offs++] &= (byte) ((1 << bitOffs) - 1);
                    while (objBitSize + offs * 8 > Page.pageSize * 8)
                    {
                        MemSet(pg, offs, 0, Page.pageSize - offs);
                        pool.Unfix(pg);
                        pg = PutBitmapPage(++pageId);
                        bitmapPageAvailableSpace[pageId] = Int32.MaxValue;
                        objBitSize -= (Page.pageSize - offs) * 8;
                        offs = 0;
                    }
                    while ((objBitSize -= 8) > 0)
                    {
                        pg.data[offs++] = 0;
                    }
                    pg.data[offs] &= (byte) ~ ((1 << (objBitSize + 8)) - 1);
                }
                else
                {
                    pg.data[offs] &= (byte) ~ (((1 << objBitSize) - 1) << bitOffs);
                }
                pool.Unfix(pg);
            }
        }

        internal void CloneBitmap(long pos, long size)
        {
            lock (objectCache)
            {
                long quantNo = SupportClass.URShift(pos, dbAllocationQuantumBits);
                int objBitSize = (int) (SupportClass.URShift((size + dbAllocationQuantum - 1), dbAllocationQuantumBits));
                int pageId = (int) (SupportClass.URShift(quantNo, (Page.pageBits + 3)));
                int offs = (int) (quantNo & (Page.pageSize * 8 - 1)) >> 3;
                int bitOffs = (int) quantNo & 7;
                int oid = GetBitmapPageId(pageId);
                pos = GetPos(oid);
                if ((pos & dbModifiedFlag) == 0)
                {
                    dirtyPagesMap[SupportClass.URShift(oid, (dbHandlesPerPageBits + 5))] |= 1 << ((SupportClass.URShift(oid, dbHandlesPerPageBits)) & 31);
                    Allocate(Page.pageSize, oid);
                    CloneBitmap(pos & ~ dbFlagsMask, Page.pageSize);
                }

                if (objBitSize > 8 - bitOffs)
                {
                    objBitSize -= (8 - bitOffs);
                    offs += 1;
                    while (objBitSize + offs * 8 > Page.pageSize * 8)
                    {
                        oid = GetBitmapPageId(++pageId);
                        pos = GetPos(oid);
                        if ((pos & dbModifiedFlag) == 0)
                        {
                            dirtyPagesMap[SupportClass.URShift(oid, (dbHandlesPerPageBits + 5))] |= 1 << ((SupportClass.URShift(oid, dbHandlesPerPageBits)) & 31);
                            Allocate(Page.pageSize, oid);
                            CloneBitmap(pos & ~ dbFlagsMask, Page.pageSize);
                        }
                        objBitSize -= (Page.pageSize - offs) * 8;
                        offs = 0;
                    }
                }
            }
        }

        public virtual void Open(string filePath)
        {
            Open(filePath, StorageConstants.DEFAULT_PAGE_POOL_SIZE);
        }

        public virtual void Open(IFile file)
        {
            Open(file, StorageConstants.DEFAULT_PAGE_POOL_SIZE);
        }

        public virtual void Open(string filePath, int pagePoolSize)
        {
            lock (this)
            {
#if OMIT_MULTIFILE
                IFile file = new OSFile(filePath, readOnly, noFlush);
#else
                IFile file;
                if (filePath.StartsWith("@"))
                    file = new MultiFile(filePath.Substring(1), readOnly, noFlush);
                else
                    file = new OSFile(filePath, readOnly, noFlush);
#endif

                try
                {
                    Open(file, pagePoolSize);
                }
                catch (StorageError ex)
                {
                    file.Close();
                    throw ex;
                }
            }
        }

        public virtual void Open(string filePath, int pagePoolSize, string cryptKey)
        {
            lock (this)
            {
                Rc4File file = new Rc4File(filePath, readOnly, noFlush, cryptKey);
                try
                {
                    Open(file, pagePoolSize);
                }
                catch (StorageError ex)
                {
                    file.Close();
                    throw ex;
                }
            }
        }

        protected internal virtual OidHashTable CreateObjectCache(string kind, int pagePoolSize, int objectCacheSize)
        {
            if (pagePoolSize == StorageConstants.INFINITE_PAGE_POOL || "strong".Equals(kind))
            {
                return new StrongHashTable(objectCacheSize);
            }
            if ("soft".Equals(kind))
            {
                //return new SoftHashTable(objectCacheSize);
                throw new StorageError(StorageError.BAD_PROPERTY_VALUE);
            }
            if ("weak".Equals(kind))
            {
                return new WeakHashTable(objectCacheSize);
            }
            return new LruObjectCache(objectCacheSize);
        }

        protected internal virtual bool IsDirty()
        {
            return header.dirty;
        }

        public virtual void Open(IFile file, int pagePoolSize)
        {
            lock (this)
            {
                if (opened)
                {
                    throw new StorageError(StorageError.STORAGE_ALREADY_OPENED);
                }
                if (lockFile)
                {
                    if (!file.Lock())
                    {
                        throw new StorageError(StorageError.STORAGE_IS_USED);
                    }
                }
                Page pg;
                int i;
                int indexSize = initIndexSize;
                if (indexSize < dbFirstUserId)
                {
                    indexSize = dbFirstUserId;
                }
                indexSize = (indexSize + dbHandlesPerPage - 1) & ~ (dbHandlesPerPage - 1);

                dirtyPagesMap = new int[dbDirtyPageBitmapSize / 4 + 1];
                gcThreshold = Int64.MaxValue;
                backgroundGcMonitor = new object();
                backgroundGcStartMonitor = new object();
                gcThread = null;
                gcActive = false;
                gcDone = false;
                allocatedDelta = 0;

                nNestedTransactions = 0;
                nBlockedTransactions = 0;
                nCommittedTransactions = 0;
                scheduledCommitTime = Int64.MaxValue;
                transactionMonitor = new object();
                transactionLock = new PersistentResource();

                modified = false;

                objectCache = CreateObjectCache(cacheKind, pagePoolSize, objectCacheInitSize);

                //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
                classDescMap = new Hashtable();
                descList = null;

                header = new Header();
                byte[] buf = new byte[Header.Sizeof];
                int rc = file.Read(0, buf);
                if (rc > 0 && rc < Header.Sizeof)
                {
                    throw new StorageError(StorageError.DATABASE_CORRUPTED);
                }

                header.Unpack(buf);
                if (header.curr < 0 || header.curr > 1)
                {
                    throw new StorageError(StorageError.DATABASE_CORRUPTED);
                }

                if (pool == null)
                {
                    pool = new PagePool(pagePoolSize / Page.pageSize);
                    pool.Open(file);
                }

                if (!header.initialized)
                {
                    header.curr = currIndex = 0;
                    long used = Page.pageSize;
                    header.root[0].index = used;
                    header.root[0].indexSize = indexSize;
                    header.root[0].indexUsed = dbFirstUserId;
                    header.root[0].freeList = 0;
                    used += indexSize * 8L;
                    header.root[1].index = used;
                    header.root[1].indexSize = indexSize;
                    header.root[1].indexUsed = dbFirstUserId;
                    header.root[1].freeList = 0;
                    used += indexSize * 8L;

                    header.root[0].shadowIndex = header.root[1].index;
                    header.root[1].shadowIndex = header.root[0].index;
                    header.root[0].shadowIndexSize = indexSize;
                    header.root[1].shadowIndexSize = indexSize;

                    int bitmapPages = (int) ((used + Page.pageSize * (dbAllocationQuantum * 8 - 1) - 1) / (Page.pageSize * (dbAllocationQuantum * 8 - 1)));
                    long bitmapSize = (long) bitmapPages * Page.pageSize;
                    int usedBitmapSize = (int) (SupportClass.URShift((used + bitmapSize), (dbAllocationQuantumBits + 3)));

                    for (i = 0; i < bitmapPages; i++)
                    {
                        pg = pool.PutPage(used + (long) i * Page.pageSize);
                        byte[] bitmap = pg.data;
                        int n = usedBitmapSize > Page.pageSize ? Page.pageSize : usedBitmapSize;
                        for (int j = 0; j < n; j++)
                        {
                            bitmap[j] = (byte) 0xFF;
                        }
                        usedBitmapSize -= Page.pageSize;
                        pool.Unfix(pg);
                    }

                    int bitmapIndexSize = ((dbBitmapId + dbBitmapPages) * 8 + Page.pageSize - 1) & ~ (Page.pageSize - 1);
                    byte[] index = new byte[bitmapIndexSize];
                    Bytes.Pack8(index, dbInvalidId * 8, dbFreeHandleFlag);
                    for (i = 0; i < bitmapPages; i++)
                    {
                        Bytes.Pack8(index, (dbBitmapId + i) * 8, used | dbPageObjectFlag);
                        used += Page.pageSize;
                    }
                    header.root[0].bitmapEnd = dbBitmapId + i;
                    header.root[1].bitmapEnd = dbBitmapId + i;
                    while (i < dbBitmapPages)
                    {
                        Bytes.Pack8(index, (dbBitmapId + i) * 8, dbFreeHandleFlag);
                        i += 1;
                    }

                    header.root[0].size = used;
                    header.root[1].size = used;
                    usedSize = used;
                    committedIndexSize = currIndexSize = dbFirstUserId;

                    pool.Write(header.root[1].index, index);
                    pool.Write(header.root[0].index, index);

                    header.dirty = true;
                    header.root[0].size = header.root[1].size;
                    pg = pool.PutPage(0);
                    header.Pack(pg.data);
                    pool.Flush();
                    pool.Modify(pg);
                    header.initialized = true;
                    header.Pack(pg.data);
                    pool.Unfix(pg);
                    pool.Flush();
                }
                else
                {
                    int curr = header.curr;
                    currIndex = curr;
                    if (header.root[curr].indexSize != header.root[curr].shadowIndexSize)
                    {
                        throw new StorageError(StorageError.DATABASE_CORRUPTED);
                    }

                    if (IsDirty())
                    {
                        if (listener != null)
                        {
                            listener.DatabaseCorrupted();
                        }

                        Console.Error.WriteLine("Database was not normally closed: start recovery");
                        header.root[1 - curr].size = header.root[curr].size;
                        header.root[1 - curr].indexUsed = header.root[curr].indexUsed;
                        header.root[1 - curr].freeList = header.root[curr].freeList;
                        header.root[1 - curr].index = header.root[curr].shadowIndex;
                        header.root[1 - curr].indexSize = header.root[curr].shadowIndexSize;
                        header.root[1 - curr].shadowIndex = header.root[curr].index;
                        header.root[1 - curr].shadowIndexSize = header.root[curr].indexSize;
                        header.root[1 - curr].bitmapEnd = header.root[curr].bitmapEnd;
                        header.root[1 - curr].rootObject = header.root[curr].rootObject;
                        header.root[1 - curr].classDescList = header.root[curr].classDescList;
                        header.root[1 - curr].bitmapExtent = header.root[curr].bitmapExtent;

                        pg = pool.PutPage(0);
                        header.Pack(pg.data);
                        pool.Unfix(pg);

                        pool.Copy(header.root[1 - curr].index, header.root[curr].index, (header.root[curr].indexUsed * 8L + Page.pageSize - 1) & ~(Page.pageSize - 1));
                        if (listener != null)
                        {
                            listener.RecoveryCompleted();
                        }
                        Console.Error.WriteLine("Recovery completed");
                    }
                    currIndexSize = header.root[1 - curr].indexUsed;
                    committedIndexSize = currIndexSize;
                    usedSize = header.root[curr].size;
                }
                int bitmapSize2 = header.root[1 - currIndex].bitmapExtent == 0 ? dbBitmapPages : dbLargeBitmapPages;
                bitmapPageAvailableSpace = new int[bitmapSize2];
                for (i = 0; i < bitmapPageAvailableSpace.Length; i++)
                {
                    bitmapPageAvailableSpace[i] = Int32.MaxValue;
                }

                currRBitmapPage = currPBitmapPage = 0;
                currRBitmapOffs = currPBitmapOffs = 0;

                opened = true;
                ReloadScheme();
            }
        }

        internal static void CheckIfFinal(ClassDescriptor desc)
        {
            Type cls = desc.cls;
            for (ClassDescriptor next = desc.next; next != null; next = next.next)
            {
                next.Load();
                if (cls.IsAssignableFrom(next.cls))
                {
                    desc.hasSubclasses = true;
                }
                else if (next.cls.IsAssignableFrom(cls))
                {
                    next.hasSubclasses = true;
                }
            }
        }

        internal void ReloadScheme()
        {
            classDescMap.Clear();
            int descListOid = header.root[1 - currIndex].classDescList;
            classDescMap[typeof(ClassDescriptor)] = new ClassDescriptor(this, typeof(ClassDescriptor));
            classDescMap[typeof(ClassDescriptor.FieldDescriptor)] = new ClassDescriptor(this, typeof(ClassDescriptor.FieldDescriptor));
            if (descListOid != 0)
            {
                ClassDescriptor desc;
                descList = FindClassDescriptor(descListOid);
                for (desc = descList; desc != null; desc = desc.next)
                {
                    desc.Load();
                }
                for (desc = descList; desc != null; desc = desc.next)
                {
                    //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                    if (classDescMap[desc.cls] == desc)
                    {
                        desc.Resolve();
                    }
                    CheckIfFinal(desc);
                }
            }
            else
            {
                descList = null;
            }
        }

        internal void AssignOid(IPersistent obj, int oid)
        {
            obj.AssignOid(this, oid, false);
        }

        internal void RegisterClassDescriptor(ClassDescriptor desc)
        {
            classDescMap[desc.cls] = desc;
            desc.next = descList;
            descList = desc;
            CheckIfFinal(desc);
            StoreObject0(desc);
            header.root[1 - currIndex].classDescList = desc.Oid;
            modified = true;
        }

        internal ClassDescriptor GetClassDescriptor(Type cls)
        {
            //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
            ClassDescriptor desc = (ClassDescriptor) classDescMap[cls];
            if (desc == null)
            {
                desc = new ClassDescriptor(this, cls);
                RegisterClassDescriptor(desc);
            }
            return desc;
        }

        public virtual IPersistent GetRoot()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                int rootOid = header.root[1 - currIndex].rootObject;
                if (rootOid == 0)
                    return null;
                return LookupObject(rootOid, null);
            }
        }

        public virtual void SetRoot(IPersistent root)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                if (!root.IsPersistent())
                {
                    StoreObject0(root);
                }
                header.root[1 - currIndex].rootObject = root.Oid;
                modified = true;
            }
        }

        public virtual void Commit()
        {
            lock (backgroundGcMonitor)
            {
                lock (this)
                {
                    if (!opened)
                        throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                    objectCache.Flush();
                    if (!modified)
                    {
                        return;
                    }
                    Commit0();
                    modified = false;
                }
            }
        }

        private void Commit0()
        {
            int i, j, n;
            int curr = currIndex;
            int[] map = dirtyPagesMap;
            int oldIndexSize = header.root[curr].indexSize;
            int newIndexSize = header.root[1 - curr].indexSize;
            int nPages = SupportClass.URShift(committedIndexSize, dbHandlesPerPageBits);
            Page pg;

            if (newIndexSize > oldIndexSize)
            {
                CloneBitmap(header.root[curr].index, oldIndexSize * 8L);
                long newIndex;
                while (true)
                {
                    newIndex = Allocate(newIndexSize * 8L, 0);
                    if (newIndexSize == header.root[1 - curr].indexSize)
                    {
                        break;
                    }
                    Free(newIndex, newIndexSize * 8L);
                    newIndexSize = header.root[1 - curr].indexSize;
                }
                header.root[1 - curr].shadowIndex = newIndex;
                header.root[1 - curr].shadowIndexSize = newIndexSize;
                Free(header.root[curr].index, oldIndexSize * 8L);
            }

            for (i = 0; i < nPages; i++)
            {
                if ((map[i >> 5] & (1 << (i & 31))) != 0)
                {
                    Page srcIndex = pool.GetPage(header.root[1 - curr].index + (long) i * Page.pageSize);
                    Page dstIndex = pool.GetPage(header.root[curr].index + (long) i * Page.pageSize);
                    for (j = 0; j < Page.pageSize; j += 8)
                    {
                        long pos = Bytes.Unpack8(dstIndex.data, j);
                        if (Bytes.Unpack8(srcIndex.data, j) != pos)
                        {
                            if ((pos & dbFreeHandleFlag) == 0)
                            {
                                if ((pos & dbPageObjectFlag) != 0)
                                {
                                    Free(pos & ~ dbFlagsMask, Page.pageSize);
                                }
                                else if (pos != 0)
                                {
                                    int offs = (int) pos & (Page.pageSize - 1);
                                    pg = pool.GetPage(pos - offs);
                                    Free(pos, ObjectHeader.GetSize(pg.data, offs));
                                    pool.Unfix(pg);
                                }
                            }
                        }
                    }
                    pool.Unfix(srcIndex);
                    pool.Unfix(dstIndex);
                }
            }
            n = committedIndexSize & (dbHandlesPerPage - 1);
            if (n != 0 && (map[i >> 5] & (1 << (i & 31))) != 0)
            {
                Page srcIndex = pool.GetPage(header.root[1 - curr].index + (long) i * Page.pageSize);
                Page dstIndex = pool.GetPage(header.root[curr].index + (long) i * Page.pageSize);
                j = 0;
                do
                {
                    long pos = Bytes.Unpack8(dstIndex.data, j);
                    if (Bytes.Unpack8(srcIndex.data, j) != pos)
                    {
                        if ((pos & dbFreeHandleFlag) == 0)
                        {
                            if ((pos & dbPageObjectFlag) != 0)
                            {
                                Free(pos & ~ dbFlagsMask, Page.pageSize);
                            }
                            else if (pos != 0)
                            {
                                int offs = (int) pos & (Page.pageSize - 1);
                                pg = pool.GetPage(pos - offs);
                                Free(pos, ObjectHeader.GetSize(pg.data, offs));
                                pool.Unfix(pg);
                            }
                        }
                    }
                    j += 8;
                }
                while (--n != 0);
                pool.Unfix(srcIndex);
                pool.Unfix(dstIndex);
            }

            for (i = 0; i <= nPages; i++)
            {
                if ((map[i >> 5] & (1 << (i & 31))) != 0)
                {
                    pg = pool.PutPage(header.root[1 - curr].index + (long) i * Page.pageSize);
                    for (j = 0; j < Page.pageSize; j += 8)
                    {
                        Bytes.Pack8(pg.data, j, Bytes.Unpack8(pg.data, j) & ~ dbModifiedFlag);
                    }
                    pool.Unfix(pg);
                }
            }

            if (currIndexSize > committedIndexSize)
            {
                long page = (header.root[1 - curr].index + committedIndexSize * 8L) & ~ (Page.pageSize - 1);
                long end = (header.root[1 - curr].index + Page.pageSize - 1 + currIndexSize * 8L) & ~ (Page.pageSize - 1);
                while (page < end)
                {
                    pg = pool.PutPage(page);
                    for (j = 0; j < Page.pageSize; j += 8)
                    {
                        Bytes.Pack8(pg.data, j, Bytes.Unpack8(pg.data, j) & ~ dbModifiedFlag);
                    }
                    pool.Unfix(pg);
                    page += Page.pageSize;
                }
            }
            header.root[1 - curr].usedSize = usedSize;
            pg = pool.PutPage(0);
            header.Pack(pg.data);
            pool.Flush();
            pool.Modify(pg);
            header.curr = curr ^= 1;
            header.dirty = true;
            header.Pack(pg.data);
            pool.Unfix(pg);
            pool.Flush();
            header.root[1 - curr].size = header.root[curr].size;
            header.root[1 - curr].indexUsed = currIndexSize;
            header.root[1 - curr].freeList = header.root[curr].freeList;
            header.root[1 - curr].bitmapEnd = header.root[curr].bitmapEnd;
            header.root[1 - curr].rootObject = header.root[curr].rootObject;
            header.root[1 - curr].classDescList = header.root[curr].classDescList;
            header.root[1 - curr].bitmapExtent = header.root[curr].bitmapExtent;

            if (currIndexSize == 0 || newIndexSize != oldIndexSize)
            {
                header.root[1 - curr].index = header.root[curr].shadowIndex;
                header.root[1 - curr].indexSize = header.root[curr].shadowIndexSize;
                header.root[1 - curr].shadowIndex = header.root[curr].index;
                header.root[1 - curr].shadowIndexSize = header.root[curr].indexSize;
                pool.Copy(header.root[1 - curr].index, header.root[curr].index, currIndexSize * 8L);
                i = SupportClass.URShift((currIndexSize + dbHandlesPerPage * 32 - 1), (dbHandlesPerPageBits + 5));
                while (--i >= 0)
                {
                    map[i] = 0;
                }
            }
            else
            {
                for (i = 0; i < nPages; i++)
                {
                    if ((map[i >> 5] & (1 << (i & 31))) != 0)
                    {
                        map[i >> 5] -= (1 << (i & 31));
                        pool.Copy(header.root[1 - curr].index + (long)i * Page.pageSize, header.root[curr].index + (long)i * Page.pageSize, Page.pageSize);
                    }
                }
                if (currIndexSize > i * dbHandlesPerPage && ((map[i >> 5] & (1 << (i & 31))) != 0 || currIndexSize != committedIndexSize))
                {
                    pool.Copy(header.root[1 - curr].index + (long)i * Page.pageSize, header.root[curr].index + (long)i * Page.pageSize, 8L * currIndexSize - (long)i * Page.pageSize);
                    j = SupportClass.URShift(i, 5);
                    n = SupportClass.URShift((currIndexSize + dbHandlesPerPage * 32 - 1), (dbHandlesPerPageBits + 5));
                    while (j < n)
                    {
                        map[j++] = 0;
                    }
                }
            }
            gcDone = false;
            currIndex = curr;
            committedIndexSize = currIndexSize;
        }

        public virtual void Rollback()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                objectCache.Invalidate();
                if (!modified)
                {
                    return;
                }
                Rollback0();
                modified = false;
            }
        }

        private void Rollback0()
        {
            int curr = currIndex;
            int[] map = dirtyPagesMap;
            if (header.root[1 - curr].index != header.root[curr].shadowIndex)
            {
                pool.Copy(header.root[curr].shadowIndex, header.root[curr].index, 8L * committedIndexSize);
            }
            else
            {
                int nPages = SupportClass.URShift((committedIndexSize + dbHandlesPerPage - 1), dbHandlesPerPageBits);
                for (int i = 0; i < nPages; i++)
                {
                    if ((map[i >> 5] & (1 << (i & 31))) != 0)
                    {
                        pool.Copy(header.root[curr].shadowIndex + (long)i * Page.pageSize, header.root[curr].index + (long)i * Page.pageSize, Page.pageSize);
                    }
                }
            }
            for (int j = SupportClass.URShift((currIndexSize + dbHandlesPerPage * 32 - 1), (dbHandlesPerPageBits + 5)); --j >= 0; map[j] = 0)
                ;
            header.root[1 - curr].index = header.root[curr].shadowIndex;
            header.root[1 - curr].indexSize = header.root[curr].shadowIndexSize;
            header.root[1 - curr].indexUsed = committedIndexSize;
            header.root[1 - curr].freeList = header.root[curr].freeList;
            header.root[1 - curr].bitmapEnd = header.root[curr].bitmapEnd;
            header.root[1 - curr].size = header.root[curr].size;
            header.root[1 - curr].rootObject = header.root[curr].rootObject;
            header.root[1 - curr].classDescList = header.root[curr].classDescList;
            header.root[1 - curr].bitmapExtent = header.root[curr].bitmapExtent;
            usedSize = header.root[curr].size;
            currIndexSize = committedIndexSize;
            currRBitmapPage = currPBitmapPage = 0;
            currRBitmapOffs = currPBitmapOffs = 0;
            ReloadScheme();
        }

        public virtual void Backup(System.IO.Stream streamOut)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                objectCache.Flush();

                int curr = 1 - currIndex;
                int nObjects = header.root[curr].indexUsed;
                long indexOffs = header.root[curr].index;
                int i, j, k;
                int nUsedIndexPages = (nObjects + dbHandlesPerPage - 1) / dbHandlesPerPage;
                int nIndexPages = (int) ((header.root[curr].indexSize + dbHandlesPerPage - 1) / dbHandlesPerPage);
                long totalRecordsSize = 0;
                long nPagedObjects = 0;
                int bitmapExtent = header.root[curr].bitmapExtent;
                long[] index = new long[nObjects];
                int[] oids = new int[nObjects];

                if (bitmapExtent == 0)
                    bitmapExtent = Int32.MaxValue;

                for (i = 0, j = 0; i < nUsedIndexPages; i++)
                {
                    Page pg = pool.GetPage(indexOffs + (long) i * Page.pageSize);
                    for (k = 0; k < dbHandlesPerPage && j < nObjects; k++, j++)
                    {
                        long pos = Bytes.Unpack8(pg.data, k * 8);
                        index[j] = pos;
                        oids[j] = j;
                        if ((pos & dbFreeHandleFlag) == 0)
                        {
                            if ((pos & dbPageObjectFlag) != 0)
                            {
                                nPagedObjects += 1;
                            }
                            else if (pos != 0)
                            {
                                int offs = (int) pos & (Page.pageSize - 1);
                                Page op = pool.GetPage(pos - offs);
                                int size = ObjectHeader.GetSize(op.data, offs & ~dbFlagsMask);
                                size = (size + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1);
                                totalRecordsSize += size;
                                pool.Unfix(op);
                            }
                        }
                    }
                    pool.Unfix(pg);
                }

                Header newHeader = new Header();
                newHeader.curr = 0;
                newHeader.dirty = false;
                newHeader.initialized = true;
                long newFileSize = (long) (nPagedObjects + nIndexPages * 2 + 1) * Page.pageSize + totalRecordsSize;
                newFileSize = (newFileSize + Page.pageSize - 1) & ~ (Page.pageSize - 1);
                newHeader.root = new RootPage[2];
                newHeader.root[0] = new RootPage();
                newHeader.root[1] = new RootPage();
                newHeader.root[0].size = newHeader.root[1].size = newFileSize;
                newHeader.root[0].index = newHeader.root[1].shadowIndex = Page.pageSize;
                newHeader.root[0].shadowIndex = newHeader.root[1].index = Page.pageSize + (long) nIndexPages * Page.pageSize;
                newHeader.root[0].shadowIndexSize = newHeader.root[0].indexSize = newHeader.root[1].shadowIndexSize = newHeader.root[1].indexSize = nIndexPages * dbHandlesPerPage;
                newHeader.root[0].indexUsed = newHeader.root[1].indexUsed = nObjects;
                newHeader.root[0].freeList = newHeader.root[1].freeList = header.root[curr].freeList;
                newHeader.root[0].bitmapEnd = newHeader.root[1].bitmapEnd = header.root[curr].bitmapEnd;

                newHeader.root[0].rootObject = newHeader.root[1].rootObject = header.root[curr].rootObject;
                newHeader.root[0].classDescList = newHeader.root[1].classDescList = header.root[curr].classDescList;
                newHeader.root[0].bitmapExtent = newHeader.root[1].bitmapExtent = header.root[curr].bitmapExtent;
                byte[] page = new byte[Page.pageSize];
                newHeader.Pack(page);
                streamOut.Write(page, 0, page.Length);

                long pageOffs = (long) (nIndexPages * 2 + 1) * Page.pageSize;
                long recOffs = (long) (nPagedObjects + nIndexPages * 2 + 1) * Page.pageSize;
                GenericSort.Sort(new AnonymousClassGenericSortArray(nObjects, index, oids, this));
                byte[] newIndex = new byte[nIndexPages * dbHandlesPerPage * 8];
                for (i = 0; i < nObjects; i++)
                {
                    long pos = index[i];
                    int oid = oids[i];
                    if ((pos & dbFreeHandleFlag) == 0)
                    {
                        if ((pos & dbPageObjectFlag) != 0)
                        {
                            Bytes.Pack8(newIndex, oid * 8, pageOffs | dbPageObjectFlag);
                            pageOffs += Page.pageSize;
                        }
                        else if (pos != 0)
                        {
                            Bytes.Pack8(newIndex, oid * 8, recOffs);
                            int offs = (int) pos & (Page.pageSize - 1);
                            Page op = pool.GetPage(pos - offs);
                            int size = ObjectHeader.GetSize(op.data, offs & ~dbFlagsMask);
                            size = (size + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1);
                            recOffs += size;
                            pool.Unfix(op);
                        }
                    }
                    else
                    {
                        Bytes.Pack8(newIndex, oid * 8, pos);
                    }
                }
                streamOut.Write(newIndex, 0, newIndex.Length);
                streamOut.Write(newIndex, 0, newIndex.Length);

                for (i = 0; i < nObjects; i++)
                {
                    long pos = index[i];
                    if (((int) pos & (dbFreeHandleFlag | dbPageObjectFlag)) == dbPageObjectFlag)
                    {
                        if (oids[i] < dbBitmapId + dbBitmapPages || (oids[i] >= bitmapExtent && oids[i] < bitmapExtent + dbLargeBitmapPages - dbBitmapPages))
                        {
                            int pageId = oids[i] < dbBitmapId + dbBitmapPages ? oids[i] - dbBitmapId : oids[i] - bitmapExtent;
                            long mappedSpace = (long) pageId * Page.pageSize * 8 * dbAllocationQuantum;
                            if (mappedSpace >= newFileSize)
                            {
                                SupportClass.ArraySupport.Fill(page, 0);
                            }
                            else if (mappedSpace + Page.pageSize * 8 * dbAllocationQuantum <= newFileSize)
                            {
                                SupportClass.ArraySupport.Fill(page, 0xff);
                            }
                            else
                            {
                                int nBits = (int) ((newFileSize - mappedSpace) >> dbAllocationQuantumBits);
                                SupportClass.ArraySupport.Fill(page, 0, nBits >> 3, 0xff);
                                page[nBits >> 3] = (byte) ((1 << (nBits & 7)) - 1);
                                SupportClass.ArraySupport.Fill(page, (nBits >> 3) + 1, Page.pageSize, 0);
                            };
                            streamOut.Write(page, 0, page.Length);
                        }
                        else
                        {
                            Page pg = pool.GetPage(pos & ~ dbFlagsMask);
                            streamOut.Write(pg.data, 0, pg.data.Length);
                            pool.Unfix(pg);
                        }
                    }
                }

                for (i = 0; i < nObjects; i++)
                {
                    long pos = index[i];
                    if (pos != 0 && ((int) pos & (dbFreeHandleFlag | dbPageObjectFlag)) == 0)
                    {
                        pos &= ~ dbFlagsMask;
                        int offs = (int) pos & (Page.pageSize - 1);
                        Page pg = pool.GetPage(pos - offs);
                        int size = ObjectHeader.GetSize(pg.data, offs);
                        size = (size + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1);

                        while (true)
                        {
                            if (Page.pageSize - offs >= size)
                            {
                                streamOut.Write(pg.data, offs, size);
                                break;
                            }
                            streamOut.Write(pg.data, offs, Page.pageSize - offs);
                            size -= (Page.pageSize - offs);
                            pos += Page.pageSize - offs;
                            offs = 0;
                            pool.Unfix(pg);
                            pg = pool.GetPage(pos);
                        }
                        pool.Unfix(pg);
                    }
                }

                if (recOffs != newFileSize)
                {
                    Assert.That(newFileSize - recOffs < Page.pageSize);
                    int align = (int) (newFileSize - recOffs);
                    SupportClass.ArraySupport.Fill(page, 0, align, (byte) 0);
                    streamOut.Write(page, 0, align);
                }
            }
        }

        public virtual IPersistentSet CreateSet()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                IPersistentSet set_Renamed = alternativeBtree ? (IPersistentSet) new AltPersistentSet() : (IPersistentSet) new PersistentSet();
                set_Renamed.AssignOid(this, 0, false);
                return set_Renamed;
            }
        }

        public virtual IPersistentSet CreateScalableSet()
        {
            lock (this)
            {
                return CreateScalableSet(8);
            }
        }

        public virtual IPersistentSet CreateScalableSet(int initialSize)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                return new ScalableSet(this, initialSize);
            }
        }

        public virtual Index CreateIndex(Type keyType, bool unique)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                Index index;
                if (alternativeBtree)
                    index = new AltBtree(keyType, unique);
                else
                    index = new Btree(keyType, unique);
                index.AssignOid(this, 0, false);
                return index;
            }
        }

        public virtual Index CreateThickIndex(Type keyType)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                return new ThickIndex(keyType, this);
            }
        }

        public virtual BitIndex CreateBitIndex()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                BitIndex index = new BitIndexImpl();
                index.AssignOid(this, 0, false);
                return index;
            }
        }

#if !OMIT_RTREE
        public virtual SpatialIndex CreateSpatialIndex()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                return new Rtree();
            }
        }
#endif

#if !OMIT_RTREER2
        public virtual SpatialIndexR2 CreateSpatialIndexR2()
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                SpatialIndexR2 index = new RtreeR2();
                index.AssignOid(this, 0, false);
                return index;
            }
        }
#endif

        public virtual FieldIndex CreateFieldIndex(Type type, string fieldName, bool unique)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                FieldIndex index = null;
                if (alternativeBtree)
                    index = (FieldIndex) new AltBtreeFieldIndex(type, fieldName, unique);
                else
                    index = (FieldIndex) new BtreeFieldIndex(type, fieldName, unique);
                index.AssignOid(this, 0, false);
                return index;
            }
        }

        public virtual FieldIndex CreateFieldIndex(Type type, string[] fieldNames, bool unique)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                FieldIndex index = null;
                if (alternativeBtree)
                    index = (FieldIndex) new AltBtreeMultiFieldIndex(type, fieldNames, unique);
                else
                    index = (FieldIndex) new BtreeMultiFieldIndex(type, fieldNames, unique);
                index.AssignOid(this, 0, false);
                return index;
            }
        }

        public virtual SortedCollection CreateSortedCollection(PersistentComparator comparator, bool unique)
        {
            if (!opened)
                throw new StorageError(StorageError.STORAGE_NOT_OPENED);
            return new Ttree(comparator, unique);
        }

        public virtual SortedCollection CreateSortedCollection(bool unique)
        {
            if (!opened)
                throw new StorageError(StorageError.STORAGE_NOT_OPENED);
            return new Ttree(new DefaultPersistentComparator(), unique);
        }

        public virtual Link CreateLink()
        {
            return CreateLink(8);
        }

        public virtual Link CreateLink(int initialSize)
        {
            return new LinkImpl(initialSize);
        }

        public virtual Relation CreateRelation(IPersistent owner)
        {
            return new RelationImpl(owner);
        }

        public virtual IBlob CreateBlob()
        {
            return new BlobImpl(this, Page.pageSize - ObjectHeader.Sizeof - 3 * 4);
        }

#if !OMIT_TIME_SERIES
        public virtual TimeSeries CreateTimeSeries(Type blockClass, long maxBlockTimeInterval)
        {
            return new TimeSeriesImpl(this, blockClass, maxBlockTimeInterval);
        }
#endif

#if !OMIT_PATRICIA_TRIE
        public virtual PatriciaTrie CreatePatriciaTrie()
        {
            return new PTrie();
        }
#endif

        internal long GetGCPos(int oid)
        {
            Page pg = pool.GetPage(header.root[currIndex].index + ((long) (SupportClass.URShift(oid, dbHandlesPerPageBits)) << Page.pageBits));
            long pos = Bytes.Unpack8(pg.data, (oid & (dbHandlesPerPage - 1)) << 3);
            pool.Unfix(pg);
            return pos;
        }

        internal void MarkOid(int oid)
        {
            if (oid != 0)
            {
                long pos = GetGCPos(oid);
                if ((pos & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
                {
                    throw new StorageError(StorageError.INVALID_OID);
                }
                int bit = (int) (SupportClass.URShift(pos, dbAllocationQuantumBits));
                if ((blackBitmap[SupportClass.URShift(bit, 5)] & (1 << (bit & 31))) == 0)
                {
                    greyBitmap[SupportClass.URShift(bit, 5)] |= 1 << (bit & 31);
                }
            }
        }

        internal Page GetGCPage(int oid)
        {
            return pool.GetPage(GetGCPos(oid) & ~ dbFlagsMask);
        }

        private void Mark()
        {
            int bitmapSize = (int) (SupportClass.URShift(header.root[currIndex].size, (dbAllocationQuantumBits + 5))) + 1;
            bool existsNotMarkedObjects;
            long pos;
            int i, j;

            if (listener != null)
            {
                listener.GcStarted();
            }

            greyBitmap = new int[bitmapSize];
            blackBitmap = new int[bitmapSize];
            int rootOid = header.root[currIndex].rootObject;
            if (rootOid != 0)
            {
                MarkOid(rootOid);
                do
                {
                    existsNotMarkedObjects = false;
                    for (i = 0; i < bitmapSize; i++)
                    {
                        if (greyBitmap[i] != 0)
                        {
                            existsNotMarkedObjects = true;
                            for (j = 0; j < 32; j++)
                            {
                                if ((greyBitmap[i] & (1 << j)) != 0)
                                {
                                    pos = (((long) i << 5) + j) << dbAllocationQuantumBits;
                                    greyBitmap[i] &= ~ (1 << j);
                                    blackBitmap[i] |= 1 << j;
                                    int offs = (int) pos & (Page.pageSize - 1);
                                    Page pg = pool.GetPage(pos - offs);
                                    int typeOid = ObjectHeader.GetType(pg.data, offs);
                                    if (typeOid != 0)
                                    {
                                        ClassDescriptor desc = FindClassDescriptor(typeOid);
                                        if (typeof(Btree).IsAssignableFrom(desc.cls))
                                        {
                                            Btree btree = new Btree(pg.data, ObjectHeader.Sizeof + offs);
                                            btree.AssignOid(this, 0, false);
                                            btree.MarkTree();
                                        }
                                        else if (desc.hasReferences)
                                        {
                                            MarkObject(pool.Get(pos), ObjectHeader.Sizeof, desc);
                                        }
                                    }
                                    pool.Unfix(pg);
                                }
                            }
                        }
                    }
                }
                while (existsNotMarkedObjects);
            }
        }

        private int Sweep()
        {
            int nDeallocated = 0;
            long pos;
            gcDone = true;
            for (int i = dbFirstUserId, j = committedIndexSize; i < j; i++)
            {
                pos = GetGCPos(i);
                if (pos != 0 && ((int) pos & (dbPageObjectFlag | dbFreeHandleFlag)) == 0)
                {
                    int bit = (int) (SupportClass.URShift(pos, dbAllocationQuantumBits));
                    if ((blackBitmap[SupportClass.URShift(bit, 5)] & (1 << (bit & 31))) == 0)
                    {
                        // object is not accessible
                        if (GetPos(i) != pos)
                        {
                            throw new StorageError(StorageError.INVALID_OID);
                        }
                        int offs = (int) pos & (Page.pageSize - 1);
                        Page pg = pool.GetPage(pos - offs);
                        int typeOid = ObjectHeader.GetType(pg.data, offs);
                        if (typeOid != 0)
                        {
                            ClassDescriptor desc = FindClassDescriptor(typeOid);
                            nDeallocated += 1;
                            if (typeof(Btree).IsAssignableFrom(desc.cls))
                            {
                                Btree btree = new Btree(pg.data, ObjectHeader.Sizeof + offs);
                                pool.Unfix(pg);
                                btree.AssignOid(this, i, false);
                                btree.Deallocate();
                            }
                            else
                            {
                                int size = ObjectHeader.GetSize(pg.data, offs);
                                pool.Unfix(pg);
                                FreeId(i);
                                objectCache.Remove(i);
                                CloneBitmap(pos, size);
                            }
                            if (listener != null)
                            {
                                listener.DeallocateObject(desc.cls, i);
                            }
                        }
                    }
                }
            }

            greyBitmap = null;
            blackBitmap = null;
            allocatedDelta = 0;
            gcActive = false;

            if (listener != null)
                listener.GcCompleted(nDeallocated);

            return nDeallocated;
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'GcThread' to access its enclosing instance.
        internal class GcThread : SupportClass.ThreadClass
        {
            private void InitBlock(StorageImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private StorageImpl enclosingInstance;

            public StorageImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }
            private bool go;

            internal GcThread(StorageImpl enclosingInstance)
            {
                InitBlock(enclosingInstance);
                Start();
            }

            internal virtual void Activate()
            {
                lock (Enclosing_Instance.backgroundGcStartMonitor)
                {
                    go = true;
                    System.Threading.Monitor.Pulse(Enclosing_Instance.backgroundGcStartMonitor);
                }
            }

            public override void Run()
            {
                try
                {
                    while (true)
                    {
                        lock (Enclosing_Instance.backgroundGcStartMonitor)
                        {
                            while (!go && Enclosing_Instance.opened)
                            {
                                System.Threading.Monitor.Wait(Enclosing_Instance.backgroundGcStartMonitor);
                            }
                            if (!Enclosing_Instance.opened)
                            {
                                return;
                            }
                            go = false;
                        }
                        lock (Enclosing_Instance.backgroundGcMonitor)
                        {
                            if (!Enclosing_Instance.opened)
                            {
                                return;
                            }

                            Enclosing_Instance.Mark();
                            lock (Enclosing_Instance)
                            {
                                lock (Enclosing_Instance.objectCache)
                                {
                                    Enclosing_Instance.Sweep();
                                }
                            }
                        }
                    }
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                }
            }
        }

        public virtual int Gc()
        {
            lock (this)
            {
                return Gc0();
            }
        }

        private int Gc0()
        {
            lock (objectCache)
            {
                if (!opened)
                {
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                }
                if (gcDone || gcActive)
                {
                    return 0;
                }
                gcActive = true;
                if (backgroundGc)
                {
                    if (gcThread == null)
                    {
                        gcThread = new GcThread(this);
                    }
                    gcThread.Activate();
                    return 0;
                }
                // System.out.println("Start GC, allocatedDelta=" + allocatedDelta + ", header[" + currIndex + "].size=" + header.root[currIndex].size + ", gcTreshold=" + gcThreshold);
                Mark();
                return Sweep();
            }
        }

        //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
        public virtual Hashtable GetMemoryDump()
        {
            lock (this)
            {
                lock (objectCache)
                {
                    if (!opened)
                    {
                        throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                    }
                    int bitmapSize = (int) (SupportClass.URShift(header.root[currIndex].size, (dbAllocationQuantumBits + 5))) + 1;
                    bool existsNotMarkedObjects;
                    long pos;
                    int i, j;

                    // mark
                    greyBitmap = new int[bitmapSize];
                    blackBitmap = new int[bitmapSize];
                    int rootOid = header.root[currIndex].rootObject;
                    //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
                    Hashtable map = new Hashtable();

                    if (rootOid != 0)
                    {
                        MemoryUsage indexUsage = new MemoryUsage(typeof(Index));
                        MemoryUsage fieldIndexUsage = new MemoryUsage(typeof(FieldIndex));
                        MemoryUsage classUsage = new MemoryUsage(typeof(Type));

                        MarkOid(rootOid);
                        do
                        {
                            existsNotMarkedObjects = false;
                            for (i = 0; i < bitmapSize; i++)
                            {
                                if (greyBitmap[i] != 0)
                                {
                                    existsNotMarkedObjects = true;
                                    for (j = 0; j < 32; j++)
                                    {
                                        if ((greyBitmap[i] & (1 << j)) != 0)
                                        {
                                            pos = (((long) i << 5) + j) << dbAllocationQuantumBits;
                                            greyBitmap[i] &= ~ (1 << j);
                                            blackBitmap[i] |= 1 << j;
                                            int offs = (int) pos & (Page.pageSize - 1);
                                            Page pg = pool.GetPage(pos - offs);
                                            int typeOid = ObjectHeader.GetType(pg.data, offs);
                                            int objSize = ObjectHeader.GetSize(pg.data, offs);
                                            int alignedSize = (objSize + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1);
                                            if (typeOid != 0)
                                            {
                                                MarkOid(typeOid);
                                                ClassDescriptor desc = FindClassDescriptor(typeOid);
                                                if (typeof(Btree).IsAssignableFrom(desc.cls))
                                                {
                                                    Btree btree = new Btree(pg.data, ObjectHeader.Sizeof + offs);
                                                    btree.AssignOid(this, 0, false);
                                                    int nPages = btree.MarkTree();
                                                    if (typeof(FieldIndex).IsAssignableFrom(desc.cls))
                                                    {
                                                        fieldIndexUsage.nInstances += 1;
                                                        fieldIndexUsage.totalSize += (long) nPages * Page.pageSize + objSize;
                                                        fieldIndexUsage.allocatedSize += (long) nPages * Page.pageSize + alignedSize;
                                                    }
                                                    else
                                                    {
                                                        indexUsage.nInstances += 1;
                                                        indexUsage.totalSize += (long) nPages * Page.pageSize + objSize;
                                                        indexUsage.allocatedSize += (long) nPages * Page.pageSize + alignedSize;
                                                    }
                                                }
                                                else
                                                {
                                                    //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                                                    MemoryUsage usage = (MemoryUsage) map[desc.cls];
                                                    if (usage == null)
                                                    {
                                                        usage = new MemoryUsage(desc.cls);
                                                        map[desc.cls] = usage;
                                                    }
                                                    usage.nInstances += 1;
                                                    usage.totalSize += objSize;
                                                    usage.allocatedSize += alignedSize;

                                                    if (desc.hasReferences)
                                                    {
                                                        MarkObject(pool.Get(pos), ObjectHeader.Sizeof, desc);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                classUsage.nInstances += 1;
                                                classUsage.totalSize += objSize;
                                                classUsage.allocatedSize += alignedSize;
                                            }
                                            pool.Unfix(pg);
                                        }
                                    }
                                }
                            }
                        }
                        while (existsNotMarkedObjects);

                        if (indexUsage.nInstances != 0)
                        {
                            map[typeof(Index)] = indexUsage;
                        }

                        if (fieldIndexUsage.nInstances != 0)
                        {
                            map[typeof(FieldIndex)] = fieldIndexUsage;
                        }

                        if (classUsage.nInstances != 0)
                        {
                            map[typeof(Type)] = classUsage;
                        }

                        MemoryUsage system = new MemoryUsage(typeof(Storage));
                        system.totalSize += header.root[0].indexSize * 8L;
                        system.totalSize += header.root[1].indexSize * 8L;
                        system.totalSize += (long) (header.root[currIndex].bitmapEnd - dbBitmapId) * Page.pageSize;
                        system.totalSize += Page.pageSize; // root page

                        if (header.root[currIndex].bitmapExtent != 0)
                            system.allocatedSize = GetBitmapUsedSpace(dbBitmapId, dbBitmapId + dbBitmapPages) + GetBitmapUsedSpace(header.root[currIndex].bitmapExtent, header.root[currIndex].bitmapExtent + header.root[currIndex].bitmapEnd - dbBitmapId);
                        else
                            system.allocatedSize = GetBitmapUsedSpace(dbBitmapId, header.root[currIndex].bitmapEnd);

                        system.nInstances = header.root[currIndex].indexSize;
                        map[typeof(Storage)] = system;
                    }
                    return map;
                }
            }
        }

        internal long GetBitmapUsedSpace(int from, int till)
        {
            long allocated = 0;
            while (from < till)
            {
                Page pg = GetGCPage(from);
                for (int j = 0; j < Page.pageSize; j++)
                {
                    int mask = pg.data[j] & 0xFF;
                    while (mask != 0)
                    {
                        if ((mask & 1) != 0)
                        {
                            allocated += dbAllocationQuantum;
                        }
                        mask >>= 1;
                    }
                }
                pool.Unfix(pg);
                from += 1;
            }
            return allocated;
        }

        internal int MarkObject(byte[] obj, int offs, ClassDescriptor desc)
        {
            ClassDescriptor.FieldDescriptor[] all = desc.allFields;

            for (int i = 0, n = all.Length; i < n; i++)
            {
                ClassDescriptor.FieldDescriptor fd = all[i];
                switch (fd.type)
                {
                    case ClassDescriptor.tpBoolean:
                    case ClassDescriptor.tpByte:
                        offs += 1;
                        continue;

                    case ClassDescriptor.tpChar:
                    case ClassDescriptor.tpShort:
                        offs += 2;
                        continue;

                    case ClassDescriptor.tpInt:
                    case ClassDescriptor.tpFloat:
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpLong:
                    case ClassDescriptor.tpDouble:
                    case ClassDescriptor.tpDate:
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpString:
                    {
                        int strlen = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (strlen > 0)
                        {
                            offs += strlen * 2;
                        }
                        else if (strlen < -1)
                        {
                            offs -= (strlen + 2);
                        }
                        continue;
                    }

                    case ClassDescriptor.tpObject:
                        MarkOid(Bytes.Unpack4(obj, offs));
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpValue:
                        offs = MarkObject(obj, offs, fd.valueDesc);
                        continue;

                    case ClassDescriptor.tpRaw:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (len > 0)
                        {
                            offs += len;
                        }
                        else if (len == -2 - ClassDescriptor.tpObject)
                        {
                            MarkOid(Bytes.Unpack4(obj, offs));
                            offs += 4;
                        }
                        else if (len < -1)
                        {
                            offs += ClassDescriptor.Sizeof[-2 - len];
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfByte:
                    case ClassDescriptor.tpArrayOfBoolean:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (len > 0)
                        {
                            offs += len;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfShort:
                    case ClassDescriptor.tpArrayOfChar:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (len > 0)
                        {
                            offs += len * 2;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfInt:
                    case ClassDescriptor.tpArrayOfFloat:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (len > 0)
                        {
                            offs += len * 4;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfLong:
                    case ClassDescriptor.tpArrayOfDouble:
                    case ClassDescriptor.tpArrayOfDate:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        if (len > 0)
                        {
                            offs += len * 8;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfString:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        while (--len >= 0)
                        {
                            int strlen = Bytes.Unpack4(obj, offs);
                            offs += 4;
                            if (strlen > 0)
                            {
                                offs += strlen * 2;
                            }
                            else if (strlen < -1)
                            {
                                offs -= (strlen + 2);
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfObject:
                    case ClassDescriptor.tpLink:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        while (--len >= 0)
                        {
                            MarkOid(Bytes.Unpack4(obj, offs));
                            offs += 4;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfValue:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        ClassDescriptor valueDesc = fd.valueDesc;
                        while (--len >= 0)
                        {
                            offs = MarkObject(obj, offs, valueDesc);
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfRaw:
                    {
                        int len = Bytes.Unpack4(obj, offs);
                        offs += 4;
                        while (--len >= 0)
                        {
                            int rawlen = Bytes.Unpack4(obj, offs);
                            offs += 4;
                            if (rawlen >= 0)
                            {
                                offs += rawlen;
                            }
                            else if (rawlen == -2 - ClassDescriptor.tpObject)
                            {
                                MarkOid(Bytes.Unpack4(obj, offs));
                                offs += 4;
                            }
                            else if (rawlen < -1)
                            {
                                offs += ClassDescriptor.Sizeof[-2 - rawlen];
                            }
                            continue;
                        }
                        continue;
                    }
                    }
            }
            return offs;
        }

        internal class ThreadTransactionContext
        {
            internal int nested;
            internal ArrayList locked = new ArrayList();
            internal ArrayList modified = new ArrayList();
        }

        public virtual void BeginThreadTransaction(int mode)
        {
            switch (mode)
            {
                case StorageConstants.SERIALIZABLE_TRANSACTION:
                    useSerializableTransactions = true;
                    TransactionContext.nested += 1;
                    break;

                case StorageConstants.EXCLUSIVE_TRANSACTION:
                case StorageConstants.COOPERATIVE_TRANSACTION:
                    lock (transactionMonitor)
                    {
                        if (scheduledCommitTime != Int64.MaxValue)
                        {
                            nBlockedTransactions += 1;
                            while ((DateTime.Now.Ticks - 621355968000000000) / 10000 >= scheduledCommitTime)
                            {
                                try
                                {
                                    System.Threading.Monitor.Wait(transactionMonitor);
                                }
                                catch (System.Threading.ThreadInterruptedException)
                                {
                                }
                            }
                            nBlockedTransactions -= 1;
                        }
                        nNestedTransactions += 1;
                    }

                    if (mode == StorageConstants.EXCLUSIVE_TRANSACTION)
                        transactionLock.ExclusiveLock();
                    else
                        transactionLock.SharedLock();

                    break;

                default:
                    throw new System.ArgumentException("Illegal transaction mode");
            }
        }

        public virtual void EndThreadTransaction()
        {
            EndThreadTransaction(Int32.MaxValue);
        }

        public virtual void EndThreadTransaction(int maxDelay)
        {
            ThreadTransactionContext ctx = TransactionContext;
            if (ctx.nested != 0)
            {
                // serializable transaction
                if (--ctx.nested == 0)
                {
                    int i = ctx.modified.Count;
                    if (i != 0)
                    {
                        do
                        {
                            ((IPersistent) ctx.modified[--i]).Store();
                        }
                        while (i != 0);

                        lock (backgroundGcMonitor)
                        {
                            lock (this)
                            {
                                Commit0();
                            }
                        }
                    }
                    for (i = ctx.locked.Count; --i >= 0; )
                    {
                        ((IResource) ctx.locked[i]).Reset();
                    }
                    ctx.modified.Clear();
                    ctx.locked.Clear();
                }
            }
            else
            {
                // exclusive or cooperative transaction
                lock (transactionMonitor)
                {
                    transactionLock.Unlock();

                    if (nNestedTransactions != 0)
                    {
                        // may be everything is already aborted
                        if (--nNestedTransactions == 0)
                        {
                            nCommittedTransactions += 1;
                            Commit();
                            scheduledCommitTime = Int64.MaxValue;
                            if (nBlockedTransactions != 0)
                            {
                                System.Threading.Monitor.PulseAll(transactionMonitor);
                            }
                        }
                        else
                        {
                            if (maxDelay != Int32.MaxValue)
                            {
                                long nextCommit = (DateTime.Now.Ticks - 621355968000000000) / 10000 + maxDelay;
                                if (nextCommit < scheduledCommitTime)
                                {
                                    scheduledCommitTime = nextCommit;
                                }
                                if (maxDelay == 0)
                                {
                                    int n = nCommittedTransactions;
                                    nBlockedTransactions += 1;
                                    do
                                    {
                                        try
                                        {
                                            System.Threading.Monitor.Wait(transactionMonitor);
                                        }
                                        catch (System.Threading.ThreadInterruptedException)
                                        {
                                        }
                                    }
                                    while (nCommittedTransactions == n);
                                    nBlockedTransactions -= 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual void RollbackThreadTransaction()
        {
            ThreadTransactionContext ctx = TransactionContext;
            if (ctx.nested != 0)
            {
                // serializable transaction
                ctx.nested = 0;
                int i = ctx.modified.Count;
                if (i != 0)
                {
                    do
                    {
                        ((IPersistent) ctx.modified[--i]).Invalidate();
                    }
                    while (i != 0);

                    lock (this)
                    {
                        Rollback0();
                    }
                }
                for (i = ctx.locked.Count; --i >= 0; )
                {
                    ((IResource) ctx.locked[i]).Reset();
                }
                ctx.modified.Clear();
                ctx.locked.Clear();
            }
            else
            {
                lock (transactionMonitor)
                {
                    transactionLock.Reset();
                    nNestedTransactions = 0;
                    if (nBlockedTransactions != 0)
                    {
                        System.Threading.Monitor.PulseAll(transactionMonitor);
                    }
                    Rollback();
                }
            }
        }

        public virtual void LockObject(IPersistent obj)
        {
            if (useSerializableTransactions)
            {
                ThreadTransactionContext ctx = TransactionContext;
                if (ctx.nested != 0)
                {
                    // serializable transaction
                    ctx.locked.Add(obj);
                }
            }
        }

        public virtual void Close()
        {
            lock (backgroundGcMonitor)
            {
                Commit();
                opened = false;
            }
            if (gcThread != null)
            {
                gcThread.Activate();
                try
                {
                    gcThread.Join();
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                }
            }
            if (IsDirty())
            {
                Page pg = pool.PutPage(0);
                header.Pack(pg.data);
                pool.Flush();
                pool.Modify(pg);
                header.dirty = false;
                header.Pack(pg.data);
                pool.Unfix(pg);
                pool.Flush();
            }
            pool.Close();
            // make GC easier
            pool = null;
            objectCache = null;
            classDescMap = null;
            bitmapPageAvailableSpace = null;
            dirtyPagesMap = null;
            descList = null;
        }

#if !OMIT_XML
        //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Writer' and 'System.IO.StreamWriter' may cause compilation errors.
        public virtual void ExportXML(System.IO.StreamWriter writer)
        {
            lock (this)
            {
                if (!opened)
                {
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                }
                objectCache.Flush();
                int rootOid = header.root[1 - currIndex].rootObject;
                if (rootOid != 0)
                {
                    XMLExporter xmlExporter = new XMLExporter(this, writer);
                    xmlExporter.ExportDatabase(rootOid);
                }
            }
        }

        //UPGRADE_ISSUE: Class hierarchy differences between 'java.io.Reader' and 'System.IO.StreamReader' may cause compilation errors.
        public virtual void ImportXML(System.IO.StreamReader reader)
        {
            lock (this)
            {
                if (!opened)
                {
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                }
                XMLImporter xmlImporter = new XMLImporter(this, reader);
                xmlImporter.ImportDatabase();
            }
        }
#endif

        private bool GetBooleanValue(object val)
        {
            if (val is bool)
            {
                return ((bool) val);
            }
            else if (val is string)
            {
                string s = (string) val;
                if ("true".ToUpper().Equals(s.ToUpper()) || "t".ToUpper().Equals(s.ToUpper()) || "1".Equals(s))
                {
                    return true;
                }
                else if ("false".ToUpper().Equals(s.ToUpper()) || "f".ToUpper().Equals(s.ToUpper()) || "0".Equals(s))
                {
                    return false;
                }
            }
            throw new StorageError(StorageError.BAD_PROPERTY_VALUE);
        }

        private long GetIntegerValue(object val)
        {
            if (val is System.ValueType)
            {
                return Convert.ToInt64(((System.ValueType) val));
            }
            else if (val is string)
            {
                try
                {
                    return Convert.ToInt64((string) val, 10);
                }
                catch (FormatException)
                {
                }
            }
            throw new StorageError(StorageError.BAD_PROPERTY_VALUE);
        }

        public virtual void SetProperty(string name, object val)
        {
            if (name.Equals("perst.implicit.values"))
            {
                ClassDescriptor.treateAnyNonPersistentObjectAsValue = GetBooleanValue(val);
            }
            else if (name.Equals("perst.serialize.transient.objects"))
            {
                ClassDescriptor.serializeNonPersistentObjects = GetBooleanValue(val);
            }
            else if (name.Equals("perst.object.cache.init.size"))
            {
                objectCacheInitSize = (int) GetIntegerValue(val);
            }
            else if (name.Equals("perst.object.cache.kind"))
            {
                cacheKind = ((string) val);
            }
            else if (name.Equals("perst.object.index.init.size"))
            {
                initIndexSize = (int) GetIntegerValue(val);
            }
            else if (name.Equals("perst.extension.quantum"))
            {
                extensionQuantum = GetIntegerValue(val);
            }
            else if (name.Equals("perst.gc.threshold"))
            {
                gcThreshold = GetIntegerValue(val);
            }
            else if (name.Equals("perst.file.readonly"))
            {
                readOnly = GetBooleanValue(val);
            }
            else if (name.Equals("perst.file.noflush"))
            {
                noFlush = GetBooleanValue(val);
            }
            else if (name.Equals("perst.alternative.btree"))
            {
                alternativeBtree = GetBooleanValue(val);
            }
            else if (name.Equals("perst.background.gc"))
            {
                backgroundGc = GetBooleanValue(val);
            }
            else if (name.Equals("perst.string.encoding"))
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Object.toString' may return a different value.
                encoding = (val == null) ? null : val.ToString();
            }
            else if (name.Equals("perst.lock.file"))
            {
                lockFile = GetBooleanValue(val);
            }
            else if (name.Equals("perst.replication.ack"))
            {
                replicationAck = GetBooleanValue(val);
            }
            else
            {
                throw new StorageError(StorageError.NO_SUCH_PROPERTY);
            }
        }

        public virtual StorageListener SetListener(StorageListener listener)
        {
            StorageListener prevListener = this.listener;
            this.listener = listener;
            return prevListener;
        }

        public virtual IPersistent GetObjectByOID(int oid)
        {
            if (0 == oid)
                return null;

            lock (this)
            {
                return LookupObject(oid, null);
            }
        }

        public virtual void ModifyObject(IPersistent obj)
        {
            lock (this)
            {
                lock (objectCache)
                {
                    if (!obj.Modified)
                    {
                        if (useSerializableTransactions)
                        {
                            ThreadTransactionContext ctx = TransactionContext;
                            if (ctx.nested != 0)
                            {
                                // serializable transaction
                                ctx.modified.Add(obj);
                            }
                        }
                        objectCache.SetDirty(obj.Oid);
                    }
                }
            }
        }

        public virtual void StoreObject(IPersistent obj)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);
                lock (objectCache)
                {
                    StoreObject0(obj);
                }
            }
        }

        public virtual void StoreFinalizedObject(IPersistent obj)
        {
            if (!opened)
                return;

            lock (objectCache)
            {
                if (obj.Oid != 0)
                    StoreObject0(obj);
            }
        }

        public virtual int MakePersistent(IPersistent obj)
        {
            lock (this)
            {
                if (!opened)
                    throw new StorageError(StorageError.STORAGE_NOT_OPENED);

                if (obj == null)
                    return 0;

                int oid = obj.Oid;
                if (oid != 0)
                    return oid;

                lock (objectCache)
                {
                    oid = AllocateId();
                    obj.AssignOid(this, oid, false);
                    SetPos(oid, 0);
                    objectCache.Put(oid, obj);
                    obj.Modify();
                    return oid;
                }
            }
        }

        private void StoreObject0(IPersistent obj)
        {
            obj.OnStore();
            int oid = obj.Oid;
            bool newObject = false;
            if (oid == 0)
            {
                oid = AllocateId();
                if (!obj.Deleted)
                {
                    objectCache.Put(oid, obj);
                }
                obj.AssignOid(this, oid, false);
                newObject = true;
            }
            else if (obj.Modified)
            {
                objectCache.ClearDirty(oid);
            }
            byte[] data = PackObject(obj);
            long pos;
            int newSize = ObjectHeader.GetSize(data, 0);
            if (newObject || (pos = GetPos(oid)) == 0)
            {
                pos = Allocate(newSize, 0);
                SetPos(oid, pos | dbModifiedFlag);
            }
            else
            {
                int offs = (int) pos & (Page.pageSize - 1);
                if ((offs & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }

                Page pg = pool.GetPage(pos - offs);
                offs &= ~ dbFlagsMask;
                int size = ObjectHeader.GetSize(pg.data, offs);
                pool.Unfix(pg);
                if ((pos & dbModifiedFlag) == 0)
                {
                    CloneBitmap(pos & ~ dbFlagsMask, size);
                    pos = Allocate(newSize, 0);
                    SetPos(oid, pos | dbModifiedFlag);
                }
                else
                {
                    if (((newSize + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1)) > ((size + dbAllocationQuantum - 1) & ~ (dbAllocationQuantum - 1)))
                    {
                        long newPos = Allocate(newSize, 0);
                        CloneBitmap(pos & ~ dbFlagsMask, size);
                        Free(pos & ~ dbFlagsMask, size);
                        pos = newPos;
                        SetPos(oid, pos | dbModifiedFlag);
                    }
                    else if (newSize < size)
                    {
                        ObjectHeader.SetSize(data, 0, size);
                    }
                }
            }
            modified = true;
            pool.Put(pos & ~ dbFlagsMask, data, newSize);
        }

        public virtual void LoadObject(IPersistent obj)
        {
            lock (this)
            {
                if (obj.IsRaw())
                {
                    LoadStub(obj.Oid, obj, obj.GetType());
                }
            }
        }

        internal IPersistent LookupObject(int oid, Type cls)
        {
            IPersistent obj = objectCache.Get(oid);
            if (obj == null || obj.IsRaw())
            {
                obj = LoadStub(oid, obj, cls);
            }
            return obj;
        }

        protected internal virtual int Swizzle(IPersistent obj)
        {
            int oid = 0;
            if (obj != null)
            {
                if (!obj.IsPersistent())
                {
                    StoreObject0(obj);
                }
                oid = obj.Oid;
            }
            return oid;
        }

        internal ClassDescriptor FindClassDescriptor(int oid)
        {
            return (ClassDescriptor) LookupObject(oid, typeof(ClassDescriptor));
        }

        protected internal virtual IPersistent Unswizzle(int oid, Type cls, bool recursiveLoading)
        {
            if (oid == 0)
            {
                return null;
            }

            if (recursiveLoading)
            {
                return LookupObject(oid, cls);
            }

            IPersistent stub = objectCache.Get(oid);
            if (stub != null)
            {
                return stub;
            }

            ClassDescriptor desc;
            //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
            if (cls == typeof(Persistent) || (desc = (ClassDescriptor) classDescMap[cls]) == null || desc.hasSubclasses)
            {
                long pos = GetPos(oid);
                int offs = (int) pos & (Page.pageSize - 1);
                if ((offs & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
                {
                    throw new StorageError(StorageError.DELETED_OBJECT);
                }
                Page pg = pool.GetPage(pos - offs);
                int typeOid = ObjectHeader.GetType(pg.data, offs & ~dbFlagsMask);
                pool.Unfix(pg);
                desc = FindClassDescriptor(typeOid);
            }
            stub = (IPersistent) desc.NewInstance();
            stub.AssignOid(this, oid, true);
            objectCache.Put(oid, stub);
            return stub;
        }

        internal IPersistent LoadStub(int oid, IPersistent obj, Type cls)
        {
            long pos = GetPos(oid);
            if ((pos & (dbFreeHandleFlag | dbPageObjectFlag)) != 0)
            {
                throw new StorageError(StorageError.DELETED_OBJECT);
            }

            byte[] body = pool.Get(pos & ~ dbFlagsMask);
            ClassDescriptor desc;
            int typeOid = ObjectHeader.GetType(body, 0);
            if (typeOid == 0)
            {
                //UPGRADE_TODO: Method 'java.util.HashMap.get' was converted to 'System.Collections.Hashtable.Item' which has a different behavior.
                desc = (ClassDescriptor) classDescMap[cls];
            }
            else
            {
                desc = FindClassDescriptor(typeOid);
            }

            if (obj == null)
            {
                obj = (IPersistent) desc.NewInstance();
                objectCache.Put(oid, obj);
            }
            obj.AssignOid(this, oid, false);
            if (obj is FastSerializable)
            {
                ((FastSerializable) obj).Unpack(body, ObjectHeader.Sizeof, encoding);
            }
            else
            {
                try
                {
                    UnpackObject(obj, desc, obj.RecursiveLoading, body, ObjectHeader.Sizeof, obj);
                }
                catch (System.Exception x)
                {
                    throw new StorageError(StorageError.ACCESS_VIOLATION, x);
                }
            }
            obj.OnLoad();
            return obj;
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'PersistentObjectInputStream' to access its enclosing instance.
        //UPGRADE_TODO: Class 'java.io.ObjectInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior.
        internal class PersistentObjectInputStream : System.IO.BinaryReader
        {
            private void InitBlock(StorageImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private StorageImpl enclosingInstance;
            public StorageImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            //UPGRADE_TODO: Class 'java.io.ObjectInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior.
            internal PersistentObjectInputStream(StorageImpl enclosingInstance, System.IO.Stream streamIn)
                : base(streamIn)
            {
                InitBlock(enclosingInstance);
                //UPGRADE_ISSUE: Method 'java.io.ObjectInputStream.enableResolveObject' was not converted.
                //TODOPORT: enableResolveObject(true);
            }

            //UPGRADE_NOTE: The equivalent of method 'java.io.ObjectInputStream.resolveObject' is not an override method.
            protected internal object ResolveObject(object obj)
            {
                if (obj is IPersistent)
                {
                    int oid = ((IPersistent) obj).Oid;
                    if (oid != 0)
                    {
                        return Enclosing_Instance.LookupObject(oid, obj.GetType());
                    }
                }
                return obj;
            }
        }

        internal int UnpackObject(object obj, ClassDescriptor desc, bool recursiveLoading, byte[] body, int offs, IPersistent po)
        {
            ClassDescriptor.FieldDescriptor[] all = desc.allFields;
            ReflectionProvider provider = ClassDescriptor.ReflectionProvider;
            int len;

            for (int i = 0, n = all.Length; i < n; i++)
            {
                ClassDescriptor.FieldDescriptor fd = all[i];
                FieldInfo f = fd.field;

                if (f == null || obj == null)
                {
                    switch (fd.type)
                    {
                        case ClassDescriptor.tpBoolean:
                        case ClassDescriptor.tpByte:
                            offs += 1;
                            continue;

                        case ClassDescriptor.tpChar:
                        case ClassDescriptor.tpShort:
                            offs += 2;
                            continue;

                        case ClassDescriptor.tpInt:
                        case ClassDescriptor.tpFloat:
                        case ClassDescriptor.tpObject:
                            offs += 4;
                            continue;

                        case ClassDescriptor.tpLong:
                        case ClassDescriptor.tpDouble:
                        case ClassDescriptor.tpDate:
                            offs += 8;
                            continue;

                        case ClassDescriptor.tpString:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                offs += len * 2;
                            }
                            else if (len < -1)
                            {
                                offs -= (len + 2);
                            }
                            continue;

                        case ClassDescriptor.tpValue:
                            offs = UnpackObject((object) null, fd.valueDesc, recursiveLoading, body, offs, po);
                            continue;

                        case ClassDescriptor.tpRaw:
                        case ClassDescriptor.tpArrayOfByte:
                        case ClassDescriptor.tpArrayOfBoolean:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                offs += len;
                            }
                            else if (len < -1)
                            {
                                offs += ClassDescriptor.Sizeof[-2 - len];
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfShort:
                        case ClassDescriptor.tpArrayOfChar:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                offs += len * 2;
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfInt:
                        case ClassDescriptor.tpArrayOfFloat:
                        case ClassDescriptor.tpArrayOfObject:
                        case ClassDescriptor.tpLink:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                offs += len * 4;
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfLong:
                        case ClassDescriptor.tpArrayOfDouble:
                        case ClassDescriptor.tpArrayOfDate:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                offs += len * 8;
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfString:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                for (int j = 0; j < len; j++)
                                {
                                    int strlen = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                    if (strlen > 0)
                                    {
                                        offs += strlen * 2;
                                    }
                                    else if (strlen < -1)
                                    {
                                        offs -= (strlen + 2);
                                    }
                                }
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfValue:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                ClassDescriptor valueDesc = fd.valueDesc;
                                for (int j = 0; j < len; j++)
                                {
                                    offs = UnpackObject((object) null, valueDesc, recursiveLoading, body, offs, po);
                                }
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfRaw:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len > 0)
                            {
                                for (int j = 0; j < len; j++)
                                {
                                    int rawlen = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                    if (rawlen > 0)
                                    {
                                        offs += rawlen;
                                    }
                                    else if (rawlen < -1)
                                    {
                                        offs += ClassDescriptor.Sizeof[-2 - rawlen];
                                    }
                                }
                            }
                            continue;
                        }
                }
                else
                {
                    switch (fd.type)
                    {
                        case ClassDescriptor.tpBoolean:
                            provider.SetBoolean(f, obj, body[offs++] != 0);
                            continue;

                        case ClassDescriptor.tpByte:
                            provider.SetByte(f, obj, body[offs++]);
                            continue;

                        case ClassDescriptor.tpChar:
                            provider.SetChar(f, obj, (char) Bytes.Unpack2(body, offs));
                            offs += 2;
                            continue;

                        case ClassDescriptor.tpShort:
                            provider.SetShort(f, obj, Bytes.Unpack2(body, offs));
                            offs += 2;
                            continue;

                        case ClassDescriptor.tpInt:
                            provider.SetInt(f, obj, Bytes.Unpack4(body, offs));
                            offs += 4;
                            continue;

                        case ClassDescriptor.tpLong:
                            provider.SetLong(f, obj, Bytes.Unpack8(body, offs));
                            offs += 8;
                            continue;

                        case ClassDescriptor.tpFloat:
                            provider.SetFloat(f, obj, Bytes.UnpackF4(body, offs));
                            offs += 4;
                            continue;

                        case ClassDescriptor.tpDouble:
                            provider.SetDouble(f, obj, Bytes.UnpackF8(body, offs));
                            offs += 8;
                            continue;

                        case ClassDescriptor.tpString:
                        {
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            string str = null;
                            if (len >= 0)
                            {
                                char[] chars = new char[len];
                                for (int j = 0; j < len; j++)
                                {
                                    chars[j] = (char) Bytes.Unpack2(body, offs);
                                    offs += 2;
                                }
                                str = new string(chars);
                            }
                            else if (len < -1)
                            {
                                if (encoding != null)
                                {
                                    string tempStr;
                                    //UPGRADE_TODO: The differences in the Format of parameters for constructor 'java.lang.String.String' may cause compilation errors.
                                    tempStr = System.Text.Encoding.GetEncoding(encoding).GetString(body);
                                    str = new string(tempStr.ToCharArray(), offs, -2 - len);
                                }
                                else
                                {
                                    str = new string(SupportClass.ToCharArray(body), offs, -2 - len);
                                }
                                offs -= (2 + len);
                            }
                            provider.Set(f, obj, str);
                            continue;
                        }

                        case ClassDescriptor.tpDate:
                        {
                            long msec = Bytes.Unpack8(body, offs);
                            offs += 8;
                            //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                            DateTime date; // = null;
                            if (msec >= 0)
                            {
                                //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                                date = new DateTime(msec);
                            }
                            else
                                date = new DateTime();
                            provider.Set(f, obj, date);
                            continue;
                        }

                        case ClassDescriptor.tpObject:
                        {
                            provider.Set(f, obj, Unswizzle(Bytes.Unpack4(body, offs), f.FieldType, recursiveLoading));
                            offs += 4;
                            continue;
                        }

                        case ClassDescriptor.tpValue:
                        {
                            object val = fd.valueDesc.NewInstance();
                            offs = UnpackObject(val, fd.valueDesc, recursiveLoading, body, offs, po);
                            provider.Set(f, obj, val);
                            continue;
                        }

                        case ClassDescriptor.tpRaw:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len >= 0)
                            {
                                System.IO.MemoryStream streamIn = new System.IO.MemoryStream(body, offs, len);
                                BinaryFormatter formatter = new BinaryFormatter();
                                object val = formatter.Deserialize(streamIn);
                                provider.Set(f, obj, val);
                                streamIn.Close();
                                offs += len;
                            }
                            else if (len < 0)
                            {
                                object val = null;
                                switch (-2 - len)
                                {
                                    case ClassDescriptor.tpBoolean:
                                        val = Convert.ToBoolean(body[offs++]);
                                        break;

                                    case ClassDescriptor.tpByte:
                                        val = (byte) body[offs++];
                                        break;

                                    case ClassDescriptor.tpChar:
                                        val = (char) Bytes.Unpack2(body, offs);
                                        offs += 2;
                                        break;

                                    case ClassDescriptor.tpShort:
                                        val = (short) Bytes.Unpack2(body, offs);
                                        offs += 2;
                                        break;

                                    case ClassDescriptor.tpInt:
                                        val = (Int32) Bytes.Unpack4(body, offs);
                                        offs += 4;
                                        break;

                                    case ClassDescriptor.tpLong:
                                        val = (long) Bytes.Unpack8(body, offs);
                                        offs += 8;
                                        break;

                                    case ClassDescriptor.tpFloat:
                                        val = Bytes.UnpackF4(body, offs);
                                        break;

                                    case ClassDescriptor.tpDouble:
                                        val = Bytes.UnpackF8(body, offs);
                                        offs += 8;
                                        break;

                                    case ClassDescriptor.tpDate:
                                        //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                                        val = new DateTime(Bytes.Unpack8(body, offs));
                                        offs += 8;
                                        break;

                                    case ClassDescriptor.tpObject:
                                        val = Unswizzle(Bytes.Unpack4(body, offs), typeof(Persistent), recursiveLoading);
                                        offs += 4;
                                        break;
                                    }
                                provider.Set(f, obj, val);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfByte:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                byte[] arr = new byte[len];
                                Array.Copy(body, offs, arr, 0, len);
                                offs += len;
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfBoolean:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                bool[] arr = new bool[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = body[offs++] != 0;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfShort:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                short[] arr = new short[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Bytes.Unpack2(body, offs);
                                    offs += 2;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfChar:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                char[] arr = new char[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = (char) Bytes.Unpack2(body, offs);
                                    offs += 2;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfInt:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                int[] arr = new int[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfLong:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                long[] arr = new long[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Bytes.Unpack8(body, offs);
                                    offs += 8;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfFloat:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                float[] arr = new float[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Bytes.UnpackF4(body, offs);
                                    offs += 4;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfDouble:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                double[] arr = new double[len];
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Bytes.UnpackF8(body, offs);
                                    offs += 8;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfDate:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                DateTime[] arr = new DateTime[len];
                                for (int j = 0; j < len; j++)
                                {
                                    long msec = Bytes.Unpack8(body, offs);
                                    offs += 8;
                                    if (msec >= 0)
                                    {
                                        //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                                        arr[j] = new DateTime(msec);
                                    }
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfString:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                string[] arr = new string[len];
                                for (int j = 0; j < len; j++)
                                {
                                    int strlen = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                    if (strlen >= 0)
                                    {
                                        char[] chars = new char[strlen];
                                        for (int k = 0; k < strlen; k++)
                                        {
                                            chars[k] = (char) Bytes.Unpack2(body, offs);
                                            offs += 2;
                                        }
                                        arr[j] = new string(chars);
                                    }
                                    else if (strlen < -1)
                                    {
                                        if (encoding != null)
                                        {
                                            string tempStr2;
                                            //UPGRADE_TODO: The differences in the Format of parameters for constructor 'java.lang.String.String' may cause compilation errors.
                                            tempStr2 = System.Text.Encoding.GetEncoding(encoding).GetString(body);
                                            arr[j] = new string(tempStr2.ToCharArray(), offs, -2 - strlen);
                                        }
                                        else
                                        {
                                            arr[j] = new string(SupportClass.ToCharArray(body), offs, -2 - strlen);
                                        }
                                        offs -= (2 + strlen);
                                    }
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfObject:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                Type elemType = f.FieldType.GetElementType();
                                IPersistent[] arr = (IPersistent[]) System.Array.CreateInstance(elemType, len);
                                for (int j = 0; j < len; j++)
                                {
                                    arr[j] = Unswizzle(Bytes.Unpack4(body, offs), elemType, recursiveLoading);
                                    offs += 4;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfValue:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                Type elemType = f.FieldType.GetElementType();
                                object[] arr = (object[]) System.Array.CreateInstance(elemType, len);
                                ClassDescriptor valueDesc = fd.valueDesc;
                                for (int j = 0; j < len; j++)
                                {
                                    object val = valueDesc.NewInstance();
                                    offs = UnpackObject(val, valueDesc, recursiveLoading, body, offs, po);
                                    arr[j] = val;
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpArrayOfRaw:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                Type elemType = f.FieldType.GetElementType();
                                object[] arr = (object[]) System.Array.CreateInstance(elemType, len);
                                for (int j = 0; j < len; j++)
                                {
                                    int rawlen = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                    if (rawlen >= 0)
                                    {
                                        // TODOPORT: 
                                        System.IO.MemoryStream streamIn = new System.IO.MemoryStream(body, offs, rawlen);
                                        //UPGRADE_TODO: Class 'java.io.ObjectInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior.
                                        //System.IO.BinaryReader streamIn = new PersistentObjectInputStream(this, bin);
                                        //UPGRADE_WARNING: Method 'java.io.ObjectInputStream.readObject' was converted to 'SupportClass.Deserialize' which may throw an exception.
                                        BinaryFormatter formatter = new BinaryFormatter();
                                        object val = formatter.Deserialize(streamIn);
                                        arr[j] = val;
                                        streamIn.Close();
                                        offs += rawlen;
                                    }
                                    else
                                    {
                                        object val = null;
                                        switch (-2 - rawlen)
                                        {
                                            case ClassDescriptor.tpBoolean:
                                                val = Convert.ToBoolean(body[offs++]);
                                                break;

                                            case ClassDescriptor.tpByte:
                                                val = (byte) body[offs++];
                                                break;

                                            case ClassDescriptor.tpChar:
                                                val = (char) Bytes.Unpack2(body, offs);
                                                offs += 2;
                                                break;

                                            case ClassDescriptor.tpShort:
                                                val = (short) Bytes.Unpack2(body, offs);
                                                offs += 2;
                                                break;

                                            case ClassDescriptor.tpInt:
                                                val = (Int32) Bytes.Unpack4(body, offs);
                                                offs += 4;
                                                break;

                                            case ClassDescriptor.tpLong:
                                                val = (long) Bytes.Unpack8(body, offs);
                                                offs += 8;
                                                break;

                                            case ClassDescriptor.tpFloat:
                                                val = Bytes.UnpackF4(body, offs);
                                                offs += 4;
                                                break;

                                            case ClassDescriptor.tpDouble:
                                                val = Bytes.UnpackF8(body, offs);
                                                offs += 8;
                                                break;

                                            case ClassDescriptor.tpDate:
                                                //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                                                val = new DateTime(Bytes.Unpack8(body, offs));
                                                offs += 8;
                                                break;

                                            case ClassDescriptor.tpObject:
                                                val = Unswizzle(Bytes.Unpack4(body, offs), typeof(Persistent), recursiveLoading);
                                                offs += 4;
                                                break;
                                            }
                                        arr[j] = val;
                                    }
                                }
                                provider.Set(f, obj, arr);
                            }
                            continue;

                        case ClassDescriptor.tpLink:
                            len = Bytes.Unpack4(body, offs);
                            offs += 4;
                            if (len < 0)
                            {
                                provider.Set(f, obj, (object) null);
                            }
                            else
                            {
                                IPersistent[] arr = new IPersistent[len];
                                for (int j = 0; j < len; j++)
                                {
                                    int elemOid = Bytes.Unpack4(body, offs);
                                    offs += 4;
                                    if (elemOid != 0)
                                    {
                                        arr[j] = new PersistentStub(this, elemOid);
                                    }
                                }
                                provider.Set(f, obj, new LinkImpl(arr, po));
                            }
                            break;
                        }
                }
            }
            return offs;
        }

        internal byte[] PackObject(IPersistent obj)
        {
            ByteBuffer buf = new ByteBuffer();
            int offs = ObjectHeader.Sizeof;
            buf.Extend(offs);
            ClassDescriptor desc = GetClassDescriptor(obj.GetType());
            if (obj is FastSerializable)
            {
                offs = ((FastSerializable) obj).Pack(buf, offs, encoding);
            }
            else
            {
                try
                {
                    offs = PackObject(obj, desc, offs, buf, obj);
                }
                catch (System.Exception x)
                {
                    throw new StorageError(StorageError.ACCESS_VIOLATION, x);
                }
            }
            ObjectHeader.SetSize(buf.arr, 0, offs);
            ObjectHeader.SetType(buf.arr, 0, desc.Oid);
            return buf.arr;
        }

        internal int PackValue(object val, int offs, ByteBuffer buf)
        {
            if (val == null)
            {
                buf.Extend(offs + 4);
                Bytes.Pack4(buf.arr, offs, -1);
                offs += 4;
            }
            else if (val is IPersistent)
            {
                buf.Extend(offs + 8);
                Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpObject);
                Bytes.Pack4(buf.arr, offs + 4, Swizzle((IPersistent) val));
                offs += 8;
            }
            else
            {
                Type c = val.GetType();
                if (c == typeof(bool))
                {
                    buf.Extend(offs + 5);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpBoolean);
                    buf.arr[offs + 4] = (byte) (((bool) val) ? 1 : 0);
                    offs += 5;
                }
                else if (c == typeof(System.Char))
                {
                    buf.Extend(offs + 6);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpChar);
                    Bytes.Pack2(buf.arr, offs + 4, (short) ((System.Char) val));
                    offs += 6;
                }
                else if (c == typeof(System.SByte))
                {
                    buf.Extend(offs + 5);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpByte);
                    buf.arr[offs + 4] = (byte) ((System.SByte) val);
                    offs += 5;
                }
                else if (c == typeof(System.Int16))
                {
                    buf.Extend(offs + 6);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpShort);
                    Bytes.Pack2(buf.arr, offs + 4, (short) ((System.Int16) val));
                    offs += 6;
                }
                else if (c == typeof(Int32))
                {
                    buf.Extend(offs + 8);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpInt);
                    Bytes.Pack4(buf.arr, offs + 4, ((Int32) val));
                    offs += 8;
                }
                else if (c == typeof(Int64))
                {
                    buf.Extend(offs + 12);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpLong);
                    Bytes.Pack8(buf.arr, offs + 4, (long) ((Int64) val));
                    offs += 12;
                }
                else if (c == typeof(System.Single))
                {
                    buf.Extend(offs + 8);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpFloat);
                    Bytes.PackF4(buf.arr, offs + 4, (float)val);
                    offs += 8;
                }
                else if (c == typeof(System.Double))
                {
                    buf.Extend(offs + 12);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpDouble);
                    Bytes.PackF8(buf.arr, offs + 4, (System.Double) val);
                    offs += 12;
                }
                else if (c == typeof(System.DateTime))
                {
                    buf.Extend(offs + 12);
                    Bytes.Pack4(buf.arr, offs, -2 - ClassDescriptor.tpDate);
                    //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                    Bytes.Pack8(buf.arr, offs + 4, ((DateTime) val).Ticks);
                    offs += 12;
                }
                else
                {
                    System.IO.MemoryStream streamOut = new System.IO.MemoryStream();
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(streamOut, val);
                    streamOut.Close();
                    byte[] arr = streamOut.ToArray();
                    int len = arr.Length;
                    buf.Extend(offs + 4 + len);
                    Bytes.Pack4(buf.arr, offs, len);
                    offs += 4;
                    Array.Copy(arr, 0, buf.arr, offs, len);
                    offs += len;
                }
            }
            return offs;
        }

        internal int PackObject(object obj, ClassDescriptor desc, int offs, ByteBuffer buf, IPersistent po)
        {
            ClassDescriptor.FieldDescriptor[] flds = desc.allFields;
            for (int i = 0, n = flds.Length; i < n; i++)
            {
                ClassDescriptor.FieldDescriptor fd = flds[i];
                FieldInfo f = fd.field;
                switch (fd.type)
                {
                    case ClassDescriptor.tpByte:
                        buf.Extend(offs + 1);
                        buf.arr[offs++] = (byte) f.GetValue(obj);
                        continue;

                    case ClassDescriptor.tpBoolean:
                        buf.Extend(offs + 1);
                        buf.arr[offs++] = (byte) ((bool) f.GetValue(obj) ? 1 : 0);
                        continue;

                    case ClassDescriptor.tpShort:
                        buf.Extend(offs + 2);
                        Bytes.Pack2(buf.arr, offs, (short) f.GetValue(obj));
                        offs += 2;
                        continue;

                    case ClassDescriptor.tpChar:
                        buf.Extend(offs + 2);
                        Bytes.Pack2(buf.arr, offs, (short) f.GetValue(obj));
                        offs += 2;
                        continue;

                    case ClassDescriptor.tpInt:
                        buf.Extend(offs + 4);
                        Bytes.Pack4(buf.arr, offs, (int) f.GetValue(obj));
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpLong:
                        buf.Extend(offs + 8);
                        Bytes.Pack8(buf.arr, offs, (long) f.GetValue(obj));
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpFloat:
                        buf.Extend(offs + 4);
                        Bytes.PackF4(buf.arr, offs, (float) f.GetValue(obj));
                        offs += 4;
                        continue;

                    case ClassDescriptor.tpDouble:
                        buf.Extend(offs + 8);
                        Bytes.PackF8(buf.arr, offs, (double) f.GetValue(obj));
                        offs += 8;
                        continue;

                    case ClassDescriptor.tpDate:
                    {
                        buf.Extend(offs + 8);
                        DateTime d = (DateTime) f.GetValue(obj);
                        //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                        //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                        long msec = (d == null) ? -1 : d.Ticks;
                        Bytes.Pack8(buf.arr, offs, msec);
                        offs += 8;
                        continue;
                    }

                    case ClassDescriptor.tpString:
                        offs = buf.PackString(offs, (string) f.GetValue(obj), encoding);
                        continue;

                    case ClassDescriptor.tpObject:
                    {
                        buf.Extend(offs + 4);
                        Bytes.Pack4(buf.arr, offs, Swizzle((IPersistent) f.GetValue(obj)));
                        offs += 4;
                        continue;
                    }

                    case ClassDescriptor.tpValue:
                    {
                        object val = f.GetValue(obj);
                        if (val == null)
                        {
                            throw new StorageError(StorageError.NULL_VALUE, fd.fieldName);
                        }
                        else if (val is IPersistent)
                        {
                            throw new StorageError(StorageError.SERIALIZE_PERSISTENT);
                        }
                        offs = PackObject(val, fd.valueDesc, offs, buf, po);
                        continue;
                    }

                    case ClassDescriptor.tpRaw:
                        offs = PackValue(f.GetValue(obj), offs, buf);
                        continue;

                    case ClassDescriptor.tpArrayOfByte:
                    {
                        byte[] arr = (byte[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            Array.Copy(arr, 0, buf.arr, offs, len);
                            offs += len;
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfBoolean:
                    {
                        bool[] arr = (bool[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++, offs++)
                            {
                                buf.arr[offs] = (byte) (arr[j] ? 1 : 0);
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfShort:
                    {
                        short[] arr = (short[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 2);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack2(buf.arr, offs, arr[j]);
                                offs += 2;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfChar:
                    {
                        char[] arr = (char[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 2);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack2(buf.arr, offs, (short) arr[j]);
                                offs += 2;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfInt:
                    {
                        int[] arr = (int[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack4(buf.arr, offs, arr[j]);
                                offs += 4;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfLong:
                    {
                        long[] arr = (long[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack8(buf.arr, offs, arr[j]);
                                offs += 8;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfFloat:
                    {
                        float[] arr = (float[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.PackF4(buf.arr, offs, arr[j]);
                                offs += 4;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfDouble:
                    {
                        double[] arr = (double[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.PackF8(buf.arr, offs, arr[j]);
                                offs += 8;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfDate:
                    {
                        DateTime[] arr = (DateTime[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 8);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                DateTime d = arr[j];
                                //UPGRADE_TODO: The 'System.DateTime' structure does not have an equivalent to NULL.
                                //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
                                long msec = (d == null) ? -1 : d.Ticks;
                                Bytes.Pack8(buf.arr, offs, msec);
                                offs += 8;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfString:
                    {
                        string[] arr = (string[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                offs = buf.PackString(offs, (string) arr[j], encoding);
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfObject:
                    {
                        IPersistent[] arr = (IPersistent[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack4(buf.arr, offs, Swizzle(arr[j]));
                                offs += 4;
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfValue:
                    {
                        object[] arr = (object[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            ClassDescriptor elemDesc = fd.valueDesc;
                            for (int j = 0; j < len; j++)
                            {
                                object val = arr[j];
                                if (val == null)
                                    throw new StorageError(StorageError.NULL_VALUE, fd.fieldName);

                                offs = PackObject(val, elemDesc, offs, buf, po);
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpArrayOfRaw:
                    {
                        object[] arr = (object[]) f.GetValue(obj);
                        if (arr == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            int len = arr.Length;
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                offs = PackValue(arr[j], offs, buf);
                            }
                        }
                        continue;
                    }

                    case ClassDescriptor.tpLink:
                    {
                        LinkImpl link = (LinkImpl) f.GetValue(obj);
                        if (link == null)
                        {
                            buf.Extend(offs + 4);
                            Bytes.Pack4(buf.arr, offs, -1);
                            offs += 4;
                        }
                        else
                        {
                            link.owner = po;
                            int len = link.Size;
                            buf.Extend(offs + 4 + len * 4);
                            Bytes.Pack4(buf.arr, offs, len);
                            offs += 4;
                            for (int j = 0; j < len; j++)
                            {
                                Bytes.Pack4(buf.arr, offs, Swizzle(link.GetRaw(j)));
                                offs += 4;
                            }
                            link.Unpin();
                        }
                        continue;
                    }
                    }
            }
            return offs;
        }

        private int initIndexSize = dbDefaultInitIndexSize;
        private int objectCacheInitSize = dbDefaultObjectCacheInitSize;
        private long extensionQuantum = dbDefaultExtensionQuantum;
        private string cacheKind = "lru";
        private bool lockFile = false;
        private bool readOnly = false;
        private bool noFlush = false;
        private bool alternativeBtree = false;
        private bool backgroundGc = false;

        internal bool replicationAck = false;

        internal string encoding = null;

        internal PagePool pool;
        internal Header header; // base address of database file mapping
        internal int[] dirtyPagesMap; // bitmap of changed pages in current index
        internal bool modified;

        internal int currRBitmapPage; //current bitmap page for allocating records
        internal int currRBitmapOffs; //offset in current bitmap page for allocating
        //unaligned records
        internal int currPBitmapPage; //current bitmap page for allocating page objects
        internal int currPBitmapOffs; //offset in current bitmap page for allocating
        //page objects
        internal Location reservedChain;

        internal int committedIndexSize;
        internal int currIndexSize;

        internal int currIndex; // copy of header.root, used to allow read access to the database
        // during transaction commit
        internal long usedSize; // total size of allocated objects since the beginning of the session
        internal int[] bitmapPageAvailableSpace;
        internal bool opened;

        internal int[] greyBitmap; // bitmap of visited during GC but not yet marked object
        internal int[] blackBitmap; // bitmap of objects marked during GC
        internal long gcThreshold;
        internal long allocatedDelta;
        internal bool gcDone;
        internal bool gcActive;
        internal object backgroundGcMonitor;
        internal object backgroundGcStartMonitor;
        internal GcThread gcThread;

        internal StorageListener listener;

        internal int nNestedTransactions;
        internal int nBlockedTransactions;
        internal int nCommittedTransactions;
        internal long scheduledCommitTime;
        internal object transactionMonitor;
        internal PersistentResource transactionLock;

        [ThreadStatic]
        internal static ThreadTransactionContext transactionContext;
        internal bool useSerializableTransactions;

        internal OidHashTable objectCache;
        //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior.
        internal Hashtable classDescMap;
        internal ClassDescriptor descList;

        static StorageImpl()
        {
            if (null == transactionContext)
                transactionContext = new ThreadTransactionContext();
        }
    }

    class RootPage
    {
        internal long size; // database file size
        internal long index; // offset to object index
        internal long shadowIndex; // offset to shadow index
        internal long usedSize; // size used by objects
        internal int indexSize; // size of object index
        internal int shadowIndexSize; // size of object index
        internal int indexUsed; // userd part of the index
        internal int freeList; // L1 list of free descriptors
        internal int bitmapEnd; // index of last allocated bitmap page
        internal int rootObject; // OID of root object
        internal int classDescList; // List of class descriptors
        internal int bitmapExtent; // Allocation bitmap offset and size

        internal const int Sizeof = 64;
    }

    class Header
    {
        internal int curr; // current root
        internal bool dirty; // database was not closed normally
        internal bool initialized; // database is initilaized

        internal RootPage[] root;

        internal const int Sizeof = 3 + RootPage.Sizeof * 2;

        internal void Pack(byte[] rec)
        {
            int offs = 0;
            rec[offs++] = (byte) curr;
            rec[offs++] = (byte) (dirty ? 1 : 0);
            rec[offs++] = (byte) (initialized ? 1 : 0);
            for (int i = 0; i < 2; i++)
            {
                Bytes.Pack8(rec, offs, root[i].size);
                offs += 8;
                Bytes.Pack8(rec, offs, root[i].index);
                offs += 8;
                Bytes.Pack8(rec, offs, root[i].shadowIndex);
                offs += 8;
                Bytes.Pack8(rec, offs, root[i].usedSize);
                offs += 8;
                Bytes.Pack4(rec, offs, root[i].indexSize);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].shadowIndexSize);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].indexUsed);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].freeList);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].bitmapEnd);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].rootObject);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].classDescList);
                offs += 4;
                Bytes.Pack4(rec, offs, root[i].bitmapExtent);
                offs += 4;
            }
        }

        internal void Unpack(byte[] rec)
        {
            int offs = 0;
            curr = rec[offs++];
            dirty = rec[offs++] != 0;
            initialized = rec[offs++] != 0;
            root = new RootPage[2];
            for (int i = 0; i < 2; i++)
            {
                root[i] = new RootPage();
                root[i].size = Bytes.Unpack8(rec, offs);
                offs += 8;
                root[i].index = Bytes.Unpack8(rec, offs);
                offs += 8;
                root[i].shadowIndex = Bytes.Unpack8(rec, offs);
                offs += 8;
                root[i].usedSize = Bytes.Unpack8(rec, offs);
                offs += 8;
                root[i].indexSize = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].shadowIndexSize = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].indexUsed = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].freeList = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].bitmapEnd = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].rootObject = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].classDescList = Bytes.Unpack4(rec, offs);
                offs += 4;
                root[i].bitmapExtent = Bytes.Unpack4(rec, offs);
                offs += 4;
            }
        }
    }
}

