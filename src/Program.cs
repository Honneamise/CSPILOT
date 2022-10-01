namespace Pilot;

using Expression;
using System.ComponentModel;
using System.Globalization;

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Interpreter intp = new();

        intp.Start();

    }
}