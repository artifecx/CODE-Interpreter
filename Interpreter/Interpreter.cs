using Interpreter;
using System;
using System.Collections.Generic;
using System.IO;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1 - Run using code");
        Console.WriteLine("2 - Run test cases");
        Console.Write("Option: ");

        var option = Console.ReadLine();
        switch (option)
        {
            case "1":
                Console.WriteLine();
                ManualTyping();
                break;
            case "2":
                Console.WriteLine();
                TestCases();
                break;
            default:
                Console.WriteLine("Invalid option selected.");
                break;
        }
    }

    static void ManualTyping()
    {
        string str_directory = Environment.CurrentDirectory.ToString();
        var projectfolder = Directory.GetParent(Directory.GetParent(Directory.GetParent(str_directory).ToString()).ToString());

        string filename = @"sample.code";
        string filePath = Path.Combine(projectfolder.ToString(), filename);

        if (File.Exists(filePath))
        {
            string code = File.ReadAllText(filePath);

            var lexer = new Lexer();
            List<Token> tokens = Lexer.Tokenize(code);

            var parser = new Parser(tokens);
            var ast = parser.Parse();

            // Debugging
            /*Console.WriteLine("\nParsed AST:");
            foreach (var statement in ast.Statements)
            {
                Console.WriteLine(statement.GetType().Name);
            }
            Console.WriteLine();*/

            var interpreter = new InterpreterClass();
            interpreter.Interpret(ast);
        }
        else
        {
            Console.WriteLine($"File not found: {filePath}");
        }
    }

    public class TestCase
    {
        public string Code { get; set; }
        public string ExpectedOutput { get; set; }
    }

    static void TestCases()
    {
        var tests = InitializeTests();
        var results = new List<string>();
        int passCount = 0;
        int failCount = 0;
        bool printToConsole = false;

        TextWriter originalConsoleOut = Console.Out;

        foreach (var test in tests)
        {
            try
            {
                var lexer = new Lexer();
                var tokens = Lexer.Tokenize(test.Code);
                var parser = new Parser(tokens);
                var ast = parser.Parse();

                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);

                    var interpreter = new InterpreterClass();
                    interpreter.Interpret(ast);

                    Console.Out.Flush();

                    var output = sw.ToString().Trim();
                    bool pass = output.Equals(test.ExpectedOutput);
                    results.Add($"\nExpected: {test.ExpectedOutput}\nActual: {output}\nResult: {(pass ? "PASS" : "FAIL")}\n");

                    if (pass) passCount++;
                    else failCount++;

                    if (printToConsole)
                    {
                        Console.SetOut(originalConsoleOut);
                        Console.WriteLine(output);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.SetOut(originalConsoleOut);
                results.Add($"\nExpected: {test.ExpectedOutput}\nActual: {ex.Message}\nResult: FAIL\n");
                failCount++;
            }
        }

        Console.SetOut(originalConsoleOut);
        int i = 1;
        foreach (var result in results)
        {
            Console.WriteLine($"TEST CASE {i}: {result}");
            i++;
        }

        Console.WriteLine($"\nTotal Test Cases: {tests.Count}\nPassed: {passCount}\nFailed: {failCount}");
    }

    public static List<TestCase> InitializeTests()
    {
        return new List<TestCase>
        {
            new TestCase {
                Code = @"BEGIN CODE
                             INT xyz, abc=100
                             xyz= ((abc *5)/10 + 10) * -1
                             DISPLAY: [[] & xyz & []]
                         END CODE",
                ExpectedOutput = "[-60]"
            },
            new TestCase {
                Code = @"BEGIN CODE
                             INT x, y, z=5
                             CHAR a_1='n'
                             BOOL t=""TRUE""
                             x=y=4
                             a_1='c'
                             # this is a comment
                             DISPLAY: x & t & z & $ & a_1 & [#] & ""last""
                         END CODE",
                ExpectedOutput = "4TRUE5\nc#last"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a=100, b=200, c=300
                            BOOL d=""FALSE""
                            d = (a < b AND c <> 200)
                            DISPLAY: d
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                             INT a=100, b=200, c=300
                             c = (-a + 1) * -2
                             DISPLAY: c
                         END CODE",
                ExpectedOutput = "198"
            },
            new TestCase {
                Code = @"BEGIN CODE
                             INT a=2, b=9, c=0
                             c = a + b
                             DISPLAY: c
                         END CODE",
                ExpectedOutput = "11"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT i = 0
                            i++
                            DISPLAY: i
                         END CODE",
                ExpectedOutput = "1"
            },
            new TestCase {
                Code = @"BEGIN CODE
                             INT i = 5
                             WHILE (i > 0)
                             BEGIN WHILE
                                i--
                                IF (i == 2)
                                BEGIN IF
                                    BREAK
                                END IF
                                DISPLAY: i & "" ""
                             END WHILE
                         END CODE",
                ExpectedOutput = "4 3"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT area = PI * (CEIL(2.1) * CEIL(2.1))
                            DISPLAY: area
                         END CODE",
                ExpectedOutput = "28.274334"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            CHAR numChar = '5'
                            INT num = TOINT(numChar)
                            FLOAT numFloat = TOFLOAT(""3.14"")
                            DISPLAY: num & "" "" & numFloat
                         END CODE",
                ExpectedOutput = "5 3.14"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT x = 10.0
                            x *= 3
                            DISPLAY: x
                         END CODE",
                ExpectedOutput = "30.0"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            DISPLAY: CEIL(TOFLOAT(TOSTRING(2 + 3) & "".75""))
                         END CODE",
                ExpectedOutput = "6"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 10
                            a %= 3
                            DISPLAY: a
                         END CODE",
                ExpectedOutput = "1"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 5
                            a += 5
                            a -= 2
                            a *= 2
                            DISPLAY: a
                         END CODE",
                ExpectedOutput = "16"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT height = 7.9
                            INT ceilingHeight = CEIL(height)
                            INT floorHeight = FLOOR(height)
                            DISPLAY: ceilingHeight & "" "" & floorHeight
                        END CODE",
                ExpectedOutput = "8 7"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT i = 1
                            DISPLAY: i++ & "" "" & i
                         END CODE",
                ExpectedOutput = "2 1"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT count = 5
                            count--
                            count--
                            DISPLAY: count
                         END CODE",
                ExpectedOutput = "3"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT num = 10.5
                            INT intNum = TOINT(num)
                            num -= intNum
                            DISPLAY: num
                         END CODE",
                ExpectedOutput = "0.5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT num = TOINT(TOSTRING(12345))
                            FLOAT div = TOFLOAT(TOSTRING(num / 50))
                            DISPLAY: FLOOR(div)
                         END CODE",
                ExpectedOutput = "246"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT price = 99.99
                            DISPLAY: ""Ceil: "" & CEIL(price) & "", Floor: "" & FLOOR(price)
                         END CODE",
                ExpectedOutput = "Ceil: 100, Floor: 99"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT x = 10, y = 20
                            BOOL result = (x * y - 5 > 150) AND (x + y == 30)
                            DISPLAY: result
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT i = 0, j = 0
                            WHILE (i < 3)
                            BEGIN WHILE
                                j = 0
                                WHILE (j < 2)
                                BEGIN WHILE
                                    DISPLAY: i & j & "" ""
                                    j = j + 1
                                END WHILE
                                i = i + 1
                            END WHILE
                         END CODE",
                ExpectedOutput = "00 01 10 11 20 21"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            BOOL a = ""TRUE"", b = ""FALSE"", c = ""TRUE""
                            DISPLAY: (a AND b) OR (b OR c)
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT score = 85
                            IF (score > 90)
                            BEGIN IF
                                DISPLAY: ""A""
                            END IF
                            ELSE IF (score > 80)
                            BEGIN IF
                                DISPLAY: ""B""
                            END IF
                            ELSE
                            BEGIN IF
                                DISPLAY: ""C""
                            END IF
                         END CODE",
                ExpectedOutput = "B"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT pi = 3.14
                            INT radius = 5
                            FLOAT area = pi * (radius * radius)
                            DISPLAY: area
                         END CODE",
                ExpectedOutput = "78.5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            DISPLAY: ""Output:"" & $ & ""End of Line""
                         END CODE",
                ExpectedOutput = "Output:\nEnd of Line"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT _1a = 10, a1 = 20
                            DISPLAY: _1a & a1
                         END CODE",
                ExpectedOutput = "1020"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            CHAR a = '['
                            DISPLAY: ""[["" & a & ""]]""
                         END CODE",
                ExpectedOutput = "[[[]]"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT x = 15
                            IF (x > 10)
                            BEGIN IF
                                IF (x < 20)
                                BEGIN IF
                                DISPLAY: ""Between 10 and 20""
                                END IF
                            END IF
                         END CODE",
                ExpectedOutput = "Between 10 and 20"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            BOOL x = NOT ""TRUE""
                            DISPLAY: x
                         END CODE",
                ExpectedOutput = "FALSE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 5
                            BOOL b = (a == 5)
                            DISPLAY: b AND ""TRUE""
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT num = 5
                            CHAR ch = 'A'
                            DISPLAY: ch & num
                         END CODE",
                ExpectedOutput = "A5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 0
                            WHILE (TRUE)
                            BEGIN WHILE
                                DISPLAY: a & "" ""
                                a = a + 1
                                IF (a == 3)
                                BEGIN IF
                                    BREAK
                                END IF
                            END WHILE
                         END CODE",
                ExpectedOutput = "0 1 2"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT result = 10 + 2 * 5
                            DISPLAY: result
                         END CODE",
                ExpectedOutput = "20"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT x = 3.5, y = 2.0
                            INT result = TOINT(x * y)
                            DISPLAY: result
                         END CODE",
                ExpectedOutput = "7"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            DISPLAY: ""The result is "" & (5 > 3)
                         END CODE",
                ExpectedOutput = "The result is TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            DISPLAY: ""Hello World[$]""
                         END CODE",
                ExpectedOutput = "Hello World[$]"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT num = 3.14159
                            DISPLAY: num
                         END CODE",
                ExpectedOutput = "3.14159"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            BOOL a = ""TRUE"", b = ""FALSE""
                            DISPLAY: (NOT a) AND b OR (a AND NOT b)
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT a = 5.5, b = 2.2
                            DISPLAY: (a * b) / (a - b) + 100
                         END CODE",
                ExpectedOutput = "103.666664"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            DISPLAY: ""#This is a test""
                         END CODE",
                ExpectedOutput = "#This is a test"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT score = 75
                            IF (score >= 90)
                            BEGIN IF
                                DISPLAY: ""A""
                            END IF
                            ELSE IF (score >= 80)
                            BEGIN IF
                                DISPLAY: ""B""
                            END IF
                            ELSE IF (score >= 70)
                            BEGIN IF
                                DISPLAY: ""C""
                            END IF
                            ELSE
                            BEGIN IF
                                DISPLAY: ""F""
                            END IF
                         END CODE",
                ExpectedOutput = "C"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            CHAR a = 'a', b = 'b'
                            DISPLAY: TOINT(a) < TOINT(b)
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT f = 3.14
                            INT i = 2
                            DISPLAY: ""Pi approx: "" & f & "", Multiplier: "" & i
                         END CODE",
                ExpectedOutput = "Pi approx: 3.14, Multiplier: 2"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT a = 2.5
                            INT b = TOINT(a * 2)
                            DISPLAY: b
                         END CODE",
                ExpectedOutput = "5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            CHAR first = 'H', last = 'W'
                            DISPLAY: ""Hello "" & first & ""orld"" & last
                         END CODE",
                ExpectedOutput = "Hello HorldW"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT i = 1
                            FLOAT f = 1.1
                            CHAR c = 'c'
                            BOOL b = ""TRUE""
                            DISPLAY: i & f & c & b
                         END CODE",
                ExpectedOutput = "11.1cTRUE"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT i = 0
                            WHILE (i < 10)
                            BEGIN WHILE
                                IF (i % 2 == 0)
                                BEGIN IF
                                    DISPLAY: i & "" is even ""
                                END IF
                                ELSE
                                BEGIN IF
                                    DISPLAY: i & "" is odd ""
                                END IF
                                i = i + 1
                            END WHILE
                         END CODE",
                ExpectedOutput = "0 is even 1 is odd 2 is even 3 is odd 4 is even 5 is odd 6 is even 7 is odd 8 is even 9 is odd"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT f = 5.0
                            INT i = 2
                            DISPLAY: f / i
                         END CODE",
                ExpectedOutput = "2.5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            BOOL a = TRUE, b = TRUE
                            IF (a OR b)
                            BEGIN IF
                                DISPLAY: ""Either true or false""
                            END IF
                            ELSE
                            BEGIN IF
                                DISPLAY: ""Neither""
                            END IF
                         END CODE",
                ExpectedOutput = "Either true or false"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 10, b = 20
                            FLOAT c = 3.5
                            CHAR d = 'x'
                            DISPLAY: a & b & c & d
                         END CODE",
                ExpectedOutput = "10203.5x"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT f = 1.999
                            INT i = TOINT(f)
                            DISPLAY: i
                         END CODE",
                ExpectedOutput = "1"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            BOOL a = ""FALSE"", b = ""TRUE""
                            IF (NOT a AND (b OR NOT (a AND b)))
                            BEGIN IF
                                DISPLAY: ""True complex""
                            END IF
                         END CODE",
                ExpectedOutput = "True complex"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 10
                            FLOAT b = 5.5
                            DISPLAY: a & b
                         END CODE",
                ExpectedOutput = "105.5"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT if = 10
                            DISPLAY: if
                         END CODE",
                ExpectedOutput = "10"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            FLOAT f = 0.1 + 0.2
                            DISPLAY: f
                         END CODE",
                ExpectedOutput = "0.3"
            },
            new TestCase {
                Code = @"BEGIN CODE
                            INT a = 10
                            BOOL b = (a > 5) AND (a < 15)
                            DISPLAY: b
                         END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
                            INT i = 0, j
                            WHILE (i < 5)
                            BEGIN WHILE
                                j = 0
                                WHILE (j < 5)
                                BEGIN WHILE
                                    IF (i == j)
                                    BEGIN IF
                                        DISPLAY: ""Diagonal"" & i & "" ""
                                    END IF
                                    IF (i + j == 4)
                                    BEGIN IF
                                        BREAK
                                    END IF
                                    j = j + 1
                                END WHILE
                                i = i + 1
                            END WHILE
                         END CODE",
                ExpectedOutput = "Diagonal0 Diagonal1 Diagonal2"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
	                        BOOL test = (10 > 5)
	                        IF (test == ""TRUE"")
	                        BEGIN IF
		                        DISPLAY: ""TRUE""
	                        END IF
	                        ELSE
	                        BEGIN IF
		                        DISPLAY: ""FALSE""
	                        END IF
                        END CODE",
                ExpectedOutput = "TRUE"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
                            INT i = 0
                            IF (i + 1 < 1)
                            BEGIN IF
                                DISPLAY: ""i is less than 1 after increment""
                            END IF
                            ELSE IF (i + 1 == 1)
                            BEGIN IF
                                DISPLAY: ""i is equal to 1 after increment""
                            END IF
                            ELSE
                            BEGIN IF
                                DISPLAY: ""i is greater than 1 after increment""
                            END IF
                         END CODE",
                ExpectedOutput = "i is equal to 1 after increment"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
                            BOOL a = TRUE, b = FALSE
                            IF (a AND (NOT b OR a))
                            BEGIN IF
                                DISPLAY: ""Complex logic passed""
                            END IF
                            ELSE
                            BEGIN IF
	                            DISPLAY: ""Complex logic failed""
                            END IF
                         END CODE",
                ExpectedOutput = "Complex logic passed"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
                            INT i = 0
                            WHILE ( i < 10 )
                            BEGIN WHILE
                                i++
                                IF (i == 5) 
                                BEGIN IF
                                    BREAK
                                END IF
                                IF (i % 2 == 0) 
                                BEGIN IF
                                    CONTINUE
                                END IF
                                DISPLAY: i & "" ""
                            END WHILE
                         END CODE",
                ExpectedOutput = "1 3"
            },
            new TestCase
            {
                Code = @"BEGIN CODE
                            INT i = 0
                            WHILE(i < 5)
                            BEGIN WHILE
                                i++
                                IF (i % 2 == 0)
                                BEGIN IF
                                    CONTINUE
                                END IF
                                DISPLAY: i & "" ""
                            END WHILE
                        END CODE",
                ExpectedOutput = "1 3 5"
            },
        };
    }
}