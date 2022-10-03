namespace Pilot;

using Expression;
using System.Data;
using System.Net;

/**********/
public class Instruction
{
    public string? label;
    public string? type;
    public bool?   cond;
    public string  body;

    public Instruction()
    {
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
    readonly int STACK_SIZE = 512;
    readonly string[] instructions_list = { "A", "BELL", "C", "CASE", "CH", "CLRS", "CUR", "DEF", "E", "END", "ERASTR",
                                            "HOLD", "INMAX", "J", "LF", "M", "MC", "R", "RESET", "SAVE", "T", "TNR", "U", "WAIT" };

    bool run;

    int pc;

    List<string> lines;
    List<Instruction> instructions;
    Dictionary<string,int> labels;
    
    Dictionary<string, float> num_vars;
    Dictionary<string, string> str_vars;

    Stack<int> routines;

    string accept;
    bool match;

    string? error;

    
    /*******************/
    /* PROGRAM SECTION */
    /*******************/
    public Interpreter()
    {
        run = false;
        pc = 0;

        lines = new List<string>();
        instructions = new List<Instruction>();

        labels = new Dictionary<string, int>();
        num_vars = new Dictionary<string, float>();
        str_vars = new Dictionary<string, string>();

        routines = new Stack<int>();

        accept = "";
        match = false;

        error = null;
    }

    /*
     * Main entry point
     */
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

        while (true)//TODO: REWRITE TO AVOID LIST == LISTATO ( use equals + get head token )
        {
            Console.Write(":>");

            string input = Console.ReadLine() ?? "";

            input = input.Trim();

            string command = GetHeadToken(input);

            if (string.IsNullOrEmpty(command)) { continue; }

            //LIST
            if (command.ToUpper().Equals("LIST"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                List();

                continue;
            }

            //LOAD
            if (command.ToUpper().Equals("LOAD"))
            {
                string param = input[command.Length..].Trim();

                if (String.IsNullOrEmpty(param)) { Console.WriteLine("Missing or invalid file name"); continue; }

                if (!File.Exists(param)) { Console.WriteLine("File does not exist : " + param); continue; }

                Init();

                Load(param);

                continue;
            }

            //SAVE
            if (command.ToUpper().Equals("SAVE"))
            {
                string param = input[command.Length..].Trim();

                if (String.IsNullOrEmpty(param)) { Console.WriteLine("Missing or invalid file name"); continue; }

                Save(param);

                continue;
            }

            //RUN
            if (command.ToUpper().Equals("RUN"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                List<string> _lines = lines;//save loaded lines

                Init();

                lines = _lines;//restore saved lines

                Parse();
                if (error != null) { Console.WriteLine(error); continue; }

                Run();
                if (error != null) { Console.WriteLine(error); continue; }

                continue;
            }

            //RESET
            if (command.ToUpper().Equals("RESET"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                Init();

                continue;
            }

            //EXIT
            if (command.ToUpper().Equals("EXIT"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                break;
            }

            //is line insertion ?
            if (int.TryParse(command, out int num))
            {
                num--;

                if (num < 0) { Console.WriteLine("Invalid line number : " + num); continue; }

                string param = input[command.Length..];

                int count = num - (lines.Count - 1);//how many empty lines to add

                for (int i = 0; i < count; i++)
                {
                    lines.Add("");
                }

                lines[num] = param.TrimStart();

                continue;
            }

            //DEFAULT CONDITION IS AN ERROR
            Console.WriteLine("Unknow command : " + command);

        }

        Console.ForegroundColor = restore_fgcolor;
        Console.BackgroundColor = restore_bgcolor;
    }

    /*
     * Clear the entire interpreter
     */
    public void Init()
    {
        run = false;
        pc = 0;

        lines = new List<string>();
        instructions = new List<Instruction>();

        labels = new Dictionary<string, int>();
        num_vars = new Dictionary<string, float>();
        str_vars = new Dictionary<string, string>();

        routines = new Stack<int>();

        accept = "";
        match = false;

        error= null;
    }

    /*
     * List current loaded lines
     */
    public void List()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine((i + 1).ToString("000") + "|" + lines[i]); ;
        }
    }

    /*
     * Load a file into the interpreter lines
     */
    public void Load(string file)
    {
        Init();
        lines = new List<string>(File.ReadAllLines(file));
    }

    /*
     * Save the lines of code to a file
     */
    public void Save(string file)
    {
        File.WriteAllLines(file, lines);
    }

    /*
     * Parse the interpreter lines and build the array o instructions.
     * Also initialize all the labels
     */
    public void Parse()
    {
        pc = 0;
        error = null;

        foreach (string line in lines)
        {
            Instruction ins = ParseInstruction(line);

            if(error != null) { break; }

            instructions.Add(ins);

            if (ins.label != null)
            {
                if (labels.ContainsKey(ins.label)) { SyntaxError("Duplicate label entry for : " + ins.label); break; }
                else { labels[ins.label] = pc; }
            }

            pc++;
        }

    }

    /*
     * Execute the current list of instructions
     */
    public void Run()
    {
        pc = 0;
        run = true;
        error = null;

        while (run && error == null)
        {
            if (pc < 0 || pc >= instructions.Count) { RuntimeError("Program counter out of range"); return; }

            Instruction ins = instructions[pc];

            ExecuteInstruction(ins);
        }
    }    

    /*********************/
    /* FUNCTIONS SECTION */
    /*********************/

    /*
     * Just choose which instruction to execute based on ins.type
     */
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

            case "CUR":
                Execute_CUR(ins);
                break;

            case "DEF":
                Execute_DEF(ins);
                break;

            case "E":
                Execute_E(ins);
                break;

            case "END":
                Execute_END(ins);
                break;

            case "ERASTR":
                Execute_ERASTR(ins);
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

            case "R":
                Execute_R(ins);
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
                RuntimeError("Unknow instruction : " + ins.type);//this should never happen
                break;
        }
    }


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
                RuntimeError("Invalid parameter : " + param);
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

