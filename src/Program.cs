namespace Pilot;

public class Program
{
    public static void Main(string[] args)
    {
        Interpreter intp = new("res/simplest3.pil");

        intp.Execute();

        intp.DumpVars();
    }
}