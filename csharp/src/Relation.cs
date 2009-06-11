namespace TenderBase
{
    using System;
    using System.Collections;
    
    /// <summary> Class representing relation between owner and members</summary>
    [Serializable]
    public abstract class Relation : Persistent, Link
    {
        //UPGRADE_NOTE: Respective javadoc comments were merged. It should be changed in order to comply with .NET documentation conventions.
        /// <summary> Get relation owner</summary>
        /// <returns> owner of the relation
        /// </returns>
        /// <summary> Set relation owner</summary>
        /// <param name="owner">new owner of the relation
        /// </param>
        public virtual IPersistent Owner
        {
            get
            {
                return owner;
            }

            set
            {
                this.owner = value;
                Store();
            }
        }

        public abstract int Size { get;  set; }

        /// <summary> Relation constructor. Creates empty relation with specified owner and no members.
        /// Members can be added to the relation later.
        /// </summary>
        /// <param name="owner">owner of the relation
        /// </param>
        public Relation(IPersistent owner)
        {
            this.owner = owner;
        }

        protected internal Relation()
        {
        }

        private IPersistent owner;
        public abstract void AddAll(IPersistent[] param1, int param2, int param3);
        public abstract bool ContainsElement(int param1, IPersistent param2);
        public abstract IPersistent GetRaw(int param1);
        public abstract IPersistent[] ToArray(IPersistent[] param1);
        public abstract void Unpin();
        public abstract void AddAll(Link param1);
        public abstract void AddAll(IPersistent[] param1);
        public abstract IEnumerator GetEnumerator();
        public abstract void Pin();
        public abstract void Set(int param1, IPersistent param2);
        public abstract IPersistent[] ToArray();
        public abstract IPersistent[] ToRawArray();
        public abstract void Clear();
        public abstract IPersistent Get(int param1);
        public abstract int IndexOf(IPersistent param1);
        public abstract void Insert(int param1, IPersistent param2);
        public abstract bool Contains(IPersistent param1);
        public abstract void Remove(int param1);
        public abstract void Add(IPersistent param1);
    }
}

