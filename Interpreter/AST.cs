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

    public abstract class Statement : ASTNode { 
        public int lineNumber { get; }
        protected Statement(int lineNumber){
            this.lineNumber = lineNumber;
        }
    }

    public class EmptyStatement : Statement { 
        public EmptyStatement(int lineNumber) : base(lineNumber) { }
    }

    public class DeclarationStatement : Statement
    {
        public TokenType Type { get; }
        public List<Variable> Variables { get; }

        public DeclarationStatement(TokenType type, List<Variable> variables, int lineNumber) : base(lineNumber)
        {
            Type = type;
            Variables = variables;
        }
    }

    public class AssignmentStatement : Statement
    {
        public Variable Variable { get; }
        public Token Operator { get; }
        public Expression Value { get; }

        public AssignmentStatement(Variable variable, Token operatorToken, Expression value, int lineNumber) : base(lineNumber)
        {
            Variable = variable;
            Operator = operatorToken;
            Value = value;
        }
    }

    public class PostIncrementStatement : Statement
    {
        public Variable Variable { get; }

        public PostIncrementStatement(Variable variable, int lineNumber) : base(lineNumber)
        {
            Variable = variable;
        }
    }

    public class PostDecrementStatement : Statement
    {
        public Variable Variable { get; }

        public PostDecrementStatement(Variable variable, int lineNumber) : base(lineNumber)
        {
            Variable = variable;
        }
    }

    public class IfStatement : Statement
    {
        public Expression Condition { get; }
        public List<Statement> ThenBranch { get; }
        public List<Statement> ElseBranch { get; }

        public IfStatement(Expression condition, List<Statement> thenBranch, List<Statement>? elseBranch, int lineNumber) : base(lineNumber)
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

        public WhileStatement(Expression condition, List<Statement> body, int lineNumber) : base(lineNumber)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class OutputStatement : Statement
    {
        public List<Expression> Expressions { get; }

        public OutputStatement(List<Expression> expressions, int lineNumber) : base(lineNumber)
        {
            Expressions = expressions;
        }
    }

    public class InputStatement : Statement
    {
        public List<Variable> Variables { get; }

        public InputStatement(List<Variable> variables, int lineNumber) : base(lineNumber)
        {
            Variables = variables;
        }
    }

    public class BreakStatement : Statement { 
        public BreakStatement(int lineNumber) : base(lineNumber) { }
    }

    public class ContinueStatement : Statement { 
        public ContinueStatement(int lineNumber) : base(lineNumber) {}
    }

    public abstract class Expression : ASTNode { 
        public int lineNumber;

        protected Expression(int lineNumber){
            this.lineNumber = lineNumber;
        }
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public BinaryExpression(Expression left, Token operatorToken, Expression right, int lineNumber) : base(lineNumber)
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

        public UnaryExpression(Token operatorToken, Expression right, int lineNumber) : base(lineNumber)
        {
            Operator = operatorToken;
            Right = right;
        }
    }

    public class LiteralExpression : Expression
    {
        public object Value { get; }

        public LiteralExpression(object value, int lineNumber) : base(lineNumber)
        {
            Value = value;
        }
    }

    public class VariableExpression : Expression
    {
        public string Name { get; }

        public VariableExpression(string name, int lineNumber) : base(lineNumber)
        {
            Name = name;
        }
    }
    public class Variable
    {
        public string Name { get; }
        public Expression? Initializer { get; }
        public int lineNumber { get; }

        public Variable(string name, int lineNumber, Expression? initializer = null)
        {
            Name = name;
            this.lineNumber = lineNumber;
            Initializer = initializer;
        }
    }

    public class AssignmentExpression : Expression
    {
        public Variable Variable { get; }
        public Token Operator { get; }
        public Expression Value { get; }

        public AssignmentExpression(Variable variable, Token operatorToken, Expression value, int lineNumber) : base(lineNumber)
        {
            Variable = variable;
            Operator = operatorToken;
            Value = value;
        }
    }

    public class LogicalExpression : Expression
    {
        public Expression Left { get; }
        public Token Operator { get; }
        public Expression Right { get; }

        public LogicalExpression(Expression left, Token operatorToken, Expression right, int lineNumber) : base(lineNumber)
        {
            Left = left;
            Operator = operatorToken;
            Right = right;
        }
    }

    public class GroupingExpression : Expression
    {
        public Expression Expression { get; }

        public GroupingExpression(Expression expression, int lineNumber) : base(lineNumber)
        {
            Expression = expression;
        }
    }

    public abstract class FunctionCallExpression : Expression
    {
        public string FunctionName { get; }
        public Expression Argument { get; }

        protected FunctionCallExpression(string functionName, Expression argument, int lineNumber) : base(lineNumber)
        {
            FunctionName = functionName;
            Argument = argument;
        }
    }

    public class CeilExpression : FunctionCallExpression
    {
        public CeilExpression(Expression argument, int lineNumber) : base("CEIL", argument, lineNumber) { }
    }

    public class FloorExpression : FunctionCallExpression
    {
        public FloorExpression(Expression argument, int lineNumber) : base("FLOOR", argument, lineNumber) { }
    }

    public class ToStringExpression : FunctionCallExpression
    {
        public ToStringExpression(Expression argument, int lineNumber) : base("TOSTRING", argument, lineNumber) { }
    }

    public class ToFloatExpression : FunctionCallExpression
    {
        public ToFloatExpression(Expression argument, int lineNumber) : base("TOFLOAT", argument, lineNumber) { }
    }

    public class ToIntExpression : FunctionCallExpression
    {
        public ToIntExpression(Expression argument, int lineNumber) : base("TOINT", argument, lineNumber) { }
    }

    public class TypeExpression : FunctionCallExpression
    {
        public TypeExpression(Expression argument, int lineNumber) : base("TYPE", argument, lineNumber) { }
    }
}
