using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using Interpreter;

public class Parser
{
    private readonly List<Token> tokens;
    private readonly Dictionary<string, TokenType> declaredVariables = new Dictionary<string, TokenType>();
    private readonly List<Statement> statements = new List<Statement>();
    private int current = 0;
    private bool variableDeclarationPhase = true;
    private bool insideLoop = false;
    private bool inDisplayContext = false;
    private bool inConditionalContext = false;
    private bool inIfBlock = false;

    private Dictionary<TokenType, Func<Expression, FunctionCallExpression>> functionMap =
        new Dictionary<TokenType, Func<Expression, FunctionCallExpression>>
        {
            { TokenType.CEIL, arg => new CeilExpression(arg, arg.lineNumber) },
            { TokenType.FLOOR, arg => new FloorExpression(arg, arg.lineNumber) },
            { TokenType.TOSTRING, arg => new ToStringExpression(arg, arg.lineNumber) },
            { TokenType.TOFLOAT, arg => new ToFloatExpression(arg, arg.lineNumber) },
            { TokenType.TOINT, arg => new ToIntExpression(arg, arg.lineNumber) },
            { TokenType.TYPE, arg => new TypeExpression(arg, arg.lineNumber) }
        };

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    public ProgramNode Parse()
    {
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
    }

    private void EnsureProgramStructure()
    {
        int beginCodeIndex = tokens.FindIndex(token => token.Type == TokenType.BEGINCODE);
        int endCodeIndex = tokens.FindLastIndex(token => token.Type == TokenType.ENDCODE);
        
        ConsumeNewlines();

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

        if (beginCodeIndex > 0 || endCodeIndex < tokens.Count - 1)
        {
            if (beginCodeIndex > 0)
            {
                var outOfBoundsToken = tokens.Take(beginCodeIndex).FirstOrDefault(token => token.Type != TokenType.COMMENT && token.Type != TokenType.NEXTLINE);
                if (outOfBoundsToken != null)
                {
                    throw new ParseException($"Error at line: {outOfBoundsToken.Line}. Invalid code or tokens outside BEGIN CODE.");
                }
            }
            if (endCodeIndex < tokens.Count - 1)
            {
                var outOfBoundsToken = tokens.Skip(endCodeIndex + 1).FirstOrDefault(token => token.Type != TokenType.COMMENT && token.Type != TokenType.NEXTLINE && token.Type != TokenType.EOF);
                if (outOfBoundsToken != null)
                {
                    throw new ParseException($"Error at line: {outOfBoundsToken.Line}. Invalid code or tokens after END CODE.");
                }
            }
        }
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
        if (Match(TokenType.INT, TokenType.CHAR, TokenType.BOOL, TokenType.FLOAT, TokenType.STRING))
        {
            if (!variableDeclarationPhase)
            {
                throw new ParseException($"Error at line: {Peek().Line}. Variable declarations cannot occur after non-variable declaration statements.");
            }
            return ParseDeclarationStatement();
        }

        if (Match(TokenType.IF)) return ParseIfStatement();
        if (Match(TokenType.WHILE)) return ParseWhileStatement();
        if (Match(TokenType.DISPLAY)) return ParseOutputStatement();
        if (Match(TokenType.SCAN)) return ParseInputStatement();
        if (Match(TokenType.BREAK)) return ParseBreakStatement();
        if (Match(TokenType.CONTINUE)) return ParseContinueStatement();

        if (Check(TokenType.IDENTIFIER) || Check(TokenType.COMMA)) // added comma to parse multiple assignments in one line
        {
            if (statements.Count > 0 && statements.Count - 1 > 0)
            {
                if (Statement(statements.Count - 1) is AssignmentStatement && Previous().Type is not TokenType.NEXTLINE)
                {
                    Consume(TokenType.COMMA, $"Error at line: {Peek().Line}. Expect ',' after an assignment.");
                }
            }

            if (Previous().Type == TokenType.NEXTLINE && variableDeclarationPhase)
            {
                variableDeclarationPhase = false;
            }

            if (!declaredVariables.ContainsKey(Peek().Value) && Check(TokenType.IDENTIFIER))
            {
                throw new ParseException($"Error at line: {Peek().Line}. Undeclared variable '{Peek().Value}'.");
            }

            if (Match(TokenType.IDENTIFIER))
            {
                Token identifierToken = Previous();
                if (Match(TokenType.INCREMENT) || Match(TokenType.DECREMENT))
                {
                    TokenType operationType = Previous().Type;
                    return operationType == TokenType.INCREMENT
                           ? new PostIncrementStatement(new Variable(identifierToken.Value, Previous().Line), Previous().Line)
                           : new PostDecrementStatement(new Variable(identifierToken.Value, Previous().Line), Previous().Line);
                }
                current--;
            }
            
            return ParseAssignmentStatement();
        }
        if (Check(TokenType.UNKNOWN))
        {
            throw new ParseException($"Error at line: {Peek().Line}. Unknown character '{Peek().Value}'");
        }

        // allow empty statements in if-else blocks
        if (inIfBlock) return new EmptyStatement(Peek().Line);

        throw new ParseException($"Error at line: {Peek().Line}. Invalid statement. Cause: '{Peek().Value}'");
    }

