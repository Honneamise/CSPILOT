namespace Pilot;

using System.Globalization;
using System.Text;

public class Program
{

    static string MegaReadLine()
    {
        StringBuilder buf = new();

        while(true)
        {
            var key = Console.ReadKey(true);
            
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buf.ToString();
            }

            else if (key.Key == ConsoleKey.Escape)
            {
                Console.WriteLine("interrotto da esc");
            }

            else if (key.Key == ConsoleKey.Backspace && buf.Length > 0)
            {
                buf.Remove(buf.Length - 1, 1);
                Console.Write("\b \b");
            }

            else if (key.KeyChar != 0)
            {
                buf.Append(key.KeyChar);
                Console.Write(key.KeyChar);
            }

            
        }
    }

    public static void Main(string[] args)
    {
        while (true)
        {
            MegaReadLine();
        }

        /*
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Layout.Init();

        Pilot pilot = new();

        pilot.Start();*/

    }
}