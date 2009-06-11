namespace TenderBaseImpl
{
    using System;
    using TenderBase;
    
    public class StrongHashTable : OidHashTable
    {
        internal Entry[] table;
        internal const float loadFactor = 0.75f;
        internal int count;
        internal int threshold;

        public StrongHashTable(int initialCapacity)
        {
            //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.
            threshold = (int) (initialCapacity * loadFactor);
            if (initialCapacity != 0)
            {
                table = new Entry[initialCapacity];
            }
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
                        e.obj = null;
                        count -= 1;
                        if (prev != null)
                        {
                            prev.next = e.next;
                        }
                        else
                        {
                            tab[index] = e.next;
                        }
                        return true;
                    }
                }
                return false;
            }
        }

        public virtual void Put(int oid, IPersistent obj)
        {
            lock (this)
            {
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index]; e != null; e = e.next)
                {
                    if (e.oid == oid)
                    {
                        e.obj = obj;
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
                tab[index] = new Entry(oid, obj, tab[index]);
                count++;
            }
        }

        public virtual IPersistent Get(int oid)
        {
            lock (this)
            {
                Entry[] tab = table;
                int index = (oid & 0x7FFFFFFF) % tab.Length;
                for (Entry e = tab[index]; e != null; e = e.next)
                {
                    if (e.oid == oid)
                    {
                        return e.obj;
                    }
                }
                return null;
            }
        }

        internal virtual void Rehash()
        {
            int oldCapacity = table.Length;
            Entry[] oldMap = table;
            int i;

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

        public virtual void Flush()
        {
            lock (this)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    for (Entry e = table[i]; e != null; e = e.next)
                    {
                        if (e.obj.Modified)
                        {
                            e.obj.Store();
                        }
                    }
                }
            }
        }

        public virtual void Invalidate()
        {
            lock (this)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    for (Entry e = table[i]; e != null; e = e.next)
                    {
                        if (e.obj.Modified)
                        {
                            e.obj.Invalidate();
                        }
                    }
                    table[i] = null;
                }
                count = 0;
            }
        }

        public virtual void SetDirty(int oid)
        {
        }

        public virtual void ClearDirty(int oid)
        {
        }

        public virtual int Size()
        {
            return count;
        }

        internal class Entry
        {
            internal Entry next;
            internal IPersistent obj;
            internal int oid;

            internal Entry(int oid, IPersistent obj, Entry chain)
            {
                next = chain;
                this.oid = oid;
                this.obj = obj;
            }
        }
    }
}