    private DeclarationStatement ParseDeclarationStatement()
    {
        TokenType type = Previous().Type;
        var variables = new List<Variable>();

        do
        {
            if (IsReservedKeyword(Peek().Value))
            {
                throw new ParseException($"Error at line: {Peek().Line}. Reserved keyword '{Peek().Value}' cannot be used as a variable name.");
            }

            string name = Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Invalid variable name starting with '{Peek().Value}'.").Value;
            if (declaredVariables.ContainsKey(name))
            {
                throw new ParseException($"Error at line: {Peek().Line}. Variable '{name}' already declared.");
            }

            Expression initializer = null;
            if (Match(TokenType.ASSIGNMENT))
            {
                initializer = ParseExpression();
            }

            switch (type)
            {
                /*case TokenType.CHAR:
                    if (!(initializer is LiteralExpression literal && literal.Value is char))
                    {
                        throw new ParseException($"Error at line: {Peek().Line}. CHAR variable '{name}' must be assigned a character enclosed in single quotes. Found: {Previous().Type} {Previous().Value}");
                    }
                    break;*/
                case TokenType.BOOL:
                    if (initializer is LiteralExpression lit && lit.Value is string)
                    {
                        string boolValue = lit.Value.ToString();
                        if (boolValue != "TRUE" && boolValue != "FALSE")
                        {
                            throw new ParseException($"Error at line: {Peek().Line}. Boolean values must be either 'TRUE' or 'FALSE' in all caps and enclosed in double quotes. Found: {boolValue}");
                        }
                    }
                    break;
            }

            variables.Add(new Variable(name, Previous().Line, initializer));
            declaredVariables.Add(name, type);
        } while (Match(TokenType.COMMA));

        if(Peek().Type == TokenType.IDENTIFIER)
        {
            throw new ParseException($"Error at line: {Peek().Line}. Expected comma for multiple declarations on one line.");
        }

        if(Match(TokenType.INT, TokenType.CHAR, TokenType.BOOL, TokenType.FLOAT, TokenType.STRING))
        {
            throw new ParseException($"Error at line: {Peek().Line}. Improper declaration. Move '{Previous().Value} {Peek().Value}' to a new line or replace '{Previous().Value}' with a comma.");
        }

        return new DeclarationStatement(type, variables, Peek().Line);
    }

