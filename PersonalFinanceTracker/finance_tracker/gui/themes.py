"""
Theme management for the Finance Tracker application
"""

class ThemeManager:
    """Manages application themes and styling"""
    
    THEMES = {
        'light': {
            'name': 'Light Theme',
            'primary': '#2E86AB',
            'secondary': '#A23B72', 
            'success': '#27AE60',
            'danger': '#E74C3C',
            'warning': '#F39C12',
            'info': '#3498DB',
            'light': '#F8F9FA',
            'dark': '#2C3E50',
            'background': '#FFFFFF',
            'surface': '#F5F5F5',
            'income': '#27AE60',
            'expense': '#E74C3C',
            'text_primary': '#2C3E50',
            'text_secondary': '#6C757D'
        },
        
        'dark': {
            'name': 'Dark Theme',
            'primary': '#4A90E2',
            'secondary': '#B83DBA',
            'success': '#2ECC71',
            'danger': '#E67E22',
            'warning': '#F1C40F',
            'info': '#5DADE2',
            'light': '#34495E',
            'dark': '#ECF0F1',
            'background': '#2C3E50',
            'surface': '#34495E',
            'income': '#2ECC71',
            'expense': '#E67E22',
            'text_primary': '#ECF0F1',
            'text_secondary': '#BDC3C7'
        },
        
        'blue': {
            'name': 'Blue Theme',
            'primary': '#1E3A8A',
            'secondary': '#7C3AED',
            'success': '#059669',
            'danger': '#DC2626',
            'warning': '#D97706',
            'info': '#0284C7',
            'light': '#F1F5F9',
            'dark': '#1E293B',
            'background': '#FFFFFF',
            'surface': '#F8FAFC',
            'income': '#059669',
            'expense': '#DC2626',
            'text_primary': '#1E293B',
            'text_secondary': '#64748B'
        },
        
        'green': {
            'name': 'Green Theme',
            'primary': '#16A085',
            'secondary': '#8E44AD',
            'success': '#27AE60',
            'danger': '#E74C3C',
            'warning': '#F39C12',
            'info': '#3498DB',
            'light': '#F0FDF4',
            'dark': '#14532D',
            'background': '#FFFFFF',
            'surface': '#F7FEF7',
            'income': '#16A085',
            'expense': '#E74C3C',
            'text_primary': '#14532D',
            'text_secondary': '#6B7280'
        }
    }
    
    def __init__(self, theme_name='light'):
        self.current_theme = theme_name
        self.colors = self.THEMES.get(theme_name, self.THEMES['light'])
    
    def get_color(self, color_name):
        """Get color value by name"""
        return self.colors.get(color_name, '#000000')
    
    def get_all_colors(self):
        """Get all colors for current theme"""
        return self.colors.copy()
    
    def set_theme(self, theme_name):
        """Change current theme"""
        if theme_name in self.THEMES:
            self.current_theme = theme_name
            self.colors = self.THEMES[theme_name]
            return True
        return False
    
    def get_available_themes(self):
        """Get list of available themes"""
        return [(name, data['name']) for name, data in self.THEMES.items()]
    
    def get_category_colors(self, theme_name=None):
        """Get category colors adapted for current theme"""
        if theme_name:
            colors = self.THEMES.get(theme_name, self.colors)
        else:
            colors = self.colors
            
        # Adapt category colors based on theme
        if self.current_theme == 'dark':
            return {
                'Salary': '#2D5A3D',
                'Bonus': '#2D5A3D', 
                'Investment': '#2D5A3D',
                'Freelance': '#2D5A3D',
                'Other Income': '#2D5A3D',
                'Groceries': '#5A4A2D',
                'Rent/Mortgage': '#2D4A5A',
                'Utilities': '#2D4A5A',
                'Transport': '#4A2D5A',
                'Entertainment': '#4A2D5A',
                'Healthcare': '#5A2D2D',
                'Shopping': '#5A2D4A',
                'Other Expense': '#3A3A3A'
            }
        else:
            # Use default light theme colors
            return {
                'Salary': '#c8e6c9',
                'Bonus': '#c8e6c9',
                'Investment': '#c8e6c9', 
                'Freelance': '#c8e6c9',
                'Other Income': '#c8e6c9',
                'Groceries': '#ffe0b2',
                'Rent/Mortgage': '#bbdefb',
                'Utilities': '#bbdefb',
                'Transport': '#e1bee7',
                'Entertainment': '#e1bee7',
                'Healthcare': '#ffcdd2',
                'Shopping': '#f8bbd0',
                'Other Expense': '#f5f5f5'
            }
    
    def apply_ttk_styles(self, style_obj):
        """Apply theme colors to ttk styles"""
        # Configure various ttk styles based on current theme
        style_obj.configure('Title.TLabel',
                          font=('Segoe UI', 16, 'bold'),
                          foreground=self.get_color('text_primary'),
                          background=self.get_color('background'))
        
        style_obj.configure('Heading.TLabel',
                          font=('Segoe UI', 12, 'bold'),
                          foreground=self.get_color('primary'),
                          background=self.get_color('background'))
        
        style_obj.configure('Card.TFrame',
                          relief='solid',
                          borderwidth=1,
                          background=self.get_color('surface'))
        
        style_obj.configure('Primary.TButton',
                          font=('Segoe UI', 10, 'bold'),
                          foreground='white',
                          background=self.get_color('primary'))
        
        style_obj.configure('Success.TButton',
                          font=('Segoe UI', 10, 'bold'),
                          foreground='white',
                          background=self.get_color('success'))
        
        style_obj.configure('Danger.TButton',
                          font=('Segoe UI', 10, 'bold'),
                          foreground='white',
                          background=self.get_color('danger'))
        
        # Configure notebook tabs
        style_obj.configure('TNotebook.Tab',
                          padding=[20, 10],
                          font=('Segoe UI', 10, 'bold'))
        
        # Configure treeview
        style_obj.configure('Treeview',
                          background=self.get_color('background'),
                          foreground=self.get_color('text_primary'),
                          fieldbackground=self.get_color('surface'))
        
        style_obj.configure('Treeview.Heading',
                          font=('Segoe UI', 10, 'bold'),
                          foreground=self.get_color('text_primary'),
                          background=self.get_color('light'))

# Global theme manager instance
theme_manager = ThemeManager()