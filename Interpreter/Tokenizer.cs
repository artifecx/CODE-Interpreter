using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public enum TokenType
{
    DATATYPE, IDENTIFIER, NUMBER, STRING, CHARACTER, BOOL,
    OPERATOR, OPERATOR_ARITHMETIC, OPERATOR_LOGICAL, OPERATOR_UNARY, 
    PUNCTUATION, COMMENT, NEWLINE, EOF, KEYWORD, CODEBLOCK, IFBLOCK, WHILEBLOCK
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
    private static string _code;

    private static char CurrentChar => _code[_index];
    private static bool EndOfFile => _index >= _code.Length;

    public static List<Token> Tokenize(string code)
    {
        _code = code;
        _index = 0;
        var tokens = new List<Token>();

        while (!EndOfFile)
        {
            char currentChar = CurrentChar;

            switch (currentChar)
            {
                case '\n':
                    tokens.Add(new Token(TokenType.NEWLINE, "\\n"));
                    _index++;
                    break;

                case ' ':
                case '\t':
                case '\r':
                    _index++;
                    break;

                case '+':
                case '-':
                case '*':
                case '/':
                case '%':
                case '&':
                    tokens.Add(new Token(TokenType.OPERATOR, currentChar.ToString()));
                    _index++;
                    break;

                case '<':
                    if (!EndOfFile && _code[_index + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, "<="));
                        _index += 2;
                    }
                    else if(!EndOfFile && _code[_index + 1] == '>')
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, "<>"));
                        _index += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, "<"));
                        _index++;
                    }
                    break;

                case '>':
                    if (!EndOfFile && _code[_index + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, ">="));
                        _index += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, ">"));
                        _index++;
                    }
                    break;

                case '=':
                    if (!EndOfFile && _code[_index + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, "=="));
                        _index += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.OPERATOR, "="));
                        _index++;
                    }
                    break;

                case '(':
                case ')':
                case '{':
                case '}':
                case ',':
                case '.':
                case ';':
                case ':':
                case '$':
                    tokens.Add(new Token(TokenType.PUNCTUATION, currentChar.ToString()));
                    _index++;
                    break;

                case '[':
                    tokens.Add(ScanEscape());
                    break;

                case '"':
                    tokens.Add(ScanString());
                    break;

                case '\'':
                    tokens.Add(ScanCharacter());
                    break;

                case '#':
                    tokens.Add(ScanComment());
                    break;

                default:
                    if (char.IsLetter(currentChar) || currentChar == '_')
                    {
                        tokens.Add(ScanIdentifierOrKeyword());
                    }
                    else if (char.IsDigit(currentChar))
                    {
                        tokens.Add(ScanNumber());
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected character at index {_index}: {currentChar}");
                    }
                    break;
            }
        }

        tokens.Add(new Token(TokenType.EOF, "END LINE"));
        return tokens;
    }

    private static Token ScanIdentifierOrKeyword()
    {
        int start = _index;
        while (!EndOfFile && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            _index++;
        }
        string identifier = _code.Substring(start, _index - start);

        // Check if the identifier is a keyword
        switch (identifier)
        {
            case "IF":
            case "ELSE":
            case "DISPLAY":
            case "SCAN":
                return new Token(TokenType.KEYWORD, identifier);

            case "AND":
            case "OR":
            case "NOT":
                return new Token(TokenType.OPERATOR, identifier);

            case "BEGIN":
                return CheckNextWord(TokenType.CODEBLOCK, "BEGIN");

            case "END":
                return CheckNextWord(TokenType.CODEBLOCK, "END");

            case "INT":
            case "CHAR":
            case "BOOL":
            case "FLOAT":
                return new Token(TokenType.DATATYPE, identifier);

            default:
                return new Token(TokenType.IDENTIFIER, identifier);
        }
    }

    private static Token CheckNextWord(TokenType tokenType, string firstWord)
    {
        while (!EndOfFile && char.IsWhiteSpace(CurrentChar))
        {
            _index++;
        }

        if (EndOfFile)
        {
            return new Token(TokenType.IDENTIFIER, tokenType.ToString());
        }

        int start = _index;
        while (!EndOfFile && (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_'))
        {
            _index++;
        }
        string nextWord = _code.Substring(start, _index - start);

        switch (nextWord.ToUpper())
        {
            case "CODE":
                return new Token(TokenType.CODEBLOCK, firstWord + " " + nextWord);

            case "IF":
                return new Token(TokenType.IFBLOCK, firstWord + " " + nextWord);

            case "WHILE":
                return new Token(TokenType.WHILEBLOCK, firstWord + " " + nextWord);

            default:
                throw new Exception($"Unidentified {firstWord} statement.");
        }
    }

    private static Token ScanNumber()
    {
        int start = _index;
        while (!EndOfFile && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
        {
            _index++;
        }
        string number = _code.Substring(start, _index - start);
        return new Token(TokenType.NUMBER, number);
    }

    private static Token ScanEscape()
    {
        int start = _index;
        int lastClosedBracket = -1;

        while (!EndOfFile)
        {
            if (CurrentChar == '[')
            {
                // If a new opening bracket is encountered, terminate the escape sequence
                if (lastClosedBracket != -1)
                {
                    _index = lastClosedBracket+1;
                    break;
                }
            }
            else if (CurrentChar == ']')
            {
                lastClosedBracket = _index; // Update the index of the last closed bracket
            }

            _index++;

            if (EndOfFile && lastClosedBracket != -1)
            {
                break;
            }
        }

        if (lastClosedBracket == -1)
        {
            throw new ArgumentException("Unterminated escape code");
        }

        string str = _code.Substring(start + 1, lastClosedBracket - start - 1);
        return new Token(TokenType.STRING, str);
    }

    private static Token ScanString()
    {
        int start = _index;
        _index++;
        while (!EndOfFile && CurrentChar != '"')
        {
            _index++;
        }
        if (EndOfFile)
        {
            throw new ArgumentException("Unterminated string literal");
        }
        _index++;
        string str = _code.Substring(start+1, _index - start-2);
        if (str.Contains("TRUE") || str.Contains("FALSE")) return new Token(TokenType.BOOL, str);
        return new Token(TokenType.STRING, str);
    }

    private static Token ScanCharacter()
    {
        int start = _index;
        _index++; // Skip the opening single quote
        while (!EndOfFile && CurrentChar != '\'')
        {
            _index++;
        }
        if (EndOfFile)
        {
            throw new ArgumentException("Unterminated character literal");
        }
        _index++; // Skip the closing single quote
        string character = _code.Substring(start+1, _index - start-2);
        return new Token(TokenType.CHARACTER, character);
    }

    private static Token ScanComment()
    {
        int start = _index;
        while (!EndOfFile && CurrentChar != '\n')
        {
            _index++;
        }
        string comment = _code.Substring(start+1, _index - start);
        return new Token(TokenType.COMMENT, comment);
    }
}
