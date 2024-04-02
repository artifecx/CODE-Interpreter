using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    public abstract class ASTNode { }

    public class ProgramNode : ASTNode
    {
        public List<Statement> Statements { get; }

        public ProgramNode(List<Statement> statements)
        {
            Statements = statements;
        }
    }

    public abstract class Statement : ASTNode { }

    public class DeclarationStatement : Statement
    {
        public TokenType Type { get; }
        public List<Variable> Variables { get; }

        public DeclarationStatement(TokenType type, List<Variable> variables)
        {
            Type = type;
            Variables = variables;
        }
    }

    public class AssignmentStatement : Statement
    {
        public Variable Variable { get; }
        public Expression Value { get; }

        public AssignmentStatement(Variable variable, Expression value)
        {
            Variable = variable;
            Value = value;
        }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> ThenBranch { get; }
        public List<Statement> ElseBranch { get; }

        public IfStatement(Expression condition, List<Statement> thenBranch, List<Statement>? elseBranch = null)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch ?? new List<Statement>();
        }
    }

    public class WhileStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> Body { get; }

        public WhileStatement(Expression condition, List<Statement> body)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class OutputStatement : Statement
    {
        public List<Expression> Expressions { get; }

        public OutputStatement(List<Expression> expressions)
        {
            Expressions = expressions;
        }
    }

    public class InputStatement : Statement
    {
        public List<Variable> Variables { get; }

        public InputStatement(List<Variable> variables)
        {
            Variables = variables;
        }
    }

    public abstract class Expression : ASTNode { }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(Expression left, Token operatorToken, Expression right)
        {
            Left = left;
            Operator = operatorToken;
            Right = right;
        }
    }

    public class UnaryExpression : Expression
    {
        public Token Operator { get; }
        public Expression Right { get; }

        public UnaryExpression(Token operatorToken, Expression right)
        {
            Operator = operatorToken;
            Right = right;
        }
    }

    public class LiteralExpression : Expression
    {
        public object Value { get; }

        public LiteralExpression(object value)
        {
            Value = value;
        }
    }

    public class VariableExpression : Expression
    {
        public string Name { get; }

        public VariableExpression(string name)
        {
            Name = name;
        }
    }
    public class Variable
    {
        public string Name { get; }
        public Expression? Initializer { get; }

        public Variable(string name, Expression? initializer = null)
        {
            Name = name;
            Initializer = initializer;
        }
    }

    public class AssignmentExpression : Expression
    {
        public Variable Variable { get; }
        public Expression Value { get; }

        public AssignmentExpression(Variable variable, Expression value)
        {
            Variable = variable;
            Value = value;
        }
    }

    public class LogicalExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public LogicalExpression(Expression left, Token operatorToken, Expression right)
        {
            Left = left;
            Operator = operatorToken;
            Right = right;
        }
    }

    public class GroupingExpression : Expression
    {
        public Expression Expression { get; }

        public GroupingExpression(Expression expression)
        {
            Expression = expression;
        }
    }
}
