namespace TenderBaseImpl 
{
    using System;

    public class LRU
    {
        internal LRU next;
        internal LRU prev;

        internal LRU()
        {
            next = prev = this;
        }

        internal void Unlink()
        {
            next.prev = prev;
            prev.next = next;
        }

        internal void Link(LRU node)
        {
            node.next = next;
            node.prev = this;
            next.prev = node;
            next = node;
        }
    }
}

