using System;
using System.Collections.Generic;
using System.Numerics;

public enum TokenType
{
    // Code blocks
    BEGINCODE,          // BEGIN CODE - start of program
    ENDCODE,            // END CODE - end of program
    BEGINIF,            // START IF - start if block
    ENDIF,              // END IF - end if block
    BEGINWHILE,         // START WHILE - start while block
    ENDWHILE,           // END WHILE - end while block

    // Data types
    INT,                 // an ordinary number with no decimal part
    CHAR,               // a single symbol
    BOOL,               // represents the literals true or false
    FLOAT,              // a number with decimal part

    // Literals
    IDENTIFIER,         // a_1, x, y
    INTEGERLITERAL,     // 5
    STRINGLITERAL,      // "Hello, World!"
    CHARACTERLITERAL,   // 'n'
    TRUE,               // "TRUE" - bool literal
    FALSE,              // "FALSE" - bool literal
    FLOATLITERAL,       // 3.0

    // Operators
    ASSIGNMENT,         // =
    ADD,                // +
    SUB,                // -
    MUL,                // *
    DIV,                // /
    MOD,                // %
    GREATERTHAN,        // >
    LESSERTHAN,         // <
    GTEQ,                // >=
    LTEQ,               // <=
    EQUAL,              // ==
    NOTEQUAL,           // <>
    AND,                // logical operator AND
    OR,                 // logical operator OR
    NOT,                // logical operator NOT
    CONCATENATE,        // &

    // Delimiters
    NEXTLINE,           // \n, $
    COMMENT,            // #
    COLON,              // :
    COMMA,              // ,
    OPENPARENTHESIS,    // (
    CLOSEPARENTHESIS,   // )
    OPENBRACKET,        // [
    CLOSEBRACKET,       // ]

    // Keywords
    BEGIN,
    END,
    CODE,
    IF,
    ELSE,
    WHILE,
    DISPLAY,            // Output to console
    SCAN,               // User input

    // Special
    UNKNOWN,            // Unrecognized token
    EOF,                 // End of File

    // New additions, will group later
    PI,                 // 3.14159...
    CEIL,               // Round up
    FLOOR,              // Round down
    TOINT,              // Convert to integer, can only convert float to integer, truncates decimal part
    TOFLOAT,            // Convert to float, can only convert integer to float
    TOSTRING,           // Convert to string, convert anything to string
    TYPE,               // Type keyword, used to define data types

    INCREMENT,          // i++
    DECREMENT,          // i--
    MODASSIGNMENT,      // %=
    ADDASSIGNMENT,      // +=
    SUBASSIGNMENT,      // -=
    MULASSIGNMENT,      // *=
    DIVASSIGNMENT,      // /=

    // not done yet
    BREAK,              // Break out of loop
    CONTINUE,           // Skip to next iteration
  
    SWITCH,             // Switch case
    DO,                 // Do while loop
    FOR,                // For loop

    STRING,             // String data type, can be used to store multiple characters

    FUNCTION,           // Function keyword, used to define functions
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public int Line { get; }

    public Token(TokenType type, string value, int line)
    {
        Type = type;
        Value = value;
        Line = line;
    }
}


public class Lexer
{
    private static int _index = 0;
    private static int _line = 1;
    private static string? _code;

    public static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
    {
        {"BEGIN", TokenType.BEGIN},
        {"END", TokenType.END},

        {"CODE", TokenType.CODE},

        {"IF", TokenType.IF},
        {"ELSE", TokenType.ELSE},
        {"SWITCH", TokenType.SWITCH},

        {"WHILE", TokenType.WHILE},
        {"FOR", TokenType.FOR},
        {"DO", TokenType.DO},

        {"DISPLAY", TokenType.DISPLAY},
        {"SCAN", TokenType.SCAN},

        {"INT", TokenType.INT},
        {"CHAR", TokenType.CHAR},
        {"BOOL", TokenType.BOOL},
        {"FLOAT", TokenType.FLOAT},
        {"TRUE", TokenType.TRUE},
        {"FALSE", TokenType.FALSE},

        {"AND", TokenType.AND},
        {"OR", TokenType.OR},
        {"NOT", TokenType.NOT},

        {"BREAK", TokenType.BREAK},
        {"CONTINUE", TokenType.CONTINUE},

        {"PI", TokenType.PI},
        {"CEIL", TokenType.CEIL},
        {"FLOOR", TokenType.FLOOR},

        {"TOINT", TokenType.TOINT},
        {"TOFLOAT", TokenType.TOFLOAT},
        {"TOSTRING", TokenType.TOSTRING},

        {"TYPE", TokenType.TYPE},
        {"FUNCTION", TokenType.FUNCTION },
    };

