using System;
using System.Collections.Generic;

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
    POSITIVE,           // +2
    NEGATIVE,           // -2

    // Delimiters
    NEXTLINE,           // $
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
    EOF                 // End of File
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }
}

public class Lexer
{
    private static int _index = 0;
    private static string? _code;

    private static readonly Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>
    {
        {"BEGIN", TokenType.BEGIN},
        {"END", TokenType.END},

        {"CODE", TokenType.CODE},
        {"IF", TokenType.IF},
        {"ELSE", TokenType.ELSE},
        {"WHILE", TokenType.WHILE},
        {"DISPLAY", TokenType.DISPLAY},
        {"SCAN", TokenType.SCAN},

        {"INT", TokenType.INT},
        {"CHAR", TokenType.CHAR},
        {"BOOL", TokenType.BOOL},
        {"FLOAT", TokenType.FLOAT},

        {"AND", TokenType.AND},
        {"OR", TokenType.OR},
        {"NOT", TokenType.NOT},  
    };

    public static List<Token> Tokenize(string code)
    {
        _code = code;
        _index = 0;
        var tokens = new List<Token>();

        while (_index < _code.Length)
        {
            char currentChar = CurrentChar;

            switch (currentChar)
            {
                case '\n':
                    tokens.Add(new Token(TokenType.NEXTLINE, "\\n"));
                    _index++;
                    break;

                case ' ':
                case '\t':
                case '\r':
                    _index++;
                    break;

                #region OPERATORS
                case '=':
                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '>':
                case '<':
                case '&':
                    tokens.Add(HandleOperator(currentChar, tokens));
                    _index +=  (currentChar == '=' || currentChar == '>' || currentChar == '<') && (PeekChar(1) == '=' || PeekChar(1) == '>') ? 2 : 1;
                    break;

                #endregion OPERATORS

                #region DELIMITERS
                case '$':
                case ':':
                case ',':
                case '(':
                case ')':
                    tokens.Add(HandleDelimiter(currentChar));
                    _index++;
                    break;

                case '#':
                    SkipComment();
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
                        throw new ArgumentException($"Unexpected character at index {_index}: {currentChar}");
                    }
                    break;
            }
        }

        tokens.Add(new Token(TokenType.EOF, "END OF LINE"));
        foreach (var token in tokens)
        {
            Console.WriteLine($"Token: {token.Type}, Value: '{token.Value}'");
        }
        return tokens;
    }

    #region HELPER METHODS

    private static char CurrentChar => _code != null ? _code[_index] : throw new InvalidOperationException("_code is null");

    private static Token HandleOperator(char currentChar, List<Token> _tokens)
    {
        switch (currentChar)
        {
            case '=':
                return PeekChar(1) == '=' ? new Token(TokenType.EQUAL, "==") : new Token(TokenType.ASSIGNMENT, "=");
            case '+':
                return IsUnaryOperator(_tokens) ? new Token(TokenType.POSITIVE, "+") : new Token(TokenType.ADD, "+");
            case '-':
                return IsUnaryOperator(_tokens) ? new Token(TokenType.NEGATIVE, "-") : new Token(TokenType.SUB, "-");
            case '*':
                return new Token(TokenType.MUL, "*");
            case '/':
                return new Token(TokenType.DIV, "/");
            case '%':
                return new Token(TokenType.MOD, "%");
            case '>':
                return PeekChar(1) == '=' ? new Token(TokenType.GTEQ, ">=") : new Token(TokenType.GREATERTHAN, ">");
            case '<':
                if (PeekChar(1) == '=') return new Token(TokenType.LTEQ, "<=");
                if (PeekChar(1) == '>') return new Token(TokenType.NOTEQUAL, "<>");
                return new Token(TokenType.LESSERTHAN, "<");
            case '&':
                return new Token(TokenType.CONCATENATE, "&");
            default:
                return new Token(TokenType.UNKNOWN, "Unknown Operator");
        }
    }

    private static Token HandleDelimiter(char currentChar)
    {
        switch (currentChar)
        {
            case '$':
                return new Token(TokenType.NEXTLINE, "$");
            case ':':
                return new Token(TokenType.COLON, ":");
            case ',':
                return new Token(TokenType.COMMA, ",");
            case '(':
                return new Token(TokenType.OPENPARENTHESIS, "(");
            case ')':
                return new Token(TokenType.CLOSEPARENTHESIS, ")");
            default:
                return new Token(TokenType.UNKNOWN, "Unknown Delimiter");
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

        return isFloat ? new Token(TokenType.FLOATLITERAL, number) : new Token(TokenType.INTEGERLITERAL, number);
    }

    private static bool IsUnaryOperator(List<Token> tokens)
    {
        if (_index == 0)
        {
            return true;
        }

        Token previousToken = tokens[^1];
        if (previousToken.Type == TokenType.OPENPARENTHESIS)
        {
            return true;
        } 
        if (previousToken.Type == TokenType.IDENTIFIER)
        {
            return false;
        }
        if(previousToken.Type == TokenType.AND || previousToken.Type == TokenType.OR || previousToken.Type == TokenType.NOT)
        {
            return true;
        }
        if (new[] { '=', '+', '-', '*', '/', '%', '>', '<' }.Any(c => previousToken.Value.Contains(c)))
        {
            return true;
        }

        return false;
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

        return new Token(TokenType.STRINGLITERAL, content);
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
        if(value == "BEGIN" || value == "END")
        {
            return value == "BEGIN" ? CheckNextWord(TokenType.BEGIN, value) : CheckNextWord(TokenType.END, value);
        }
        if (keywords.TryGetValue(value.ToUpper(), out TokenType type))
        {
            return new Token(type, value);
        }
        return new Token(TokenType.IDENTIFIER, value);
    }

    private static Token ScanCharacter()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        if (_index + 2 >= _code.Length || _code[_index + 2] != '\'')
        {
            throw new ArgumentException("Invalid or unterminated character literal");
        }

        int start = _index;
        _index++;

        string character = _code.Substring(_index, 1);

        _index += 2;

        return new Token(TokenType.CHARACTERLITERAL, character);
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
        if (str.Contains("TRUE") || str.Contains("FALSE")) return str.Contains("TRUE") ? new Token(TokenType.TRUE, str)  : new Token(TokenType.FALSE, str);
        return new Token(TokenType.STRINGLITERAL, str);
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
            return new Token(TokenType.IDENTIFIER, tokenType.ToString());
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
                    return new Token(TokenType.BEGINCODE, concatenated);
                }
                else
                {
                    return new Token(TokenType.ENDCODE, concatenated);
                }

            case "IF":
                if (firstWord.Contains("BEGIN"))
                {
                    return new Token(TokenType.BEGINIF, concatenated);
                }
                else
                {
                    return new Token(TokenType.ENDIF, concatenated);
                }

            case "WHILE":
                if (firstWord.Contains("BEGIN"))
                {
                    return new Token(TokenType.BEGINWHILE, concatenated);
                }
                else
                {
                    return new Token(TokenType.ENDWHILE, concatenated);
                }

            default:
                throw new Exception($"Unidentified {firstWord} statement.");
        }
    }
    #endregion HELPER METHODS
}