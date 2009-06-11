namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Interface of persistent set. </summary>
    public interface IPersistentSet : IPersistent, IResource
    {
        /* TODOPORT: convert those to C# collection idioms */
        int Count { get; }
        void Clear();
        bool IsEmpty();
        bool Contains(object o);
        object[] ToArray();
        object[] ToArray(object[] a);
        bool Add(object o);
        bool Remove(object o);
        bool ContainsAll(ICollection c);
        bool AddAll(ICollection c);
        bool RetainAll(ICollection c);
        bool RemoveAll(ICollection c);
        IEnumerator GetEnumerator();
    }
}

