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
                    return @"cars = [""Orange"", ""Siamese"", ""Bengal""]
	                    a = cars[0]
	                    output(a)

                        cars[0] = 'Mawa'
	                    for(i = 0; i < length(cars); i++){
		                   output(cars[i]).sameline(' ')
	                    }
                    
                        cars.add('Yasha')
	                    output(cars[length(cars)-1])

	                    cars.pop(1)
	                    cars.remove('Bengal')
	                    for(i = 0; i < length(cars); i++){
		                    output(cars[i]).sameline(',')
	                    }
                    ";
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
                    return @"a = 6
                        b = 9
                        c = ((a*b)+(b/a)*(a+7.5)+a) * -1
                        output([ [ ] c [ ] ])
                        ";
                default:
                    return "invalid choice";
            }
        }

        static void Main(string[] args)
        {
            string inputCode = Code(2);
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