    private AssignmentStatement ParseAssignmentStatement()
    {
        Token name = Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Expect variable name.");
        if (IsReservedKeyword(name.Value))
        {
            throw new ParseException($"Error at line: {Peek().Line}. '{name}' is a reserved keyword and cannot be used as a variable name.");
        }

        List<TokenType> assignmentType = new List<TokenType> {
            TokenType.ASSIGNMENT, TokenType.ADDASSIGNMENT, TokenType.SUBASSIGNMENT, 
            TokenType.MULASSIGNMENT, TokenType.DIVASSIGNMENT, TokenType.MODASSIGNMENT
        };

        Token operatorToken = null;
        for (int i = 0; i < assignmentType.Count; i++)
        {
            if (Check(assignmentType[i]))
            {
                operatorToken = Peek();
                Advance();
                break;
            }
            if (i == assignmentType.Count - 1)
            {
                throw new ParseException($"Error at line: {Peek().Line}. Expect proper assignment operator after variable name.");
            }
        }

        if (IsReservedKeyword(Peek().Value) && Peek().Type == TokenType.IDENTIFIER)
        {
            throw new ParseException($"Error at line: {Peek().Line}. Cannot assign reserved keyword '{Peek().Value}' to variable '{name.Value}'. Enclose boolean literals in double quotes.");
        }

        //Consume(TokenType.ASSIGNMENT, $"Error at line: {Peek().Line}. Expect '=' after variable name.");
        Expression value = ParseExpression();

        if (name.Type == TokenType.IDENTIFIER && value is LiteralExpression literal && literal.Value is string)
        {
            if (GetVariableType(name.Value) == TokenType.BOOL) { 
                string boolValue = literal.Value.ToString();
                if (boolValue != "TRUE" && boolValue != "FALSE")
                {
                    throw new ParseException($"Error at line: {Peek().Line}. Boolean values must be either 'TRUE' or 'FALSE' in all caps. Found: {boolValue}");
                }
            }

            if (GetVariableType(name.Value) == TokenType.CHAR) {
                throw new ParseException($"Error at line: {Peek().Line}. Cannot assign a string literal to character variable '{Previous().Value}'. Use single quotes for characters.");
            }
        }

        return new AssignmentStatement(new Variable(name.Value, value.lineNumber), operatorToken, value, value.lineNumber);
    }

    private IfStatement ParseIfStatement()
    {
        variableDeclarationPhase = false;

        Consume(TokenType.OPENPARENTHESIS, $"Error at line: {Peek().Line}. Expect '(' after 'IF'.");
        inConditionalContext = true;
        Expression condition = ParseExpression();
        inConditionalContext = false;
        Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after if condition.");
        Advance();

        Consume(TokenType.BEGINIF, $"Error at line: {Peek().Line}. Expect 'BEGIN IF'.");
        inIfBlock = true;
        List<Statement> thenBranch = ParseBlock(TokenType.ENDIF);
        List<Statement> elseBranch = null;

        if (Match(TokenType.ELSE))
        {
            ConsumeNewlines();
            if (Match(TokenType.IF))
            {
                elseBranch = new List<Statement> { ParseIfStatement() };
            }
            else
            {
                Consume(TokenType.BEGINIF, $"Error at line: {Peek().Line}. Expect 'BEGIN IF' after 'ELSE'.");
                elseBranch = ParseBlock(TokenType.ENDIF);
            }
        }
        inIfBlock = false;
        return new IfStatement(condition, thenBranch, elseBranch, condition.lineNumber);
    }

    private WhileStatement ParseWhileStatement()
    {
        variableDeclarationPhase = false;

        Consume(TokenType.OPENPARENTHESIS, $"Error at line: {Peek().Line}. Expect '(' after 'WHILE'.");
        inConditionalContext = true;
        Expression condition = ParseExpression();
        inConditionalContext = false;
        Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after condition.");
        Advance();

        //Console.WriteLine($"Current: {Peek().Value}, Previous: {Previous().Value}");

        Consume(TokenType.BEGINWHILE, $"Error at line: {Peek().Line}. Expect 'BEGIN WHILE'.");
        insideLoop = true;
        List<Statement> body = ParseBlock(TokenType.ENDWHILE);
        insideLoop = false;

        return new WhileStatement(condition, body, condition.lineNumber);
    }

    private List<Statement> ParseBlock(TokenType endToken)
    {
        List<Statement> statements = new List<Statement>();

        while (!Check(endToken) && !IsAtEnd() && !Check(TokenType.ENDCODE))
        {
            ConsumeNewlines();
            statements.Add(ParseStatement());
            ConsumeNewlines();

            //Console.WriteLine("Statement added: " + statements[statements.Count-1]);
        }

        Consume(endToken, $"Error at line: {Peek().Line}. Expect '{endToken}' after block.");
        ConsumeNewlines();

        // Debugging
        //Console.WriteLine($"Current: {Peek().Value}, Previous: {Previous().Value}");
        return statements;
    }

