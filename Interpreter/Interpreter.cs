using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    internal class Program
    {
        public static string Code(int code)
        {
            switch (code)
            {
                case 1:
                    return @"BEGIN CODE
                                INT xyz, abc=100
                                xyz= ((abc *5)/10 + 10) * -1
                                DISPLAY: [[] & xyz & []]
                             END CODE";
                case 2:
                    return @"BEGIN CODE
                                INT x, y, z=5
                                CHAR a_1='n'
                                BOOL t=""TRUE""
                                    x=y=4
                                    a_1='c'
                                    # this is a comment
                                DISPLAY: x & t & z & $ & a_1 & [#] & ""last""
                             END CODE";
                case 3:
                    return @"BEGIN CODE
                                INT a=100, b=200, c=300
                                BOOL d=""FALSE""
                                d = (a < b AND c <> 200)
                                DISPLAY: d
                             END CODE";
                case 4:
                    return @"BEGIN CODE
                                INT a=100, b=200, c=300
                                c = (-a + 1) * -2
                                DISPLAY: c
                             END CODE";
                case 5:
                    return @"BEGIN CODE
                                INT a=100, b=200
                                FLOAT c = 4.2
                                IF (a < b)
                                BEGIN IF
                                    a = b
                                END IF
                                # comment should not appear in console
                                ""][""
                             END CODE";
                case 6:
                    return @"BEGIN CODE
                             INT a=10, b=2, c=0
                             c = a + b
                             DISPLAY: c
                             END CODE";
                case 7:
                    return @"BEGIN CODE
                                INT a=0
                                SCAN: a
                                DISPLAY: a
                             END CODE";
                default:
                    return "invalid choice";
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Enter the code number to execute (1-7): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 7)
            {
                string code = Code(choice);

                // Tokenize the input
                List<Token> tokens = Lexer.Tokenize(code);

                // Parse the tokens into an AST
                var parser = new Parser(tokens);
                var ast = parser.Parse();
                Console.WriteLine("Parsed AST:");
                foreach (var statement in ast.Statements)
                {
                    Console.WriteLine(statement.GetType().Name);
                }

                // Execute the AST
                var interpreter = new Interpreter(); // Ensure this is the correct initialization
                interpreter.Interpret(ast);

                Console.WriteLine("Program executed successfully.");
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter a number between 1 and 7.");
            }
        }
    }
}
