package org.garret.perst.impl;

import org.garret.perst.*;
import java.util.*;

class PTrie<T extends IPersistent> extends PersistentCollection<T> implements PatriciaTrie<T> 
{ 
    private PTrieNode<T> rootZero;
    private PTrieNode<T> rootOne;
    private int          count;

    public int size() { 
        return count;
    }

    public ArrayList<T> elements() { 
        ArrayList<T> list = new ArrayList<T>(count);
        fill(list, rootZero);
        fill(list, rootOne);
        return list;
    }

    public Object[] toArray() { 
        return elements().toArray();
    }

    public <E> E[] toArray(E[] arr) { 
        return elements().toArray(arr);
    }

    public Iterator<T> iterator() { 
        return elements().iterator();
    }

    private static <E extends IPersistent> void fill(ArrayList<E> list, PTrieNode<E> node) { 
        if (node != null) {
            list.add(node.obj);
            fill(list, node.childZero);
            fill(list, node.childOne);
        }
    }

    private static int firstDigit(long key, int keyLength)
    {
        return (int)(key >>> (keyLength - 1)) & 1;
    }

    private static int getCommonPart(long keyA, int keyLengthA, long keyB, int keyLengthB)
    {
        // truncate the keys so they are the same size (discard low bits)
        if (keyLengthA > keyLengthB) {
            keyA >>>= keyLengthA - keyLengthB;
            keyLengthA = keyLengthB;
        } else {
            keyB >>>= keyLengthB - keyLengthA;
            keyLengthB = keyLengthA;
        }
        // now get common part
        long diff = keyA ^ keyB;
        
        // finally produce common key part
        int count = 0;
        while (diff != 0) {
            diff >>>= 1;
            count += 1;
        }
        return keyLengthA - count;
    }

    public T add(PatriciaTrieKey key, T obj) 
    { 
        modify();
        count += 1;

        if (firstDigit(key.mask, key.length) == 1) {
            if (rootOne != null) { 
                return rootOne.add(key.mask, key.length, obj);
            } else { 
                rootOne = new PTrieNode<T>(key.mask, key.length, obj);
                return null;
            }
        } else { 
            if (rootZero != null) { 
                return rootZero.add(key.mask, key.length, obj);
            } else { 
                rootZero = new PTrieNode<T>(key.mask, key.length, obj);
                return null;
            }
        }            
    }
    
    public T findBestMatch(PatriciaTrieKey key) 
    {
        if (firstDigit(key.mask, key.length) == 1) {
            if (rootOne != null) { 
                return rootOne.findBestMatch(key.mask, key.length);
            } 
        } else { 
            if (rootZero != null) { 
                return rootZero.findBestMatch(key.mask, key.length);
            } 
        }
        return null;
    }

    public T findExactMatch(PatriciaTrieKey key) 
    {
        if (firstDigit(key.mask, key.length) == 1) {
            if (rootOne != null) { 
                return rootOne.findExactMatch(key.mask, key.length);
            } 
        } else { 
            if (rootZero != null) { 
                return rootZero.findExactMatch(key.mask, key.length);
            } 
        }
        return null;
    }
    
    public T remove(PatriciaTrieKey key) 
    { 
        if (firstDigit(key.mask, key.length) == 1) {
            if (rootOne != null) { 
                T obj = rootOne.remove(key.mask, key.length);
                if (obj != null) { 
                    modify();
                    count -= 1;
                    if (rootOne.isNotUsed()) { 
                        rootOne.deallocate();
                        rootOne = null;
                    }
                    return obj;
                }
            }  
        } else { 
            if (rootZero != null) { 
                T obj = rootZero.remove(key.mask, key.length);
                if (obj != null) { 
                    modify();
                    count -= 1;
                    if (rootZero.isNotUsed()) { 
                        rootZero.deallocate();
                        rootZero = null;
                    }
                    return obj;
                }
            }  
        }
        return null;
    }

    public void clear() 
    {
        if (rootOne != null) { 
            rootOne.deallocate();
            rootOne = null;
        }
        if (rootZero != null) { 
            rootZero.deallocate();
            rootZero = null;
        }
        count = 0;
    }

    static class PTrieNode<T extends IPersistent> extends Persistent 
    {
        long        key;
        int         keyLength;
        T            obj;
        PTrieNode<T> childZero;
        PTrieNode<T> childOne;

        PTrieNode(long key, int keyLength, T obj)
        {
            this.obj = obj;
            this.key = key;
            this.keyLength = keyLength; 
        }

        PTrieNode() {}

