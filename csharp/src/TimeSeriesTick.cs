#if !OMIT_TIME_SERIES
namespace TenderBase
{
    using System;
    
    /// <summary> Interface for timeseries element.
    /// You should derive your time series element from this class
    /// and implement getTime method.
    /// </summary>
    public interface TimeSeriesTick : IValue
    {
        /// <summary> Get time series element timestamp</summary>
        /// <returns> timestamp in milliseconds
        /// </returns>
        long Time
        {
            get;
        }
    }
}
#endif

