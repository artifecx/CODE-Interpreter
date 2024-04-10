using System;
using System.Collections.Generic;
using Interpreter;

public class Parser
{
    private readonly List<Token> tokens;
    private readonly HashSet<string> declaredVariables = new HashSet<string>();
    private readonly List<Statement> statements = new List<Statement>();
    private int current = 0;
    private int _current = 0;
    private bool variableDeclarationPhase = true;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    private void EnsureProgramStructure()
    {
        int beginCodeIndex = tokens.FindIndex(token => token.Type == TokenType.BEGINCODE);
        int endCodeIndex = tokens.FindLastIndex(token => token.Type == TokenType.ENDCODE);

        if (beginCodeIndex == -1 || endCodeIndex == -1)
        {
            string missingToken = beginCodeIndex == -1 ? "BEGIN CODE" : "END CODE";
            throw new ParseException($"{missingToken} must exist for the program to run.");
        }

        if (tokens.Count(token => token.Type == TokenType.BEGINCODE) > 1 || tokens.Count(token => token.Type == TokenType.ENDCODE) > 1)
        {
            int duplicateBeginCodeIndex = tokens.FindIndex(beginCodeIndex + 1, token => token.Type == TokenType.BEGINCODE);
            int duplicateEndCodeIndex = tokens.FindIndex(token => token.Type == TokenType.ENDCODE);
            int errorLine = duplicateBeginCodeIndex > -1 ? tokens[duplicateBeginCodeIndex].Line : tokens[duplicateEndCodeIndex].Line;
            string duplicateToken = duplicateBeginCodeIndex > -1 ? "BEGIN CODE" : "END CODE";
            throw new ParseException($"Error at line: {errorLine}. Only one {duplicateToken} should exist. ");
        }
    }

    public ProgramNode Parse()
    {
        //var statements = new List<Statement>();
        try
        {
            EnsureProgramStructure();
            return ParseProgram(statements);
        }
        catch (ParseException ex)
        {
            Console.WriteLine($"{ex.Message}");
            Environment.Exit(1);
            return null;
        }
        //return new ProgramNode(statements ?? new List<Statement>());
    }

    private ProgramNode ParseProgram(List<Statement> statements)
    {
        Consume(TokenType.BEGINCODE, "Expect 'BEGIN CODE' at the start of the program.");
        ConsumeNewlines();

        while (!IsAtEnd() && !Check(TokenType.ENDCODE))
        {
            ConsumeNewlines();
            statements.Add(ParseStatement());
            ConsumeNewlines();
        }

        Consume(TokenType.ENDCODE, "Expect 'END CODE' at the end of the program.");
        return new ProgramNode(statements);
    }

    private Statement ParseStatement()
    {
        if (Match(TokenType.INT, TokenType.CHAR, TokenType.BOOL, TokenType.FLOAT))
        {
            if (!variableDeclarationPhase)
            {
                throw new ParseException($"Error at line: {Peek().Line}. Variable declarations cannot occur after non-variable declaration statements.");
            }
            return ParseDeclarationStatement();
        }

        if (Match(TokenType.IF))
            return ParseIfStatement();

        if (Match(TokenType.WHILE))
            return ParseWhileStatement();

        if (Match(TokenType.DISPLAY))
            return ParseOutputStatement();

        if (Match(TokenType.SCAN))
            return ParseInputStatement();

        if (Check(TokenType.IDENTIFIER) || Check(TokenType.COMMA)) // added comma to parse multiple assignments in one line
        {
            if (statements.Count > 0 && _current - 1 > 0)
            {
                if (Statement(_current - 1) is AssignmentStatement && Previous().Type is not TokenType.NEXTLINE)
                {
                    Consume(TokenType.COMMA, $"Error at line: {Peek().Line}. Expect ',' after an assignment.");
                }
            }

            if (Previous().Type == TokenType.NEXTLINE && variableDeclarationPhase)
            {
                variableDeclarationPhase = false;
            }

            if (!declaredVariables.Contains(Peek().Value) && Check(TokenType.IDENTIFIER))
            {
                throw new ParseException($"Error at line: {Peek().Line}. Undeclared variable '{Peek().Value}'.");
            }
            
            return ParseAssignmentStatement();
        }

        throw new ParseException($"Error at line: {Peek().Line}. Expect statement.");
    }

