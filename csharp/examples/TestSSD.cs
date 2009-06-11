// Supplier - Shipment - Detail example
using System;
using TenderBase;

public class TestSSD : Persistent
{
    internal class Supplier : Persistent
    {
        internal string name;
        internal string location;
    }

    internal class Detail : Persistent
    {
        internal string id;
        internal float weight;
    }

    internal class Shipment : Persistent
    {
        internal Supplier supplier;
        internal Detail detail;
        internal int quantity;
        internal long price;
    }

    internal FieldIndex supplierName;
    internal FieldIndex detailId;
    internal FieldIndex shipmentSupplier;
    internal FieldIndex shipmentDetail;

    internal static char[] inputBuffer = new char[256];

    internal static void skip(string prompt)
    {
        try
        {
            Console.Out.Write(prompt);
            Console.In.Read(inputBuffer, 0, inputBuffer.Length);
        }
        catch (System.IO.IOException)
        {
        }
    }

    internal static string input(string prompt)
    {
        while (true)
        {
            try
            {
                Console.Out.Write(prompt);
                int len = Console.In.Read(inputBuffer, 0, inputBuffer.Length);
                string answer = new string(inputBuffer, 0, len).Trim();
                if (answer.Length != 0)
                {
                    return answer;
                }
            }
            catch (System.IO.IOException)
            {
            }
        }
    }

    internal static long inputLong(string prompt)
    {
        while (true)
        {
            try
            {
                return Convert.ToInt64(input(prompt), 10);
            }
            catch (System.FormatException)
            {
                Console.Error.WriteLine("Invalid integer constant");
            }
        }
    }

    internal static double inputDouble(string prompt)
    {
        while (true)
        {
            try
            {
                return System.Double.Parse(input(prompt));
            }
            catch (System.FormatException)
            {
                Console.Error.WriteLine("Invalid floating point constant");
            }
        }
    }

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();
        Supplier supplier;
        Detail detail;
        Shipment shipment;
        Shipment[] shipments;
        System.Collections.IEnumerator iterator;
        int i;

        db.Open("testssd.dbs");

        TestSSD root = (TestSSD) db.GetRoot();
        if (root == null)
        {
            root = new TestSSD();
            root.supplierName = db.CreateFieldIndex(typeof(Supplier), "name", true);
            root.detailId = db.CreateFieldIndex(typeof(Detail), "id", true);
            root.shipmentSupplier = db.CreateFieldIndex(typeof(Shipment), "supplier", false);
            root.shipmentDetail = db.CreateFieldIndex(typeof(Shipment), "detail", false);
            db.SetRoot(root);
        }
        while (true)
        {
            try
            {
                switch ((int) inputLong("-------------------------------------\n" + "Menu:\n" + "1. Add supplier\n" + "2. Add detail\n" + "3. Add shipment\n" + "4. List of suppliers\n" + "5. List of details\n" + "6. Suppliers of detail\n" + "7. Details shipped by supplier\n" + "8. Exit\n\n>>"))
                {

                    case 1:
                        supplier = new Supplier();
                        supplier.name = input("Supplier name: ");
                        supplier.location = input("Supplier location: ");
                        root.supplierName.Put(supplier);
                        db.Commit();
                        continue;

                    case 2:
                        detail = new Detail();
                        detail.id = input("Detail id: ");
                        detail.weight = (float) inputDouble("Detail weight: ");
                        root.detailId.Put(detail);
                        db.Commit();
                        continue;

                    case 3:
                        supplier = (Supplier) root.supplierName.Get(new Key(input("Supplier name: ")));
                        if (supplier == null)
                        {
                            Console.Error.WriteLine("No such supplier!");
                            break;
                        }
                        detail = (Detail) root.detailId.Get(new Key(input("Detail ID: ")));
                        if (detail == null)
                        {
                            Console.Error.WriteLine("No such detail!");
                            break;
                        }
                        shipment = new Shipment();
                        shipment.quantity = (int) inputLong("Shipment quantity: ");
                        shipment.price = inputLong("Shipment price: ");
                        shipment.detail = detail;
                        shipment.supplier = supplier;
                        root.shipmentSupplier.Put(shipment);
                        root.shipmentDetail.Put(shipment);
                        db.Commit();
                        continue;

                    case 4:
                        iterator = root.supplierName.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            supplier = (Supplier) iterator.Current;
                            Console.Out.WriteLine("Supplier name: " + supplier.name + ", supplier.location: " + supplier.location);
                        }
                        break;

                    case 5:
                        iterator = root.detailId.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            detail = (Detail) iterator.Current;
                            Console.Out.WriteLine("Detail ID: " + detail.id + ", detail.weight: " + detail.weight);
                        }
                        break;

                    case 6:
                        detail = (Detail) root.detailId.Get(new Key(input("Detail ID: ")));
                        if (detail == null)
                        {
                            Console.Error.WriteLine("No such detail!");
                            break;
                        }
                        shipments = (Shipment[]) root.shipmentDetail.Get(new Key(detail), new Key(detail));
                        for (i = 0; i < shipments.Length; i++)
                        {
                            Console.Out.WriteLine("Suppplier name: " + shipments[i].supplier.name);
                        }
                        break;

                    case 7:
                        supplier = (Supplier) root.supplierName.Get(new Key(input("Supplier name: ")));
                        if (supplier == null)
                        {
                            Console.Error.WriteLine("No such supplier!");
                            break;
                        }
                        shipments = (Shipment[]) root.shipmentSupplier.Get(new Key(supplier), new Key(supplier));
                        for (i = 0; i < shipments.Length; i++)
                        {
                            Console.Out.WriteLine("Detail ID: " + shipments[i].detail.id);
                        }
                        break;

                    case 8:
                        db.Close();
                        return;
                    }
                skip("Press ENTER to continue...");
            }
            catch (StorageError x)
            {
                Console.Out.WriteLine("Error: " + x.Message);
                skip("Press ENTER to continue...");
            }
        }
    }
}

