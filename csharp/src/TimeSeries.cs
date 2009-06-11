#if !OMIT_TIME_SERIES
namespace TenderBase
{
    using System;
    using System.Collections;

    /// <summary> <p>
    /// Time series interface. Time series class is used for efficient
    /// handling of time series data. Ussually time series contains a very large number
    /// if relatively small elements which are ussually acessed in sucessive order.
    /// To avoid overhead of loading from the disk each particular time series element,
    /// this class group several subsequent time series elements together and store them
    /// as single object (block).
    /// </p><p>
    /// As far as Java currently has no templates and
    /// Perst need to know format of block class, it is responsibity of prgorammer
    /// to create block implementation derived from TimeSeriesBlock class
    /// and containing array of time series elements. Size of this array specifies
    /// the size of the block.
    /// </p>
    /// </summary>
    public interface TimeSeries : IPersistent, IResource
    {
        /// <summary> Get timestamp of first time series element</summary>
        /// <returns> time of time series start
        /// </returns>
        DateTime FirstTime
        {
            get;
        }

        /// <summary> Get timestamp of last time series element</summary>
        /// <returns> time of time series end
        /// </returns>
        DateTime LastTime
        {
            get;
        }

        /// <summary> Add new tick to time series</summary>
        /// <param name="tick">new time series element
        /// </param>
        void Add(TimeSeriesTick tick);

        /// <summary> Get forward iterator through all time series elements</summary>
        /// <returns> forward iterator
        /// </returns>
        IEnumerator GetEnumerator();

        /// <summary> Get forward iterator for time series elements belonging to the specified range</summary>
        /// <param name="from">inclusive time of the begging of interval,
        /// if null then take all elements from the beginning of time series
        /// </param>
        /// <param name="till">inclusive time of the ending of interval,
        /// if null then take all elements till the end of time series
        /// </param>
        /// <returns> forward iterator within specified range.
        /// </returns>
        IEnumerator GetEnumerator(DateTime from, DateTime till);

        /// <summary> Get iterator through all time series elements</summary>
        /// <param name="ascent">direction of iteration
        /// </param>
        /// <returns> iterator in specified direction
        /// </returns>
        IEnumerator GetEnumerator(bool ascent);

        /// <summary> Get forward iterator for time series elements belonging to the specified range</summary>
        /// <param name="from">inclusive time of the begging of interval,
        /// if null then take all elements from the beginning of time series
        /// </param>
        /// <param name="till">inclusive time of the ending of interval,
        /// if null then take all elements till the end of time series
        /// </param>
        /// <param name="ascent">direction of iteration
        /// </param>
        /// <returns> iterator within specified range in specified direction
        /// </returns>
        IEnumerator GetEnumerator(DateTime from, DateTime till, bool ascent);

        /// <summary> Get number of elements in time series</summary>
        /// <returns> number of elements in time series
        /// </returns>
        long Size();

        /// <summary> Get tick for specified data</summary>
        /// <param name="timestamp">time series element timestamp
        /// return time series element for the specified timestamp or null
        /// if no such element was found
        /// </param>
        TimeSeriesTick GetTick(DateTime timestamp);

        /// <summary> Check if data is available in time series for the specified time</summary>
        /// <param name="timestamp">time series element timestamp
        /// </param>
        /// <returns> <code>true</code> if there is element in time series with such timestamp,
        /// <code>false</code> otherwise
        /// </returns>
        bool Has(DateTime timestamp);

        /// <summary> Remove timeseries elements belonging to the specified range</summary>
        /// <param name="from">inclusive time of the begging of interval,
        /// if null then remove all elements from the beginning of time series
        /// </param>
        /// <param name="till">inclusive time of the ending of interval,
        /// if null then remove all elements till the end of time series
        /// </param>
        /// <returns> number of removed elements
        /// </returns>
        long Remove(DateTime from, DateTime till);
    }
}
#endif
