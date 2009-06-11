// Supplier - Order - Detail example
// This example illustrates alternative apporach for implementing many-to-many relations
// based on using Projection class. See aslo TestSSD example.
using System;
using TenderBase;

public class TestSOD : Persistent
{
    private class AnonymousClassComparator : System.Collections.IComparer
    {
        public virtual int Compare(System.Object o1, System.Object o2)
        {
            return ((Order) o1).quantity - ((Order) o2).quantity;
        }
    }

    internal class Supplier : Persistent
    {
        internal string name;
        internal string location;
        internal Relation orders;
    }

    internal class Detail : Persistent
    {
        internal string id;
        internal float weight;
        internal Relation orders;
    }

    internal class Order : Persistent
    {
        internal Relation supplier;
        internal Relation detail;
        internal int quantity;
        internal long price;
    }

    internal FieldIndex supplierName;
    internal FieldIndex detailId;

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
                    return answer;
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
        Order order;
        Order[] orders;
        System.Collections.IEnumerator iterator;
        Projection d2o = new Projection(typeof(Detail), "orders");
        Projection s2o = new Projection(typeof(Supplier), "orders");
        int i;

        db.Open("testsod.dbs");

        TestSOD root = (TestSOD) db.GetRoot();
        if (root == null)
        {
            root = new TestSOD();
            root.supplierName = db.CreateFieldIndex(typeof(Supplier), "name", true);
            root.detailId = db.CreateFieldIndex(typeof(Detail), "id", true);
            db.SetRoot(root);
        }
        while (true)
        {
            try
            {
                switch ((int) inputLong("-------------------------------------\n" + "Menu:\n" + "1. Add supplier\n" + "2. Add detail\n" + "3. Add order\n" + "4. List of suppliers\n" + "5. List of details\n" + "6. Suppliers of detail\n" + "7. Details shipped by supplier\n" + "8. Orders for detail of supplier\n" + "9. Exit\n\n>>"))
                {

                    case 1:
                        supplier = new Supplier();
                        supplier.name = input("Supplier name: ");
                        supplier.location = input("Supplier location: ");
                        supplier.orders = db.CreateRelation(supplier);
                        root.supplierName.Put(supplier);
                        db.Commit();
                        continue;

                    case 2:
                        detail = new Detail();
                        detail.id = input("Detail id: ");
                        //UPGRADE_WARNING: Data types in Visual C# might be different. Verify the accuracy of narrowing conversions.  
                        detail.weight = (float) inputDouble("Detail weight: ");
                        detail.orders = db.CreateRelation(detail);
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
                        order = new Order();
                        order.quantity = (int) inputLong("Order quantity: ");
                        order.price = inputLong("Order price: ");
                        order.detail = detail.orders;
                        order.supplier = supplier.orders;
                        detail.orders.Add(order);
                        supplier.orders.Add(order);
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
                        iterator = detail.orders.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            order = (Order) iterator.Current;
                            supplier = (Supplier) order.supplier.Owner;
                            Console.Out.WriteLine("Suppplier name: " + supplier.name);
                        }
                        break;

                    case 7:
                        supplier = (Supplier) root.supplierName.Get(new Key(input("Supplier name: ")));
                        if (supplier == null)
                        {
                            Console.Error.WriteLine("No such supplier!");
                            break;
                        }
                        iterator = supplier.orders.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            order = (Order) iterator.Current;
                            detail = (Detail) order.detail.Owner;
                            Console.Out.WriteLine("Detail ID: " + detail.id);
                        }
                        break;

                    case 8:
                        d2o.Reset();
                        d2o.Project(root.detailId.GetPrefix(input("Detail ID prefix: ")));
                        s2o.Reset();
                        s2o.Project(root.supplierName.GetPrefix(input("Supplier name prefix: ")));
                        s2o.Join(d2o);
                        orders = (Order[]) s2o.ToArray(new Order[s2o.Size()]);
                        System.Array.Sort(orders, new AnonymousClassComparator());
                        for (i = 0; i < orders.Length; i++)
                        {
                            order = orders[i];
                            supplier = (Supplier) order.supplier.Owner;
                            detail = (Detail) order.detail.Owner;
                            Console.Out.WriteLine("Detail ID: " + detail.id + ", supplier name: " + supplier.name + ", quantity: " + order.quantity);
                        }
                        break;

                    case 9:
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

