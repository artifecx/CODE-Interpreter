# Additional Features <br/>
### Operations:
- Compound-assignment operators: syntax: <_variable><_operator><_value>
  - += -> adds the value to the variable
  - -= -> subtracts the value to the variable
  - *= -> multiplies the value and the variable
  - /= -> divides the variable with the value
  - %= -> modulos the variable with the value
- Postfix increment and decrement: syntax: <_variable><_operator>
  - ++ -> adds 1 to the variable value
  - -- -> subtracts 1 to the variable value
- Character, string, and boolean comparison: syntax: <variable/literal><_operator><variable/literal>
  - == -> returns TRUE if both values are equal, otherwise FALSE
  - <> -> returns TRUE if both values are not equal, otherwise FALSE

### Keywords:
- Within loops:
  - BREAK -> breaks the loop
  - CONTINUE -> skips the current loop iteration
- STRING -> new data type that holds string literals

### Math functions:
- PI - returns the floating point value of pi (up to 7 decimal places)
- CEIL(<expression>) - returns the smallest integer that is greater than or equal to the value
- FLOOR(<expression>) - returns the largest integer that is smaller than or equal to the value

### Parsing and Type functions:
- TYPE(<variable/literal>) - returns the data type of whatever is inside the parenthesis
- TOSTRING(<variable/literal>) - converts the input to string, converts any data type to string
- TOINT(<variable/literal>) - converts the input to int, valid conversions from: float, numerical string/char, char to ascii decimal number, e.g. TOINT('a') -> 97 in ASCII
- TOFLOAT(<variable/literal>) - converts the input to float, valid conversions from: int, numerical string/char
