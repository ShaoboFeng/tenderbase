namespace TenderBase
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    
    /// <summary> Base class for all persistent capable objects</summary>
    [Serializable]
    public class Persistent : IPersistent
    {
        public virtual bool Modified
        {
            get
            {
                return (state & DIRTY) != 0;
            }
        }

        public virtual bool Deleted
        {
            get
            {
                return (state & DELETED) != 0;
            }
        }

        public virtual int Oid
        {
            get
            {
                return oid;
            }
        }

        public virtual Storage Storage
        {
            get
            {
                return storage;
            }
        }

        public virtual void Load()
        {
            if (oid != 0 && (state & RAW) != 0)
                storage.LoadObject(this);
        }

        public virtual void LoadAndModify()
        {
            Load();
            Modify();
        }

        public bool IsRaw()
        {
            return (state & RAW) != 0;
        }

        public bool IsPersistent()
        {
            return oid != 0;
        }

        public virtual void MakePersistent(Storage storage)
        {
            if (oid == 0)
                storage.MakePersistent(this);
        }

        public virtual void Store()
        {
            if ((state & RAW) != 0)
                throw new StorageError(StorageError.ACCESS_TO_STUB);

            if (storage != null)
            {
                storage.StoreObject(this);
                state &= ~ DIRTY;
            }
        }

        public virtual void Modify()
        {
            if ((state & DIRTY) == 0 && oid != 0)
            {
                if ((state & RAW) != 0)
                {
                    throw new StorageError(StorageError.ACCESS_TO_STUB);
                }
                Assert.That((state & DELETED) == 0);
                storage.ModifyObject(this);
                state |= DIRTY;
            }
        }

        public Persistent()
        {
        }

        public Persistent(Storage storage)
        {
            this.storage = storage;
        }

        public virtual void Deallocate()
        {
            if (oid != 0)
            {
                storage.DeallocateObject(this);
                state = 0;
                oid = 0;
                storage = null;
            }
        }

        public virtual bool RecursiveLoading
        {
            get
            {
                return true;
            }
        }

        public override bool Equals(object o)
        {
            if (oid == 0)
                return base.Equals(o);

            IPersistent p = o as IPersistent;
            if (p == null)
                return false;

            return p.Oid == oid;
        }

        public override int GetHashCode()
        {
            return oid;
        }

        public virtual void OnLoad()
        {
        }

        public virtual void OnStore()
        {
        }

        public virtual void Invalidate()
        {
            state &= ~ DIRTY;
            state |= RAW;
        }

        ~Persistent()
        {
            if ((state & DIRTY) != 0 && oid != 0)
                storage.StoreFinalizedObject(this);

            state = DELETED;
        }

        [NonSerialized]
        internal Storage storage;
        [NonSerialized]
        internal int oid;
        [NonSerialized]
        internal int state;

        private const int RAW = 1;
        private const int DIRTY = 2;
        private const int DELETED = 4;

        public virtual void AssignOid(Storage storage, int oid, bool raw)
        {
            this.oid = oid;
            this.storage = storage;
            if (raw)
                state |= RAW;
            else
                state &= ~RAW;
        }

        protected Persistent(SerializationInfo s, StreamingContext context)
        {
            oid = s.GetInt32("TenderBase.Persistentdata1");
        }

        public virtual void GetObjectData(SerializationInfo s, StreamingContext context)
        {
            s.AddValue("TenderBase.Persistentdata1", oid);
        }
    }
}

