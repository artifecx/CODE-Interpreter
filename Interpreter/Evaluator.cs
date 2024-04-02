using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    public class ExecutionContext
    {
        private readonly Dictionary<string, object> variables = new();

        public void DeclareVariable(string name, object value)
        {
            // Temporary will change later
            if (value is string stringValue && int.TryParse(stringValue, out int intValue))
            {
                variables[name] = intValue; // Store as int
            }
            else
            {
                variables[name] = value; // Store as is
            }
        }

        public object GetVariable(string name)
        {
            if (variables.TryGetValue(name, out var value))
            {
                return value;
            }

            throw new Exception($"Variable '{name}' is not defined.");
        }

        public void SetVariable(string name, object value)
        {
            if (variables.ContainsKey(name))
            {
                variables[name] = value;
                return;
            }

            throw new Exception($"Variable '{name}' is not defined.");
        }
    }

    public class Interpreter
    {
        private readonly ExecutionContext context = new();

        public void Interpret(ProgramNode program)
        {
            if (program == null || program.Statements == null)
            {
                Console.WriteLine("No program to interpret.");
                return;
            }

            foreach (var statement in program.Statements)
            {
                ExecuteStatement(statement);
            }
        }

        private void ExecuteStatement(Statement statement)
        {
            switch (statement)
            {
                case DeclarationStatement decl:
                    foreach (var variable in decl.Variables)
                    {
                        var value = variable.Initializer != null ? EvaluateExpression(variable.Initializer) : default;
                        context.DeclareVariable(variable.Name, value);
                    }
                    break;
                case AssignmentStatement assign:
                    var assignValue = EvaluateExpression(assign.Value);
                    context.SetVariable(assign.Variable.Name, assignValue);
                    break;
                case InputStatement inputStmt:
                    foreach (var variable in inputStmt.Variables)
                    {
                        Console.WriteLine($"Enter value for {variable.Name}: ");
                        var input = Console.ReadLine();
                        context.DeclareVariable(variable.Name, input);
                    }
                    break;
                case OutputStatement output:
                    foreach (var expression in output.Expressions)
                    {
                        Console.WriteLine(EvaluateExpression(expression));
                    }
                    break;
                case IfStatement ifStmt:
                    bool condition = (bool)EvaluateExpression(ifStmt.Condition);
                    if (condition)
                    {
                        foreach (var thenStmt in ifStmt.ThenBranch)
                        {
                            ExecuteStatement(thenStmt);
                        }
                    }
                    else
                    {
                        foreach (var elseStmt in ifStmt.ElseBranch)
                        {
                            ExecuteStatement(elseStmt);
                        }
                    }
                    break;
                case WhileStatement whileStmt:
                    while ((bool)EvaluateExpression(whileStmt.Condition))
                    {
                        foreach (var bodyStmt in whileStmt.Body)
                        {
                            ExecuteStatement(bodyStmt);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException($"Execution not implemented for statement type {statement.GetType().Name}");
            }
        }

        private object EvaluateExpression(Expression expression)
        {
            switch (expression)
            {
                case LiteralExpression lit:
                    return lit.Value;
                case VariableExpression varExpr:
                    return context.GetVariable(varExpr.Name);
                case BinaryExpression binExpr:
                    var left = EvaluateExpression(binExpr.Left);
                    var right = EvaluateExpression(binExpr.Right);
                    return EvaluateBinaryExpression(left, binExpr.Operator, right);
                case LogicalExpression logExpr:
                    var leftLogic = EvaluateExpression(logExpr.Left);
                    var rightLogic = EvaluateExpression(logExpr.Right);
                    return EvaluateLogicalExpression(leftLogic, logExpr.Operator, rightLogic);
                default:
                    throw new NotImplementedException($"Evaluation not implemented for expression type {expression.GetType().Name}");
            }
        }

        private object EvaluateBinaryExpression(object left, Token operatorToken, object right)
        {
            if (left is int leftInt && right is int rightInt)
            {
                return operatorToken.Type switch
                {
                    TokenType.ADD => leftInt + rightInt,
                    TokenType.SUB => leftInt - rightInt,
                    TokenType.MUL => leftInt * rightInt,
                    TokenType.DIV => leftInt / rightInt,
                    _ => throw new NotImplementedException($"Operator {operatorToken.Type} is not implemented.")
                };
            }
            else if (left is float leftFloat && right is float rightFloat)
            {
                switch (operatorToken.Type)
                {
                    case TokenType.ADD: return leftFloat + rightFloat;
                    case TokenType.SUB: return leftFloat - rightFloat;
                    case TokenType.MUL: return leftFloat * rightFloat;
                    case TokenType.DIV: return leftFloat / rightFloat;
                }
            }
            else if (left is string leftStr && right is string rightStr && operatorToken.Type == TokenType.CONCATENATE)
            {
                return leftStr + rightStr;
            }

            throw new Exception("Invalid operands for binary expression.");
        }

        private object EvaluateLogicalExpression(object left, Token operatorToken, object right)
        {
            if (left is bool leftBool && right is bool rightBool)
            {
                return operatorToken.Type switch
                {
                    TokenType.AND => leftBool && rightBool,
                    TokenType.OR => leftBool || rightBool,
                    _ => throw new NotImplementedException($"Logical operator {operatorToken.Type} is not implemented."),
                };
            }

            throw new Exception("Invalid operands for logical expression.");
        }
    }
}
