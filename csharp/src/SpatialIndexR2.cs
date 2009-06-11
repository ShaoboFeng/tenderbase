#if !OMIT_RTREER2
namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Interface of object spatial index.
    /// Spatial index is used to allow fast selection of spatial objects belonging to the specified rectangle.
    /// Spatial index is implemented using Guttman R-Tree with quadratic split algorithm.
    /// </summary>
    public interface SpatialIndexR2 : IPersistent, IResource
    {
        /// <summary> Get wrapping rectangle </summary>
        /// <returns> minimal rectangle containing all rectangles in the index, <code>null</code> if index is empty
        /// </returns>
        RectangleR2 WrappingRectangle
        {
            get;
        }

        /// <summary> Find all objects located in the selected rectangle</summary>
        /// <param name="r">selected rectangle
        /// </param>
        /// <returns> array of objects which enveloping rectangle intersects with specified rectangle
        /// </returns>
        IPersistent[] Get(RectangleR2 r);

        /// <summary> Get array of all members of the index</summary>
        /// <returns> array of index members
        /// </returns>
        IPersistent[] ToArray();

        /// <summary> Get all objects in the index.
        /// The runtime type of the returned array is that of the specified array.
        /// If the index fits in the specified array, it is returned therein.
        /// Otherwise, a new array is allocated with the runtime type of the
        /// specified array and the size of this index.<p>
        ///
        /// If this index fits in the specified array with room to spare
        /// (i.e., the array has more elements than this index), the element
        /// in the array immediately following the end of the index is set to
        /// <tt>null</tt>. This is useful in determining the length of this
        /// index <i>only</i> if the caller knows that this index does
        /// not contain any <tt>null</tt> elements.)<p>
        /// </summary>
        /// <param name="arr">specified array
        /// </param>
        /// <returns> array of all objects in the index
        /// </returns>
        IPersistent[] ToArray(IPersistent[] arr);

        /// <summary> Find all objects located in the selected rectangle</summary>
        /// <param name="r">selected rectangle
        /// </param>
        /// <returns> array list of objects which enveloping rectangle intersects with specified rectangle
        /// </returns>
        ArrayList GetList(RectangleR2 r);

        /// <summary> Put new object in the index. </summary>
        /// <param name="r">enveloping rectangle for the object
        /// </param>
        /// <param name="obj">object associated with this rectangle. Object can be not yet persistent, in this case
        /// its forced to become persistent by assigning OID to it.
        /// </param>
        void Put(RectangleR2 r, IPersistent obj);

        /// <summary> Remove object with specified enveloping rectangle from the tree.</summary>
        /// <param name="r">enveloping rectangle for the object
        /// </param>
        /// <param name="obj">object removed from the index
        /// </param>
        /// <exception cref="StorageError(StorageError.KEY_NOT_FOUND)">exception if there is no such key in the index
        /// </exception>
        void Remove(RectangleR2 r, IPersistent obj);

        /// <summary> Get number of objects in the index</summary>
        /// <returns> number of objects in the index
        /// </returns>
        int Size();

        /// <summary> Remove all objects from the index</summary>
        void Clear();

        /// <summary> Get iterator through all members of the index</summary>
        /// <returns> iterator through all objects in the index
        /// </returns>
        IEnumerator GetEnumerator();

        /// <summary> Get entry iterator through all members of the index</summary>
        /// <returns> entry iterator which key specifies recrtangle and value - correspondent object
        /// </returns>
        IEnumerator GetEntryEnumerator();

        /// <summary> Get objects which rectangle intersects with specified rectangle</summary>
        /// <param name="r">selected rectangle
        /// </param>
        /// <returns> iterator for objects which enveloping rectangle overlaps with specified rectangle
        /// </returns>
        IEnumerator GetEnumerator(RectangleR2 r);

        /// <summary> Get entry iterator through objects which rectangle intersects with specified rectangle</summary>
        /// <param name="r">selected rectangle
        /// </param>
        /// <returns> entry iterator for objects which enveloping rectangle overlaps with specified rectangle
        /// </returns>
        IEnumerator GetEntryEnumerator(RectangleR2 r);
    }
}
#endif

