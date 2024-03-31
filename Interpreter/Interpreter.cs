using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    internal class Interpreter
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
                default:
                    return "invalid choice";
            }
        }

        static void Main(string[] args)
        {
            string inputCode = Code(1);
            List<Token> tokens = Lexer.Tokenize(inputCode);
            foreach (Token token in tokens)
            {
                Console.WriteLine($"{token.Type}: {token.Value}");
            }


            // Create a parser instance
            //Parser parser = new Parser(tokens);

            // Parse the code
            //ASTNode rootNode = parser.Parse();

            // Display the AST (for demonstration)
            //DisplayAST(rootNode);
        }

        static void DisplayAST(ASTNode node)
        {
            Console.WriteLine($"Type: {node.Type}, Value: {node.Value}");

            foreach (var child in node.Children)
            {
                DisplayAST(child);
            }
        }
    }
}
