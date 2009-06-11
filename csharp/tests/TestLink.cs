using System;
using TenderBase;

class Detail : Persistent
{
    internal string name;
    internal string color;
    internal double weight;
    internal Link orders;
}

class Supplier : Persistent
{
    internal string name;
    internal string address;
    internal Link orders;
}

class Order : Persistent
{
    internal Detail detail;
    internal Supplier supplier;
    internal int quantity;
    internal long price;
}

class Root : Persistent
{
    internal FieldIndex details;
    internal FieldIndex suppliers;
}

public class TestLink
{
    internal static char[] inputBuffer = new char[256];

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

    internal static int inputInt(string prompt)
    {
        while (true)
        {
            try
            {
                return Convert.ToInt32(input(prompt), 10);
            }
            catch (System.FormatException)
            {
            }
        }
    }

    internal static double inputReal(string prompt)
    {
        while (true)
        {
            try
            {
                return System.Double.Parse(input(prompt));
            }
            catch (System.FormatException)
            {
            }
        }
    }

    [STAThread]
    public static void Main(string[] args)
    {
        string name;
        Supplier supplier;
        Detail detail;
        Supplier[] suppliers;
        Detail[] details;
        Order order;
        Storage db = StorageFactory.Instance.CreateStorage();
        db.Open("testlist.dbs");
        Root root = (Root) db.GetRoot();

        if (root == null)
        {
            root = new Root();
            root.details = db.CreateFieldIndex(typeof(Detail), "name", true);
            root.suppliers = db.CreateFieldIndex(typeof(Supplier), "name", true);
            db.SetRoot(root);
        }

        while (true)
        {
            Console.Out.WriteLine("------------------------------------------");
            Console.Out.WriteLine("1. Add supplier");
            Console.Out.WriteLine("2. Add detail");
            Console.Out.WriteLine("3. Add order");
            Console.Out.WriteLine("4. Search suppliers");
            Console.Out.WriteLine("5. Search details");
            Console.Out.WriteLine("6. Suppliers of detail");
            Console.Out.WriteLine("7. Deails shipped by supplier");
            Console.Out.WriteLine("8. Exit");
            string str = input("> ");
            int cmd;

            try
            {
                cmd = Convert.ToInt32(str, 10);
            }
            catch (System.FormatException)
            {
                Console.Out.WriteLine("Invalid command");
                continue;
            }

            switch (cmd)
            {
                case 1:
                    supplier = new Supplier();
                    supplier.name = input("Supplier name: ");
                    supplier.address = input("Supplier address: ");
                    supplier.orders = db.CreateLink();
                    root.suppliers.Put(supplier);
                    break;

                case 2:
                    detail = new Detail();
                    detail.name = input("Detail name: ");
                    detail.weight = inputReal("Detail weight: ");
                    detail.color = input("Detail color: ");
                    detail.orders = db.CreateLink();
                    root.details.Put(detail);
                    break;

                case 3:
                    order = new Order();
                    name = input("Supplier name: ");
                    order.supplier = (Supplier) root.suppliers.Get(new Key(name));
                    if (order.supplier == null)
                    {
                        Console.Out.WriteLine("No such supplier");
                        continue;
                    }
                    name = input("Detail name: ");
                    order.detail = (Detail) root.details.Get(new Key(name));
                    if (order.detail == null)
                    {
                        Console.Out.WriteLine("No such detail");
                        continue;
                    }
                    order.quantity = inputInt("Quantity: ");
                    order.price = inputInt("Price: ");
                    order.detail.orders.Add(order);
                    order.supplier.orders.Add(order);
                    order.detail.Store();
                    order.supplier.Store();
                    break;

                case 4:
                    name = input("Supplier name prefix: ");
                    suppliers = (Supplier[]) root.suppliers.Get(new Key(name), new Key(name + (char) 255, false));
                    if (suppliers.Length == 0)
                    {
                        Console.Out.WriteLine("No such suppliers found");
                    }
                    else
                    {
                        for (int i = 0; i < suppliers.Length; i++)
                        {
                            Console.Out.WriteLine(suppliers[i].name + '\t' + suppliers[i].address);
                        }
                    }
                    continue;

                case 5:
                    name = input("Detail name prefix: ");
                    details = (Detail[]) root.details.Get(new Key(name), new Key(name + (char) 255, false));
                    if (details.Length == 0)
                    {
                        Console.Out.WriteLine("No such details found");
                    }
                    else
                    {
                        for (int i = 0; i < details.Length; i++)
                        {
                            Console.Out.WriteLine(details[i].name + '\t' + details[i].weight + '\t' + details[i].color);
                        }
                    }
                    continue;

                case 6:
                    name = input("Detail name: ");
                    detail = (Detail) root.details.Get(new Key(name));
                    if (detail == null)
                    {
                        Console.Out.WriteLine("No such detail");
                    }
                    else
                    {
                        for (int i = detail.orders.Size; --i >= 0; )
                        {
                            Console.Out.WriteLine(((Order) detail.orders.Get(i)).supplier.name);
                        }
                    }
                    continue;

                case 7:
                    name = input("Supplier name: ");
                    supplier = (Supplier) root.suppliers.Get(new Key(name));
                    if (supplier == null)
                    {
                        Console.Out.WriteLine("No such supplier");
                    }
                    else
                    {
                        for (int i = supplier.orders.Size; --i >= 0; )
                        {
                            Console.Out.WriteLine(((Order) supplier.orders.Get(i)).detail.name);
                        }
                    }
                    continue;

                case 8:
                    db.Close();
                    Console.Out.WriteLine("End of session");
                    return;

                default:
                    Console.Out.WriteLine("Invalid command");
                    break;

            }
            db.Commit();
        }
    }
}
