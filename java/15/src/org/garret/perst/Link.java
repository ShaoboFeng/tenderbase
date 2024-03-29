package org.garret.perst;

import java.util.Iterator;
import java.util.Collection;

/**
 * Interface for one-to-many relation. There are two types of relations:
 * embedded (when references to the related objects are stored in relation
 * owner object itself) and standalone (when relation is separate object, which contains
 * the reference to the relation owner and relation members). Both kinds of relations
 * implements Link interface. Embedded relation is created by Storage.createLink method
 * and standalone relation is represented by Relation persistent class created by
 * Storage.createRelation method.
 */
public interface Link<T extends IPersistent> extends Collection<T> {
    /**
     * Get number of the linked objects 
     * @return the number of related objects
     */
    public int size();
    
    /**
     * Set number of the linked objects 
     * @param newSize new number of linked objects (if it is greater than original number, 
     * than extra elements will be set to null)
     */
    public void setSize(int newSize);
    
    /**
     * Returns <tt>true</tt> if there are no related object
     *
     * @return <tt>true</tt> if there are no related object
     */
    boolean isEmpty();

    /**
     * Get related object by index
     * @param i index of the object in the relation
     * @return referenced object
     */
    public T get(int i);

    /**
     * Get related object by index without loading it.
     * Returned object can be used only to get it OID or to compare with other objects using
     * <code>equals</code> method
     * @param i index of the object in the relation
     * @return stub representing referenced object
     */
    public IPersistent getRaw(int i);

    /**
     * Replace i-th element of the relation
     * @param i index in the relation
     * @param obj object to be included in the relation     
     */
    public void set(int i, T obj);

    /**
     * Remove object with specified index from the relation
     * @param i index in the relation
     */
    public void remove(int i);

    /**
     * Remove object from the relation
     * @param o removed object
     * @return <code>true</code> if relation is changed as the result of this operation
     */
    public boolean remove(Object o);

    /**
     * Insert new object in the relation
     * @param i insert position, should be in [0,size()]
     * @param obj object inserted in the relation
     */
    public void insert(int i, T obj);

    /**
     * Add new object to the relation
     * @param obj object inserted in the relation
     */
    public boolean add(T obj);

    /**
     * Add all elements of the array to the relation
     * @param arr array of objects which should be added to the relation
     */
    public void addAll(T[] arr);
    
    /**
     * Add specified elements of the array to the relation
     * @param arr array of objects which should be added to the relation
     * @param from index of the first element in the array to be added to the relation
     * @param length number of elements in the array to be added in the relation
     */
    public void addAll(T[] arr, int from, int length);

    /**
     * Add all object members of the other relation to this relation
     * @param link another relation
     */
    public boolean addAll(Link<T> link);

   /**
     * Return array with relation members. Members are not loaded and 
     * size of the array can be greater than actual number of members. 
     * @return array of object with relation members used in implementation of Link class
     */
    public IPersistent[] toRawArray(); 

    /**
     * Get relation members as array of object
     * @return array of object with relation members
     */
    public IPersistent[] toPersistentArray();
    
    /**
     * Get all relation members as array.
     * The runtime type of the returned array is that of the specified array.  
     * If the index fits in the specified array, it is returned therein.  
     * Otherwise, a new array is allocated with the runtime type of the 
     * specified array and the size of this index.<p>
     *
     * If this index fits in the specified array with room to spare
     * (i.e., the array has more elements than this index), the element
     * in the array immediately following the end of the index is set to
     * <tt>null</tt>.  This is useful in determining the length of this
     * index <i>only</i> if the caller knows that this index does
     * not contain any <tt>null</tt> elements.)<p>
     * @return array of object with relation members
     */
    public <T> T[] toArray(T[] arr);

    /**
     * Checks if relation contains specified object
     * @param obj specified object
     */
    public boolean contains(Object obj);

    /**
     * Check if i-th element of Link is the same as specified obj
     * @param i element index
     * @param obj object to compare with
     * @return <code>true</code> if i-th element of Link reference the same object as "obj"
     */
    public boolean containsElement(int i, T obj);

    /**
     * Get index of the specified object in the relation
     * @param obj specified object
     * @return zero based index of the object or -1 if object is not in the relation
     */
    public int indexOf(Object obj);

    /**
     * Remove all members from the relation
     */
    public void clear();

    /**
     * Get iterator through link members
     */
    public Iterator<T> iterator();

    /**
     * Replace all direct references to linked objects with stubs. 
     * This method is needed to avoid memory exhaustion in case when 
     * there is a large number of objects in database, mutually
     * referencing each other (each object can directly or indirectly 
     * be accessed from other objects).
     */
    public void unpin();
     
    /**
     * Replace references to elements with direct references.
     * It will improve speed of manipulations with links, but it can cause
     * recursive loading in memory large number of objects and as a result - memory
     * overflow, because garbage collector will not be able to collect them
     */
    public void pin();     
}





