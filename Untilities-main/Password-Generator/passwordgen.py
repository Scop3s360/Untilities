import random
import string

def let_sel():
    # Include both uppercase and lowercase letters for stronger passwords
    letters = string.ascii_letters
    return random.choice(letters)

def num_sel():
    return random.choice(string.digits)

def sym_sel():
    # Use string.punctuation for comprehensive symbol set
    return random.choice(string.punctuation)

def generate_mixed_password(char_types, length, avoid_consecutive):
    """Generate password with mixed character types"""
    password = ""
    consecutive_count = 0
    last_type = None
    
    while len(password) < length:
        char_type = random.choice(char_types)
        
        if char_type == 'letter':
            new_char = let_sel()
        elif char_type == 'number':
            new_char = num_sel()
        else:  # symbol
            new_char = sym_sel()
        
        # Check for consecutive character types if restriction is enabled
        if avoid_consecutive and len(password) >= 2:
            current_type = char_type
            if current_type == last_type:
                consecutive_count += 1
                if consecutive_count >= 3:
                    continue  # Skip this character to avoid 3+ consecutive
            else:
                consecutive_count = 1
            last_type = current_type
        
        password += new_char
    
    return password

def main():
    while True:
        print("\nWelcome to the password generator!")
        print("Please select the type of password you would like to generate:")
        print("1. Password with letters, numbers, and symbols")
        print("2. Password with letters and numbers")
        print("3. Password with only letters")
        print("4. Password with only numbers")
        print("5. Password with only symbols")
        print("6. Exit")

        choice = input("Enter your choice (1-6): ")

        if choice == '1':
            try:
                length = int(input("How long does your password need to be? "))
                if length < 1:
                    print("Password length must be at least 1")
                    continue
                    
                check_three = input("Can there be more than 3 of the same type of character in a row? (Yes or No): ").lower()
                avoid_consecutive = check_three == 'no'
                
                char_types = ['letter', 'number', 'symbol']
                password = generate_mixed_password(char_types, length, avoid_consecutive)
                print("Your password is:", password)
                
            except ValueError:
                print("Please enter a valid number for length")

        elif choice == '2':
            try:
                length = int(input("How long does your password need to be? "))
                if length < 1:
                    print("Password length must be at least 1")
                    continue
                    
                check_three = input("Can there be more than 3 of the same type of character in a row? (Yes or No): ").lower()
                avoid_consecutive = check_three == 'no'
                
                char_types = ['letter', 'number']
                password = generate_mixed_password(char_types, length, avoid_consecutive)
                print("Your password is:", password)
                
            except ValueError:
                print("Please enter a valid number for length")
                
        elif choice == '3':
            try:
                length = int(input("How long does your password need to be? "))
                if length < 1:
                    print("Password length must be at least 1")
                    continue
                    
                password = "".join(let_sel() for _ in range(length))
                print("Your password is:", password)
            
            except ValueError:
                print("Please enter a valid number for length")

        elif choice == '4':
            try:
                length = int(input("How long does your password need to be? "))
                if length < 1:
                    print("Password length must be at least 1")
                    continue
                    
                password = "".join(num_sel() for _ in range(length))
                print("Your password is:", password)

            except ValueError:
                print("Please enter a valid number for length")

        elif choice == '5':
            try:
                length = int(input("How long does your password need to be? "))
                if length < 1:
                    print("Password length must be at least 1")
                    continue
                    
                password = "".join(sym_sel() for _ in range(length))
                print("Your password is:", password)

            except ValueError:
                print("Please enter a valid number for length")

        elif choice == '6':
            print("Goodbye!")
            break
        else:
            print("Invalid choice. Please enter a number between 1 and 6.")

if __name__ == "__main__":
    main()