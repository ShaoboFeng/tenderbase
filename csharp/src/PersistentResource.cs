namespace TenderBase
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    
    /// <summary> Base class for persistent objects supporting locking</summary>
    [Serializable]
    public class PersistentResource : Persistent, IResource
    {
        public virtual void SharedLock()
        {
            lock (this)
            {
                Thread currThread = Thread.CurrentThread;
                try
                {
                    while (true)
                    {
                        if (owner == currThread)
                        {
                            nWriters += 1;
                            break;
                        }
                        else if (nWriters == 0)
                        {
                            if (nReaders == 0 && storage != null)
                            {
                                storage.LockObject(this);
                            }
                            nReaders += 1;
                            break;
                        }
                        else
                        {
                            System.Threading.Monitor.Wait(this);
                        }
                    }
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    throw new StorageError(StorageError.LOCK_FAILED);
                }
            }
        }

        public virtual bool SharedLock(long timeout)
        {
            Thread currThread = Thread.CurrentThread;
            long startTime = (DateTime.Now.Ticks - 621355968000000000) / 10000;
            lock (this)
            {
                try
                {
                    while (true)
                    {
                        if (owner == currThread)
                        {
                            nWriters += 1;
                            return true;
                        }
                        else if (nWriters == 0)
                        {
                            if (nReaders == 0 && storage != null)
                            {
                                storage.LockObject(this);
                            }
                            nReaders += 1;
                            return true;
                        }
                        else
                        {
                            long currTime = (DateTime.Now.Ticks - 621355968000000000) / 10000;
                            if (startTime + timeout <= currTime)
                            {
                                return false;
                            }
                            System.Threading.Monitor.Wait(this, TimeSpan.FromMilliseconds(startTime + timeout - currTime));
                        }
                    }
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    return false;
                }
            }
        }

        public virtual void ExclusiveLock()
        {
            lock (this)
            {
                Thread currThread = Thread.CurrentThread;
                try
                {
                    while (true)
                    {
                        if (owner == currThread)
                        {
                            nWriters += 1;
                            break;
                        }
                        else if (nReaders == 0 && nWriters == 0)
                        {
                            nWriters = 1;
                            owner = currThread;
                            if (storage != null)
                            {
                                storage.LockObject(this);
                            }
                            break;
                        }
                        else
                        {
                            System.Threading.Monitor.Wait(this);
                        }
                    }
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    throw new StorageError(StorageError.LOCK_FAILED);
                }
            }
        }

        public virtual bool ExclusiveLock(long timeout)
        {
            Thread currThread = Thread.CurrentThread;
            long startTime = (DateTime.Now.Ticks - 621355968000000000) / 10000;
            lock (this)
            {
                try
                {
                    while (true)
                    {
                        if (owner == currThread)
                        {
                            nWriters += 1;
                            return true;
                        }
                        else if (nReaders == 0 && nWriters == 0)
                        {
                            nWriters = 1;
                            owner = currThread;
                            if (storage != null)
                            {
                                storage.LockObject(this);
                            }
                            return true;
                        }
                        else
                        {
                            long currTime = (DateTime.Now.Ticks - 621355968000000000) / 10000;
                            if (startTime + timeout <= currTime)
                            {
                                return false;
                            }
                            System.Threading.Monitor.Wait(this, TimeSpan.FromMilliseconds(startTime + timeout - currTime));
                        }
                    }
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    return false;
                }
            }
        }

        public virtual void Unlock()
        {
            lock (this)
            {
                if (nWriters != 0)
                {
                    if (--nWriters == 0)
                    {
                        owner = null;
                        System.Threading.Monitor.PulseAll(this);
                    }
                }
                else if (nReaders != 0)
                {
                    if (--nReaders == 0)
                    {
                        System.Threading.Monitor.PulseAll(this);
                    }
                }
            }
        }

        public virtual void Reset()
        {
            lock (this)
            {
                nReaders = 0;
                nWriters = 0;
                owner = null;
                System.Threading.Monitor.PulseAll(this);
            }
        }

        public PersistentResource()
        {
        }

        public PersistentResource(Storage storage) : base(storage)
        {
        }

        [NonSerialized]
        private Thread owner;
        [NonSerialized]
        private int nReaders;
        [NonSerialized]
        private int nWriters;
    }
}

