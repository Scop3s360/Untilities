# 🧮 Enhanced Calculator

A modern, feature-rich calculator application with both Command Line Interface (CLI) and Graphical User Interface (GUI) options.

## Features

### ✨ Core Operations
- **Basic Arithmetic**: Addition, Subtraction, Multiplication, Division
- **Advanced Functions**: Square Root, Square, Power (x^y)
- **Percentage Calculations**: Calculate percentages easily
- **Error Handling**: Robust error handling for invalid operations

### 📊 Enhanced Features
- **Calculation History**: Track and view past calculations
- **Multiple Interfaces**: Choose between CLI and GUI
- **Input Validation**: Prevents invalid inputs and operations
- **Modern UI**: Clean, intuitive interface design

## Installation & Usage

### Prerequisites
- Python 3.6 or higher
- tkinter (usually included with Python)

### Quick Start

1. **Clone or download the repository**
2. **Run the launcher** to choose your interface:
   ```bash
   python launcher.py
   ```

### Interface Options

#### 🖥️ Command Line Interface (CLI)
```bash
python calc.py
```
- Menu-driven interface
- Step-by-step operation selection
- Perfect for terminal users

#### 🖼️ Graphical User Interface (GUI)
```bash
python calculator_gui.py
```
- Modern button-based interface
- Real-time display updates
- Visual calculation history
- Mouse and keyboard friendly

## File Structure

```
Calculator/
├── calc.py              # Core calculator logic + CLI interface
├── calculator_gui.py    # GUI interface using tkinter
├── launcher.py          # Interface selector
└── README.md           # This file
```

## GUI Interface Guide

### Button Layout
```
[C]  [CE] [√]  [^]
[7]  [8]  [9]  [/]
[4]  [5]  [6]  [*]
[1]  [2]  [3]  [-]
[0]  [.]  [=]  [+]
[x²] [%]  [History] [Clear History]
```

### Button Functions
- **C**: Clear all (reset calculator)
- **CE**: Clear entry (clear current input)
- **√**: Square root of current number
- **^**: Power operation (x^y)
- **x²**: Square of current number
- **%**: Percentage calculation
- **History**: View full calculation history
- **Clear History**: Clear all saved calculations

### Display Areas
- **Main Display**: Shows current number/result
- **Operation Display**: Shows current operation being performed
- **History Panel**: Shows recent calculations (scrollable)

## CLI Interface Guide

### Menu Options
1. **Enter First Number**: Input the first operand
2. **Enter Second Number**: Input the second operand
3. **Select Operation**: Choose from available operations
4. **Show Current Values**: Display current inputs
5. **Calculate**: Perform the calculation
6. **View History**: Show calculation history
7. **Clear History**: Clear saved calculations
8. **Clear Screen**: Clear the terminal
9. **Exit**: Close the calculator

## Error Handling

The calculator handles various error conditions:
- **Division by Zero**: Prevents division by zero operations
- **Negative Square Root**: Prevents square root of negative numbers
- **Invalid Input**: Validates numeric inputs
- **Missing Operands**: Ensures required numbers are provided

## Examples

### Basic Calculation
```
10 + 5 = 15
```

### Advanced Operations
```
√25 = 5.0
3² = 9
2^8 = 256.0
20% of 150 = 30.0
```

## Development

### Core Classes
- **Calculator**: Main calculation engine with operation methods
- **CalculatorGUI**: GUI interface implementation
- **CLI Functions**: Command-line interface helpers

### Adding New Operations
1. Add method to `Calculator` class in `calc.py`
2. Update operation mapping in `calculate()` method
3. Add GUI button and handler in `calculator_gui.py`
4. Update CLI menu in `calc.py`

## Contributing

Feel free to contribute by:
- Adding new mathematical operations
- Improving the user interface
- Enhancing error handling
- Adding unit tests
- Improving documentation

## License

This project is open source and available under the MIT License.

---

**Enjoy calculating! 🎉**