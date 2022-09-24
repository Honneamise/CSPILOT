using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Pilot;

/**********/
public class Instruction
{
    public string? label;
    public string? type;
    public bool?   cond;
    public string? body;

    public Instruction()
    {
        label = null;
        type  = null;
        cond  = null;
        body  = null;
    }

    public override string ToString()
    {
        string str = "\n**********";

        str += "\nLabel:";
        str += label ?? "null";

        str += "\nType:";
        str += type ?? "null";

        str += "\nCond:";
        str += (cond==null) ? "null" : cond;

        str += "\nBody:";
        str += body ?? "null";

        return str;
    }
}

/**********/
public class Interpreter
{
    const int STACK_SIZE = 512;

    bool run;

    int pc;
    List<Instruction?> instructions;
    Dictionary<string,int> labels;
    
    Dictionary<string, int> num_vars;
    Dictionary<string, List<string>> str_vars;

    Stack<int> routines;

    string accept;
    bool match;

    /***********************/
    /* INTERPRETER SECTION */
    /***********************/
    public Interpreter(string file)
    {
        run = false;
        pc = 0;

        instructions = new List<Instruction?>();

        labels = new Dictionary<string, int>();
        num_vars = new Dictionary<string, int>();
        str_vars = new Dictionary<string, List<string>>();   

        routines = new Stack<int>();

        accept = "";
        match = false;

        string[] lines = File.ReadAllLines(file);      

        foreach (string line in lines)
        {
            Instruction? ins = ParseInstruction(line);

            instructions.Add(ins);

            if(ins != null && ins.label != null)
            {
                if(labels.ContainsKey(ins.label)) { Error("Duplicate label entry for : " + ins.label); }
                else { labels[ins.label] = pc; }
            }

            pc++;
        }

        pc = 0;

    }

    public void Execute()
    {
        var restore_fgcolor = Console.ForegroundColor;
        var restore_bgcolor = Console.BackgroundColor;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.BackgroundColor = ConsoleColor.Black;

        run = true;

        while (run)
        {
            if(pc<0 || pc>=instructions.Count ) { Error("Program counter out of range"); }

            Instruction? ins = instructions[pc];

            ExecuteInstruction(ins);
        }

        Console.ForegroundColor = restore_fgcolor;
        Console.BackgroundColor = restore_bgcolor;
    }

    void Error(string str)
    {
        Console.WriteLine(pc + ":" + str);

        Environment.Exit(-1);
    }

    Instruction? ParseInstruction(string str)
    {
        if (String.IsNullOrEmpty(str)) { return null; };

        Instruction ins = new();

        str = str.TrimStart();

        if (String.IsNullOrEmpty(str)) { return null; };

        //we have label
        if (str[0] == '*')
        {
            //only label in the line
            if (str.IndexOf(' ')==-1)
            {
                ins.label = str;
                return ins;
            }
            else //extract label
            {
                ins.label = str[0..str.IndexOf(' ')];

                str = str[str.IndexOf(' ')..];

                str = str.TrimStart();

                if (String.IsNullOrEmpty(str)) { return ins;  }
            }
        }

        //get type
        if (!str.Contains(':')) { Error("Missing instruction separator");  }

        ins.type = str[0..str.IndexOf(':')];
        ins.type = ins.type.TrimEnd();

        if (ins.type[^1] == 'Y') { ins.cond = true;  ins.type = ins.type[..^1]; }
        if (ins.type[^1] == 'N') { ins.cond = false; ins.type = ins.type[..^1]; }

        string body = str[(str.IndexOf(':') + 1)..];

        if (String.IsNullOrEmpty(body)) { ins.body = null; }
        else { ins.body = body; };

        return ins;
    }


    void ExecuteInstruction(Instruction? ins)
    {
        
        if (ins == null || ins.type == null) { pc++; return; }

        if (ins.cond != null && ins.cond != match) { pc++; return; }

        switch (ins.type)
        {
            case "A":
                Execute_A(ins);
                break;

            case "BELL":
                Execute_BELL(ins);
                break;

            case "C":
                Execute_C(ins);
                break;

            case "CASE":
                Execute_CASE(ins);
                break;

            case "CH":
                Execute_CH(ins);
                break;

            case "CLRS":
                Execute_CLRS(ins);
                break;

            case "CPM":
                Execute_CPM(ins);
                break;

            case "CUR":
                Execute_CUR(ins);
                break;

            case "DEF":
                Execute_DEF(ins);
                break;

            case "DI":
                Execute_DI(ins);
                break;

            case "E":
                Execute_E(ins);
                break;

            case "EI":
                Execute_EI(ins);
                break;

            case "END":
                Execute_END(ins);
                break;

            case "ERASTR":
                Execute_ERASTR(ins);
                break;

            case "ESC":
                Execute_ESC(ins);
                break;

            case "EXIST":
                Execute_EXIST(ins);
                break;

            case "HOLD":
                Execute_HOLD(ins);
                break;

            case "INMAX":
                Execute_INMAX(ins);
                break;

            case "J":
                Execute_J(ins);
                break;

            case "LF":
                Execute_LF(ins);
                break;

            case "M":
                Execute_M(ins);
                break;

            case "MC":
                Execute_MC(ins);
                break;

            case "OUT":
                Execute_OUT(ins);
                break;

            case "PR":
                Execute_PR(ins);
                break;

            case "R":
                Execute_R(ins);
                break;

            case "RESET":
                Execute_RESET(ins);
                break;

            case "SAVE":
                Execute_SAVE(ins);
                break;

            case "T":
                Execute_T(ins);
                break;

            case "TNR":
                Execute_TNR(ins);
                break;

            case "U":
                Execute_U(ins);
                break;

            case "WAIT":
                Execute_WAIT(ins);
                break;

            default:
                Error("Unknow instruction : " + ins.type);
                break;
        }
    }

