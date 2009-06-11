namespace TenderBase
{
    using System;
    using System.Runtime.Serialization;
    
    /// <summary> Interface of all persistent capable objects</summary>
    public interface IPersistent : ISerializable
    {
        /// <summary> Check if object was modified within current transaction</summary>
        /// <returns> <code>true</code> if object is persistent and was modified within current transaction
        /// </returns>
        bool Modified
        {
            get;
        }

        /// <summary> Check if object is deleted by Java GC from process memory</summary>
        /// <returns> <code>true</code> if object is deleted by GC
        /// </returns>
        bool Deleted
        {
            get;
        }

       /// <summary> Get object identifier (OID)</summary>
        /// <returns> OID (0 if object is not persistent yet)
        /// </returns>
        int Oid
        {
            get;
        }

       /// <summary> Get storage in which this object is stored</summary>
        /// <returns> storage containing this object (null if object is not persistent yet)
        /// </returns>
        Storage Storage
        {
            get;
        }

        /// <summary> Load object from the database (if needed)</summary>
        void Load();

        /// <summary> Check if object is stub and has to be loaded from the database</summary>
        /// <returns> <code>true</code> if object has to be loaded from the database
        /// </returns>
        bool IsRaw();

        /// <summary> Check if object is persistent </summary>
        /// <returns> <code>true</code> if object has assigned OID
        /// </returns>
        bool IsPersistent();

        /// <summary> Explicitely make object peristent. Usually objects are made persistent
        /// implicitlely using "persistency on reachability apporach", but this
        /// method allows to do it explicitly
        /// </summary>
        /// <param name="storage">storage in which object should be stored
        /// </param>
        void MakePersistent(Storage storage);

        /// <summary> Save object in the database</summary>
        void Store();

        /// <summary> Mark object as modified. Object will be saved to the database during transaction commit.</summary>
        void Modify();

        /// <summary> Load object from the database (if needed) and mark it as modified</summary>
        void LoadAndModify();

        /// <summary> Deallocate persistent object from the database</summary>
        void Deallocate();

        /// <summary> Specified whether object should be automatically loaded when it is referenced
        /// by other loaded peristent object. Default implementation of this method
        /// returns <code>true</code> making all cluster of referenced objects loaded together.
        /// To avoid main memory overflow you should stop recursive loading of all objects
        /// from the database to main memory by redefining this method in some classes and returing
        /// <code>false</code> in it. In this case object has to be loaded explicitely
        /// using Persistent.load method.
        /// </summary>
        /// <returns> <code>true</code> if object is automatically loaded
        /// </returns>
        bool RecursiveLoading
        {
            get;
        }

        /// <summary> Method called by the database after loading of the object.
        /// It can be used to initialize transient fields of the object.
        /// Default implementation of this method do nothing
        /// </summary>
        void OnLoad();

        /// <summary> Method called by the database before storing of the object.
        /// It can be used to save or close transient fields of the object.
        /// Default implementation of this method do nothing
        /// </summary>
        void OnStore();

        /// <summary> Invalidate object. Invalidated object has to be explicitly
        /// reloaded usin3g Load() method. Attempt to store invalidated object
        /// will cause StoraegError exception.
        /// </summary>
        void Invalidate();

        /// <summary> Assign OID to the object. This method is used by storage class and
        /// you should not invoke it directly
        /// </summary>
        /// <param name="storage">associated storage
        /// </param>
        /// <param name="oid">object identifier
        /// </param>
        void AssignOid(Storage storage, int oid, bool raw);
    }
}

