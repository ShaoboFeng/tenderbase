namespace TenderBase
{
    using System;
    using System.Collections;

    /// <summary> Interface of bit index.
    /// Bit index allows to effiicently search object with specified
    /// set of properties. Each object has associated mask of 32 bites.
    /// Meaning of bits is application dependent. Usually each bit stands for
    /// some binary or boolean property, for example "sex", but it is possible to
    /// use group of bits to represent enumerations with more possible values.
    /// </summary>
    public interface BitIndex : IPersistent, IResource
    {
        /// <summary> Get properties of specified object</summary>
        /// <param name="obj">object which properties are requested
        /// </param>
        /// <returns> bit mask associated with this objects
        /// </returns>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no object in the index
        /// </exception>
        int Get(IPersistent obj);

        /// <summary> Put new object in the index. If such objct already exists in index, then its
        /// mask will be rewritten
        /// </summary>
        /// <param name="obj">object to be placed in the index. Object can be not yet peristent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        /// <param name="mask">bit mask associated with this objects
        /// </param>
        void Put(IPersistent obj, int mask);

        /// <summary> Remove object from the index </summary>
        /// <param name="obj">object removed from the index
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such object in the index
        /// </exception>
        void Remove(IPersistent obj);

        /// <summary> Get number of objects in the index</summary>
        /// <returns> number of objects in the index
        /// </returns>
        int Size();

        /// <summary> Remove all objects from the index</summary>
        void Clear();

        /// <summary> Get iterator for selecting objects with specified properties.
        /// To select all record this method should be invoked with (0, 0) parameters
        /// </summary>
        /// <param name="set">bitmask specifying bits which should be set (1)
        /// </param>
        /// <param name="clear">bitmask specifying bits which should be cleared (0)
        /// </param>
        /// <returns> selection iterator
        /// </returns>
        IEnumerator GetEnumerator(int set_Renamed, int clear);

        /// <summary> Get iterator through all objects in the index</summary>
        /// <returns> iterator through all objects in the index
        /// </returns>
        IEnumerator GetEnumerator();
    }
}