    public static List<Token> Tokenize(string code)
    {
        _code = code;
        _line = 1;
        _index = 0;
        var tokens = new List<Token>();
        try
        {
            while (_index < _code.Length)
            {
                char currentChar = CurrentChar;

                switch (currentChar)
                {
                    #region SPECIAL
                    case '\n':
                        tokens.Add(new Token(TokenType.NEXTLINE, "\\n", _line));
                        _index++;
                        _line++;
                        break;

                    case ' ':
                    case '\t':
                    case '\r':
                        _index++;
                        break;
                    #endregion SPECIAL

                    #region OPERATORS
                    case '=':
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '%':
                    case '>':
                    case '<':
                        tokens.Add(HandleOperator(currentChar, tokens));
                        _index += addIndex(currentChar);
                        break;

                    case '&':
                        if (ScanNextAndPrev()) // do not read & - added to handle & $ &, & $, $ &
                        {
                            _index++;
                        }
                        else
                        {
                            tokens.Add(HandleOperator(currentChar, tokens));
                            _index++;
                        }
                        break;

                    #endregion OPERATORS

                    #region DELIMITERS
                    case '$':
                    case ':':
                    case ',':
                    case '(':
                    case ')':
                        tokens.Add(HandleDelimiter(currentChar));
                        if (currentChar == '$')
                        {
                            _line++;
                        }
                        _index++;
                        break;

                    case '#':
                        SkipComment();
                        tokens.Add(new Token(TokenType.NEXTLINE, "\\n", _line)); // add newline so it doesnt break the code :)
                        _line++;
                        break;

                    case '[':
                        tokens.Add(ScanEscape());
                        break;

                    #endregion DELIMITERS

                    case '"':
                        tokens.Add(ScanString());
                        break;

                    case '\'':
                        tokens.Add(ScanCharacter());
                        break;

                    default:
                        if (char.IsLetter(currentChar) || currentChar == '_')
                        {
                            tokens.Add(ScanIdentifier());
                        }
                        else if (char.IsDigit(currentChar))
                        {
                            tokens.Add(FloatOrInteger());
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.UNKNOWN, currentChar.ToString(), _line));
                            _index++;
                            //throw new ArgumentException($"Unexpected character at index {_index}: {currentChar}");
                        }
                        break;
                }
            }

            tokens.Add(new Token(TokenType.EOF, "END OF LINE", _line));

