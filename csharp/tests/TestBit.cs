using System;
using System.Collections;
using TenderBase;

class Car : Persistent
{
    //internal int hps;
    //internal int maxSpeed;
    //internal int timeTo100;
    internal int options;
    internal string model;
    //internal string vendor;
    //internal string specification;

    internal const int CLASS_A = 0x00000001;
    internal const int CLASS_B = 0x00000002;
    internal const int CLASS_C = 0x00000004;
    internal const int CLASS_D = 0x00000008;

    internal const int UNIVERAL = 0x00000010;
    internal const int SEDAN = 0x00000020;
    internal const int HATCHBACK = 0x00000040;
    internal const int MINIWAN = 0x00000080;

    internal const int AIR_COND = 0x00000100;
    internal const int CLIMANT_CONTROL = 0x00000200;
    internal const int SEAT_HEATING = 0x00000400;
    internal const int MIRROR_HEATING = 0x00000800;

    internal const int ABS = 0x00001000;
    internal const int ESP = 0x00002000;
    internal const int EBD = 0x00004000;
    internal const int TC = 0x00008000;

    internal const int FWD = 0x00010000;
    internal const int REAR_DRIVE = 0x00020000;
    internal const int FRONT_DRIVE = 0x00040000;

    internal const int GPS_NAVIGATION = 0x00100000;
    internal const int CD_RADIO = 0x00200000;
    internal const int CASSETTE_RADIO = 0x00400000;
    internal const int LEATHER = 0x00800000;

    internal const int XEON_LIGHTS = 0x01000000;
    internal const int LOW_PROFILE_TIRES = 0x02000000;
    internal const int AUTOMATIC = 0x04000000;

    internal const int DISEL = 0x10000000;
    internal const int TURBO = 0x20000000;
    internal const int GASOLINE = 0x40000000;
}

class Catalogue : Persistent
{
    internal FieldIndex modelIndex;
    internal BitIndex optionIndex;
}

public class TestBit
{
    internal const int nRecords = 1000000;
    internal const int pagePoolSize = 48 * 1024 * 1024;

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testbit.dbs", pagePoolSize);

        Catalogue root = (Catalogue) db.GetRoot();
        if (root == null)
        {
            root = new Catalogue();
            root.optionIndex = db.CreateBitIndex();
            root.modelIndex = db.CreateFieldIndex(typeof(Car), "model", true);
            db.SetRoot(root);
        }
        BitIndex index = root.optionIndex;
        long start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        long rnd = 1999;
        int i, n;

        int selectedOptions = Car.TURBO | Car.DISEL | Car.FWD | Car.ABS | Car.EBD | Car.ESP | Car.AIR_COND | Car.HATCHBACK | Car.CLASS_C;
        int unselectedOptions = Car.AUTOMATIC;

        for (i = 0, n = 0; i < nRecords; i++)
        {
            rnd = (3141592621L * rnd + 2718281829L) % 1000000007L;
            int options = (int) rnd;
            Car car = new Car();
            car.model = Convert.ToString(rnd);
            car.options = options;
            root.modelIndex.Put(car);
            root.optionIndex.Put(car, options);
            if ((options & selectedOptions) == selectedOptions && (options & unselectedOptions) == 0)
            {
                n += 1;
            }
        }
        Console.Out.WriteLine("Elapsed time for inserting " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        IEnumerator iterator = root.optionIndex.GetEnumerator(selectedOptions, unselectedOptions);
        for (i = 0; iterator.MoveNext(); i++)
        {
            Car car = (Car) iterator.Current;
            Assert.That((car.options & selectedOptions) == selectedOptions);
            Assert.That((car.options & unselectedOptions) == 0);
        }

        Console.Out.WriteLine("Number of selected cars: " + i);
        Assert.That(i == n);
        Console.Out.WriteLine("Elapsed time for bit search through " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");
        start = (DateTime.Now.Ticks - 621355968000000000) / 10000;
        foreach (var o in root.modelIndex)
        {
            Car car = (Car) o;
            root.optionIndex.Remove(car);
            car.Deallocate();
        }
        root.optionIndex.Clear();
        Console.Out.WriteLine("Elapsed time for removing " + nRecords + " records: " + ((DateTime.Now.Ticks - 621355968000000000) / 10000 - start) + " milliseconds");

        db.Close();
    }
}
