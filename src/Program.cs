namespace Pilot;

using System.Globalization;
using System.Text;

public class Program
{
    

    public static void Main(string[] args)
    {
        
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Layout.Init();

        Pilot pilot = new();

        pilot.Start();

    }
}