    /*********************/
    /* FUNCTIONS SECTION */
    /*********************/
    void Execute_A(Instruction ins)
    {
        //simple accepts, no body parameters
        if (ins.body == null || String.IsNullOrEmpty(ins.body.Trim()) )
        {
            string? input = Console.ReadLine()?.Trim();

            accept = input ?? "";
        }
        else//body have parameters
        {
            string var_name = ins.body.Trim();
            if(var_name.Contains(' ') || var_name.Length<2)
            { 
                Error("Invalid var name : " + var_name);
                return;
            }

            if (var_name[0]=='#')//it is a numeric variable
            {
                int num;

                while(true)//repeat until valid user input
                {
                    string? input = Console.ReadLine()?.Trim();

                    if(!String.IsNullOrEmpty(input) && int.TryParse(input, out int _num))
                    {
                        num = _num;
                        break;
                    }

                    Console.WriteLine("WARNING : invalid input");
                }
                
                num_vars[var_name] = num;
            }
            else if (var_name[0] == '$')//it is a string variable
            {
                string? input = Console.ReadLine()?.Trim();

                str_vars[var_name] = new List<string> { input ?? "" };
            }
            else//error uknow variable type
            {
                Error("Invalid variable type : " + var_name);
            }
        }

        pc++;
    }

    void Execute_BELL(Instruction ins)
    {
        Console.Beep();
        pc++;
    }

    void Execute_C(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_CASE(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_CH(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_CLRS(Instruction ins)//da testare
    {
        Console.Clear();
        pc++;
    }

    void Execute_CPM(Instruction ins)//sostituire con execl ?
    {
        Error("Instruction not available : " + ins.type);
    }

    void Execute_CUR(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_DEF(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_DI(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_E(Instruction ins)
    {
        if (routines.Count <= 0) { Error("Routine Stack Underflow"); return; }

        pc = routines.Pop();

        pc++;
    }

    void Execute_EI(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_END(Instruction ins)
    {
        run = false;
    }

    void Execute_ERASTR(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_ESC(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_EXIST(Instruction ins)//vedi CPM
    {
        Error("Instruction not available : " + ins.type);
    }

    void Execute_HOLD(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_INMAX(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_J(Instruction ins)
    {
        if (ins.body != null)
        {
            string label = ins.body.Trim();

            if (labels.ContainsKey(label)) { pc = labels[label]; }
            else { Error("Label not found : " + label); }
        }
        else
        {
            Error("Misssing label");
        }
    }

    void Execute_LF(Instruction ins)
    {
        if(ins.body == null || String.IsNullOrEmpty(ins.body.Trim()) ) { Error("Missing parameter"); return; }

        string num_str = ins.body.Trim();

        if (!uint.TryParse(num_str, out uint num) || num<0) { Error("Invalid parameter : " + num_str); return; }

        for (int i = 0; i<num; i++)
        {
            Console.WriteLine();
        }

        pc++;
    }

    void Execute_M(Instruction ins)
    {
        match = false;

        if (ins.body == null) { Error("Missing match parameter"); return; }
       
        string[] tokens = ins.body.Split(',');

        foreach (string token in tokens)
        {
            if (token.Length > 1)
            {
                //leading and trailing spaces
                if (token[0] == ' ' && token[^1] == ' ')
                {
                    match = accept.ToUpper().Equals(token.Trim().ToUpper());
                }

                //only leading spaces
                else if (token[0] == ' ')
                {
                    match = accept.ToUpper().StartsWith(token.TrimStart().ToUpper());
                }

                //only trailing spaces
                else if (token[^1] == ' ')
                {
                    match = accept.ToUpper().EndsWith(token.TrimEnd().ToUpper());
                }

                //no spaces at all
                else
                {
                    match = accept.ToUpper().Contains(token.ToUpper());
                }
            }
            else//string is a single character
            {
                match = accept.ToUpper().Contains(token.ToUpper());
            }

            //a match has been found
            if(match==true) { break; }
        }

        pc++;

    }

    void Execute_MC(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_OUT(Instruction ins)
    {
        Error("Instruction not available : " + ins.type);
    }

    void Execute_PR(Instruction ins)
    {
        Error("Instruction not available : " + ins.type);
    }

    void Execute_R(Instruction ins)
    {
        pc++;
    }

    void Execute_RESET(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_SAVE(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_T(Instruction ins)
    {
        if(String.IsNullOrEmpty(ins.body)) { pc++; return; }

        string[] num_tokens = ins.body.Split('#');
        string[] str_tokens = ins.body.Split('$');

        Console.WriteLine(ins.body);

        pc++;
    }

    void Execute_TNR(Instruction ins)
    {
        Console.Write(ins.body);
        pc++;
    }

    void Execute_U(Instruction ins)
    {
        if(routines.Count>=STACK_SIZE) { Error("Routine Stack overflow"); return; }

        if(ins.body==null) { Error("Missing label"); return; }

        string label = ins.body.Trim();
        
        if(!labels.ContainsKey(label)) { Error("Label not found"); return; }

        routines.Push(pc);

        pc = labels[label];
    }

    void Execute_WAIT(Instruction ins)//vedi note
    {
        Error("Instruction not implemented : " + ins.type);
    }
}