namespace Pilot;

using Expression;
using System.Data;
using System.Security.AccessControl;

/**********/
public class Instruction
{
    public string raw;
    public string? label;
    public string? type;
    public bool?   cond;
    public string  body;

    public Instruction()
    {
        raw = "";

        label = null;
        type  = null;
        cond  = null;
        body  = "";
    }

    public override string ToString()//REMOVE AFTER DEBUG
    {
        string str = "\n**********";

        str += "\nLabel:";
        str += label ?? "null";

        str += "\nType:";
        str += type ?? "null";

        str += "\nCond:";
        str += (cond==null) ? "null" : cond;

        str += "\nBody:";

        return str;
    }
}

/**********/
public class Interpreter
{
    const int STACK_SIZE = 512;

    bool run;

    int pc;
    List<Instruction> instructions;
    Dictionary<string,int> labels;
    
    Dictionary<string, float> num_vars;
    Dictionary<string, string> str_vars;

    Stack<int> routines;

    string accept;
    bool match;

    string? error;

    /***********************/
    /* INTERPRETER SECTION */
    /***********************/
    public Interpreter()
    {
        run = false;
        pc = 0;

        instructions = new List<Instruction>();

        labels = new Dictionary<string, int>();
        num_vars = new Dictionary<string, float>();
        str_vars = new Dictionary<string, string>();

        routines = new Stack<int>();

        accept = "";
        match = false;

        error = null;
    }

    public void Reset()
    {
        run = false;
        pc = 0;

        instructions = new List<Instruction>();

        labels = new Dictionary<string, int>();
        num_vars = new Dictionary<string, float>();
        str_vars = new Dictionary<string, string>();

        routines = new Stack<int>();

        accept = "";
        match = false;

        error= null;
    }

    public void Start()
    {
        var restore_fgcolor = Console.ForegroundColor;
        var restore_bgcolor = Console.BackgroundColor;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.BackgroundColor = ConsoleColor.Black;

        Console.WriteLine("************************************");
        Console.WriteLine("* PILOT INTERPRETER                *");
        Console.WriteLine("*                                  *");
        Console.WriteLine("* COMMANDS : LOAD, LIST, RUN, EXIT *");
        Console.WriteLine("************************************");

        string input;

        while(true)
        {
            Console.Write(":>");

            input = Console.ReadLine() ?? "";

            if (String.IsNullOrEmpty(input.Trim())) { continue; }

            if (input.StartsWith("LOAD"))
            {
                string file_name = input.Replace("LOAD", "").Trim();

                if (String.IsNullOrEmpty(file_name)) { Console.WriteLine("Missing or invalid file name"); continue; }

                if(!File.Exists(file_name)) { Console.WriteLine("File does not exist : " + file_name); continue;  }
                
                Load(file_name);

                if (error != null) { Console.WriteLine(error); }
            }
            else if (input.StartsWith("RUN"))
            {
                Run();

                if (error != null) { Console.WriteLine(error); }
            }
            else if (input.StartsWith("LIST"))
            {
                List();
            }
            else if (input.StartsWith("CLEAR"))
            {
                Reset();
            }
            else if (input.StartsWith("EXIT"))
            {
                break;
            }
            else
            {
                Console.WriteLine("Unknow command : " + input);
            }

        }

        Console.ForegroundColor = restore_fgcolor;
        Console.BackgroundColor = restore_bgcolor;
    }

    //load intp with file and set instructions
    public void Load(string file)
    {
        Reset();

        string[] lines = File.ReadAllLines(file);      

        foreach (string line in lines)
        {
            if(error!=null) { return; }//set error and exit

            Instruction ins = ParseInstruction(line);

            instructions.Add(ins);

            if(ins.label != null)
            {
                if(labels.ContainsKey(ins.label)) { Error("Duplicate label entry for : " + ins.label); }
                else { labels[ins.label] = pc; }
            }

            pc++;
        }

        pc = 0;

    }

    //list current instructions loaded
    public void List()
    {
        foreach(Instruction ins in instructions)
        {
            Console.WriteLine(ins.raw);
        }
    }

    //run the current loaded instructions
    //if something weird happen error will be set 
    public void Run()
    {
        pc = 0;
        run = true;
        error = null;

        while (run && error==null)
        {
            if (pc < 0 || pc >= instructions.Count) { Error("Program counter out of range"); return; }

            Instruction ins = instructions[pc];

            ExecuteInstruction(ins);
        }
    }

