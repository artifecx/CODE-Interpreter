using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interpreter
{
    public class ExecutionContext
    {
        private readonly Dictionary<string, (object Value, TokenType Type)> variables = new();

        public void DeclareVariable(string name, object value, TokenType type)
        {
            object typedValue = type switch
            {
                TokenType.INT => Convert.ToInt32(value),
                TokenType.FLOAT => Convert.ToSingle(value),
                TokenType.CHAR => Convert.ToChar(value),
                TokenType.BOOL => Convert.ToBoolean(value),
                _ => value
            };

            variables[name] = (typedValue, type);
        }

        public object GetVariable(string name)
        {
            if (variables.TryGetValue(name, out var variable))
            {
                return variable.Value;
            }
            throw new Exception($"Variable '{name}' is not defined.");
        }

        public void SetVariable(string name, object value)
        {
            if (variables.ContainsKey(name))
            {
                var type = variables[name].Type;
                object typedValue = ConvertToType(value, type);
                variables[name] = (typedValue, type);
                return;
            }

            throw new Exception($"Variable '{name}' is not defined.");
        }

        private object ConvertToType(object value, TokenType type)
        {
            switch (type)
            {
                case TokenType.INT: return Convert.ToInt32(value);
                case TokenType.FLOAT: return Convert.ToSingle(value);
                case TokenType.CHAR: return Convert.ToChar(value);
                case TokenType.BOOL: return Convert.ToBoolean(value);
                default: return value;
            }
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
                        var value = variable.Initializer != null ? EvaluateExpression(variable.Initializer) : GetDefaultValue(decl.Type);
                        context.DeclareVariable(variable.Name, value, decl.Type);
                    }
                    break;
                case AssignmentStatement assign:
                    var assignValue = EvaluateExpression(assign.Value);
                    context.SetVariable(assign.Variable.Name, assignValue);
                    break;
                case InputStatement inputStmt:
                    foreach (var variable in inputStmt.Variables)
                    {
                        //Console.WriteLine($"Enter value for {variable.Name}: ");
                        var input = Console.ReadLine();
                        context.SetVariable(variable.Name, input);
                    }
                    break;
                case OutputStatement output:
                    foreach (var expression in output.Expressions)
                    {
                        Console.Write(EvaluateExpression(expression));
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
            if (operatorToken.Type == TokenType.CONCATENATE)
            {
                string leftStr = ConvertToString(left);
                string rightStr = ConvertToString(right);
                return leftStr + rightStr;
            }
            else if (left is int leftInt && right is int rightInt)
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

        private string ConvertToString(object value)
        {
            return value switch
            {
                bool boolValue => boolValue ? "TRUE" : "FALSE",
                null => "",
                _ => value.ToString()
            };
        }

        private object GetDefaultValue(TokenType type)
        {
            return type switch
            {
                TokenType.INT => 0,
                TokenType.FLOAT => 0.0f,
                TokenType.CHAR => '\0',
                TokenType.BOOL => false,
                _ => null
            };
        }
    }
}
