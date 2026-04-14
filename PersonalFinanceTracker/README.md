# Personal Finance Tracker

A comprehensive desktop application for tracking personal income and expenses built with Python and Tkinter.

## Features

### Core Functionality
- ✅ Add income and expense transactions
- ✅ Categorize transactions with color-coded display
- ✅ Date-based filtering (Today, This Week, This Month, All Time)
- ✅ Real-time summary calculations (Income, Expenses, Net Total)
- ✅ Edit and delete existing transactions
- ✅ Export data to CSV and Excel formats

### Data Management
- ✅ SQLite database with automatic setup
- ✅ Unique reference number generation for each transaction
- ✅ Comprehensive error handling and logging
- ✅ Input validation and sanitization

### User Interface Options

#### Classic Interface
- ✅ Clean, functional GUI with organized layout
- ✅ Color-coded categories for easy identification
- ✅ Date picker with quick filter buttons
- ✅ Scrollable transaction history
- ✅ Summary panel with real-time updates

#### Modern Interface (New!)
- ✅ **Tabbed interface** with Dashboard, Transactions, Analytics, and Settings
- ✅ **Enhanced dashboard** with statistics cards and recent transactions
- ✅ **Quick add form** for faster transaction entry
- ✅ **Analytics tab** with pie charts and trend analysis
- ✅ **Modern styling** with improved colors and typography
- ✅ **Theme support** with multiple color schemes
- ✅ **Better responsive layout** that adapts to window size

## Installation

### Prerequisites
- Python 3.7 or higher
- pip (Python package installer)

### Quick Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/Scop3s360/PersonalFinanceTracker.git
   cd PersonalFinanceTracker
   ```

2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

3. Start the application:
   
   **Quick Start (Windows):**
   ```bash
   run.bat
   ```
   
   **Command Line Options:**
   ```bash
   python finance_tracker/main.py --ui modern    # Modern interface (default)
   python finance_tracker/main.py --ui classic   # Classic interface
   python ui_demo.py                             # UI selection demo
   ```

### Manual Installation
If you prefer to install manually:

1. Install required packages:
   ```bash
   pip install -r requirements.txt
   ```

2. Run the application:
   ```bash
   run.bat                                       # Windows launcher
   python finance_tracker/main.py               # Modern UI (default)
   python finance_tracker/main.py --ui classic  # Classic UI
   python ui_demo.py                            # Choose interface
   ```

## User Interface Options

The application now offers two interface options:

### 🔷 Classic Interface
- Simple, straightforward design
- All features in a single window
- Lightweight and fast
- Perfect for basic usage

### ✨ Modern Interface (Recommended)
- **Dashboard Tab**: Overview with statistics cards, quick add form, and recent transactions
- **Transactions Tab**: Full transaction management with advanced filtering
- **Analytics Tab**: Visual charts and category breakdowns
- **Settings Tab**: Configuration and data management options
- **Theme Support**: Multiple color schemes (Light, Dark, Blue, Green)
- **Enhanced UX**: Better spacing, typography, and visual hierarchy

### 🎯 UI Demo
Run `python ui_demo.py` to choose between interfaces or try both!

## Usage

### Adding Transactions
1. Enter the transaction amount
2. Select transaction type (Income/Expense)
3. Choose appropriate category
4. Add optional description
5. Click "Add Transaction"

### Filtering Data
- Use date pickers to set custom date ranges
- Quick filter buttons: Today, This Week, This Month, All Time
- Click "Apply Filter" to update the view

### Managing Transactions
- Select a transaction from the list
- Click "Amend Selected" to edit
- Click "Delete Selected" to remove

### Exporting Data
- Set desired date range using filters
- Click "Export CSV" or "Export Excel"
- Choose save location and filename

## Project Structure

```
PersonalFinanceTracker/
├── finance_tracker/
│   ├── config/
│   │   └── config.py          # Application configuration
│   ├── database/
│   │   └── db_manager.py      # Database operations
│   ├── gui/
│   │   ├── app.py             # Classic UI
│   │   ├── modern_app.py      # Modern UI
│   │   └── themes.py          # Theme management
│   ├── utils/
│   │   └── helpers.py         # Utility functions
│   └── main.py                # Unified entry point
├── build.py                   # Build script for distribution
├── ui_demo.py                 # UI selection demo
├── run.bat                    # Windows launcher
├── requirements.txt           # Python dependencies
└── README.md                  # This file
```

## Configuration

The application uses a centralized configuration system in `finance_tracker/config/config.py`. You can customize:

- Currency symbol and formatting
- Date formats
- Category definitions and colors
- Validation limits
- Export settings

## Categories

### Income Categories
- Salary
- Bonus
- Investment
- Freelance
- Other Income

### Expense Categories
- Groceries
- Rent/Mortgage
- Utilities
- Transport
- Entertainment
- Healthcare
- Shopping
- Other Expense

## Database

The application uses SQLite for data storage with the following features:
- Automatic database creation
- Unique reference number generation
- Transaction history with timestamps
- Data integrity and error handling

## Troubleshooting

### Common Issues

1. **Import Error for tkcalendar**
   ```bash
   pip install tkcalendar
   ```

2. **Export functionality not working**
   ```bash
   pip install pandas openpyxl
   ```

3. **Database permission errors**
   - Ensure write permissions in the application directory
   - Check if antivirus is blocking database creation

### Logging
The application logs important events and errors. Check the console output for debugging information.

## Building for Distribution

To create a shareable executable for friends:

### Quick Build
```bash
build_for_sharing.bat    # Windows one-click build
python build.py          # Cross-platform build script
```

### Build Options
```bash
python build.py --help           # Show all options
python build.py --icon-only      # Create app icon only
python build.py --exe-only       # Build executable only
python build.py --no-clean       # Skip cleaning previous build
```

### What You Get
- `PersonalFinanceTracker_v1.0_YYYYMMDD.zip` - Complete package for sharing
- `dist/PersonalFinanceTracker.exe` - Standalone executable
- `dist/PersonalFinanceTracker_Portable/` - Portable version

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is open source and available under the MIT License.

## Future Enhancements

- [ ] Budget setting and alerts
- [ ] Monthly/yearly reports
- [ ] Data visualization with charts
- [ ] Multiple account support
- [ ] Receipt image storage
- [ ] Backup and restore functionality
- [ ] Dark mode theme
- [ ] Multi-currency support