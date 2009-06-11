namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class WeakHashTable : OidHashTable
    {
        internal Entry[] table;
        internal const float loadFactor = 0.75f;
        internal int count;
        internal int threshold;

        public WeakHashTable(int initialCapacity)
        {
            threshold = (int) (initialCapacity * loadFactor);
            table = new Entry[initialCapacity];
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
                        count -= 1;
                        return true;
                    }
                }
                return false;
            }
        }

        protected internal virtual WeakReference CreateReference(System.Object obj)
        {
            return new WeakReference(obj);
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
                            else if (obj.Deleted)
                            {
                                //UPGRADE_ISSUE: Method 'java.lang.ref.Reference.clear' was not converted.
                                e.ref_Renamed.Target = null;
                                return null;
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

            if (count <= (SupportClass.URShift(threshold, 1)))
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

