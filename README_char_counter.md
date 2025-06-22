# Character Counter

A Go program that counts the frequency of each character in a given input string, excluding spaces. The results are sorted alphabetically by character.

## Usage

1. Save the script as `char_counter.go`.
2. Open a terminal.
3. Run the script with the following command:

   ```bash
   go run char_counter.go '<input_text>'
   ```

   Replace `<input_text>` with the text you want to analyze.

## Examples

### Example 1: Simple text
```bash
go run char_counter.go 'hello world'
```

**Output:**
```
Character Count: 'd':1, 'e':1, 'h':1, 'l':3, 'o':2, 'r':1, 'w':1
```

### Example 2: Text with special characters
```bash
go run char_counter.go 'Hello, World!'
```

**Output:**
```
Character Count: '!':1, ',':1, 'H':1, 'W':1, 'd':1, 'e':1, 'l':3, 'o':2, 'r':1
```

### Example 3: Numbers and symbols
```bash
go run char_counter.go 'abc123!@#'
```

**Output:**
```
Character Count: '!':1, '#':1, '1':1, '2':1, '3':1, '@':1, 'a':1, 'b':1, 'c':1
```

## Features

- Counts frequency of each character in the input string
- Excludes spaces from the count
- Results are sorted alphabetically by character
- Handles special characters, numbers, and symbols
- Simple command-line interface

## Requirements

- Go 1.16 or higher 