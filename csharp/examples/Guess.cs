using System;
using TenderBase;

public class Guess:Persistent
{
    public Guess yes;
    public Guess no;
    public string question;

    internal static char[] inputBuffer = new char[256];

    public Guess(Guess no, string question, Guess yes)
    {
        this.yes = yes;
        this.question = question;
        this.no = no;
    }

    internal Guess()
    {
    }

    internal static string input(string prompt)
    {
        while (true)
        {
            try
            {
                Console.Out.Write(prompt);
                int len = System.Console.In.Read(inputBuffer, 0, inputBuffer.Length);
                string answer = new string(inputBuffer, 0, len).Trim();
                if (answer.Length != 0)
                    return answer;
            }
            catch (System.IO.IOException)
            {
            }
        }
    }

    internal static bool askQuestion(string question)
    {
        string answer = input(question);
        return answer.ToUpper().Equals("y".ToUpper()) || answer.ToUpper().Equals("yes".ToUpper());
    }

    internal static Guess whoIsIt(Guess parent)
    {
        string animal = input("What is it ? ");
        string difference = input("What is a difference from other ? ");
        return new Guess(parent, difference, new Guess(null, animal, null));
    }

    internal virtual Guess dialog()
    {
        if (askQuestion("May be, " + question + " (y/n) ? "))
        {
            if (yes == null)
            {
                Console.Out.WriteLine("It was very simple question for me...");
            }
            else
            {
                Guess clarify = yes.dialog();
                if (clarify != null)
                {
                    yes = clarify;
                    Store();
                }
            }
        }
        else
        {
            if (no == null)
            {
                if (yes == null)
                {
                    return whoIsIt(this);
                }
                else
                {
                    no = whoIsIt(null);
                    Store();
                }
            }
            else
            {
                Guess clarify = no.dialog();
                if (clarify != null)
                {
                    no = clarify;
                    Store();
                }
            }
        }
        return null;
    }

    [STAThread]
    static public void Main(string[] args)
    {
        Storage db = StorageFactory.Instance.CreateStorage();

        db.Open("guess.dbs");
        Guess root = (Guess) db.GetRoot();

        while (askQuestion("Think of an animal. Ready (y/n) ? "))
        {
            if (root == null)
            {
                root = whoIsIt(null);
                db.SetRoot(root);
            }
            else
            {
                root.dialog();
            }
            db.Commit();
        }

        Console.Out.WriteLine("End of the game");
        db.Close();
    }
}
