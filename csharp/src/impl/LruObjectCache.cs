namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class LruObjectCache : OidHashTable
    {
        internal Entry[] table;
        internal const float loadFactor = 0.75f;
        internal const int defaultInitSize = 1319;
        internal int count;
        internal int threshold;
        internal int pinLimit;
        internal int nPinned;
        internal Entry pinList;

        public LruObjectCache(int size)
        {
            int initialCapacity = size;
            if (initialCapacity == 0)
                initialCapacity = defaultInitSize;

            threshold = (int) (initialCapacity * loadFactor);
            table = new Entry[initialCapacity];
            pinList = new Entry(0, null, null);
            pinLimit = size;
            pinList.lru = pinList.mru = pinList;
        }

        public virtual bool Remove(int oid)
        {
            lock (this)
            {
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index], prev = null; e != null; prev = e, e = e.next)
                {
                    if (e.oid == oid)
                    {
                        if (prev != null)
                        {
                            prev.next = e.next;
                        }
                        else
                        {
                            tab[index] = e.next;
                        }
                        e.Clear();
                        UnpinObject(e);
                        count -= 1;
                        return true;
                    }
                }
                return false;
            }
        }

        protected internal virtual WeakReference CreateReference(object obj)
        {
            return new WeakReference(obj);
        }

        private void UnpinObject(Entry e)
        {
            if (e.pin != null)
            {
                e.Unpin();
                nPinned -= 1;
            }
        }

        private void PinObject(Entry e, IPersistent obj)
        {
            if (pinLimit != 0)
            {
                if (e.pin != null)
                {
                    e.Unlink();
                }
                else
                {
                    if (nPinned == pinLimit)
                    {
                        pinList.lru.Unpin();
                    }
                    else
                    {
                        nPinned += 1;
                    }
                }
                e.LinkAfter(pinList, obj);
            }
        }

        public virtual void Put(int oid, IPersistent obj)
        {
            lock (this)
            {
                WeakReference ref_Renamed = CreateReference(obj);
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index]; e != null; e = e.next)
                {
                    if (e.oid == oid)
                    {
                        e.ref_Renamed = ref_Renamed;
                        PinObject(e, obj);
                        return;
                    }
                }
                if (count >= threshold)
                {
                    // Rehash the table if the threshold is exceeded
                    Rehash();
                    tab = table;
                    index = (oid & 0x7FFFFFFF) % tab.Length;
                }

                // Creates the new entry.
                tab[index] = new Entry(oid, ref_Renamed, tab[index]);
                PinObject(tab[index], obj);
                count++;
            }
        }

        public virtual IPersistent Get(int oid)
        {
            while (true)
            {
                lock (this)
                {
                    Entry[] tab = table;
                    int index = (oid & 0x7FFFFFFF) % tab.Length;
                    for (Entry e = tab[index]; e != null; e = e.next)
                    {
                        if (e.oid == oid)
                        {
                            //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.get' was not converted.
                            IPersistent obj = e.ref_Renamed.Target as IPersistent;
                            if (obj == null)
                            {
                                if (e.dirty != 0)
                                    goto cs;
                            }
                            else
                            {
                                if (obj.Deleted)
                                {
                                    //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.clear' was not converted.
                                    e.ref_Renamed.Target = null;
                                    UnpinObject(e);
                                    return null;
                                }
                                PinObject(e, obj);
                            }
                            return obj;
                        }
                    }
                    return null;
                }
cs:
                System.GC.WaitForPendingFinalizers();
            }
        }

        public virtual void Flush()
        {
            while (true)
            {
                lock (this)
                {
                    for (int i = 0; i < table.Length; i++)
                    {
                        for (Entry e = table[i]; e != null; e = e.next)
                        {
                            //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.get' was not converted.
                            IPersistent obj = e.ref_Renamed.Target as IPersistent;
                            if (obj != null)
                            {
                                if (obj.Modified)
                                {
                                    obj.Store();
                                }
                            }
                            else if (e.dirty != 0)
                            {
                                goto cs1;
                            }
                        }
                    }
                    return;
                }
cs1:
                System.GC.WaitForPendingFinalizers();
            }
        }

        public virtual void Invalidate()
        {
            while (true)
            {
                lock (this)
                {
                    for (int i = 0; i < table.Length; i++)
                    {
                        for (Entry e = table[i]; e != null; e = e.next)
                        {
                            //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.get' was not converted.
                            IPersistent obj = e.ref_Renamed.Target as IPersistent;
                            if (obj != null)
                            {
                                if (obj.Modified)
                                {
                                    e.dirty = 0;
                                    UnpinObject(e);
                                    obj.Invalidate();
                                }
                            }
                            else if (e.dirty != 0)
                            {
                                goto cs1;
                            }
                        }
                        table[i] = null;
                    }
                    count = 0;
                    return;
                }
cs1:
                System.GC.WaitForPendingFinalizers();
            }
        }

        internal virtual void Rehash()
        {
            int oldCapacity = table.Length;
            Entry[] oldMap = table;
            int i;
            for (i = oldCapacity; --i >= 0; )
            {
                Entry e, next, prev;
                for (prev = null, e = oldMap[i]; e != null; e = next)
                {
                    next = e.next;
                    //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.get' was not converted.
                    if (e.ref_Renamed.Target == null && e.dirty == 0)
                    {
                        count -= 1;
                        e.Clear();
                        if (prev == null)
                        {
                            oldMap[i] = next;
                        }
                        else
                        {
                            prev.next = next;
                        }
                    }
                    else
                    {
                        prev = e;
                    }
                }
            }

            if (count <= SupportClass.URShift(threshold, 1))
            {
                return;
            }
            int newCapacity = oldCapacity * 2 + 1;
            Entry[] newMap = new Entry[newCapacity];

            //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
            threshold = (int) (newCapacity * loadFactor);
            table = newMap;

            for (i = oldCapacity; --i >= 0; )
            {
                for (Entry old = oldMap[i]; old != null; )
                {
                    Entry e = old;
                    old = old.next;

                    int index = (e.oid & 0x7FFFFFFF) % newCapacity;
                    e.next = newMap[index];
                    newMap[index] = e;
                }
            }
        }

        public virtual void SetDirty(int oid)
        {
            lock (this)
            {
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index]; e != null; e = e.next)
                {
                    if (e.oid == oid)
                    {
                        e.dirty += 1;
                        return;
                    }
                }
            }
        }

        public virtual void ClearDirty(int oid)
        {
            lock (this)
            {
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index]; e != null; e = e.next)
                {
                    if (e.oid == oid)
                    {
                        if (e.dirty > 0)
                        {
                            e.dirty -= 1;
                        }
                        return;
                    }
                }
            }
        }

        public virtual int Size()
        {
            return count;
        }

        internal class Entry
        {
            internal Entry next;
            internal WeakReference ref_Renamed;
            internal int oid;
            internal int dirty;
            internal Entry lru;
            internal Entry mru;
            internal IPersistent pin;

            internal virtual void Unlink()
            {
                lru.mru = mru;
                mru.lru = lru;
            }

            internal virtual void Unpin()
            {
                Unlink();
                lru = mru = null;
                pin = null;
            }

            internal virtual void LinkAfter(Entry head, IPersistent obj)
            {
                mru = head.mru;
                mru.lru = this;
                head.mru = this;
                lru = head;
                pin = obj;
            }

            internal virtual void Clear()
            {
                //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.clear' was not converted.
                ref_Renamed.Target = null;
                ref_Renamed = null;
                dirty = 0;
                next = null;
            }

            internal Entry(int oid, WeakReference ref_Renamed, Entry chain)
            {
                next = chain;
                this.oid = oid;
                this.ref_Renamed = ref_Renamed;
            }
        }
    }
}

