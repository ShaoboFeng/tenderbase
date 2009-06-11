namespace TenderBase
{
    using System;
    
    /// <summary> Exception throw by storage implementation</summary>
    [Serializable]
    public class StorageError : System.SystemException
    {
        /// <summary> Get exception error code (see definitions above)</summary>
        public virtual int ErrorCode
        {
            get
            {
                return errorCode;
            }
        }

        /// <summary> Get original exception if StorageError excepotion was thrown as the result
        /// of catching some other exception within Storage implementation.
        /// StorageError is used as wrapper of other exceptions to avoid cascade propagation
        /// of throws and try/catch constructions.
        /// </summary>
        /// <returns> original exception or <code>null</code> if there is no such exception
        /// </returns>
        public virtual System.Exception OriginalException
        {
            get
            {
                return origEx;
            }
        }

        // TODOPORT: should this be an enum?
        public const int STORAGE_NOT_OPENED = 1;
        public const int STORAGE_ALREADY_OPENED = 2;
        public const int FILE_ACCESS_ERROR = 3;
        public const int KEY_NOT_UNIQUE = 4;
        public const int KEY_NOT_FOUND = 5;
        public const int SCHEMA_CHANGED = 6;
        public const int UNSUPPORTED_TYPE = 7;
        public const int UNSUPPORTED_INDEX_TYPE = 8;
        public const int INCOMPATIBLE_KEY_TYPE = 9;
        public const int NOT_ENOUGH_SPACE = 10;
        public const int DATABASE_CORRUPTED = 11;
        public const int CONSTRUCTOR_FAILURE = 12;
        public const int DESCRIPTOR_FAILURE = 13;
        public const int ACCESS_TO_STUB = 14;
        public const int INVALID_OID = 15;
        public const int DELETED_OBJECT = 16;
        public const int ACCESS_VIOLATION = 17;
        public const int CLASS_NOT_FOUND = 18;
        public const int NULL_VALUE = 19;
        public const int INDEXED_FIELD_NOT_FOUND = 20;
        public const int LOCK_FAILED = 21;
        public const int NO_SUCH_PROPERTY = 22;
        public const int BAD_PROPERTY_VALUE = 23;
        public const int SERIALIZE_PERSISTENT = 24;
        public const int EMPTY_VALUE = 25;
        public const int UNSUPPORTED_ENCODING = 26;
        public const int STORAGE_IS_USED = 27;

        private static readonly string[] messageText = new string[]
        {
            "",
            "Storage not opened",
            "Storage already opened",
            "File access error",
            "Key not unique",
            "Key not found",
            "Database schema was changed for",
            "Unsupported type",
            "Unsupported index type",
            "Incompatible key type",
            "Not enough space",
            "Database file is corrupted",
            "Failed to instantiate the object of",
            "Failed to build descriptor for",
            "Stub object is accessed",
            "Invalid object reference",
            "Access to the deleted object",
            "Object access violation",
            "Failed to locate",
            "Null value",
            "Could not find indexed field",
            "Lock could not be granted",
            "No such database property",
            "Attempt to store persistent object as raw object",
            "Attempt to store java.lang.Object as value",
            "Unsupported encoding",
            "Storage is used by other application"
        };

        public StorageError(int errorCode)
            : base(messageText[errorCode])
        {
            this.errorCode = errorCode;
        }

        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value.
        public StorageError(int errorCode, System.Exception x)
            : base(messageText[errorCode] + ": " + x)
        {
            this.errorCode = errorCode;
            origEx = x;
        }

        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Object.toString' may return a different value.
        public StorageError(int errorCode, object param)
            : base(messageText[errorCode] + " " + param)
        {
            this.errorCode = errorCode;
        }

        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Object.toString' may return a different value.
        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value.
        public StorageError(int errorCode, object param, System.Exception x)
            : base(messageText[errorCode] + " " + param + ": " + x)
        {
            this.errorCode = errorCode;
            origEx = x;
        }

        private int errorCode;
        private System.Exception origEx;
    }
}