    private OutputStatement ParseOutputStatement()
    {
        variableDeclarationPhase = false;
        inDisplayContext = true;

        Consume(TokenType.COLON, $"Error at line: {Peek().Line}. Expected ':' after DISPLAY statement.");

        var expressions = new List<Expression>();
        bool expectConcatOperator = false;

        if ((Check(TokenType.NEXTLINE) && Peek().Value != "$") || Check(TokenType.ENDCODE))
        {
            throw new ParseException($"Error at line: {Peek().Line}. Nothing to display.");
        }

        do
        {
            if (Check(TokenType.NEXTLINE))
            {
                Advance();
                expressions.Add(new LiteralExpression("\n", Previous().Line));
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

        if (!declaredVariables.ContainsKey(Previous().Value) && Previous().Type == TokenType.IDENTIFIER)
        {
            throw new ParseException($"Error at line: {Peek().Line}. Variable '{Previous().Value}' is not declared.");
        }

        inDisplayContext = false;
        return new OutputStatement(expressions, Peek().Line);
    }

    private InputStatement ParseInputStatement()
    {
        variableDeclarationPhase = false;

        Consume(TokenType.COLON, $"Error at line: {Peek().Line}. Expected ':' after SCAN statement.");

        var variables = new List<Variable>();
        do
        {
            if(!declaredVariables.ContainsKey(Peek().Value))
            {
                if (IsReservedKeyword(Peek().Value))
                {
                    throw new ParseException($"Error at line: {Peek().Line}. '{Peek().Value}' is a reserved keyword and cannot be used as a variable name.");
                }
                throw Peek().Type == TokenType.IDENTIFIER ? new ParseException($"Error at line: {Peek().Line}. Variable '{Peek().Value}' is not declared.") : new ParseException($"Error at line: {Peek().Line}. Invalid variable '{Peek().Value}'");
            }

            variables.Add(new Variable(Consume(TokenType.IDENTIFIER, $"Error at line: {Peek().Line}. Variable name for input expected.").Value, Previous().Line));
        } while (Match(TokenType.COMMA));

        if (Check(TokenType.IDENTIFIER)) throw new ParseException($"Error at line: {Peek().Line}. Expect comma between variables, received '{Peek().Value}'.");

        return new InputStatement(variables, Peek().Line);
    }

    private Statement ParseBreakStatement()
    {
        if (!insideLoop)
            throw new ParseException($"Error at line: {Peek().Line}. BREAK not inside a loop.");
        return new BreakStatement(Peek().Line);
    }

    private Statement ParseContinueStatement()
    {
        if (!insideLoop)
            throw new ParseException($"Error at line: {Peek().Line}. CONTINUE not inside a loop.");
        return new ContinueStatement(Peek().Line);
    }

    private Expression ParseExpression()
    {
        return ParseAssignment();
    }

    private Expression ParseAssignment()
    {
        Expression expr = ParseOr();

        if (Match(TokenType.ASSIGNMENT, TokenType.ADDASSIGNMENT, TokenType.SUBASSIGNMENT,
            TokenType.MULASSIGNMENT, TokenType.DIVASSIGNMENT, TokenType.MODASSIGNMENT))
        {
            if (inConditionalContext && Previous().Type == TokenType.ASSIGNMENT)
            {
                throw new ParseException($"Error at line: {Peek().Line}. Cannot use assignment operator '=' within conditional statements.");
            }

            Token equals = Previous();
            Expression value = ParseAssignment();

            if (expr is VariableExpression)
            {
                String name = ((VariableExpression)expr).Name;
                return new AssignmentExpression(new Variable(name, value.lineNumber, null), equals, value, equals.Line);
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
            expr = new LogicalExpression(expr, operatorToken, right, operatorToken.Line);
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
            expr = new LogicalExpression(expr, operatorToken, right, operatorToken.Line);
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
            expr = new BinaryExpression(expr, operatorToken, right, operatorToken.Line);
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
            expr = new BinaryExpression(expr, operatorToken, right, operatorToken.Line);
        }

        return expr;
    }

    private Expression ParseAddition()
    {
        Expression expr = ParseMultiplication();

        while (Match(TokenType.ADD, TokenType.SUB, TokenType.CONCATENATE))
        {
            if(Previous().Type == TokenType.CONCATENATE && !inDisplayContext)
            {
                throw new ParseException($"Error at line: {Peek().Line}. Cannot perform concatenation '&' outside DISPLAY statement.");
            }
            Token operatorToken = Previous();
            Expression right = ParseMultiplication();
            expr = new BinaryExpression(expr, operatorToken, right, operatorToken.Line);
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
            expr = new BinaryExpression(expr, operatorToken, right, operatorToken.Line);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenType.SUB, TokenType.ADD))
        {
            Token operatorToken = Previous();
            Expression right = ParseUnary();
            return new UnaryExpression(operatorToken, right, operatorToken.Line);
        }
        if (Match(TokenType.NOT))
        {
            Token operatorToken = Previous();
            Expression right = ParseUnary();
            return new UnaryExpression(operatorToken, right, operatorToken.Line);
        }

        return ParsePrimary();
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenType.FALSE)) return new LiteralExpression(false, Previous().Line);
        if (Match(TokenType.TRUE)) return new LiteralExpression(true, Previous().Line);
        if (Match(TokenType.INTEGERLITERAL)) return new LiteralExpression(int.Parse(Previous().Value), Previous().Line);
        if (Match(TokenType.FLOATLITERAL)) return new LiteralExpression(float.Parse(Previous().Value), Previous().Line);
        if (Match(TokenType.CHARACTERLITERAL)) return new LiteralExpression(Convert.ToChar(Previous().Value), Previous().Line);
        if (Match(TokenType.STRINGLITERAL)) return new LiteralExpression(Previous().Value, Previous().Line);
        
        if (Match(TokenType.PI)) return new LiteralExpression(Math.PI, Previous().Line);

        if (functionMap.ContainsKey(Peek().Type) && Match(Peek().Type))
        {
            return ParseFunctionCall();
        }

        if (Match(TokenType.IDENTIFIER))
        {
            if (Check(TokenType.OPENPARENTHESIS))
            {
                return ParseFunctionCall();
            }

            VariableExpression varExpr = new VariableExpression(Previous().Value, Previous().Line);
            if (Match(TokenType.INCREMENT) || Match(TokenType.DECREMENT))
            {
                Token operatorToken = Previous();
                return new UnaryExpression(operatorToken, varExpr, Previous().Line);
            }

            return new VariableExpression(Previous().Value, Previous().Line);
        }

        if (Match(TokenType.OPENPARENTHESIS))
        {
            Expression expr = ParseExpression();
            Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after expression.");
            return new GroupingExpression(expr, Peek().Line);
        }

        throw Match(TokenType.UNKNOWN) ? new ParseException($"Error at line: {Peek().Line}. Unknown character '{Previous().Value}'") : new ParseException($"Error at line: {Peek().Line}. Invalid/empty expression.");
    }

