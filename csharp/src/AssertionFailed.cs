namespace TenderBase
{
    using System;
    
    /// <summary> Exception raised by <code>Assert</code> class when assertion was failed.</summary>
    [Serializable]
    public class AssertionFailed : System.ApplicationException
    {
        internal AssertionFailed()
            : base("Assertion failed")
        {
        }

        internal AssertionFailed(string description)
            : base("Assertion '" + description + "' failed")
        {
        }
    }
}
