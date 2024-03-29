package org.garret.perst;

import java.util.*;

/**
 * Interface of object index.
 * This is base interface for Index and FieldIndex, allowing to write generic algorithms 
 * working with both types of indices.
 */
public interface GenericIndex extends IPersistent, IResource { 
    /**
     * Get object by key (exact match)     
     * @param key specified key. It should match with type of the index and should be inclusive.
     * @return object with this value of the key or <code>null</code> if key not found
     * @exception StorageError(StorageError.KEY_NOT_UNIQUE) exception if there is more than 
     * one object in the index with specified value of the key.
     */
    public IPersistent   get(Key key);
    
    /**
     * Get objects which key value belongs to the specified range.
     * Both from and till can be <code>null</code>.
     * If both are null, the method returns all objects from the index.
     * @param from low boundary. If <code>null</code> then low boundary is not specified.
     * Low boundary can be inclusive or exclusive. 
     * @param till high boundary. If <code>null</code> then high boundary is not specified.
     * High boundary can be inclusive or exclusive. 
     * @return array of objects which keys belongs to the specified interval, ordered by key value
     */
    public IPersistent[] get(Key from, Key till);


    /**
     * Get object by string key (exact match)     
     * @param key string key 
     * @return object with this value of the key or <code>null</code> if key not[ found
     * @exception StorageError(StorageError.KEY_NOT_UNIQUE) exception if there are more than 
     * one objects in the index with specified value of the key.
     */
    public IPersistent   get(String key);
    
    /**
     * Get objects with string key prefix 
     * @param prefix string key prefix
     * @return array of objects whose key starts with this prefix 
     */
    public IPersistent[] getPrefix(String prefix);
    
    /**
     * Locate all objects which key is prefix of specified word.
     * @param word string which prefixes are located in index
     * @return array of objects which key is prefix of specified word, ordered by key value
     */
    public IPersistent[] prefixSearch(String word);

    /**
     * Get number of objects in the index
     * @return number of objects in the index
     */
    public int           size();
    
    /**
     * Remove all objects from the index
     */
    public void          clear();

    /**
     * Get all objects in the index as array ordered by index key.
     * @return array of objects in the index ordered by key value
     */
    public IPersistent[] toPersistentArray();

    /**
     * Get all objects in the index as array ordered by index key.
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
     * @param arr specified array
     * @return array of all objects in the index
     */
    public IPersistent[] toPersistentArray(IPersistent[] arr);

    /**
     * Get iterator for traversing all objects in the index. 
     * Objects are iterated in the ascending key order. 
     * You should not update/remove or add members to the index during iteration
     * @return index iterator
     */
    public Iterator iterator();

    /**
     * Get iterator for traversing all entries in the index. 
     * Iterator next() method returns object implementing <code>Map.Entry</code> interface
     * which allows to get entry's key and value.
     * Objects are iterated in the ascending key order. 
     * You should not update/remove or add members to the index during iteration
     * @return index iterator
     */
    public Iterator entryIterator();

    static final int ASCENT_ORDER  = 0;
    static final int DESCENT_ORDER = 1;
    /**
     * Get iterator for traversing objects in the index with key belonging to the specified range. 
     * You should not update/remove or add members to the index during iteration
     * @param from low boundary. If <code>null</code> then low boundary is not specified.
     * Low boundary can be inclusive or exclusive. 
     * @param till high boundary. If <code>null</code> then high boundary is not specified.
     * High boundary can be inclusive or exclusive. 
     * @param order <code>ASCENT_ORDER</code> or <code>DESCENT_ORDER</code>
     * @return selection iterator
     */
    public Iterator iterator(Key from, Key till, int order);

    /**
     * Get iterator for traversing index entries with key belonging to the specified range. 
     * Iterator next() method returns object implementing <code>Map.Entry</code> interface
     * You should not update/remove or add members to the index during iteration
     * @param from low boundary. If <code>null</code> then low boundary is not specified.
     * Low boundary can be inclusive or exclusive. 
     * @param till high boundary. If <code>null</code> then high boundary is not specified.
     * High boundary can be inclusive or exclusive. 
     * @param order <code>ASCENT_ORDER</code> or <code>DESCENT_ORDER</code>
     * @return selection iterator
     */
    public Iterator entryIterator(Key from, Key till, int order);

    /**
     * Get iterator for objects whose keys start with specified prefix
     * Objects are iterated in the ascending key order. 
     * You should not update/remove or add members to the index during iteration
     * @param prefix key prefix
     * @return selection iterator
     */
    public Iterator prefixIterator(String prefix);

    /**
     * Get type of index key
     * @return type of index key
     */
    public Class getKeyType();
}