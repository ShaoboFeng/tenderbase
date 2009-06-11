#if !OMIT_PATRICIA_TRIE
namespace TenderBaseImpl
{
    using System;
    using System.Collections;
    using TenderBase;
    
    [Serializable]
    class PTrie : PersistentResource, PatriciaTrie
    {
        private PTrieNode rootZero;
        private PTrieNode rootOne;
        private int count;

        public virtual ArrayList Elements()
        {
            ArrayList list = new ArrayList(count);
            Fill(list, rootZero);
            Fill(list, rootOne);
            return list;
        }

        public virtual IPersistent[] ToArray()
        {
            return (IPersistent[]) Elements().ToArray();
        }

        public virtual IPersistent[] ToArray(IPersistent[] arr)
        {
            return (IPersistent[]) SupportClass.ICollectionSupport.ToArray(Elements(), arr);
        }

        public virtual IEnumerator GetEnumerator()
        {
            return Elements().GetEnumerator();
        }

        private static void Fill(ArrayList list, PTrieNode node)
        {
            if (node != null)
            {
                list.Add(node.obj);
                Fill(list, node.childZero);
                Fill(list, node.childOne);
            }
        }

        private static int FirstDigit(long key, int keyLength)
        {
            return (int) (SupportClass.URShift(key, (keyLength - 1))) & 1;
        }

        private static int GetCommonPart(long keyA, int keyLengthA, long keyB, int keyLengthB)
        {
            // truncate the keys so they are the same size (discard low bits)
            if (keyLengthA > keyLengthB)
            {
                keyA = SupportClass.URShift(keyA, keyLengthA - keyLengthB);
                keyLengthA = keyLengthB;
            }
            else
            {
                keyB = SupportClass.URShift(keyB, keyLengthB - keyLengthA);
                keyLengthB = keyLengthA;
            }
            // now get common part
            long diff = keyA ^ keyB;

            // finally produce common key part
            int count = 0;
            while (diff != 0)
            {
                diff = SupportClass.URShift(diff, 1);
                count += 1;
            }
            return keyLengthA - count;
        }

        public virtual IPersistent Add(PatriciaTrieKey key, IPersistent obj)
        {
            Modify();
            count += 1;

            if (FirstDigit(key.mask, key.length) == 1)
            {
                if (rootOne != null)
                {
                    return rootOne.Add(key.mask, key.length, obj);
                }
                else
                {
                    rootOne = new PTrieNode(key.mask, key.length, obj);
                    return null;
                }
            }
            else
            {
                if (rootZero != null)
                {
                    return rootZero.Add(key.mask, key.length, obj);
                }
                else
                {
                    rootZero = new PTrieNode(key.mask, key.length, obj);
                    return null;
                }
            }
        }

        public virtual IPersistent FindBestMatch(PatriciaTrieKey key)
        {
            if (FirstDigit(key.mask, key.length) == 1)
            {
                if (rootOne != null)
                {
                    return rootOne.FindBestMatch(key.mask, key.length);
                }
            }
            else
            {
                if (rootZero != null)
                {
                    return rootZero.FindBestMatch(key.mask, key.length);
                }
            }
            return null;
        }

        public virtual IPersistent FindExactMatch(PatriciaTrieKey key)
        {
            if (FirstDigit(key.mask, key.length) == 1)
            {
                if (rootOne != null)
                {
                    return rootOne.FindExactMatch(key.mask, key.length);
                }
            }
            else
            {
                if (rootZero != null)
                {
                    return rootZero.FindExactMatch(key.mask, key.length);
                }
            }
            return null;
        }

