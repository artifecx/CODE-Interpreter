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
