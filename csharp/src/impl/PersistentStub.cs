namespace TenderBaseImpl
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using TenderBase;
    
    [Serializable]
    public class PersistentStub : IPersistent, ISerializable
    {
        public virtual bool Modified
        {
            get
            {
                return false;
            }
        }

        public virtual bool Deleted
        {
            get
            {
                return false;
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
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        public virtual void LoadAndModify()
        {
            Load();
            Modify();
        }

        public bool IsRaw()
        {
            return true;
        }

        public bool IsPersistent()
        {
            return true;
        }

        public virtual void MakePersistent(Storage storage)
        {
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        public virtual void Store()
        {
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        public virtual void Modify()
        {
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        public PersistentStub(Storage storage, int oid)
        {
            this.storage = storage;
            this.oid = oid;
        }

        public virtual void Deallocate()
        {
            throw new StorageError(StorageError.ACCESS_TO_STUB);
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
            return o is IPersistent && ((IPersistent) o).Oid == oid;
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
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        [NonSerialized]
        internal Storage storage;
        [NonSerialized]
        internal int oid;

        public virtual void AssignOid(Storage storage, int oid, bool raw)
        {
            throw new StorageError(StorageError.ACCESS_TO_STUB);
        }

        protected PersistentStub(SerializationInfo s, StreamingContext context)
        {
            oid = s.GetInt32("TenderBaseImpl.PersistentStubdata1");
        }

        public virtual void GetObjectData(SerializationInfo s, StreamingContext context)
        {
            s.AddValue("TenderBaseImpl.PersistentStubdata1", oid);
        }

        //UPGRADE_NOTE: A parameterless constructor was added for a serializable class to avoid compile errors.
        public PersistentStub()
        {
        }
    }
}

