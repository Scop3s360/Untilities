# Password Generator

A Python-based password generator that creates secure passwords with customizable character types and length.

## Features

- Generate passwords with different character combinations:
  - Letters, numbers, and symbols (most secure)
  - Letters and numbers only
  - Letters only
  - Numbers only
  - Symbols only
- Customizable password length
- Option to prevent more than 3 consecutive characters of the same type
- Interactive command-line interface
- Input validation and error handling

## Requirements

- Python 3.6 or higher

## How to Run

```bash
py passwordgen.py
```

Or on systems where `python` is available:
```bash
python passwordgen.py
```

## Usage

1. Run the script
2. Choose from 6 menu options (1-5 for different password types, 6 to exit)
3. Enter desired password length
4. For mixed character types, choose whether to allow consecutive characters
5. Your generated password will be displayed

## Recent Improvements (v2.0)

### Security Enhancements
- **Added uppercase letters**: Passwords now include both uppercase and lowercase letters for stronger security
- **Fixed consecutive character logic**: The bug preventing proper consecutive character checking has been resolved
- **Expanded symbol set**: Now uses Python's `string.punctuation` for comprehensive symbol coverage

### Code Quality Improvements
- **Reduced code duplication**: Created `generate_mixed_password()` function to handle mixed character type passwords
- **Better input validation**: Added checks for minimum password length
- **Cleaner character selection**: Uses Python's `string` module instead of manually defined character sets
- **More efficient generation**: Improved logic for single character type passwords using list comprehensions
- **Enhanced user experience**: Added spacing and clearer prompts

### Technical Changes
- **Import optimization**: Added `import string` for better character set management
- **Function refactoring**: Consolidated duplicate logic into reusable functions
- **Logic fixes**: Corrected the consecutive character prevention algorithm
- **Performance improvements**: More efficient password generation for all types

## Password Security Tips

- Use at least 12-16 characters for strong passwords
- Include a mix of uppercase, lowercase, numbers, and symbols
- Avoid using the same password for multiple accounts
- Consider using a password manager for storing generated passwords

## Example Output

```
Welcome to the password generator!
Please select the type of password you would like to generate:
1. Password with letters, numbers, and symbols
2. Password with letters and numbers
3. Password with only letters
4. Password with only numbers
5. Password with only symbols
6. Exit
Enter your choice (1-6): 1
How long does your password need to be? 14
Can there be more than 3 of the same type of character in a row? (Yes or No): no
Your password is: K9#mP2$vL8@nQ4
```

## License

This project is open source and available under the MIT License.