        if (String.IsNullOrEmpty(str)) { RuntimeError("Missing compute statement"); }

        //get assign variable
        int end = str.IndexOf('=');

        if (str[0]!='#' || end==-1 || end==str.Length-1) { RuntimeError("Invalid compute statement"); }

        string var_name = str[..end].Trim();

        //get infix expression
        string infix = str[(end+1)..];
        if (String.IsNullOrEmpty(infix)) { RuntimeError("Invalid compute statement"); }

        //infix to postfix
        string postfix = Expression.InfixToPostfix(infix);

        //substitute variables
        string[] tokens = postfix.Split(' ');
        postfix = "";
        foreach(String token in tokens)
        {
            if(token.Length>1 && token[0]=='#')
            {
                if (!num_vars.ContainsKey(token)) { RuntimeError("Variable not found : " + token); }
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

        if(f==null) { RuntimeError("Expression error"); return; }//should return

        num_vars[var_name] = (float)f;

        pc++;
    }

    void Execute_CASE(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_CH(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_CLRS(Instruction ins)
    {
        Console.Clear();
        pc++;
    }

    void Execute_CUR(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_DEF(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_E(Instruction ins)
    {
        if (routines.Count <= 0) { RuntimeError("Routine Stack Underflow"); }

        pc = routines.Pop();

        pc++;
    }

    void Execute_END(Instruction ins)
    {
        run = false;
    }

    void Execute_ERASTR(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_HOLD(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_INMAX(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_J(Instruction ins)
    {
        string label = ins.body.Trim();

        if (String.IsNullOrEmpty(label)) { RuntimeError("Missing label"); }

        if (labels.ContainsKey(label)) { pc = labels[label]; }
        else { RuntimeError("Label not found : " + label); }
        
    }

    void Execute_LF(Instruction ins)
    {
        string num_str = ins.body.Trim();

        if (String.IsNullOrEmpty(num_str)) { RuntimeError("Missing parameter"); }

        if (!uint.TryParse(num_str, out uint num) ) { RuntimeError("Invalid parameter : " + num_str); }

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
            RuntimeError("Missing match parameter");
        }        

        if (ins.body.Trim()[0] == '$')//it is a label
        {
            string label = ins.body.Trim();

            if (!str_vars.ContainsKey(label)) { RuntimeError("Label not found : " + label);  }

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
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    void Execute_R(Instruction ins)
    {
        pc++;
    }

    void Execute_SAVE(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
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
        if(routines.Count>=STACK_SIZE) { RuntimeError("Routine overflow"); }

        string label = ins.body.Trim();

        if (String.IsNullOrEmpty(label)) { RuntimeError("Missing label"); }

        if (!labels.ContainsKey(label)) { RuntimeError("Label not found"); }

        routines.Push(pc);

        pc = labels[label];
    }

    void Execute_WAIT(Instruction ins)
    {
        RuntimeError("Instruction not implemented : " + ins.type);
    }

    /*****************/
    /* UTILS SECTION */
    /*****************/

    /*
     * Given a string return a substring from beggining to first 'Whitespace' character
     */
    public string GetHeadToken(string str)
    {
        if (String.IsNullOrEmpty(str)) { return ""; }

        string s = "";
        int i = 0;
        while (i < str.Length && !Char.IsWhiteSpace(str[i]))
        {
            s += str[i];
            i++;
        }

        return s;
    }

    /*
     * Given a line plit it to tokens and build a struct of type Instruction
     * Can set error, otherwise error will be null
     */
    Instruction ParseInstruction(string str)
    {
        Instruction ins = new();

        if (String.IsNullOrEmpty(str.Trim())) { return ins; };//empty line

        str = str.TrimStart();

        string token = GetHeadToken(str);

        if (token.Length < 2) { SyntaxError("Invalid label or instruction"); return ins; }//error

        if (token[0] == '*')//label 
        {
            if (!token.Equals(str.TrimEnd())) { SyntaxError("Invalid label found"); return ins; }

            ins.label = token;
            return ins;
        }
        else if (str.Contains(':'))//instruction
        {
            int end = str.IndexOf(':');
            ins.type = str[..end];

            //we have condition !
            if (ins.type.Length > 1)
            {
                if (ins.type[^1] == 'Y' || ins.type[^1] == 'y') { ins.cond = true; ins.type = ins.type[..^1]; }
                if (ins.type[^1] == 'N' || ins.type[^1] == 'n') { ins.cond = false; ins.type = ins.type[..^1]; }
            }

            if (!instructions_list.Contains(ins.type.ToUpper())) { SyntaxError("Unknow instruction found : " + ins.type); return ins; }

            //we have body ?
            if (end + 1 < str.Length) { ins.body = str[(end + 1)..]; }//get the body

            return ins;
        }
        else//error
        {
            SyntaxError("Invalid label or instruction");
            return ins;
        }
    }

    /*
     * Given a string format using Pilot language directives
     */
    public string FormatString(string str)//TODO : REVWRITE ME !!!
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

                    if (label.Length == 1) { RuntimeError("Missing label"); }

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
                        RuntimeError("Label not found : " + label);//is an error :-)
                    }

                    index = end;
                    continue;
                }

            }

            //identifier at last position
            if (index == str.Length - 1 && (str[index] == '#' || str[index] == '$'))
            {
                RuntimeError("Missing label");
            }

            s += str[index];
            index++;
        }

        return s;
    }

    /*
     * If error is not already set (null), set it !!!
     * Call it if parsing.
     */
    void SyntaxError(string str)
    {
        if (error == null) { error = "Syntax Error : " + (pc + 1) + "|" + str; }
    }

    /*
     * If error is not already set (null), set it !!!
     * Call it if Running
     */
    void RuntimeError(string str)
    {
        if (error == null) { error = "Runtime Error : " + (pc + 1) + "|" + str; }
    }
}