    private Expression ParseFunctionCall()
    {
        Token functionName = Previous();
        Consume(TokenType.OPENPARENTHESIS, $"Error at line: {Peek().Line}. Expect '(' after function name. Found: '{Peek().Value}'.");

        if (Check(TokenType.CLOSEPARENTHESIS))
        {
            throw new ParseException($"Error at line {Peek().Line}: Function '{functionName.Value}' expects an argument.");
        }

        Expression argument = ParseExpression();
        Consume(TokenType.CLOSEPARENTHESIS, $"Error at line: {Peek().Line}. Expect ')' after argument. Found: '{Peek().Value}'.");

        if (functionMap.TryGetValue(functionName.Type, out var constructor))
        {
            return constructor(argument);
        }
        else
        {
            throw new ParseException($"Error at line: {Peek().Line}. Unsupported function '{functionName.Value}'.");
        }
    }


    #region HELPER METHODS
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

    private bool IsReservedKeyword(string name)
    {
        return Lexer.keywords.ContainsKey(name);
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

    public TokenType GetVariableType(string variableName)
    {
        if (declaredVariables.TryGetValue(variableName, out TokenType type))
        {
            return type;
        }
        throw new ParseException($"Error at line: {Peek().Line}. Undeclared variable '{variableName}'.");
    }
    #endregion HELPER METHODS
}