            // Debugging
            /*foreach (var token in tokens)
            {
                Console.WriteLine($"Token: {token.Type}, Value: '{token.Value}'");
            }*/
            return tokens;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(1);
            return null;
        }
    }

    #region HELPER METHODS
    private static char CurrentChar => _code != null ? _code[_index] : throw new InvalidOperationException("_code is null");

    private static Token HandleOperator(char currentChar, List<Token> _tokens)
    {
        switch (currentChar)
        {
            case '=':
                return PeekChar(1) == '=' ? new Token(TokenType.EQUAL, "==", _line) : new Token(TokenType.ASSIGNMENT, "=", _line);
            case '+':
                return PeekChar(1) == '+' ? new Token(TokenType.INCREMENT, "++", _line)  : (PeekChar(1) == '=' ? new Token(TokenType.ADDASSIGNMENT, "+=", _line)  : new Token(TokenType.ADD, "+", _line));
            case '-':
                return PeekChar(1) == '-' ? new Token(TokenType.DECREMENT, "--", _line) :  (PeekChar(1) == '=' ? new Token(TokenType.SUBASSIGNMENT, "-=", _line) :  new Token(TokenType.SUB, "-", _line));
            case '*':
                return PeekChar(1) == '=' ? new Token(TokenType.MULASSIGNMENT, "*=", _line) : new Token(TokenType.MUL, "*", _line);
            case '/':
                return PeekChar(1) == '=' ? new Token(TokenType.DIVASSIGNMENT, "/=", _line) : new Token(TokenType.DIV, "/", _line);
            case '%':
                return PeekChar(1) == '=' ? new Token(TokenType.MODASSIGNMENT, "%=", _line) : new Token(TokenType.MOD, "%", _line);
            case '>':
                return PeekChar(1) == '=' ? new Token(TokenType.GTEQ, ">=", _line) : new Token(TokenType.GREATERTHAN, ">", _line);
            case '<':
                if (PeekChar(1) == '=') return new Token(TokenType.LTEQ, "<=", _line);
                if (PeekChar(1) == '>') return new Token(TokenType.NOTEQUAL, "<>", _line);
                return new Token(TokenType.LESSERTHAN, "<", _line);
            case '&':
                return new Token(TokenType.CONCATENATE, "&", _line);
            default:
                return new Token(TokenType.UNKNOWN, "Unknown Operator", _line);
        }
    }

    private static Token HandleDelimiter(char currentChar)
    {
        switch (currentChar)
        {
            case '$':
                return new Token(TokenType.NEXTLINE, "$" , _line);
            case ':':
                return new Token(TokenType.COLON, ":", _line);
            case ',':
                return new Token(TokenType.COMMA, ",", _line);
            case '(':
                return new Token(TokenType.OPENPARENTHESIS, "(", _line);
            case ')':
                return new Token(TokenType.CLOSEPARENTHESIS, ")", _line);
            default:
                return new Token(TokenType.UNKNOWN, "Unknown Delimiter", _line);
        }
    }

    private static Token FloatOrInteger()
    {
        string number = "";
        bool isFloat = false;

        while (char.IsDigit(CurrentChar) || CurrentChar == '.')
        {
            if (CurrentChar == '.')
            {
                if (isFloat) break;
                isFloat = true;
                number += '.';
            }
            else
            {
                number += CurrentChar;
            }
            _index++;
        }

        return isFloat ? new Token(TokenType.FLOATLITERAL, number, _line) : new Token(TokenType.INTEGERLITERAL, number, _line);
    }

    private static void SkipComment()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        while (_index < _code.Length && CurrentChar != '\n')
        {
            _index++;
        }

        if (_index < _code.Length && CurrentChar == '\n')
        {
            _index++;
        }
    }

    private static Token ScanEscape()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index + 1;
        int lastClosedBracket = -1;
        _index++;

        while (_index < _code.Length)
        {
            if (CurrentChar == '[')
            {
                if (lastClosedBracket != -1)
                {
                    _index = lastClosedBracket + 1;
                    break;
                }
            }
            else if (CurrentChar == ']')
            {
                lastClosedBracket = _index;
            }

            _index++;

            if (_index == _code.Length && lastClosedBracket != -1)
            {
                _index = lastClosedBracket + 1;
                break;
            }
        }

        if (lastClosedBracket == -1)
        {
            throw new ArgumentException("Unterminated escape sequence");
        }

        string content = _code[start..lastClosedBracket];

        return new Token(TokenType.STRINGLITERAL, content, _line);
    }

    private static Token ScanCharacter()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        if (_code[_index + 2] != '\'')
        {
            if (_code[_index + 1] == '\'')
            {
                throw new ArgumentException($"Error at line {_line}. Empty character literal.");
            }
            throw new ArgumentException($"Error at line {_line}. Invalid or unterminated character literal.");
        }

        _index++;

        string character = _code.Substring(_index, 1);

        _index += 2;

        return new Token(TokenType.CHARACTERLITERAL, character, _line);
    }

    private static Token ScanString()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        _index++;
        while (_index < _code.Length && CurrentChar != '"')
        {
            _index++;
        }
        if (_index == _code.Length)
        {
            throw new ArgumentException("Unterminated string literal");
        }
        _index++;
        string str = _code[(start + 1)..(_index - 1)].ToString();
        if (str.Contains("TRUE") || str.Contains("FALSE")) return str.Contains("TRUE") ? new Token(TokenType.TRUE, str, _line) : new Token(TokenType.FALSE, str, _line);
        return new Token(TokenType.STRINGLITERAL, str, _line);
    }

    private static Token ScanIdentifier()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        while (_index < _code.Length && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            _index++;
        }

        string value = _code[start.._index].ToString();
        if (value == "BEGIN" || value == "END")
        {
            return value == "BEGIN" ? CheckNextWord(TokenType.BEGIN, value) : CheckNextWord(TokenType.END, value);
        }
        if (keywords.TryGetValue(value, out TokenType type))
        {
            return new Token(type, value, _line);
        }
        return new Token(TokenType.IDENTIFIER, value, _line);
    }

    private static bool ScanNextAndPrev()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int peekIndexLeft = _index - 1;
        int peekIndexRight = _index + 1;

        while (peekIndexLeft >= 0 && char.IsWhiteSpace(_code[peekIndexLeft]))
        {
            peekIndexLeft--;
        }

        while (peekIndexRight < _code.Length && char.IsWhiteSpace(_code[peekIndexRight]))
        {
            peekIndexRight++;
        }

        if ((peekIndexLeft >= 0 && _code[peekIndexLeft] == '$') || (peekIndexRight < _code.Length && _code[peekIndexRight] == '$'))
        {
            return true;
        }

        return false;
    }

    private static Token CheckNextWord(TokenType tokenType, string firstWord)
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        while (_index < _code.Length && char.IsWhiteSpace(CurrentChar))
        {
            _index++;
        }

        if (_index == _code.Length)
        {
            return new Token(TokenType.IDENTIFIER, tokenType.ToString(), _line);
        }

        int start = _index;
        while (_index < _code.Length && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            _index++;
        }
        string nextWord = _code[start.._index].ToString();
        string concatenated = firstWord + " " + nextWord;

        switch (nextWord)
        {
            case "CODE":
                if (firstWord.Contains("BEGIN"))
                {
                    return new Token(TokenType.BEGINCODE, concatenated, _line);
                }
                else
                {
                    return new Token(TokenType.ENDCODE, concatenated, _line);
                }

            case "IF":
                if (firstWord.Contains("BEGIN"))
                {
                    return new Token(TokenType.BEGINIF, concatenated, _line);
                }
                else
                {
                    return new Token(TokenType.ENDIF, concatenated, _line);
                }

            case "WHILE":
                if (firstWord.Contains("BEGIN"))
                {
                    return new Token(TokenType.BEGINWHILE, concatenated, _line);
                }
                else
                {
                    return new Token(TokenType.ENDWHILE, concatenated, _line);
                }

            default:
                return firstWord.Contains("BEGIN")  ? new Token(TokenType.BEGIN, firstWord, _line) : new Token(TokenType.END, firstWord, _line);
        }
    }

    private static char PeekChar(int offset)
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int peekIndex = _index + offset;
        if (peekIndex < 0 || peekIndex >= _code.Length)
        {
            return '\0';
        }
        return _code[peekIndex];
    }

    private static int addIndex(char currentChar)
    {
        switch (currentChar)
        {
            case '+':
            case '-':
            case '*':
            case '/':
            case '%':
                if (PeekChar(1) == '=') return 2;
                if (PeekChar(1) == '+' && currentChar == '+') return 2;
                if (PeekChar(1) == '-' && currentChar == '-') return 2;
                return 1;
            case '=':
                if (PeekChar(1) == '=') return 2;
                return 1;
            case '>':
                if (PeekChar(1) == '=') return 2;
                return 1;
            case '<':
                if (PeekChar(1) == '=' || PeekChar(1) == '>') return 2;
                return 1;
            default:
                return 1;
        }
    }
    #endregion HELPER METHODS
}