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
                                DISPLAY: ""Hello, Gayshit""
                             END CODE";
                case 6:
                    return @"BEGIN CODE
                             INT a=2, b=9, c=0
                             c = a + b
                             DISPLAY: c
                             END CODE";
                case 7:
                    return @"BEGIN CODE
                                INT x = 5
                                INT y = 10
                                INT sum = x + y
                                DISPLAY: sum
                            END CODE";
                default:
                    return "invalid choice";
            }
        }

        static void Main(string[] args)
        {
            //Console.WriteLine("Enter the code number to execute: ");
            //string code = Code(choice);
            string str_directory = Environment.CurrentDirectory.ToString();
            var projectfolder = Directory.GetParent(Directory.GetParent(Directory.GetParent(str_directory).ToString()).ToString());

            string filename = @"sample.code";

            // Combine the project folder path and the relative path
            string filePath = Path.Combine(projectfolder.ToString(), filename);

            if (File.Exists(filePath))
            {
                string code = File.ReadAllText(filePath);

                var lexer = new Lexer();
                List<Token> tokens = Lexer.Tokenize(code);

                var parser = new Parser(tokens);
                var ast = parser.Parse();

                // Debugging
                Console.WriteLine("\nParsed AST:");
                foreach (var statement in ast.Statements)
                {
                    Console.WriteLine(statement.GetType().Name);
                }
                Console.WriteLine();

                var interpreter = new Interpreter();
                interpreter.Interpret(ast);

                //Console.WriteLine("\nProgram executed successfully.");
            }
            else
            {
                Console.WriteLine($"File not found: {filePath}");
            }
        }
    }
}