        T add(long key, int keyLength, T obj) 
        {
            if (key == this.key && keyLength == this.keyLength) {
                modify();
                // the new is matched exactly by this node's key, so just replace the node object
                T prevObj = this.obj;
                this.obj = obj;
                return prevObj;
            }
            int keyLengthCommon = getCommonPart(key, keyLength, this.key, this.keyLength);
            int keyLengthDiff = this.keyLength - keyLengthCommon;
            long keyCommon = key >>> (keyLength - keyLengthCommon);
            long keyDiff = this.key - (keyCommon << keyLengthDiff);
            // process diff with this node's key, if any
            if (keyLengthDiff > 0) {
                modify();
                // create a new node with the diff
                PTrieNode<T> newNode = new PTrieNode<T>(keyDiff, keyLengthDiff, this.obj);
                // transfer infos of this node to the new node
                newNode.childZero = childZero;
                newNode.childOne = childOne;
                
                // update this node to hold common part
                this.key = keyCommon;
                this.keyLength = keyLengthCommon;
                this.obj = null;
                
                // and set the new node as child of this node
                if (firstDigit(keyDiff, keyLengthDiff) == 1) {
                    childZero = null;
                    childOne = newNode;
                } else {
                    childZero = newNode;
                    childOne = null;
                }
            }
            
            // process diff with the new key, if any
            if (keyLength > keyLengthCommon) {
                // get diff with the new key
                keyLengthDiff = keyLength - keyLengthCommon;
                keyDiff = key - (keyCommon << keyLengthDiff);
                
                // get which child we use as insertion point and do insertion (recursive)
                if (firstDigit(keyDiff, keyLengthDiff) == 1) {
                    if (childOne != null) {
                        return childOne.add(keyDiff, keyLengthDiff, obj);
                    } else { 
                        modify();
                        childOne = new PTrieNode<T>(keyDiff, keyLengthDiff, obj);
                        return null;
                    }
                } else {
                    if (childZero != null) { 
                        return childZero.add(keyDiff, keyLengthDiff, obj);
                    } else { 
                        modify();
                        childZero = new PTrieNode<T>(keyDiff, keyLengthDiff, obj);
                        return null;
                    }
                }
            } else { // the new key was containing within this node's original key, so just set this node as terminator
                T prevObj = this.obj;
                this.obj = obj;
                return prevObj;
            }            
        }
    
        T findBestMatch(long key, int keyLength) 
        {             
            if (keyLength > this.keyLength) { 
                int keyLengthCommon = getCommonPart(key, keyLength, this.key, this.keyLength);
                int keyLengthDiff = keyLength - keyLengthCommon;
                long keyCommon = key >>> keyLengthDiff;
                long keyDiff = key - (keyCommon << keyLengthDiff);

                if (firstDigit(keyDiff, keyLengthDiff) == 1) {
                    if (childOne != null) { 
                        return childOne.findBestMatch(keyDiff, keyLengthDiff);
                    }
                } else {
                    if (childZero != null) { 
                        return childZero.findBestMatch(keyDiff, keyLengthDiff);
                    }
                }
            }
            return obj;
        }

        T findExactMatch(long key, int keyLength) 
        {             
            if (keyLength >= this.keyLength) { 
                if (key == this.key && keyLength == this.keyLength) { 
                    return obj;
                } else { 
                    int keyLengthCommon = getCommonPart(key, keyLength, this.key, this.keyLength);
                    int keyLengthDiff = keyLength - keyLengthCommon;
                    long keyCommon = key >>> keyLengthDiff;
                    long keyDiff = key - (keyCommon << keyLengthDiff);
                    
                    if (firstDigit(keyDiff, keyLengthDiff) == 1) {
                        if (childOne != null) { 
                            return childOne.findBestMatch(keyDiff, keyLengthDiff);
                        }
                    } else {
                        if (childZero != null) { 
                            return childZero.findBestMatch(keyDiff, keyLengthDiff);
                        } 
                    }
                }
            }
            return null;
        }		

        boolean isNotUsed() { 
            return obj == null && childOne == null && childZero == null;
        }

        T remove(long key, int keyLength) 
        {             
            if (keyLength >= this.keyLength) { 
                if (key == this.key && keyLength == this.keyLength) { 
                    T obj = this.obj;
                    this.obj = null;
                    return obj;
                } else { 
                    int keyLengthCommon = getCommonPart(key, keyLength, this.key, this.keyLength);
                    int keyLengthDiff = keyLength - keyLengthCommon;
                    long keyCommon = key >>> keyLengthDiff;
                    long keyDiff = key - (keyCommon << keyLengthDiff);
                    
                    if (firstDigit(keyDiff, keyLengthDiff) == 1) {
                        if (childOne != null) { 
                            T obj = childOne.findBestMatch(keyDiff, keyLengthDiff);
                            if (obj != null) { 
                                if (childOne.isNotUsed()) {
                                    modify();
                                    childOne.deallocate();
                                    childOne = null;
                                }
                                return obj;                                    
                            }
                        }
                    } else {
                        if (childZero != null) { 
                            T obj = childZero.findBestMatch(keyDiff, keyLengthDiff);
                            if (obj != null) { 
                                if (childZero.isNotUsed()) { 
                                    modify();
                                    childZero.deallocate();
                                    childZero = null;
                                }
                                return obj;                                    
                            }
                        } 
                    }
                }
            }
            return null;
        }

        public void deallocate() 
        {
            if (childOne != null) { 
                childOne.deallocate();
            }
            if (childZero != null) { 
                childZero.deallocate();
            }
            super.deallocate();
        }
    }
}