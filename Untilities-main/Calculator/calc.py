import math
from typing import Optional, Union

class Calculator:
    """Enhanced calculator class with improved error handling and operation mapping."""
    
    def __init__(self):
        self.history = []
    
    def add(self, x: float, y: float) -> float:
        """Add two numbers."""
        return x + y
    
    def subtract(self, x: float, y: float) -> float:
        """Subtract y from x."""
        return x - y
    
    def multiply(self, x: float, y: float) -> float:
        """Multiply two numbers."""
        return x * y
    
    def divide(self, x: float, y: float) -> float:
        """Divide x by y."""
        if y == 0:
            raise ValueError("Cannot divide by zero")
        return x / y
    
    def square_root(self, x: float) -> float:
        """Calculate square root of x."""
        if x < 0:
            raise ValueError("Cannot calculate square root of negative number")
        return math.sqrt(x)
    
    def square(self, x: float) -> float:
        """Calculate square of x."""
        return x * x
    
    def percentage(self, x: float, y: float) -> float:
        """Calculate x percent of y."""
        return (x / 100) * y
    
    def power(self, x: float, y: float) -> float:
        """Calculate x raised to the power of y."""
        return math.pow(x, y)
    
    def calculate(self, operation: str, x: float, y: Optional[float] = None) -> float:
        """
        Perform calculation based on operation string.
        
        Args:
            operation: The operation to perform
            x: First number
            y: Second number (optional for single-operand operations)
            
        Returns:
            Result of the calculation
            
        Raises:
            ValueError: For invalid operations or mathematical errors
        """
        operations = {
            'add': lambda a, b: self.add(a, b),
            'subtract': lambda a, b: self.subtract(a, b),
            'multiply': lambda a, b: self.multiply(a, b),
            'divide': lambda a, b: self.divide(a, b),
            'square_root': lambda a, b=None: self.square_root(a),
            'square': lambda a, b=None: self.square(a),
            'percentage': lambda a, b: self.percentage(a, b),
            'power': lambda a, b: self.power(a, b)
        }
        
        if operation not in operations:
            raise ValueError(f"Unknown operation: {operation}")
        
        # Check if operation requires two operands
        two_operand_ops = {'add', 'subtract', 'multiply', 'divide', 'percentage', 'power'}
        if operation in two_operand_ops and y is None:
            raise ValueError(f"Operation '{operation}' requires two numbers")
        
        result = operations[operation](x, y)
        
        # Store in history
        if y is not None:
            self.history.append(f"{x} {operation} {y} = {result}")
        else:
            self.history.append(f"{operation}({x}) = {result}")
        
        return result
    
    def get_history(self) -> list:
        """Get calculation history."""
        return self.history.copy()
    
    def clear_history(self) -> None:
        """Clear calculation history."""
        self.history.clear()

def clear_screen():
    """Clear the console screen."""
    import os
    os.system('cls' if os.name == 'nt' else 'clear')

def get_number_input(prompt: str) -> Optional[float]:
    """Get a valid number from user input."""
    try:
        return float(input(prompt))
    except ValueError:
        print("❌ Please enter a valid number")
        return None

def display_menu():
    """Display the main calculator menu."""
    print("\n" + "="*40)
    print("🧮 ENHANCED CALCULATOR")
    print("="*40)
    print("1. Enter First Number")
    print("2. Enter Second Number") 
    print("3. Select Operation")
    print("4. Show Current Values")
    print("5. Calculate")
    print("6. View History")
    print("7. Clear History")
    print("8. Clear Screen")
    print("9. Exit")
    print("="*40)

def display_operations():
    """Display available operations."""
    print("\n📋 Select Operation:")
    print("a. Addition (+)")
    print("b. Subtraction (-)")
    print("c. Multiplication (×)")
    print("d. Division (÷)")
    print("e. Square Root (√)")
    print("f. Square (x²)")
    print("g. Percentage (%)")
    print("h. Power (x^y)")

def get_operation_choice() -> Optional[str]:
    """Get operation choice from user."""
    display_operations()
    choice = input("Enter choice (a-h): ").lower().strip()
    
    operation_map = {
        'a': 'add',
        'b': 'subtract', 
        'c': 'multiply',
        'd': 'divide',
        'e': 'square_root',
        'f': 'square',
        'g': 'percentage',
        'h': 'power'
    }
    
    if choice in operation_map:
        return operation_map[choice]
    else:
        print("❌ Invalid operation choice")
        return None

def main():
    """Main calculator program."""
    calc = Calculator()
    input1 = None
    input2 = None
    selected_operation = None
    
    print("🎉 Welcome to the Enhanced Calculator!")
    
    while True:
        display_menu()
        choice = input("Enter choice (1-9): ").strip()

        if choice == '1':
            number = get_number_input("Enter first number: ")
            if number is not None:
                input1 = number
                print(f"✅ First number set to: {input1}")

        elif choice == '2':
            number = get_number_input("Enter second number: ")
            if number is not None:
                input2 = number
                print(f"✅ Second number set to: {input2}")

        elif choice == '3':
            operation = get_operation_choice()
            if operation:
                selected_operation = operation
                print(f"✅ Operation set to: {selected_operation}")

        elif choice == '4':
            print("\n📊 Current Values:")
            print(f"   First Number: {input1 if input1 is not None else 'Not set'}")
            print(f"   Second Number: {input2 if input2 is not None else 'Not set'}")
            print(f"   Operation: {selected_operation if selected_operation else 'Not set'}")

        elif choice == '5':
            if selected_operation is None:
                print("❌ Please select an operation first")
            elif input1 is None:
                print("❌ Please enter the first number")
            else:
                try:
                    # Single operand operations
                    single_ops = {'square_root', 'square'}
                    if selected_operation in single_ops:
                        result = calc.calculate(selected_operation, input1)
                        print(f"\n🎯 Result: {result}")
                    else:
                        # Two operand operations
                        if input2 is None:
                            print("❌ Please enter the second number")
                        else:
                            result = calc.calculate(selected_operation, input1, input2)
                            print(f"\n🎯 Result: {result}")
                            
                except ValueError as e:
                    print(f"❌ Error: {e}")

        elif choice == '6':
            history = calc.get_history()
            if history:
                print("\n📜 Calculation History:")
                for i, calculation in enumerate(history[-10:], 1):  # Show last 10
                    print(f"   {i}. {calculation}")
            else:
                print("\n📜 No calculations in history")

        elif choice == '7':
            calc.clear_history()
            print("✅ History cleared")

        elif choice == '8':
            clear_screen()

        elif choice == '9':
            print("👋 Thank you for using Enhanced Calculator!")
            break

        else:
            print("❌ Invalid choice. Please enter a number between 1 and 9.")
        
        # Pause for user to see the result
        if choice in ['5', '6']:
            input("\nPress Enter to continue...")

if __name__ == "__main__":
    main()
