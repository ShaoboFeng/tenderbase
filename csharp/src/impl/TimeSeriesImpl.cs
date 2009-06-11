#if !OMIT_TIME_SERIES
namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using TenderBase;
    
    [Serializable]
    public class TimeSeriesImpl : PersistentResource, TimeSeries
    {
        public virtual DateTime FirstTime
        {
            get
            {
                IEnumerator blockIterator = index.GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                if (blockIterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                    //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                    return new DateTime(block.timestamp);
                }
                return DateTime.MinValue;
            }
        }

        public virtual DateTime LastTime
        {
            get
            {
                IEnumerator blockIterator = index.GetEnumerator(null, null, IndexSortOrder.Descent);
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                if (blockIterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                    //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'DateTime.DateTime' which has a different behavior.
                    return new DateTime(block.Ticks[block.used - 1].Time);
                }
                return DateTime.MaxValue;
            }
        }

        public virtual void Add(TimeSeriesTick tick)
        {
            long time = tick.Time;
            IPersistent[] blocks = index.Get(new Key(time - maxBlockTimeInterval), new Key(time));
            if (blocks.Length != 0)
                InsertInBlock((TimeSeriesBlock) blocks[blocks.Length - 1], tick);
            else
                AddNewBlock(tick);
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'TimeSeriesIterator' to access its enclosing instance.
        internal class TimeSeriesIterator : IEnumerator
        {
            private void InitBlock(TimeSeriesImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private TimeSeriesImpl enclosingInstance;

            public virtual object Current
            {
                get
                {
                    if (pos < 0)
                        throw new System.ArgumentOutOfRangeException();

                    TimeSeriesTick tick = currBlock.Ticks[pos];
                    if (++pos == currBlock.used)
                    {
                        //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                        if (blockIterator.MoveNext())
                        {
                            //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                            currBlock = (TimeSeriesBlock) blockIterator.Current;
                            pos = 0;
                        }
                        else
                        {
                            pos = -1;
                            return tick;
                        }
                    }

                    if (currBlock.Ticks[pos].Time > till)
                        pos = -1;
                    return tick;
                }
            }

            public TimeSeriesImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal TimeSeriesIterator(TimeSeriesImpl enclosingInstance, long from, long till)
            {
                InitBlock(enclosingInstance);
                pos = -1;
                this.till = till;
                blockIterator = Enclosing_Instance.index.GetEnumerator(new Key(from - Enclosing_Instance.maxBlockTimeInterval), new Key(till), IndexSortOrder.Ascent);
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (blockIterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                    int n = block.used;
                    TimeSeriesTick[] e = block.Ticks;
                    int l = 0, r = n;
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (from > e[i].Time)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }

                    Assert.That(l == r && (l == n || e[l].Time >= from));
                    if (l < n)
                    {
                        if (e[l].Time <= till)
                        {
                            pos = l;
                            currBlock = block;
                        }
                        return;
                    }
                }
            }

            public virtual bool MoveNext()
            {
                return pos >= 0;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            private IEnumerator blockIterator;
            private TimeSeriesBlock currBlock;
            private int pos;
            private long till;

            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'TimeSeriesReverseIterator' to access its enclosing instance.
        internal class TimeSeriesReverseIterator : IEnumerator
        {
            private void InitBlock(TimeSeriesImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }

            private TimeSeriesImpl enclosingInstance;

            public virtual object Current
            {
                get
                {
                    if (pos < 0)
                        throw new System.ArgumentOutOfRangeException();

                    TimeSeriesTick tick = currBlock.Ticks[pos];
                    if (--pos < 0)
                    {
                        //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                        if (blockIterator.MoveNext())
                        {
                            //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                            currBlock = (TimeSeriesBlock) blockIterator.Current;
                            pos = currBlock.used - 1;
                        }
                        else
                        {
                            pos = -1;
                            return tick;
                        }
                    }
                    if (currBlock.Ticks[pos].Time < from)
                        pos = -1;

                    return tick;
                }
            }

            public TimeSeriesImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            internal TimeSeriesReverseIterator(TimeSeriesImpl enclosingInstance, long from, long till)
            {
                InitBlock(enclosingInstance);
                pos = -1;
                this.from = from;
                blockIterator = Enclosing_Instance.index.GetEnumerator(new Key(from - Enclosing_Instance.maxBlockTimeInterval), new Key(till), IndexSortOrder.Descent);
                //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
                while (blockIterator.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                    TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                    int n = block.used;
                    TimeSeriesTick[] e = block.Ticks;
                    int l = 0, r = n;
                    while (l < r)
                    {
                        int i = (l + r) >> 1;
                        if (till >= e[i].Time)
                        {
                            l = i + 1;
                        }
                        else
                        {
                            r = i;
                        }
                    }
                    Assert.That(l == r && (l == n || e[l].Time > till));
                    if (l > 0)
                    {
                        if (e[l - 1].Time >= from)
                        {
                            pos = l - 1;
                            currBlock = block;
                        }
                        return;
                    }
                }
            }

            public virtual bool MoveNext()
            {
                return pos >= 0;
            }

            //UPGRADE_NOTE: The equivalent of method 'java.util.Iterator.remove' is not an override method.
            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            private IEnumerator blockIterator;
            private TimeSeriesBlock currBlock;
            private int pos;
            private long from;
            //UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic.
            public virtual void Reset()
            {
                throw new System.NotSupportedException();
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return GetEnumerator(DateTime.MinValue, DateTime.MaxValue, true);
        }

        public virtual IEnumerator GetEnumerator(DateTime from, DateTime till)
        {
            return GetEnumerator(from, till, true);
        }

        public virtual IEnumerator GetEnumerator(bool ascent)
        {
            return GetEnumerator(DateTime.MinValue, DateTime.MaxValue, ascent);
        }

        public virtual IEnumerator GetEnumerator(DateTime from, DateTime till, bool ascent)
        {
            long low = from.Ticks;
            long high = till.Ticks;
            if (ascent)
                return new TimeSeriesIterator(this, low, high);
            else
                return new TimeSeriesReverseIterator(this, low, high);
        }

        public virtual long Size()
        {
            long n = 0;
            IEnumerator blockIterator = index.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (blockIterator.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                n += block.used;
            }
            return n;
        }

        public virtual TimeSeriesTick GetTick(DateTime timestamp)
        {
            //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'DateTime.Ticks' which has a different behavior.
            long time = timestamp.Ticks;
            IEnumerator blockIterator = index.GetEnumerator(new Key(time - maxBlockTimeInterval), new Key(time), IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (blockIterator.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                int n = block.used;
                TimeSeriesTick[] e = block.Ticks;
                int l = 0, r = n;
                while (l < r)
                {
                    int i = (l + r) >> 1;
                    if (time > e[i].Time)
                    {
                        l = i + 1;
                    }
                    else
                    {
                        r = i;
                    }
                }

                Assert.That(l == r && (l == n || e[l].Time >= time));
                if (l < n && e[l].Time == time)
                    return e[l];
            }

            return null;
        }

        public virtual bool Has(DateTime timestamp)
        {
            return GetTick(timestamp) != null;
        }

        public virtual long Remove(DateTime from, DateTime till)
        {
            long low = from.Ticks;
            long high = till.Ticks;
            long nRemoved = 0;
            Key fromKey = new Key(low - maxBlockTimeInterval);
            Key tillKey = new Key(high);
            IEnumerator blockIterator = index.GetEnumerator(fromKey, tillKey, IndexSortOrder.Ascent);
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (blockIterator.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                int n = block.used;
                TimeSeriesTick[] e = block.Ticks;
                int l = 0, r = n;
                while (l < r)
                {
                    int i = (l + r) >> 1;
                    if (low > e[i].Time)
                    {
                        l = i + 1;
                    }
                    else
                    {
                        r = i;
                    }
                }

                Assert.That(l == r && (l == n || e[l].Time >= low));
                while (r < n && e[r].Time <= high)
                {
                    r += 1;
                    nRemoved += 1;
                }

                if (l == 0 && r == n)
                {
                    index.Remove(new Key(block.timestamp), block);
                    blockIterator = index.GetEnumerator(fromKey, tillKey, IndexSortOrder.Ascent);
                    block.Deallocate();
                }
                else if (l < n && l != r)
                {
                    if (l == 0)
                    {
                        index.Remove(new Key(block.timestamp), block);
                        block.timestamp = e[r].Time;
                        index.Put(new Key(block.timestamp), block);
                        blockIterator = index.GetEnumerator(fromKey, tillKey, IndexSortOrder.Ascent);
                    }

                    while (r < n)
                    {
                        e[l++] = e[r++];
                    }
                    block.used = l;
                    block.Modify();
                }
            }
            return nRemoved;
        }

        private void AddNewBlock(TimeSeriesTick t)
        {
            TimeSeriesBlock block;
            try
            {
                block = (TimeSeriesBlock) System.Activator.CreateInstance(blockClass);
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.CONSTRUCTOR_FAILURE, blockClass, x);
            }
            block.timestamp = t.Time;
            block.used = 1;
            block.Ticks[0] = t;
            index.Put(new Key(block.timestamp), block);
        }

        internal virtual void InsertInBlock(TimeSeriesBlock block, TimeSeriesTick tick)
        {
            long t = tick.Time;
            int i, n = block.used;

            TimeSeriesTick[] e = block.Ticks;
            int l = 0, r = n;
            while (l < r)
            {
                i = (l + r) >> 1;
                if (t > e[i].Time)
                {
                    l = i + 1;
                }
                else
                {
                    r = i;
                }
            }
            Assert.That(l == r && (l == n || e[l].Time >= t));
            if (r == 0)
            {
                if (e[n - 1].Time - t > maxBlockTimeInterval || n == e.Length)
                {
                    AddNewBlock(tick);
                    return;
                }
                block.timestamp = t;
            }
            else if (r == n)
            {
                if (t - e[0].Time > maxBlockTimeInterval || n == e.Length)
                {
                    AddNewBlock(tick);
                    return;
                }
            }

            if (n == e.Length)
            {
                AddNewBlock(e[n - 1]);
                for (i = n; --i > r; )
                {
                    e[i] = e[i - 1];
                }
            }
            else
            {
                for (i = n; i > r; i--)
                {
                    e[i] = e[i - 1];
                }
                block.used += 1;
            }

            e[r] = tick;
            block.Modify();
        }

        internal TimeSeriesImpl(Storage storage, Type blockClass, long maxBlockTimeInterval)
        {
            this.blockClass = blockClass;
            this.maxBlockTimeInterval = maxBlockTimeInterval;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value.
            blockClassName = blockClass.FullName;
            index = storage.CreateIndex(typeof(long), true);
        }

        internal TimeSeriesImpl()
        {
        }

        public override void OnLoad()
        {
            blockClass = ClassDescriptor.LoadClass(Storage, blockClassName);
        }

        public override void Deallocate()
        {
            IEnumerator blockIterator = index.GetEnumerator();
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (blockIterator.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                TimeSeriesBlock block = (TimeSeriesBlock) blockIterator.Current;
                block.Deallocate();
            }
            index.Deallocate();
            base.Deallocate();
        }

        private Index index;
        private long maxBlockTimeInterval;
        private string blockClassName;
        [NonSerialized]
        private Type blockClass;
    }
}
#endif

