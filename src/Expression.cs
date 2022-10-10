using System.Runtime.InteropServices;

namespace Expression;

public static class Expression
{
    static int OperatorPrecedence(char op)
    {
        switch (op)
        {
            case '+':
            case '-':
                return 0;

            case '*':
            case '/':
                return 1;

            case '(':
            case ')':
                return 2;

            default:
                break;
        }

        return -1;//not an operator

    }

    public static String InfixToPostfix(String infix)
    {
        Stack<char> stack = new Stack<char>();

        char[] str = infix.ToCharArray();

        String postfix = "";

        int count = 0;

        while (count < str.Length)
        {
            if (Char.IsWhiteSpace(str[count])) { count++; continue; }

            //Print the operand as they arrive.
            if (OperatorPrecedence(str[count]) == -1)
            {
                while (count < str.Length && OperatorPrecedence(str[count]) == -1)
                {
                    if(!Char.IsWhiteSpace(str[count]))  postfix += str[count];
                    
                    count++;
                }
                postfix += ' ';

                continue;
            }

            //If the stack is empty or contains a left parenthesis on top, push the incoming operator on to the stack.
            if (stack.Count==0 || stack.Peek() == '(')
            {
                stack.Push(str[count]);
                count++;
                continue;
            }

            //If the incoming symbol is '(', push it on to the stack.
            if (str[count] == '(')
            {
                stack.Push(str[count]);
                count++;
                continue;
            }

            //If the incoming symbol is ')', pop the stack and print the operators until the left parenthesis is found.
            if (str[count] == ')')
            {
                while (stack.Peek() != '(')
                {
                    postfix += stack.Pop();
                    postfix += ' ';
                }

                stack.Pop();
                count++;
                continue;
            }

            //If the incoming symbol has higher precedence than the top of the stack, push it on the stack.
            if (OperatorPrecedence(str[count]) > OperatorPrecedence(stack.Peek()))
            {
                stack.Push(str[count]);
                count++;
                continue;
            }

            //If the incoming symbol has lower precedence than the top of the stack, pop and print the top of the stack. 
            //Then test the incoming operator against the new top of the stack.
            if (OperatorPrecedence(str[count]) < OperatorPrecedence(stack.Peek()))
            {
                postfix += stack.Pop();
                postfix += ' ';
                continue;
            }

            //If the incoming operator has the same precedence with the top of the stack then use the associativity rules. 
            if (OperatorPrecedence(str[count]) == OperatorPrecedence(stack.Peek()))
            {
                //If the associativity is from left to right then pop and print the top of the stack then push the incoming operator. 
                postfix += stack.Pop();
                postfix += ' ';
                stack.Push(str[count]);
                count++;
                continue;

                //If the associativity is from right to left then push the incoming operator.
                /*stack.push(val);
                count++;
                continue;*/
            }

        }

        //At the end of the expression, pop and print all the operators of the stack.
        while (stack.Count!=0)
        {
            char c = stack.Pop();

            postfix += c;
            postfix += ' ';
        }

        return postfix;
    }

   
    //null if error ( ex: division by 0 )
    public static float? Evaluate(String postfix)
    {
        String[] tokens;

        Stack<float> stack;

        stack = new Stack<float>();

        tokens = postfix.Split(" ");

        int count = 0;

        while (count < tokens.Length)
        {

            if (String.IsNullOrEmpty(tokens[count].Trim()))
            {
                count++;
                continue;
            }

            switch (tokens[count].Trim())
            {
                case "(":
                case ")":
                    break;

                case "+":
                    stack.Push(stack.Pop() + stack.Pop());
                    break;

                case "-":
                    {
                        float b = stack.Pop();
                        float a = stack.Pop();
                        stack.Push(a - b);
                    }
                    break;

                case "*":
                    stack.Push(stack.Pop() * stack.Pop());
                    break;

                case "/":
                    {
                        float b = stack.Pop();
                        float a = stack.Pop();

                        if (b == 0) return null;//division by 0

                        stack.Push(a / b);
                    }
                    break;

                default://is a number
                    if (!float.TryParse(tokens[count], out float f)) { return null; }//parsing failed
                    stack.Push(f);
                    break;
            }

            count++;
        }

        return stack.Pop();
    }

}

