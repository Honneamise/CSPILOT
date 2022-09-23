namespace Pilot;

public class Program
{
    public static void Main(string[] args)
    {
        Interpreter intp = new("res/test.pil");

        intp.Execute();
    }
}