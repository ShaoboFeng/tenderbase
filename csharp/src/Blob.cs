namespace TenderBase
{
    using System;
    using System.IO;
    
    /// <summary> Interface to store/fetch large binary objects</summary>
    public interface IBlob : IPersistent, IResource
    {
        /// <summary> Gets input stream. InputStream.availabe method can be used to get BLOB size</summary>
        /// <returns> input stream with BLOB data
        /// </returns>
        Stream InputStream
        {
            get;
        }

        /// <summary> Get output stream to append data to the BLOB.</summary>
        /// <returns> output srteam
        /// </returns>
        Stream GetOutputStream();

        /// <summary> Get output stream to append data to the BLOB.</summary>
        /// <param name="multisession">whether BLOB allows further appends of data or closing
        /// this output streat means that BLOB will not be changed any more.
        /// </param>
        /// <returns> output srteam
        /// </returns>
        Stream GetOutputStream(bool multisession);
    }
}

