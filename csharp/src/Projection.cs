namespace TenderBase
{
    using System;
    using System.Collections;
    using System.Reflection;
    
    /// <summary> Class use to project selected objects using relation field.
    /// For all selected objects (specified by array ort iterator),
    /// value of specified field (of IPersistent, array of IPersistent, Link or Relation type)
    /// is inspected and all referenced object for projection (duplicate values are eliminated)
    /// </summary>
    public class Projection
    {
        /// <summary> Constructor of projection specified by class and field name of projected objects</summary>
        /// <param name="type">base class for selected objects
        /// </param>
        /// <param name="fieldName">field name used to perform projection
        /// </param>
        public Projection(Type type, string fieldName)
        {
            SetProjectionField(type, fieldName);
        }

        /// <summary> Default constructor of projection. This constructor should be used
        /// only when you are going to derive your class from Projection and redefine
        /// map method in it or sepcify type and fieldName later using setProjectionField
        /// method
        /// </summary>
        public Projection()
        {
        }

        /// <summary> Specify class of the projected objects and projection field name</summary>
        /// <param name="type">base class for selected objects
        /// </param>
        /// <param name="fieldName">field name used to perform projection
        /// </param>
        public virtual void SetProjectionField(Type type, string fieldName)
        {
            try
            {
                //UPGRADE_TODO: The differences in the expected value of parameters for method 'java.lang.Class.getDeclaredField' may cause compilation errors.
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static);
                //UPGRADE_ISSUE: Method 'java.lang.reflect.AccessibleObject.setAccessible' was not converted.
                //TODOPORT: field.setAccessible(true);
            }
            catch (System.Exception x)
            {
                throw new StorageError(StorageError.KEY_NOT_FOUND, x);
            }
        }

        /// <summary> Project specified selection</summary>
        /// <param name="selection">array with selected object
        /// </param>
        public virtual void Project(IPersistent[] selection)
        {
            for (int i = 0; i < selection.Length; i++)
            {
                Map(selection[i]);
            }
        }

        /// <summary> Project specified object</summary>
        /// <param name="obj">selected object
        /// </param>
        public virtual void Project(IPersistent obj)
        {
            Map(obj);
        }

        /// <summary> Project specified selection</summary>
        /// <param name="selection">iterator specifying seleceted objects
        /// </param>
        public virtual void Project(IEnumerator selection)
        {
            //UPGRADE_TODO: Method 'java.util.Iterator.hasNext' was converted to 'IEnumerator.MoveNext' which has a different behavior.
            while (selection.MoveNext())
            {
                //UPGRADE_TODO: Method 'java.util.Iterator.next' was converted to 'IEnumerator.Current' which has a different behavior.
                Map((IPersistent) selection.Current);
            }
        }

        /// <summary> Join this projection with another projection.
        /// Result of this join is set of objects present in both projections.
        /// </summary>
        public virtual void Join(Projection prj)
        {
            SupportClass.ICollectionSupport.RetainAll(Set, prj.Set);
        }

        /// <summary> Get result of preceding project and join operations</summary>
        /// <returns> array of objects
        /// </returns>
        public virtual IPersistent[] ToArray()
        {
            return (IPersistent[])SupportClass.ICollectionSupport.ToArray(Set, new IPersistent[Set.Count]);
        }

        /// <summary> Get result of preceding project and join operations
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
        /// <param name="arr">destination array
        /// </param>
        /// <returns> array of objects
        /// </returns>
        public virtual IPersistent[] ToArray(IPersistent[] arr)
        {
            return (IPersistent[])SupportClass.ICollectionSupport.ToArray(Set, arr);
        }

        /// <summary> Get number of objects in the result </summary>
        public virtual int Size()
        {
            return Set.Count;
        }

        /// <summary> Get iterator for result of preceding project and join operations</summary>
        /// <returns> iterator
        /// </returns>
        public virtual IEnumerator GetEnumerator()
        {
            return Set.GetEnumerator();
        }

        /// <summary> Reset projection - clear result of prceding project and join operations</summary>
        public virtual void Reset()
        {
            Set.Clear();
        }

        /// <summary> Add object to the set</summary>
        /// <param name="obj">objcet to be added
        /// </param>
        protected internal virtual void Add(IPersistent obj)
        {
            if (obj != null)
                Set.Add(obj);
        }

        /// <summary> Get related objects for the object obj.
        /// It is possible to redifine this method in derived classes
        /// to provide application specific mapping
        /// </summary>
        /// <param name="obj">object from the selection
        /// </param>
        protected internal virtual void Map(IPersistent obj)
        {
            if (field == null)
            {
                Add(obj);
            }
            else
            {
                try
                {
                    object o = field.GetValue(obj);
                    if (o is Link)
                    {
                        IPersistent[] arr = ((Link) o).ToArray();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Add(arr[i]);
                        }
                    }
                    else if (o is IPersistent[])
                    {
                        IPersistent[] arr = (IPersistent[]) o;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Add(arr[i]);
                        }
                    }
                    else
                    {
                        Add((IPersistent) o);
                    }
                }
                catch (System.Exception x)
                {
                    throw new StorageError(StorageError.ACCESS_VIOLATION, x);
                }
            }
        }

        //UPGRADE_TODO: Class 'java.util.HashSet' was converted to 'SupportClass.HashSetSupport' which has a different behavior.
        private SupportClass.HashSetSupport Set = new SupportClass.HashSetSupport();
        private FieldInfo field;
    }
}