        public virtual IPersistent Remove(PatriciaTrieKey key)
        {
            if (FirstDigit(key.mask, key.length) == 1)
            {
                if (rootOne != null)
                {
                    IPersistent obj = rootOne.Remove(key.mask, key.length);
                    if (obj != null)
                    {
                        Modify();
                        count -= 1;
                        if (rootOne.NotUsed)
                        {
                            rootOne.Deallocate();
                            rootOne = null;
                        }
                        return obj;
                    }
                }
            }
            else
            {
                if (rootZero != null)
                {
                    IPersistent obj = rootZero.Remove(key.mask, key.length);
                    if (obj != null)
                    {
                        Modify();
                        count -= 1;
                        if (rootZero.NotUsed)
                        {
                            rootZero.Deallocate();
                            rootZero = null;
                        }
                        return obj;
                    }
                }
            }
            return null;
        }

        public virtual void Clear()
        {
            if (rootOne != null)
            {
                rootOne.Deallocate();
                rootOne = null;
            }
            if (rootZero != null)
            {
                rootZero.Deallocate();
                rootZero = null;
            }
            count = 0;
        }

        [Serializable]
        internal class PTrieNode : Persistent
        {
            internal virtual bool NotUsed
            {
                get
                {
                    return obj == null && childOne == null && childZero == null;
                }
            }

            internal long key;
            internal int keyLength;
            internal IPersistent obj;
            internal PTrieNode childZero;
            internal PTrieNode childOne;

            internal PTrieNode(long key, int keyLength, IPersistent obj)
            {
                this.obj = obj;
                this.key = key;
                this.keyLength = keyLength;
            }

            internal PTrieNode()
            {
            }

            internal virtual IPersistent Add(long key, int keyLength, IPersistent obj)
            {
                if (key == this.key && keyLength == this.keyLength)
                {
                    Modify();
                    // the new is matched exactly by this node's key, so just replace the node object
                    IPersistent prevObj = this.obj;
                    this.obj = obj;
                    return prevObj;
                }
                int keyLengthCommon = TenderBaseImpl.PTrie.GetCommonPart(key, keyLength, this.key, this.keyLength);
                int keyLengthDiff = this.keyLength - keyLengthCommon;
                long keyCommon = SupportClass.URShift(key, (keyLength - keyLengthCommon));
                long keyDiff = this.key - (keyCommon << keyLengthDiff);
                // process diff with this node's key, if any
                if (keyLengthDiff > 0)
                {
                    Modify();
                    // create a new node with the diff
                    PTrieNode newNode = new PTrieNode(keyDiff, keyLengthDiff, this.obj);
                    // transfer infos of this node to the new node
                    newNode.childZero = childZero;
                    newNode.childOne = childOne;

                    // update this node to hold common part
                    this.key = keyCommon;
                    this.keyLength = keyLengthCommon;
                    this.obj = null;

                    // and set the new node as child of this node
                    if (TenderBaseImpl.PTrie.FirstDigit(keyDiff, keyLengthDiff) == 1)
                    {
                        childZero = null;
                        childOne = newNode;
                    }
                    else
                    {
                        childZero = newNode;
                        childOne = null;
                    }
                }

                // process diff with the new key, if any
                if (keyLength > keyLengthCommon)
                {
                    // get diff with the new key
                    keyLengthDiff = keyLength - keyLengthCommon;
                    keyDiff = key - (keyCommon << keyLengthDiff);

                    // get which child we use as insertion point and do insertion (recursive)
                    if (TenderBaseImpl.PTrie.FirstDigit(keyDiff, keyLengthDiff) == 1)
                    {
                        if (childOne != null)
                        {
                            return childOne.Add(keyDiff, keyLengthDiff, obj);
                        }
                        else
                        {
                            Modify();
                            childOne = new PTrieNode(keyDiff, keyLengthDiff, obj);
                            return null;
                        }
                    }
                    else
                    {
                        if (childZero != null)
                        {
                            return childZero.Add(keyDiff, keyLengthDiff, obj);
                        }
                        else
                        {
                            Modify();
                            childZero = new PTrieNode(keyDiff, keyLengthDiff, obj);
                            return null;
                        }
                    }
                }
                else
                {
                    // the new key was containing within this node's original key, so just set this node as terminator
                    IPersistent prevObj = this.obj;
                    this.obj = obj;
                    return prevObj;
                }
            }

