using System;
using System.Collections.Generic;
using Interpreter;

public class Parser
{
    private readonly List<Token> tokens;
    private int current = 0;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    public ProgramNode Parse()
    {
        var statements = new List<Statement>();
        try
        {
            return ParseProgram(statements);
        }
        catch (ParseException ex)
        {
            Console.WriteLine($"Parse error: {ex.Message}");
        }
        return new ProgramNode(statements ?? new List<Statement>());
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
            return ParseDeclarationStatement();

        if (Match(TokenType.IF))
            return ParseIfStatement();

        if (Match(TokenType.WHILE))
            return ParseWhileStatement();

        if (Match(TokenType.DISPLAY))
            return ParseOutputStatement();

        if (Match(TokenType.SCAN))
            return ParseInputStatement();

        if (Check(TokenType.IDENTIFIER))
            return ParseAssignmentStatement();

        throw new ParseException("Expect statement.");
    }

    private DeclarationStatement ParseDeclarationStatement()
    {
        TokenType type = Previous().Type;
        var variables = new List<Variable>();

        do
        {
            string name = Consume(TokenType.IDENTIFIER, "Expect variable name.").Value;
            Expression initializer = null;
            if (Match(TokenType.ASSIGNMENT))
            {
                initializer = ParseExpression();
            }
            variables.Add(new Variable(name, initializer));
        } while (Match(TokenType.COMMA));

        return new DeclarationStatement(type, variables);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token name = Consume(TokenType.IDENTIFIER, "Expect variable name.");
        Consume(TokenType.ASSIGNMENT, "Expect '=' after variable name.");
        Expression value = ParseExpression();
        return new AssignmentStatement(new Variable(name.Value), value);
    }

    private IfStatement ParseIfStatement()
    {
        Consume(TokenType.OPENPARENTHESIS, "Expect '(' after 'IF'.");
        Expression condition = ParseExpression();
        Consume(TokenType.CLOSEPARENTHESIS, "Expect ')' after if condition.");

        List<Statement> thenBranch = ParseBlock(TokenType.ENDIF);
        List<Statement> elseBranch = null;
        if (Match(TokenType.ELSE))
        {
            elseBranch = ParseBlock(TokenType.ENDIF);
        }

        return new IfStatement(condition, thenBranch, elseBranch);
    }

    private WhileStatement ParseWhileStatement()
    {
        Consume(TokenType.OPENPARENTHESIS, "Expect '(' after 'WHILE'.");
        Expression condition = ParseExpression();
        Consume(TokenType.CLOSEPARENTHESIS, "Expect ')' after condition.");

        List<Statement> body = ParseBlock(TokenType.ENDWHILE);

        return new WhileStatement(condition, body);
    }

    private OutputStatement ParseOutputStatement()
    {
        Match(TokenType.COLON);

        var expressions = new List<Expression>();

        while (!Check(TokenType.NEXTLINE) && !Check(TokenType.ENDCODE) && !IsAtEnd())
        {
            expressions.Add(ParseExpression());
        }

        return new OutputStatement(expressions);
    }

    private InputStatement ParseInputStatement()
    {
        Match(TokenType.COLON);

        var variables = new List<Variable>();
        do
        {
            variables.Add(new Variable(Consume(TokenType.IDENTIFIER, "Expect variable name.").Value));
        } while (Match(TokenType.COMMA));

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

            if (expr is VariableExpression varExpr)
            {
                string name = varExpr.Name;
                return new AssignmentExpression(new Variable(name), value);
            }

            throw new ParseException("Invalid assignment target.");
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

        while (Match(TokenType.ADD, TokenType.SUB))
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
        if (Match(TokenType.NOT, TokenType.SUB))
        {
            Token operatorToken = Previous();
            Expression right = ParseUnary();
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
            Consume(TokenType.CLOSEPARENTHESIS, "Expect ')' after expression.");
            return new GroupingExpression(expr);
        }

        throw new ParseException("Expect expression.");
    }


    private List<Statement> ParseBlock(TokenType endToken)
    {
        List<Statement> statements = new List<Statement>();

        while (!Check(endToken) && !IsAtEnd())
        {
            statements.Add(ParseStatement());
        }

        Consume(endToken, $"Expect '{endToken}' after block.");

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

}

public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
