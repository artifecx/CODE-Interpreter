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

        public void DeclareVariable(string name, object value, TokenType type, int lineNumber)
        {
            object typedValue;
            try
            {
                typedValue = type switch
                {
                    TokenType.INT => InterpreterClass.ConvertToInt(value, lineNumber),
                    TokenType.FLOAT => Convert.ToSingle(value),
                    TokenType.CHAR => Convert.ToChar(value),
                    TokenType.BOOL => Convert.ToBoolean(value),
                    _ => value
                };

                if (!IsTypeCompatible(typedValue, type))
                {
                    throw new InvalidOperationException($"Error at line: {lineNumber}. Incompatible type after conversion.");
                }

                variables[name] = (typedValue, type);
            }
            catch
            {
                string valueString = InterpreterClass.ConvertToString(value);
                string actualType = RetrieveType(value).ToString();
                throw new Exception($"Error at line: {lineNumber}. Type mismatch: Cannot declare '{name}' as {type} with value '{valueString}' type '{actualType}'.");
            }
        }

        public object GetVariable(string name, int lineNumber)
        {
            if (variables.TryGetValue(name, out var variable))
            {
                return variable.Value;
            }
            throw Lexer.keywords.TryGetValue(name, out var _type) ? throw new Exception($"Error at line: {lineNumber}. Invalid use of reserved keyword '{name}'.") :  new Exception($"Variable '{name}' is not defined.");
        }

        public void SetVariable(string name, object value, int lineNumber)
        {
            if (variables.ContainsKey(name))
            {
                var (existingValue, type) = variables[name];
                object typedValue;

                try
                {
                    typedValue = ConvertToType(value, type, lineNumber);

                    if (!IsTypeCompatible(typedValue, type))
                    {
                        throw new InvalidOperationException($"Error at line: {lineNumber}. Incompatible type after conversion.");
                    }

                    variables[name] = (typedValue, type);
                }
                catch
                {
                    string valueString = InterpreterClass.ConvertToString(value);
                    string actualType = RetrieveType(value).ToString();
                    throw new Exception($"Error at line: {lineNumber}. Type mismatch: Cannot assign value '{valueString}' type '{actualType}' to '{name}' type {type}.");
                }

                return;
            }

            throw Lexer.keywords.TryGetValue(name, out var _type) ? throw new Exception($"Error at line: {lineNumber}. Invalid use of reserved keyword '{name}'.") : new Exception($"Variable '{name}' is not defined.");
        }

        private object ConvertToType(object value, TokenType type, int lineNumber)
        {
            switch (type)
            {
                case TokenType.INT: return InterpreterClass.ConvertToInt(value, lineNumber);
                case TokenType.FLOAT: return Convert.ToSingle(value);
                case TokenType.CHAR: return Convert.ToChar(value);
                case TokenType.BOOL: return Convert.ToBoolean(value);
                case TokenType.STRING: return value.ToString();
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
                case TokenType.STRING:
                    return value is string;
                default:
                    return false;
            }
        }

        private object RetrieveType(object value)
        {
            return value switch
            {
                int _ => "INT",
                float _ => "FLOAT",
                char _ => "CHAR",
                bool _ => "BOOLEAN",
                string _ => "STRING",
                _ => value.GetType().Name.ToUpper()
            };
        }
    }

    public class InterpreterClass
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
                Console.Clear(); // :D
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
                return;
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
                        context.DeclareVariable(variable.Name, value, decl.Type, variable.lineNumber);
                    }
                    break;
                case AssignmentStatement assign:
                    var assignValue = EvaluateExpression(assign.Value);
                    var variableValue = context.GetVariable(assign.Variable.Name, assign.lineNumber);
                    if (assign.Operator.Type != TokenType.ASSIGNMENT) assignValue = EvaluateBinaryExpression(variableValue, assign.Operator, assignValue);
                    context.SetVariable(assign.Variable.Name, assignValue, assign.lineNumber);
                    break;
                case PostIncrementStatement postIncrement:
                    IncrementVariable(postIncrement.Variable);
                    break;
                case PostDecrementStatement postDecrement:
                    DecrementVariable(postDecrement.Variable);
                    break;
                case InputStatement inputStmt:
                    foreach (var variable in inputStmt.Variables)
                    {
                        //Console.WriteLine($"Enter value for {variable.Name}: ");
                        var input = Console.ReadLine();
                        if (input == null || input == "") throw new Exception($"Error at line: {inputStmt.lineNumber}. Input is invalid, try again.");
                        context.SetVariable(variable.Name, input, variable.lineNumber);
                    }
                    break;
                case OutputStatement output:
                    foreach (var expression in output.Expressions) Console.Write(ConvertToString(EvaluateExpression(expression)));
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
                        try { 
                            foreach (var bodyStmt in whileStmt.Body)
                            {
                                ExecuteStatement(bodyStmt);
                            }
                        }
                        catch (ContinueException)
                        {
                            continue;
                        }
                        catch (BreakException)
                        {
                            break;
                        }
                    }
                    break;
                case BreakStatement:
                    throw new BreakException();
                case ContinueStatement:
                    throw new ContinueException();
                case EmptyStatement:
                    break;
                default:
                    throw new NotImplementedException($"Error at line: {statement.lineNumber}. Execution not implemented for statement type {statement.GetType().Name}");
            }
        }

        private object EvaluateExpression(Expression expression)
        {
            switch (expression)
            {
                case LiteralExpression lit:
                    return lit.Value;
                case VariableExpression varExpr:
                    return context.GetVariable(varExpr.Name, varExpr.lineNumber);
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
                    context.SetVariable(assignExpr.Variable.Name, value, assignExpr.lineNumber);
                    return value;
                case GroupingExpression groupExpr:
                    return EvaluateExpression(groupExpr.Expression);
                case FunctionCallExpression func:
                    return EvaluateFunctionCall(func);
                default:
                    throw new NotImplementedException($"Error at line: {expression.lineNumber}. Evaluation not implemented for expression type {expression.GetType().Name}");
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
                ToIntExpression _ => ConvertToInt(argumentValue, func.lineNumber),
                TypeExpression _ => EvaluateTypeExpression(argumentValue),
                _ => throw new NotImplementedException($"Error at line: {func.lineNumber}. Function {func.FunctionName} is not implemented.")
            };
        }

        private string EvaluateTypeExpression(object value)
        {
            return value switch
            {
                int _ => "INT",
                float _ => "FLOAT",
                double _ => "FLOAT",
                bool _ => "BOOL",
                string _ => "STRING",
                char _ => "CHAR",
                null => "NULL",
                _ => "UNKNOWN"
            };
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

            left = left is VariableExpression leftVar ? context.GetVariable(leftVar.Name, leftVar.lineNumber) : left;
            right = right is VariableExpression rightVar ? context.GetVariable(rightVar.Name, rightVar.lineNumber) : right;

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
                TokenType.ADD => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.SUB => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.MUL => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.DIV => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.MOD => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.GREATERTHAN => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.LESSERTHAN => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.GTEQ => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.LTEQ => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.EQUAL => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.NOTEQUAL => PerformOperation(left, right, operatorToken.Value, operatorToken.Line),
                TokenType.ADDASSIGNMENT => PerformOperation(left, right, "+", operatorToken.Line),
                TokenType.SUBASSIGNMENT => PerformOperation(left, right, "-", operatorToken.Line),
                TokenType.MULASSIGNMENT => PerformOperation(left, right, "*", operatorToken.Line),
                TokenType.DIVASSIGNMENT => PerformOperation(left, right, "/", operatorToken.Line),
                TokenType.MODASSIGNMENT => PerformOperation(left, right, "%", operatorToken.Line),
                _ => throw new NotImplementedException($"Error at line: {operatorToken.Line}. Operator {operatorToken.Type} is not implemented."),
            };
        }

        private object EvaluateLogicalExpression(object left, Token operatorToken, object right)
        {
            if (left is bool leftBool && right is bool rightBool)
            {
                return operatorToken.Type switch
                {
                    TokenType.AND => leftBool && rightBool,
                    TokenType.OR => leftBool || rightBool,
                    _ => throw new NotImplementedException($"Error at line: {operatorToken.Line}. Logical operator {operatorToken.Type} is not implemented."),
                };
            }

            throw new Exception($"Error at line: {operatorToken.Line}. Invalid operands for logical expression. Found: '{left}' and '{right}'");
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
                    _ => throw new Exception($"Error at line: {expr.lineNumber}. Unary '-' expects a numeric operand.")
                },
                TokenType.ADD => right switch
                {
                    float rightFloat => rightFloat,
                    int rightInt => rightInt,
                    _ => throw new Exception($"Error at line: {expr.lineNumber}. Unary '+' expects a numeric operand.")
                },
                TokenType.NOT => right is bool rightBool ? !rightBool : throw new Exception($"Error at line: {expr.lineNumber}. Unary 'NOT' expects a boolean operand. Found: '{right}'"),
                TokenType.INCREMENT => right switch
                {
                    int rightInt => HandleIntegerOverflow(rightInt, 1, "+", expr.lineNumber),
                    _ => throw new Exception($"Error at line: {expr.lineNumber}. Can only use increment operator on integers.")
                },
                TokenType.DECREMENT => right switch
                {
                    int rightInt => HandleIntegerOverflow(rightInt, 1, "-", expr.lineNumber),
                    _ => throw new Exception($"Error at line: {expr.lineNumber}. Can only use decrement operator on integers.")
                },
                _ => throw new Exception($"Error at line: {expr.lineNumber}. Unsupported unary operator {expr.Operator.Type}.")
            };
        }

        #region HELPER METHODS
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

        private object PerformOperation(object left, object right, string operation, int lineNumber)
        {
            // Debugging
            //Console.WriteLine($"Performing operation {operation} on {left} {left.GetType()} and {right} {right.GetType()}.");

            if (left is float leftFloat && right is float rightFloat)
            {
                if (rightFloat == 0 && (operation == "/" || operation == "%")) throw new Exception($"Error at line: {lineNumber}. Error: Division by zero.");
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
                if (rightInt == 0 && (operation == "/" || operation == "%")) throw new Exception($"Error at line: {lineNumber}. Error: Division by zero.");
                switch (operation)
                {
                    case "+": return HandleIntegerOverflow(leftInt, rightInt, operation, lineNumber);
                    case "-": return HandleIntegerOverflow(leftInt, rightInt, operation, lineNumber);
                    case "*": return HandleIntegerOverflow(leftInt, rightInt, operation, lineNumber);
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

            if ((left is char leftChar && right is char rightChar) && (operation == "==" || operation == "<>"))
            {
                return operation == "==" ? left.Equals(right) : !left.Equals(right);
            }
            if ((left is string leftString && right is string rightString) && (operation == "==" || operation == "<>"))
            {
                return operation == "==" ? leftString.Equals(rightString) : !leftString.Equals(rightString);
            }
            if ((left is bool leftBool && right is bool rightBool) && (operation == "==" || operation == "<>"))
            {
                return operation == "==" ? leftBool == rightBool : leftBool != rightBool;
            }

            throw new Exception($"Error at line: {lineNumber}.Invalid operands for operation '{operation}'. Found: '{left}' and '{right}'.");
        }

        private void IncrementVariable(Variable variable)
        {
            object value = context.GetVariable(variable.Name, variable.lineNumber);
            if (value is int intValue) context.SetVariable(variable.Name, HandleIntegerOverflow(intValue, 1, "+", variable.lineNumber), variable.lineNumber);
            else throw new EvaluatorException($"Error at line: {variable.lineNumber}. Can only use increment operator on integers.");
        }

        private void DecrementVariable(Variable variable)
        {
            object value = context.GetVariable(variable.Name, variable.lineNumber);
            if (value is int intValue) context.SetVariable(variable.Name, HandleIntegerOverflow(intValue, 1, "-", variable.lineNumber), variable.lineNumber);
            else throw new EvaluatorException($"Error at line: {variable.lineNumber}. Can only use decrement operator on integers.");
        }

        private object HandleIntegerOverflow(int left, int right, string operation, int lineNumber)
        {
            try
            {
                return operation switch
                {
                    "+" => checked(left + right),
                    "-" => checked(left - right),
                    "*" => checked(left * right),
                    _ => left / right
                };
            }
            catch (OverflowException)
            {
                throw new Exception($"Error at line: {lineNumber}. Error: Integer overflow.");
            }
        }

        public static int ConvertToInt(object value, int lineNumber)
        {
            if (value is float floatValue)
            {
                return (int)floatValue;
            }
            if (value is double doubleValue)
            {
                return (int)doubleValue;
            }
            if (value is string stringValue)
            {
                if (float.TryParse(stringValue, out float result))
                {
                    return (int)result;
                }
                else
                {
                    throw new Exception($"Error at line: {lineNumber}. Cannot convert '{stringValue}' to int.");
                }
            }
            if (value is char charValue)
            {
                if (char.IsDigit(charValue))
                {
                    return charValue - '0';
                }
                else
                {
                    // ASCII representation
                    Convert.ToInt32(value);
                }
            }
            return Convert.ToInt32(value);
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
                if (double.TryParse(stringValue, out double doubleValue))
                {
                    return doubleValue;
                }
            }
            return value;
        }

        public static string ConvertToString(object value)
        {
            return value switch
            {
                bool boolValue => boolValue ? "TRUE" : "FALSE",
                float floatValue => floatValue % 1 == 0 ? $"{floatValue}.0" : floatValue.ToString(),
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
                TokenType.STRING => "",
                _ => null
            };
        }
        #endregion HELPER METHODS
    }
}
