using System;
using System.Collections;
using TenderBase;

public class TestTimeSeries
{
    public class Quote : TimeSeriesTick
    {
        public virtual long Time
        {
            get
            {
                return (long) timestamp * 1000;
            }
        }

        internal int timestamp;
        internal float low;
        internal float high;
        internal float open;
        internal float close;
        internal int volume;
    }

    public class QuoteBlock : TimeSeriesBlock
    {
        public override TimeSeriesTick[] Ticks
        {
            get
            {
                if (quotes == null)
                {
                    quotes = new Quote[N_ELEMS_PER_BLOCK];
                    for (int i = 0; i < N_ELEMS_PER_BLOCK; i++)
                    {
                        quotes[i] = new Quote();
                    }
                }
                return quotes;
            }
        }

        private Quote[] quotes;
        internal const int N_ELEMS_PER_BLOCK = 100;
    }

    internal class Stock : Persistent
    {
        internal string name;
        internal TimeSeries quotes;
    }

    internal const int nElements = 10000000;
    internal const int pagePoolSize = 32 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Stock stock;
        int i;

        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testts.dbs", pagePoolSize);
        FieldIndex stocks = (FieldIndex) db.GetRoot();
        if (stocks == null)
        {
            stocks = db.CreateFieldIndex(typeof(Stock), "name", true);
            stock = new Stock();
            stock.name = "BORL";
            stock.quotes = db.CreateTimeSeries(typeof(QuoteBlock), (long) QuoteBlock.N_ELEMS_PER_BLOCK * 1000 * 2);
            stocks.Put(stock);
            db.SetRoot(stocks);
        }
        else
        {
            stock = (Stock) stocks.Get("BORL");
        }

        Random rand = new Random((System.Int32) 2004);
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        int time = (int) (start / 1000) - nElements;
        for (i = 0; i < nElements; i++)
        {
            Quote quote = new Quote();
            quote.timestamp = time + i;
            quote.open = (float) rand.Next(10000) / 100;
            quote.close = (float) rand.Next(10000) / 100;
            quote.high = Math.Max(quote.open, quote.close);
            quote.low = Math.Min(quote.open, quote.close);
            quote.volume = rand.Next(1000);
            stock.quotes.Add(quote);
        }

        db.Commit();
        Console.Out.WriteLine("Elapsed time for storing " + nElements + " quotes: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        rand = new Random((Int32) 2004);
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        IEnumerator iterator = stock.quotes.GetEnumerator();
        for (i = 0; iterator.MoveNext(); i++)
        {
            Quote quote = (Quote) iterator.Current;
            Assert.That(quote.timestamp == time + i);
            float open = (float) rand.Next(10000) / 100;
            Assert.That(quote.open == open);
            float close = (float) rand.Next(10000) / 100;
            Assert.That(quote.close == close);
            Assert.That(quote.high == System.Math.Max(quote.open, quote.close));
            Assert.That(quote.low == System.Math.Min(quote.open, quote.close));
            Assert.That(quote.volume == rand.Next(1000));
        }

        Assert.That(i == nElements);
        Console.Out.WriteLine("Elapsed time for extracting " + nElements + " quotes: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        Assert.That(stock.quotes.Size() == nElements);

        long from = (long) (time + 1000) * 1000;
        int count = 1000;
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        iterator = stock.quotes.GetEnumerator(new System.DateTime(from), new System.DateTime(from + count * 1000), false);
        for (i = 0; iterator.MoveNext(); i++)
        {
            Quote quote = (Quote) iterator.Current;
            Assert.That(quote.timestamp == time + 1000 + count - i);
        }

        Assert.That(i == count + 1);
        Console.Out.WriteLine("Elapsed time for extracting " + i + " quotes: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long n = stock.quotes.Remove(stock.quotes.FirstTime, stock.quotes.LastTime);
        Assert.That(n == nElements);
        Console.Out.WriteLine("Elapsed time for removing " + nElements + " quotes: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        Assert.That(stock.quotes.Size() == 0);
        db.Close();
    }
}

