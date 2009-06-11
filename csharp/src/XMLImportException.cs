#if !OMIT_XML
namespace TenderBase
{
    using System;
    
    /// <summary> Exception thrown during import of data from XML file in database</summary>
    [Serializable]
    public class XMLImportException : System.Exception
    {
        public virtual string MessageText
        {
            get
            {
                return message;
            }
        }

        public virtual int Line
        {
            get
            {
                return line;
            }
        }

        public virtual int Column
        {
            get
            {
                return column;
            }
        }

        public XMLImportException(int line, int column, string message)
            : base("In line " + line + " column " + column + ": " + message)
        {
            this.line = line;
            this.column = column;
            this.message = message;
        }

        private int line;
        private int column;
        private string message;
    }
}
#endif
