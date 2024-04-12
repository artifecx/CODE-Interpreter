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
            object typedValue;
            try
            {
                typedValue = type switch
                {
                    TokenType.INT => Convert.ToInt32(value),
                    TokenType.FLOAT => Convert.ToSingle(value),
                    TokenType.CHAR => Convert.ToChar(value),
                    TokenType.BOOL => Convert.ToBoolean(value),
                    _ => value
                };
            }
            catch
            {
                throw new Exception($"Type mismatch: Cannot declare '{name}' as {type} with value {value}.");
            }

            if (!IsTypeCompatible(value, type))
            {
                throw new Exception($"Type mismatch: Cannot declare '{name}' as {type} with value {value}.");
            }

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
                var (existingValue, type) = variables[name];

                object typedValue;
                try
                {
                    typedValue = ConvertToType(value, type);
                }
                catch
                {
                    throw new Exception($"Type mismatch: Cannot assign value {value} to '{name}' of type {type}.");
                }

                if (!IsTypeCompatible(value, type))
                {
                    throw new Exception($"Type mismatch: Cannot assign value {value} to '{name}' of type {type}.");
                }

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

        private bool IsTypeCompatible(object value, TokenType type)
        {
            switch (type)
            {
                case TokenType.INT:
                    return int.TryParse(value.ToString(), out _);
                case TokenType.FLOAT:
                    return float.TryParse(value.ToString(), out _);
                case TokenType.CHAR:
                    return value.ToString().Length == 1;
                case TokenType.BOOL:
                    return bool.TryParse(value.ToString(), out _);
                default:
                    return false;
            }
        }
    }

    public class Interpreter
    {
        private readonly ExecutionContext context = new();

        public void Interpret(ProgramNode program)
        {
            try
            {
                if (program == null || program.Statements == null)
                {
                    throw new Exception("No program to interpret.");
                }

                foreach (var statement in program.Statements)
                {
                    ExecuteStatement(statement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                    var variableValue = context.GetVariable(assign.Variable.Name);
                    if (assign.Operator.Type != TokenType.ASSIGNMENT) assignValue = EvaluateBinaryExpression(variableValue, assign.Operator, assignValue);
                    context.SetVariable(assign.Variable.Name, assignValue);
                    break;
                case InputStatement inputStmt:
                    foreach (var variable in inputStmt.Variables)
                    {
                        //Console.WriteLine($"Enter value for {variable.Name}: ");
                        var input = Console.ReadLine();
                        if (input == null || input == "") throw new Exception($"Input is invalid, try again.");
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
                case UnaryExpression unaryExpr:
                    return EvaluateUnaryExpression(unaryExpr);
                case LogicalExpression logExpr:
                    var leftLogic = EvaluateExpression(logExpr.Left);
                    var rightLogic = EvaluateExpression(logExpr.Right);
                    return EvaluateLogicalExpression(leftLogic, logExpr.Operator, rightLogic);
                case AssignmentExpression assignExpr:
                    var value = EvaluateExpression(assignExpr.Value);
                    context.SetVariable(assignExpr.Variable.Name, value);
                    return value;
                case GroupingExpression groupExpr:
                    return EvaluateExpression(groupExpr.Expression);
                case FunctionCallExpression func:
                    return EvaluateFunctionCall(func);
                default:
                    throw new NotImplementedException($"Evaluation not implemented for expression type {expression.GetType().Name}");
            }
        }

        private object EvaluateFunctionCall(FunctionCallExpression func)
        {
            var argumentValue = EvaluateExpression(func.Argument);
            return func switch
            {
                CeilExpression _ => Math.Ceiling(Convert.ToDouble(argumentValue)),
                FloorExpression _ => Math.Floor(Convert.ToDouble(argumentValue)),
                ToStringExpression _ => ConvertToString(argumentValue),
                ToFloatExpression _ => Convert.ToSingle(argumentValue),
                ToIntExpression _ => ConvertToInt(argumentValue),
                TypeExpression _ => EvaluateTypeExpression(argumentValue),
                _ => throw new NotImplementedException($"Function {func.FunctionName} is not implemented.")
            };
        }

        private string EvaluateTypeExpression(object value)
        {
            if (value is int)
                return "INT";
            if (value is float || value is double)
                return "FLOAT";
            if (value is bool)
                return "BOOL";
            if (value is string)
                return "STRING";
            if (value is char)
                return "CHAR";
            if (value is null)
                return "NULL";
            return "UNKNOWN";
        }

        private object EvaluateBinaryExpression(object left, Token operatorToken, object right)
        {
            if (operatorToken.Type == TokenType.CONCATENATE)
            {
                string leftStr = ConvertToString(left);
                string rightStr = ConvertToString(right);
                return leftStr + rightStr;
            }

            left = ConvertIfString(left);
            right = ConvertIfString(right);

            left = left is VariableExpression leftVar ? context.GetVariable(leftVar.Name) : left;
            right = right is VariableExpression rightVar ? context.GetVariable(rightVar.Name) : right;

            bool isLeftFloat = left is float;
            bool isRightFloat = right is float;

            // for PI, not ideal, only holds 7 decimal places
            if (left is double || right is double)
            {
                left = Convert.ToSingle(left);
                right = Convert.ToSingle(right);
            }

            if (left is int && isRightFloat)
            {
                left = Convert.ToSingle(left);
            }
            if (right is int && isLeftFloat)
            {
                right = Convert.ToSingle(right);
            }

            return operatorToken.Type switch
            {
                TokenType.ADD => PerformOperation(left, right, operatorToken.Value),
                TokenType.SUB => PerformOperation(left, right, operatorToken.Value),
                TokenType.MUL => PerformOperation(left, right, operatorToken.Value),
                TokenType.DIV => PerformOperation(left, right, operatorToken.Value),
                TokenType.MOD => PerformOperation(left, right, operatorToken.Value),
                TokenType.GREATERTHAN => PerformOperation(left, right, operatorToken.Value),
                TokenType.LESSERTHAN => PerformOperation(left, right, operatorToken.Value),
                TokenType.GTEQ => PerformOperation(left, right, operatorToken.Value),
                TokenType.LTEQ => PerformOperation(left, right, operatorToken.Value),
                TokenType.EQUAL => PerformOperation(left, right, operatorToken.Value),
                TokenType.NOTEQUAL => PerformOperation(left, right, operatorToken.Value),
                TokenType.ADDASSIGNMENT => PerformOperation(left, right, "+"),
                TokenType.SUBASSIGNMENT => PerformOperation(left, right, "-"),
                TokenType.MULASSIGNMENT => PerformOperation(left, right, "*"),
                TokenType.DIVASSIGNMENT => PerformOperation(left, right, "/"),
                TokenType.MODASSIGNMENT => PerformOperation(left, right, "%"),
                _ => throw new NotImplementedException($"Operator {operatorToken.Type} is not implemented."),
            };
        }

        private object ConvertIfString(object value)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int intValue))
                {
                    return intValue;
                }
                if (float.TryParse(stringValue, out float floatValue))
                {
                    return floatValue;
                }
            }
            return value;
        }

        private object PerformOperation(object left, object right, string operation)
        {
            // Debugging
            //Console.WriteLine($"Performing operation {operation} on {left} {left.GetType()} and {right} {right.GetType()}.");

            if (left is float leftFloat && right is float rightFloat)
            {
                switch (operation)
                {
                    case "+": return leftFloat + rightFloat;
                    case "-": return leftFloat - rightFloat;
                    case "*": return leftFloat * rightFloat;
                    case "/": return leftFloat / rightFloat;
                    case "%": return leftFloat % rightFloat;
                    case "==": return leftFloat == rightFloat;
                    case "<>": return leftFloat != rightFloat;
                    case ">": return leftFloat > rightFloat;
                    case "<": return leftFloat < rightFloat;
                    case ">=": return leftFloat >= rightFloat;
                    case "<=": return leftFloat <= rightFloat;
                }
            }
            if (left is int leftInt && right is int rightInt)
            {
                switch (operation)
                {
                    case "+": return leftInt + rightInt;
                    case "-": return leftInt - rightInt;
                    case "*": return leftInt * rightInt;
                    case "/": return leftInt / rightInt;
                    case "%": return leftInt % rightInt;
                    case "==": return leftInt == rightInt;
                    case "<>": return leftInt != rightInt;
                    case ">": return leftInt > rightInt;
                    case "<": return leftInt < rightInt;
                    case ">=": return leftInt >= rightInt;
                    case "<=": return leftInt <= rightInt;
                }
            }

            throw new Exception($"Invalid operands for operation '{operation}'.");
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

        private object EvaluateUnaryExpression(UnaryExpression expr)
        {
            object right = EvaluateExpression(expr.Right);
            right = EnsureCorrectType(right, expr.Operator.Type);

            return expr.Operator.Type switch
            {
                TokenType.SUB => right switch
                {
                    float rightFloat => -rightFloat,
                    int rightInt => -rightInt,
                    _ => throw new Exception("Unary '-' expects a numeric operand.")
                },
                TokenType.ADD => right switch
                {
                    float rightFloat => rightFloat,
                    int rightInt => rightInt,
                    _ => throw new Exception("Unary '+' expects a numeric operand.")
                },
                TokenType.NOT => right is bool rightBool ? !rightBool : throw new Exception($"{right} Unary 'NOT' expects a boolean operand."),
                TokenType.INCREMENT => right switch
                {
                    int rightInt => ++rightInt,
                    _ => throw new Exception("Can only use increment operator on integers.")
                },
                TokenType.DECREMENT => right switch
                {
                    int rightInt => --rightInt,
                    _ => throw new Exception("Can only use decrement operator on integers.")
                },
                _ => throw new Exception($"Unsupported unary operator {expr.Operator.Type}.")
            };
        }


        private object EnsureCorrectType(object value, TokenType operationType)
        {
            switch (operationType)
            {
                case TokenType.SUB or TokenType.ADD:
                    if (value is string stringValue)
                    {
                        if (float.TryParse(stringValue, out float floatValue))
                        {
                            return floatValue;
                        }
                        if (int.TryParse(stringValue, out int intValue))
                        {
                            return intValue;
                        }
                    }
                    break;
                case TokenType.NOT:
                    if (value is string strValue && bool.TryParse(strValue, out bool boolValue))
                    {
                        return boolValue;
                    }
                    break;
            }
            return value;
        }

        // for function call TOINT, error if trying to convert a float to int if I use Convert.ToInt32()
        private int ConvertToInt(object value)
        {
            if (value is float floatValue)
            {
                return (int)floatValue;
            }
            else if (value is double doubleValue)
            {
                return (int)doubleValue;
            }
            else if (value is string stringValue)
            {
                if (float.TryParse(stringValue, out float result))
                {
                    return (int)result;
                }
                else
                {
                    throw new Exception($"Cannot convert '{stringValue}' to int.");
                }
            }
            else
            {
                return Convert.ToInt32(value);
            }
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
