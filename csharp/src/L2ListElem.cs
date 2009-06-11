namespace TenderBase
{
    using System;
    
    /// <summary> Double linked list element.</summary>
    [Serializable]
    public class L2ListElem : PersistentResource
    {
        /// <summary> Get next list element.
        /// Been call for the last list element, this method will return first element of the list
        /// or list header
        /// </summary>
        public virtual L2ListElem Next
        {
            get
            {
                return next;
            }
        }

        /// <summary> Get previous list element.
        /// Been call for the first list element, this method will return last element of the list
        /// or list header
        /// </summary>
        public virtual L2ListElem Prev
        {
            get
            {
                return prev;
            }
        }

        protected internal L2ListElem next;
        protected internal L2ListElem prev;

        /// <summary> Make list empty.
        /// This method should be applied to list header.
        /// </summary>
        public virtual void Prune()
        {
            Modify();
            next = prev = null;
        }

        /// <summary> Link specified element in the list after this element</summary>
        /// <param name="elem">element to be linked in the list after this elemen
        /// </param>
        public virtual void LinkAfter(L2ListElem elem)
        {
            Modify();
            next.Modify();
            elem.Modify();
            elem.next = next;
            elem.prev = this;
            next.prev = elem;
            next = elem;
        }

        /// <summary> Link specified element in the list before this element</summary>
        /// <param name="elem">element to be linked in the list before this elemen
        /// </param>
        public virtual void LinkBefore(L2ListElem elem)
        {
            Modify();
            prev.Modify();
            elem.Modify();
            elem.next = this;
            elem.prev = prev;
            prev.next = elem;
            prev = elem;
        }

        /// <summary> Remove element from the list</summary>
        public virtual void Unlink()
        {
            next.Modify();
            prev.Modify();
            next.prev = prev;
            prev.next = next;
        }
    }
}