    private DeclarationStatement ParseDeclarationStatement()
    {
        TokenType type = Previous().Type;
        var variables = new List<Variable>();

        do
        {
            string name = Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Expect variable name.").Value;
            Expression initializer = null;
            if (!declaredVariables.Add(name))
            {
                throw new ParseException($"Error at line: {Peek().Line}. Variable '{name}' already declared.");
            }

            if (Match(TokenType.ASSIGNMENT))
            {
                initializer = ParseExpression();
            }
            variables.Add(new Variable(name, initializer));
            declaredVariables.Add(name);
        } while (Match(TokenType.COMMA));

        _current++;
        return new DeclarationStatement(type, variables);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token name = Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Expect variable name.");
        Consume(TokenType.ASSIGNMENT, $"Error at line: {Peek().Line}. Expect '=' after variable name.");
        Expression value = ParseExpression();

        _current++;
        return new AssignmentStatement(new Variable(name.Value), value);
    }

    private IfStatement ParseIfStatement()
    {
        Consume(TokenType.OPENPARENTHESIS, $"Error at line: {Peek().Line}. Expect '(' after 'IF'.");
        Expression condition = ParseExpression();
        Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after if condition.");

        List<Statement> thenBranch = ParseBlock(TokenType.ENDIF);
        List<Statement> elseBranch = null;
        if (Match(TokenType.ELSE))
        {
            elseBranch = ParseBlock(TokenType.ENDIF);
        }

        _current++;
        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private WhileStatement ParseWhileStatement()
    {
        Consume(TokenType.OPENPARENTHESIS, $"Error at line: {Peek().Line}. Expect '(' after 'WHILE'.");
        Expression condition = ParseExpression();
        Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after condition.");

        List<Statement> body = ParseBlock(TokenType.ENDWHILE);

        _current++;
        return new WhileStatement(condition, body);
    }

    private OutputStatement ParseOutputStatement()
    {
        Consume(TokenType.COLON, $"Error at line: {Peek().Line}. Expected ':' after DISPLAY statement.");

        var expressions = new List<Expression>();
        bool expectConcatOperator = false;

        do
        {
            if (Check(TokenType.NEXTLINE))
            {
                Advance();
                expressions.Add(new LiteralExpression("\n"));
                expectConcatOperator = false;
                continue;
            }

            if (expectConcatOperator)
            {
                if (!Match(TokenType.CONCATENATE))
                {
                    throw new ParseException($"Error at line: {Peek().Line}. Expect '&' for concatenation between expressions.");
                }
                expectConcatOperator = false;
            }

            expressions.Add(ParseExpression());
            expectConcatOperator = true;
        } while (!Check(TokenType.ENDCODE) && !IsAtEnd() && !Peek().Value.Contains("\\n"));

        _current++;
        return new OutputStatement(expressions);
    }

    private InputStatement ParseInputStatement()
    {
        Consume(TokenType.COLON, $"Error at line: {Peek().Line}. Expected ':' after SCAN statement.");

        var variables = new List<Variable>();
        do
        {
            variables.Add(new Variable(Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Variable name for input expected.").Value));
        } while (Match(TokenType.COMMA));

        _current++;
        return new InputStatement(variables);
    }

    private Expression ParseExpression()
    {
        return ParseAssignment();
    }

    private Expression ParseAssignment()
    {
        Expression expr = ParseOr();

        if (Match(TokenType.ASSIGNMENT))
        {
            Token equals = Previous();
            Expression value = ParseAssignment();

            if (expr is VariableExpression)
            {
                String name = ((VariableExpression)expr).Name;
                return new AssignmentExpression(new Variable(name, null), value);
            }
            else
            {
                throw new ParseException($"Error at line: {Peek().Line}. Invalid assignment target.");
            }
        }

        return expr;
    }

    private Expression ParseOr()
    {
        Expression expr = ParseAnd();

        while (Match(TokenType.OR))
        {
            Token operatorToken = Previous();
            Expression right = ParseAnd();
            expr = new LogicalExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseAnd()
    {
        Expression expr = ParseEquality();

        while (Match(TokenType.AND))
        {
            Token operatorToken = Previous();
            Expression right = ParseEquality();
            expr = new LogicalExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseEquality()
    {
        Expression expr = ParseComparison();

        while (Match(TokenType.EQUAL, TokenType.NOTEQUAL))
        {
            Token operatorToken = Previous();
            Expression right = ParseComparison();
            expr = new BinaryExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseComparison()
    {
        Expression expr = ParseAddition();

        while (Match(TokenType.GREATERTHAN, TokenType.LESSERTHAN, TokenType.GTEQ, TokenType.LTEQ))
        {
            Token operatorToken = Previous();
            Expression right = ParseAddition();
            expr = new BinaryExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseAddition()
    {
        Expression expr = ParseMultiplication();

        while (Match(TokenType.ADD, TokenType.SUB, TokenType.CONCATENATE))
        {
            Token operatorToken = Previous();
            Expression right = ParseMultiplication();
            expr = new BinaryExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseMultiplication()
    {
        Expression expr = ParseUnary();

        while (Match(TokenType.MUL, TokenType.DIV, TokenType.MOD))
        {
            Token operatorToken = Previous();
            Expression right = ParseUnary();
            expr = new BinaryExpression(expr, operatorToken, right);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.SUB))
        {
            Token operatorToken = Previous();
            Expression right = ParseUnary();
            return new UnaryExpression(operatorToken, right);
        }
        if (Match(TokenType.NOT))
        {
            Token operatorToken = Previous();
            Expression right = ParseExpression();
            return new UnaryExpression(operatorToken, right);
        }

        return ParsePrimary();
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenType.FALSE)) return new LiteralExpression(false);
        if (Match(TokenType.TRUE)) return new LiteralExpression(true);
        if (Match(TokenType.INTEGERLITERAL, TokenType.FLOATLITERAL, TokenType.STRINGLITERAL, TokenType.CHARACTERLITERAL))
        {
            return new LiteralExpression(Previous().Value);
        }
        if (Match(TokenType.IDENTIFIER))
        {
            return new VariableExpression(Previous().Value);
        }
        if (Match(TokenType.OPENPARENTHESIS))
        {
            Expression expr = ParseExpression();
            Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after expression.");
            return new GroupingExpression(expr);
        }

        throw new ParseException($"Error at line: {Peek().Line}. Expect expression.");
    }

    private List<Statement> ParseBlock(TokenType endToken)
    {
        List<Statement> statements = new List<Statement>();

        while (!Check(endToken) && !IsAtEnd())
        {
            statements.Add(ParseStatement());
        }

        Consume(endToken, $"Error at line: {Peek().Line}. Expect '{endToken}' after block.");

        return statements;
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private Token Consume(TokenType type, string errorMessage)
    {
        if (Check(type)) return Advance();

        throw new ParseException(errorMessage);
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd()) current++;
        return Previous();
    }

    private bool IsAtEnd()
    {
        return Peek().Type == TokenType.EOF;
    }

    private Token Peek()
    {
        return tokens[current];
    }

    private Token Previous()
    {
        return tokens[current - 1];
    }

    private void ConsumeNewlines()
    {
        while (Match(TokenType.NEXTLINE)) { }
    }

    private Statement Statement(int index)
    {
        try
        {
            return statements[index];
        }
        catch (Exception ex)
        {
            throw new ParseException($"Error at line: {Peek().Line}. {ex.Message}");
        }
    }
}

public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