    void ExecuteInstruction(Instruction ins)
    {
        if (ins.type == null) { pc++; return; }

        if (ins.cond != null && ins.cond != match) { pc++; return; }

        switch (ins.type.ToUpper())
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


    /*****************/
    /* UTILS SECTION */
    /*****************/

    public void DumpVars()//REMOVE ME AFTER DEBUG
    {
        Console.WriteLine("**********");

        foreach (KeyValuePair<string, float> pair in num_vars)
        {

            Console.WriteLine(pair.Key + " : " + pair.Value);
        }

        foreach (KeyValuePair<string, string> pair in str_vars)
        {
            Console.WriteLine(pair.Key + " : " + pair.Value);
        }
    }

    Instruction ParseInstruction(string str)
    {
        Instruction ins = new();
        ins.raw = str;

        str = str.TrimStart();

        if (String.IsNullOrEmpty(str)) { return ins; };

        //we have label
        if (str[0] == '*')
        {
            ins.label = "*";
            int i = 1;
            while (i < str.Length && (Char.IsLetter(str[i]) || Char.IsNumber(str[i])))
            {
                ins.label += str[i];
                i++;
            }

            if (i == 1) { Error("Missing label name"); }

            if (i == str.Length) { return ins; }; //only label in the line

            if (!Char.IsWhiteSpace(str[i])) { Error("Invalid label format"); }

            str = str[i..].TrimStart();

            if (String.IsNullOrEmpty(str)) { return ins; };//only label followed by shit
        }

        //get type 
        if (!str.Contains(':')) { Error("Missing instruction separator"); }

        ins.type = str[..str.IndexOf(':')];
        ins.type = ins.type.TrimEnd();

        if (String.IsNullOrEmpty(ins.type)) { Error("Missing instruction type"); }

        //we have instruction condition
        if (ins.type.Length > 1)
        {
            if (ins.type[^1] == 'Y') { ins.cond = true; ins.type = ins.type[..^1]; }
            if (ins.type[^1] == 'N') { ins.cond = false; ins.type = ins.type[..^1]; }
        }

        //get body 
        str = str[str.IndexOf(':')..];

        if (str.Length == 1) { return ins; };//empty body

        ins.body = str[1..];

        return ins;
    }

    public string FormatString(string str)
    {
        int index = 0;
        string s = "";

        // EXAMPLES :
        // T: ## --> #
        // T: #NUMBER --> stampa il valore della variabile NUMBER
        // T: ###NUMBER --> #123
        // T: ##NUMBER --> #NUMBER
        // T: ### --> # + ERRORE
        while (index < str.Length)
        {
            if ((index + 1 < str.Length) && (str[index] == '#' || str[index] == '$'))
            {
                if (str[index] == str[index + 1])//double ##
                {
                    s += str[index];//symbol
                    index += 2;
                    continue;
                }
                else// single # it is a variable
                {
                    int end = index;
                    while (end < str.Length && !Char.IsWhiteSpace(str[end]))
                    { end++; }

                    string label = str[index..end];

                    if (label.Length == 1) { Error("Missing label"); }

                    if (num_vars.ContainsKey(label))//is numeric ?
                    {
                        s += num_vars[label];
                    }
                    else if (str_vars.ContainsKey(label))//is string ?
                    {
                        s += str_vars[label];
                    }
                    else
                    {
                        Error("Label not found : " + label);//is an error :-)
                    }

                    index = end;
                    continue;
                }

            }

            //identifier at last position
            if (index == str.Length - 1 && (str[index] == '#' || str[index] == '$'))
            {
                Error("Missing label");
            }

            s += str[index];
            index++;
        }

        return s;
    }

    void Error(string str)
    {
        if(error==null) { error = pc + 1 + ":" + str; }
    }


    /*********************/
    /* FUNCTIONS SECTION */
    /*********************/
    void Execute_A(Instruction ins)
    {
        string param = ins.body.Trim();

        //simple accepts, no body parameters
        if (String.IsNullOrEmpty(param))
        {
            string? input = Console.ReadLine()?.Trim();

            accept = input ?? "";
        }
        else//body have parameters
        {
            if (param[0]=='#' && param.Length>1)//it is a numeric variable
            {
                float num;

                while(true)//repeat until valid user input
                {
                    string? input = Console.ReadLine()?.Trim();

                    if(!String.IsNullOrEmpty(input) && float.TryParse(input, out float _num))
                    {
                        num = _num;
                        break;
                    }

                    Console.WriteLine("WARNING : invalid input");
                }
                
                num_vars[param] = num;
            }
            else if (param[0] == '$' && param.Length > 1)//it is a string variable
            {
                string? input = Console.ReadLine()?.Trim();

                str_vars[param] = input ?? "";

            }
            else//error uknow variable type
            {
                Error("Invalid parameter : " + param);
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
        string str = ins.body.Trim();

        if (String.IsNullOrEmpty(str)) { Error("Missing compute statement"); }

        //get assign variable
        int end = str.IndexOf('=');

        if (str[0]!='#' || end==-1 || end==str.Length-1) { Error("Invalid compute statement"); }

        string var_name = str[..end].Trim();

        //get infix expression
        string infix = str[(end+1)..];
        if (String.IsNullOrEmpty(infix)) { Error("Invalid compute statement"); }

        //infix to postfix
        string postfix = Expression.InfixToPostfix(infix);

        //substitute variables
        string[] tokens = postfix.Split(' ');
        postfix = "";
        foreach(String token in tokens)
        {
            if(token.Length>1 && token[0]=='#')
            {
                if (!num_vars.ContainsKey(token)) { Error("Variable not found : " + token); }
                else { postfix += num_vars[token]; }
            }
            else
            {
                postfix += token;
            }

            postfix += ' ';
        }

        //evaluate expression
        float? f = Expression.Evaluate(postfix);

        if(f==null) { Error("Expression error"); return; }//should return

        num_vars[var_name] = (float)f;

        pc++;
    }

    void Execute_CASE(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_CH(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }

    void Execute_CLRS(Instruction ins)
    {
        Console.Clear();
        pc++;
    }

    void Execute_CPM(Instruction ins)
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
        if (routines.Count <= 0) { Error("Routine Stack Underflow"); }

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

    void Execute_EXIST(Instruction ins)
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
        string label = ins.body.Trim();

        if (String.IsNullOrEmpty(label)) { Error("Missing label"); }

        if (labels.ContainsKey(label)) { pc = labels[label]; }
        else { Error("Label not found : " + label); }
        
    }

    void Execute_LF(Instruction ins)
    {
        string num_str = ins.body.Trim();

        if (String.IsNullOrEmpty(num_str)) { Error("Missing parameter"); }

        if (!uint.TryParse(num_str, out uint num) ) { Error("Invalid parameter : " + num_str); }

        for (int i = 0; i<num; i++)
        {
            Console.WriteLine();
        }

        pc++;
    }

    void Execute_M(Instruction ins)
    {
        match = false;
        string[] tokens;

        if (String.IsNullOrEmpty(ins.body) || String.IsNullOrEmpty(ins.body.Trim())) 
        { 
            Error("Missing match parameter");
        }        

        if (ins.body.Trim()[0] == '$')//it is a label
        {
            string label = ins.body.Trim();

            if (!str_vars.ContainsKey(label)) { Error("Label not found : " + label);  }

            tokens =  str_vars[label].Split(',');
        }
        else//parse the body
        {
            tokens = ins.body.Split(',');
        }

        //match every token
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
        if (String.IsNullOrEmpty(ins.body)) { pc++; return; }

        string str = FormatString(ins.body);

        Console.WriteLine(str);

        pc++;
    }

    void Execute_TNR(Instruction ins)
    {
        if (String.IsNullOrEmpty(ins.body)) { pc++; return; }

        string str = FormatString(ins.body);

        Console.Write(str);

        pc++;
    }

    void Execute_U(Instruction ins)
    {
        if(routines.Count>=STACK_SIZE) { Error("Routine overflow"); }

        string label = ins.body.Trim();

        if (String.IsNullOrEmpty(label)) { Error("Missing label"); }

        if (!labels.ContainsKey(label)) { Error("Label not found"); }

        routines.Push(pc);

        pc = labels[label];
    }

    void Execute_WAIT(Instruction ins)
    {
        Error("Instruction not implemented : " + ins.type);
    }
}