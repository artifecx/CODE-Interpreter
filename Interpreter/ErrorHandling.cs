using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }

    /*public class ParserException : Exception
    {
        public ParserException(int line, string code, object value)
            : base($"Error at line: {line}. {GetMessageFromCode(line, code, value)}") { }

        // Static method to map error codes to messages
        private static string GetMessageFromCode(int line, string code, object value)
        {
            switch (code)
            {
                case "INVALIDSTRUCTURE":
                    return "Invalid input format.";
                case 2:
                    return "Data missing from the request.";
                case 3:
                    return "Input value out of range.";
                default:
                    return "Unknown error.";
            }
        }
    }*/


    public class EvaluatorException : Exception
    {
        public EvaluatorException(string message) : base(message) { }
    }

    public class LexerException : Exception
    {
        public LexerException(string message) : base(message) { }
    }

    public class BreakException : Exception { }

    public class ContinueException : Exception { }

}
