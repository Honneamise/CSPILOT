﻿namespace Pilot;

public class Program
{
    public static void Main(string[] args)
    {
        Interpreter intp = new("res/simplest2.pil");

        intp.Execute();
    }
}