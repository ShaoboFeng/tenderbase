namespace TenderBase
{
    using System;
    
    /// <summary> Class encapsulating native Java string. java.lang.String is not persistent object
    /// so it can not be stored in Perst as independent persistent object.
    /// But sometimes it is needed. This class sole this problem providing implcit conversion
    /// operator from java.lang.String to PerisstentString.
    /// Also PersistentString class is mutable, allowing to change it's values.
    /// </summary>
    [Serializable]
    public class PersistentString : PersistentResource
    {
        /// <summary> Consutrctor of perisstent string</summary>
        /// <param name="str">Java string
        /// </param>
        public PersistentString(string str)
        {
            this.str = str;
        }

        /// <summary> Get Java string</summary>
        /// <returns> Java string
        /// </returns>
        public override string ToString()
        {
            return str;
        }

        /// <summary> Append string to the current string value of PersistentString</summary>
        /// <param name="tail">appended string
        /// </param>
        public virtual void Append(string tail)
        {
            Modify();
            str = str + tail;
        }

        /// <summary> Assign new string value to the PersistentString</summary>
        /// <param name="str">new string value
        /// </param>
        public virtual void Set(string str)
        {
            Modify();
            this.str = str;
        }

        /// <summary> Get current string value</summary>
        /// <returns> Java string
        /// </returns>
        public virtual string Get()
        {
            return str;
        }

        private string str;
    }
}