            internal virtual IPersistent FindBestMatch(long key, int keyLength)
            {
                if (keyLength > this.keyLength)
                {
                    int keyLengthCommon = TenderBaseImpl.PTrie.GetCommonPart(key, keyLength, this.key, this.keyLength);
                    int keyLengthDiff = keyLength - keyLengthCommon;
                    long keyCommon = SupportClass.URShift(key, keyLengthDiff);
                    long keyDiff = key - (keyCommon << keyLengthDiff);

                    if (TenderBaseImpl.PTrie.FirstDigit(keyDiff, keyLengthDiff) == 1)
                    {
                        if (childOne != null)
                        {
                            return childOne.FindBestMatch(keyDiff, keyLengthDiff);
                        }
                    }
                    else
                    {
                        if (childZero != null)
                        {
                            return childZero.FindBestMatch(keyDiff, keyLengthDiff);
                        }
                    }
                }
                return obj;
            }

            internal virtual IPersistent FindExactMatch(long key, int keyLength)
            {
                if (keyLength >= this.keyLength)
                {
                    if (key == this.key && keyLength == this.keyLength)
                    {
                        return obj;
                    }
                    else
                    {
                        int keyLengthCommon = TenderBaseImpl.PTrie.GetCommonPart(key, keyLength, this.key, this.keyLength);
                        int keyLengthDiff = keyLength - keyLengthCommon;
                        long keyCommon = SupportClass.URShift(key, keyLengthDiff);
                        long keyDiff = key - (keyCommon << keyLengthDiff);

                        if (TenderBaseImpl.PTrie.FirstDigit(keyDiff, keyLengthDiff) == 1)
                        {
                            if (childOne != null)
                            {
                                return childOne.FindBestMatch(keyDiff, keyLengthDiff);
                            }
                        }
                        else
                        {
                            if (childZero != null)
                            {
                                return childZero.FindBestMatch(keyDiff, keyLengthDiff);
                            }
                        }
                    }
                }
                return null;
            }

            internal virtual IPersistent Remove(long key, int keyLength)
            {
                if (keyLength >= this.keyLength)
                {
                    if (key == this.key && keyLength == this.keyLength)
                    {
                        IPersistent obj = this.obj;
                        this.obj = null;
                        return obj;
                    }
                    else
                    {
                        int keyLengthCommon = TenderBaseImpl.PTrie.GetCommonPart(key, keyLength, this.key, this.keyLength);
                        int keyLengthDiff = keyLength - keyLengthCommon;
                        long keyCommon = SupportClass.URShift(key, keyLengthDiff);
                        long keyDiff = key - (keyCommon << keyLengthDiff);

                        if (TenderBaseImpl.PTrie.FirstDigit(keyDiff, keyLengthDiff) == 1)
                        {
                            if (childOne != null)
                            {
                                IPersistent obj = childOne.FindBestMatch(keyDiff, keyLengthDiff);
                                if (obj != null)
                                {
                                    if (childOne.NotUsed)
                                    {
                                        Modify();
                                        childOne.Deallocate();
                                        childOne = null;
                                    }
                                    return obj;
                                }
                            }
                        }
                        else
                        {
                            if (childZero != null)
                            {
                                IPersistent obj = childZero.FindBestMatch(keyDiff, keyLengthDiff);
                                if (obj != null)
                                {
                                    if (childZero.NotUsed)
                                    {
                                        Modify();
                                        childZero.Deallocate();
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

            public override void Deallocate()
            {
                if (childOne != null)
                {
                    childOne.Deallocate();
                }
                if (childZero != null)
                {
                    childZero.Deallocate();
                }
                base.Deallocate();
            }
        }
    }
}
#endif
