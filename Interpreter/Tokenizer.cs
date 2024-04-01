using System;
using System.Collections.Generic;

public enum TokenType
{
    DATATYPE, IDENTIFIER, KEYWORD, PUNCTUATION, COMMENT,
    NUMBER, STRING, CHARACTER, BOOL,
    OPERATOR, OPERATOR_ARITHMETIC, OPERATOR_LOGICAL, OPERATOR_UNARY,
    CODEBLOCK, IFBLOCK, WHILEBLOCK,
    NEWLINE, EOF,
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
                    tokens.Add(new Token(TokenType.NEWLINE, "\\n"));
                    _index++;
                    break;

                case '$':
                    tokens.Add(new Token(TokenType.NEWLINE, "$"));
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
                    tokens.Add(IsUnaryOperator(tokens) ? new Token(TokenType.OPERATOR_UNARY, currentChar.ToString()) : new Token(TokenType.OPERATOR_ARITHMETIC, currentChar.ToString()));
                    _index++;
                    break;

                case '&':
                    tokens.Add(new Token(TokenType.OPERATOR, currentChar.ToString()));
                    _index++;
                    break;

                case '<':
                case '>':
                    TokenType operatorType = TokenType.OPERATOR_ARITHMETIC;

                    if (currentChar == '<' && PeekChar(1) == '=')
                    {
                        tokens.Add(new Token(operatorType, "<="));
                        _index += 2;
                    }
                    else if (currentChar == '>' && PeekChar(1) == '=')
                    {
                        tokens.Add(new Token(operatorType, ">="));
                        _index += 2;
                    }
                    else if (PeekChar(1) == '>')
                    {
                        tokens.Add(new Token(operatorType, "<>"));
                        _index += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(operatorType, currentChar.ToString()));
                        _index++;
                    }
                    break;

                case '=':
                    TokenType eqOperatorType = TokenType.OPERATOR;

                    if (PeekChar(1) == '=')
                    {
                        tokens.Add(new Token(TokenType.OPERATOR_ARITHMETIC, "=="));
                        _index += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(eqOperatorType, "="));
                        _index++;
                    }
                    break;

                case '(':
                case ')':
                case ',':
                case ':':
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

    private static bool IsUnaryOperator(List<Token> tokens)
    {
        if (_index == 0)
        {
            return true;
        }

        Token previousToken = tokens[^1];
        if (previousToken.Type == TokenType.PUNCTUATION && previousToken.Value == "(")
        {
            return true;
        }
        else if (previousToken.Type == TokenType.OPERATOR_ARITHMETIC || previousToken.Type == TokenType.OPERATOR_LOGICAL)
        {
            return true;
        }

        return false;
    }

    private static char CurrentChar =>  _code != null ? _code[_index] : throw new InvalidOperationException("_code is null");

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

    private static Token ScanIdentifierOrKeyword()
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
        string identifier = _code[start.._index].ToString();

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
                return new Token(TokenType.OPERATOR_LOGICAL, identifier);

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
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        while (_index < _code.Length && (char.IsDigit(CurrentChar) || CurrentChar == '.'))
        {
            _index++;
        }
        string number = _code[start.._index].ToString();
        return new Token(TokenType.NUMBER, number);
    }

    private static Token ScanEscape()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        int lastClosedBracket = -1;

        while (_index < _code.Length)
        {
            if (CurrentChar == '[')
            {
                // If a new opening bracket is encountered, terminate the escape sequence
                if (lastClosedBracket != -1)
                {
                    _index = lastClosedBracket + 1;
                    break;
                }
            }
            else if (CurrentChar == ']')
            {
                lastClosedBracket = _index; // Update the index of the last closed bracket
            }

            _index++;

            if (_index == _code.Length && lastClosedBracket != -1)
            {
                break;
            }
        }

        if (lastClosedBracket == -1)
        {
            throw new ArgumentException("Unterminated escape code");
        }

        string str = _code[(start + 1)..lastClosedBracket].ToString();
        return new Token(TokenType.STRING, str);
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
        if (str.Contains("TRUE") || str.Contains("FALSE")) return new Token(TokenType.BOOL, str);
        return new Token(TokenType.STRING, str);
    }

    private static Token ScanCharacter()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        _index++;
        while (_index < _code.Length && CurrentChar != '\'')
        {
            _index++;
        }
        if (_index == _code.Length)
        {
            throw new ArgumentException("Unterminated character literal");
        }
        _index++;
        string character = _code[(start + 1)..(_index - 1)].ToString();
        return new Token(TokenType.CHARACTER, character);
    }

    private static Token ScanComment()
    {
        if (_code == null)
        {
            throw new InvalidOperationException("_code is null");
        }

        int start = _index;
        while (_index < _code.Length && CurrentChar != '\n')
        {
            _index++;
        }
        string comment = _code[(start + 1).._index].ToString();
        return new Token(TokenType.COMMENT, comment);
    }
}
