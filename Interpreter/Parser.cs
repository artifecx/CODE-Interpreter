using System;
using System.Collections.Generic;

public class ASTNode
{
    public string Type { get; set; }
    public string Value { get; set; }
    public List<ASTNode> Children { get; set; }

    public ASTNode(string type, string value)
    {
        Type = type;
        Value = value;
        Children = new List<ASTNode>();
    }

    public void AddChild(ASTNode child)
    {
        Children.Add(child);
    }
}

public class Parser
{
    private List<Token> tokens;
    private int currentTokenIndex;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
        currentTokenIndex = 0;
    }

    public ASTNode Parse()
    {
        ASTNode rootNode = new ASTNode("Program", "");

        while (currentTokenIndex < tokens.Count)
        {
            ASTNode statementNode = ParseStatement();
            if (statementNode != null)
            {
                rootNode.AddChild(statementNode);
            }
        }

        return rootNode;
    }

    private ASTNode ParseStatement()
    {
        Token currentToken = GetCurrentToken();

        if (currentToken.Type == TokenType.IDENTIFIER)
        {
            // Parse variable declaration or assignment
            ASTNode assignmentNode = ParseAssignment();
            return assignmentNode;
        }
        else if (currentToken.Type == TokenType.KEYWORD)
        {
            // Parse other statements based on keyword
            string keyword = currentToken.Value;
            switch (keyword)
            {
                case "if":
                    return ParseIfStatement();
                case "switch":
                    return ParseSwitchStatement();
                case "while":
                    return ParseWhileLoop();
                case "for":
                    return ParseForLoop();
                default:
                    // Handle unrecognized keywords
                    throw new NotImplementedException($"Parsing for keyword '{keyword}' is not implemented.");
            }
        }

        // If no token is consumed, increment the index to avoid an infinite loop
        ConsumeToken();

        return null;
    }

    private ASTNode ParseIfStatement()
    {
        ConsumeToken(); // Consume 'if'
        ASTNode ifNode = new ASTNode("IfStatement", "");

        // Parse condition
        ASTNode conditionNode = ParseExpression();
        ifNode.AddChild(conditionNode);

        // Parse body of if statement
        while (GetCurrentToken().Type != TokenType.NEWLINE)
        {
            ASTNode statementNode = ParseStatement();
            if (statementNode != null)
            {
                ifNode.AddChild(statementNode);
            }
        }

        return ifNode;
    }

    private ASTNode ParseSwitchStatement()
    {
        // Implement parsing for switch-case statements
        return null;
    }

    private ASTNode ParseWhileLoop()
    {
        // Implement parsing for while loop
        return null;
    }

    private ASTNode ParseForLoop()
    {
        // Implement parsing for for loop
        return null;
    }

    private ASTNode ParseAssignment()
    {
        Token currentToken = GetCurrentToken();
        ASTNode assignmentNode = new ASTNode("Assignment", currentToken.Value);
        ConsumeToken(); // Consume identifier

        if (GetCurrentToken().Value == "=")
        {
            ConsumeToken(); // Consume "="
            ASTNode expressionNode = ParseExpression();
            assignmentNode.AddChild(expressionNode);
        }

        return assignmentNode;
    }

    private ASTNode ParseExpression()
    {
        // Very simple expression parsing for demonstration purposes
        Token currentToken = GetCurrentToken();
        return new ASTNode("Expression", currentToken.Value);
    }

    private Token GetCurrentToken()
    {
        return tokens[currentTokenIndex];
    }

    private void ConsumeToken()
    {
        currentTokenIndex++;
    }
}
