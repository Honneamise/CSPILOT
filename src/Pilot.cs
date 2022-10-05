namespace Pilot;

using Expression;
using System.Runtime.InteropServices;

/***********************/
/* LAYOUT CLASS        *
 * NOTE : WINDOWS ONLY */
/***********************/
public static class Layout
{
    public static readonly int COLS = 80;
    public static readonly int ROWS = 25;
        
    public static bool IsWindows;

    const int MF_BYCOMMAND = 0x00000000;
    const int SC_MINIMIZE = 0xF020;
    const int SC_MAXIMIZE = 0xF030;
    const int SC_SIZE = 0xF000;

    [DllImport("user32.dll")]
    public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    /*
     * If at runtime we are on windows set the console 80x25
     * NOTE : needed for cursor functions in interpreter
     */
    public static void Init()
    {
        IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (IsWindows)
        {
            Console.SetWindowSize(COLS, ROWS);
            Console.SetBufferSize(COLS, ROWS);

            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MINIMIZE, MF_BYCOMMAND);
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_MAXIMIZE, MF_BYCOMMAND);
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_SIZE, MF_BYCOMMAND);
        }
    }
}

/*********************/
/* INSTRUCTION CLASS */
/*********************/
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

}

/***************************/
/* PILOT INTERPRETER CLASS */
/***************************/
public class Pilot
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

    
    /*****************/
    /* PILOT SECTION */
    /*****************/
    public Pilot()
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

        Console.WriteLine("******************************************");
        Console.WriteLine("* PILOT INTERPRETER (H)                  *");
        Console.WriteLine("* Vers. 2022 by Honny                    *");
        Console.WriteLine("*                                        *");
        Console.WriteLine("* Type HELP for commands list            *");
        Console.WriteLine("******************************************");

        while (true)//TODO: REWRITE TO AVOID LIST == LISTATO ( use equals + get head token )
        {
            Console.Write(":>");

            string input = Console.ReadLine() ?? "";

            input = input.Trim();

            string command = GetHeadToken(input);

            if (string.IsNullOrEmpty(command)) { continue; }

            //HELP
            if (command.Equals("HELP"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                Usage();

                continue;
            }

            //MANUAL
            if (command.Equals("MANUAL"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                if (!File.Exists("res/MANUAL.TXT")) { Console.WriteLine("Manual file not found"); continue; }
                
                Console.Clear();
                
                Manual();

                continue;
            }

            //CLEAR
            if (command.Equals("CLEAR"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                Console.Clear();

                continue;
            }

            //LIST
            if (command.Equals("LIST"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                if( lines.Count <= 0) { continue; }

                Console.Clear();
                List();

                continue;
            }

            //LOAD
            if (command.Equals("LOAD"))
            {
                string param = input[command.Length..].Trim();

                if (String.IsNullOrEmpty(param)) { Console.WriteLine("Missing or invalid file name"); continue; }

                if (!File.Exists(param)) { Console.WriteLine("File does not exist : " + param); continue; }

                Init();

                Load(param);

                continue;
            }

            //SAVE
            if (command.Equals("SAVE"))
            {
                string param = input[command.Length..].Trim();

                if (String.IsNullOrEmpty(param)) { Console.WriteLine("Missing or invalid file name"); continue; }

                Save(param);

                continue;
            }

            //RUN
            if (command.Equals("RUN"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                Console.Clear();

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
            if (command.Equals("RESET"))
            {
                if (!command.Equals(input)) { Console.WriteLine(command + " does not expect parameters"); continue; }

                Init();

                continue;
            }

            //EXIT
            if (command.Equals("EXIT"))
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

            //default condition is an error
            Console.WriteLine("Unknow command : " + command);

        }

        Console.ForegroundColor = restore_fgcolor;
        Console.BackgroundColor = restore_bgcolor;
    }

    public void Usage()
    {
        Console.WriteLine("******************************************");
        Console.WriteLine("* HELP        : this menu                *");
        Console.WriteLine("* MANUAL      : short PILOT manual       *");
        Console.WriteLine("* CLEAR       : clear the console        *");
        Console.WriteLine("* LIST        : show current program     *");
        Console.WriteLine("* LOAD <file> : load program into memory *");
        Console.WriteLine("* SAVE <file> : save program to disk     *");
        Console.WriteLine("* RESET       : clear current program    *");
        Console.WriteLine("* RUN         : run current program      *");
        Console.WriteLine("* EXIT        : exit                     *");
        Console.WriteLine("******************************************");
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
        int index = 0;

        while(index < lines.Count)
        {
            int count = 0;
            while (index < lines.Count && count< (Console.WindowHeight-1))
            {
                Console.WriteLine((index + 1).ToString("000") + "|" + lines[index]); ;
                index++;
                count++;
            }

            if(index >= lines.Count) { return; }
            Console.ReadKey();
            
        }

    }

    /*
     * Print the manual
     */
    public void Manual()
    {
        int index = 0;

        List<string>text = new List<string>(File.ReadAllLines("res/MANUAL.TXT"));

        while (index < text.Count)
        {
            int count = 0;
            while (index < text.Count && count < (Console.WindowHeight - 1))
            {
                Console.WriteLine(text[index]); ;
                index++;
                count++;
            }

            if (index >= text.Count) { return; }
            Console.ReadKey();
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
    /* NOTE : if a function will create a LABEL, or VARIABLE check if with FormatIsValid function */

    /*
     * Just choose which instruction to execute based on ins.type
     */
    void ExecuteInstruction(Instruction ins)
    {
        if (ins.type == null) { pc++; return; }

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
        //always save the user input
        string? input = Console.ReadLine()?.Trim();
        accept = input ?? "";

        string param = ins.body.Trim();

        //we have parameters in the body of the instruction
        if (!String.IsNullOrEmpty(param))
        {
            if (!FormatIsValid(param)) { RuntimeError("Invalid parameter format : " + param); return; }

            if (param[0]=='#' && param.Length>1)//it is a numeric variable
            {
                float num;

                while(true)//repeat until valid user input
                {
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
                str_vars[param] = input ?? "";

            }
            else//error uknow variable type
            {
                RuntimeError("Invalid parameter : " + param);
                return;
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

        if (String.IsNullOrEmpty(str)) { RuntimeError("Missing compute statement"); return; }

        //get assign variable
        int end = str.IndexOf('=');

        if (str[0]!='#' || end==-1 || end==str.Length-1) { RuntimeError("Invalid compute statement"); return; }

        string var_name = str[..end].Trim();

        if (!FormatIsValid(var_name)) { RuntimeError("Invalid variable format"); return; }

        //get infix expression
        string infix = str[(end+1)..];
        if (String.IsNullOrEmpty(infix)) { RuntimeError("Invalid compute statement"); return; }

        //infix to postfix
        string postfix = Expression.InfixToPostfix(infix);

        //substitute variables
        string[] tokens = postfix.Split(' ');
        postfix = "";
        foreach(String token in tokens)
        {
            if(token.Length>1 && token[0]=='#')
            {
                if (!num_vars.ContainsKey(token)) { RuntimeError("Variable not found : " + token); return; }
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
        string str = ins.body.Trim();

        string var = GetHeadToken(str);

        if(!num_vars.ContainsKey(var)) { RuntimeError("Variable not found : " + var); return; }

        str = str[var.Length..].TrimStart();

        string[] options = str.Split(',');

        int selected = (int)num_vars[var] - 1;

        selected = Math.Clamp(selected, 0, options.Length-1);//limit

        string label = options[selected].Trim();

        if (!labels.ContainsKey(label)) { RuntimeError("Label not found : " + label); return; }

        pc = labels[label];
    }

    void Execute_CH(Instruction ins)
    {
        string str = ins.body.Trim();

        string file = GetHeadToken(str);

        if (String.IsNullOrEmpty(file)) { RuntimeError("File name not found."); return; }

        if (!file.Equals(str.TrimEnd())) { RuntimeError("File does not match line"); return; }

        if (!File.Exists(file)) { RuntimeError("File not found : " + file); return; }

        Init();
        
        Load(file);
        
        Parse();

        Run();
    }

    void Execute_CLRS(Instruction ins)
    {
        Console.Clear();
        pc++;
    }

    void Execute_CUR(Instruction ins)
    {
        if (!Layout.IsWindows) { RuntimeError("Function available only on Windows platform"); return; }

        string[]  coords = ins.body.Split(',');

        if (coords.Length != 2) { RuntimeError("Too many parameters"); return; }

        //parse x
        if (!int.TryParse(coords[0], out int x)) { RuntimeError("Invalid x parameter : " + coords[0]); return; }
        //parse x
        if (!int.TryParse(coords[1], out int y)) { RuntimeError("Invalid y parameter : " + coords[1]); return; }

        if (x < 0 || x >= 80) { RuntimeError("Parameter X out of range"); return; }

        if (y < 0 || y >= 25) { RuntimeError("Parameter Y out of range"); return; }

        Console.SetCursorPosition(x, y);

        pc++;

    }

    void Execute_DEF(Instruction ins)
    {
        string str = ins.body.TrimStart();

        string var = GetHeadToken(str);

        if(!FormatIsValid(var) || var[0]!='$') { RuntimeError("Invalid variable : " + var); return; }

        str_vars[var] = str[var.Length..];

        pc++;
    }

    void Execute_E(Instruction ins)
    {
        if (routines.Count <= 0) { RuntimeError("Routine Stack Underflow"); return; }

        pc = routines.Pop();

        pc++;
    }

    void Execute_END(Instruction ins)
    {
        run = false;
    }

    void Execute_ERASTR(Instruction ins)
    {
        foreach(KeyValuePair <string,string>var in str_vars)
        {
            str_vars[var.Key] = "";
        }

        pc++;
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

        if (String.IsNullOrEmpty(num_str)) { RuntimeError("Missing parameter"); return; }

        if (!uint.TryParse(num_str, out uint num) ) { RuntimeError("Invalid parameter : " + num_str); return; }

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
            return;
        }        

        if (ins.body.Trim()[0] == '$')//it is a label
        {
            string label = ins.body.Trim();

            if (!str_vars.ContainsKey(label)) { RuntimeError("Label not found : " + label); return; }

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

        if(error!=null) {  return; }

        Console.WriteLine(str);

        pc++;
    }

    void Execute_TNR(Instruction ins)
    {
        if (String.IsNullOrEmpty(ins.body)) { pc++; return; }

        string str = FormatString(ins.body);

        if (error != null) { return; }

        Console.Write(str);

        pc++;
    }

    void Execute_U(Instruction ins)
    {
        if(routines.Count>=STACK_SIZE) { RuntimeError("Routine overflow"); return; }

        string label = ins.body.Trim();

        if (String.IsNullOrEmpty(label)) { RuntimeError("Missing label"); return; }

        if (!labels.ContainsKey(label)) { RuntimeError("Label not found : " + label); return; }

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
     * Given a string check for valid LABEL or VARIABLE : LETTERS(UPPERCASE),NUMBERS
     */
    public bool FormatIsValid(string str)
    {
        if(String.IsNullOrEmpty(str)) { return false; }

        if ( !(str[0]=='*' || str[0]=='#' || str[0]=='$')) { return false; };

        for(int i=1;i<str.Length;i++)
        {
            if (!((Char.IsLetter(str[i]) && Char.IsUpper(str[i])) || Char.IsNumber(str[i]))) { return false; }
        }

        return true;
    }

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
            if (!FormatIsValid(token)) { SyntaxError("Invalid label format"); return ins; }//check for label all uppercase

            if (!token.Equals(str.TrimEnd())) { SyntaxError("Label does not match line"); return ins; }//label is not alone

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
                if (!Char.IsUpper(ins.type[^1])) { SyntaxError("Invalid instruction format"); return ins; }//condition is lowercase
            
                if (ins.type[^1] == 'Y') { ins.cond = true; ins.type = ins.type[..^1]; }
                if (ins.type[^1] == 'N') { ins.cond = false; ins.type = ins.type[..^1]; }
            }

            if (!ins.type.All(Char.IsUpper) ) { SyntaxError("Invalid instruction format"); return ins; }//ins type is lowercase

            if (!instructions_list.Contains(ins.type)) { SyntaxError("Unknow instruction found : " + ins.type); return ins; }

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
     * NOTE : can set error
     */
    public string FormatString(string str)
    {
        int index = 0;
        string s = "";

        // EXAMPLES :
        // T: ## --> #
        // T: #NUMBER --> 123
        // T: ###NUMBER --> #123
        // T: ##NUMBER --> #NUMBER
        // T: #### --> ##
        // T: ### --> ##
        while (index < str.Length)
        {
            //double symbol 
            if ( (index + 1 < str.Length) && (str[index] == str[index + 1]) && (str[index] == '#' || str[index] == '$') )
            {
                s += str[index];
                index += 2;
                continue;
            }

            //single symbol
            if ( (index + 1 < str.Length) && (str[index] == '#' || str[index] == '$') )
            {
                int end = index;
                while (end < str.Length && !Char.IsWhiteSpace(str[end])) { end++; }

                string var = str[index..end];

                if (!FormatIsValid(var)) { RuntimeError("Invalid variable format : " + var); return s; }

                if (num_vars.ContainsKey(var))//is numeric ?
                {
                    s += num_vars[var];
                }
                else if (str_vars.ContainsKey(var))//is string ?
                {
                    s += str_vars[var];
                }
                else
                {
                    RuntimeError("Variable not found : " + var);//is an error :-)
                    return s;
                }

                index = end;
                continue;
            }
            
            //default append
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
        if (error == null) { error = "SYNTAX ERROR : " + (pc + 1).ToString("000") + "|" + str; }
    }

    /*
     * If error is not already set (null), set it !!!
     * Call it if Running
     */
    void RuntimeError(string str)
    {
        if (error == null) { error = "RUNTIME ERROR : " + (pc + 1).ToString("000") + "|" + str; }
    }
}