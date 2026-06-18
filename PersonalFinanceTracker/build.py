#!/usr/bin/env python3
"""
Unified build script for Personal Finance Tracker
Handles icon creation, executable building, and distribution packaging
"""

import os
import sys
import subprocess
import shutil
import zipfile
from pathlib import Path
from datetime import datetime
import argparse

class FinanceTrackerBuilder:
    def __init__(self):
        self.project_root = Path(__file__).parent
        self.dist_dir = self.project_root / "dist"
        self.build_dir = self.project_root / "build"
        
    def clean_build(self):
        """Clean previous build artifacts"""
        print("🧹 Cleaning previous build...")
        
        # Remove build directories
        if self.build_dir.exists():
            shutil.rmtree(self.build_dir)
        if self.dist_dir.exists():
            shutil.rmtree(self.dist_dir)
            
        # Remove spec file
        spec_file = self.project_root / "finance_tracker.spec"
        if spec_file.exists():
            spec_file.unlink()
            
        print("✓ Build cleaned")
    
    def install_dependencies(self):
        """Install required build dependencies"""
        print("📦 Installing build dependencies...")
        
        dependencies = ["pyinstaller", "pillow"]
        
        for dep in dependencies:
            try:
                __import__(dep.replace("-", "_"))
                print(f"✓ {dep} already installed")
            except ImportError:
                print(f"Installing {dep}...")
                subprocess.check_call([sys.executable, "-m", "pip", "install", dep])
                print(f"✓ {dep} installed")
    
    def create_app_icon(self):
        """Create application icon"""
        print("🎨 Creating app icon...")
        
        try:
            from PIL import Image, ImageDraw, ImageFont
            
            # Create a 256x256 image
            size = 256
            img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)
            
            # Create gradient background
            for i in range(size):
                color = (46, 134, 171, int(255 * (1 - i / size)))
                draw.line([(0, i), (size, i)], fill=color)
            
            # Draw circle background
            circle_margin = 20
            circle_bbox = [circle_margin, circle_margin, size - circle_margin, size - circle_margin]
            draw.ellipse(circle_bbox, fill=(46, 134, 171, 255), outline=(255, 255, 255, 255), width=4)
            
            # Draw dollar sign
            try:
                font = ImageFont.truetype("arial.ttf", 120)
            except:
                font = ImageFont.load_default()
            
            text = "$"
            bbox = draw.textbbox((0, 0), text, font=font)
            text_width = bbox[2] - bbox[0]
            text_height = bbox[3] - bbox[1]
            
            x = (size - text_width) // 2
            y = (size - text_height) // 2 - 10
            
            # Draw shadow and main text
            draw.text((x + 3, y + 3), text, font=font, fill=(0, 0, 0, 128))
            draw.text((x, y), text, font=font, fill=(255, 255, 255, 255))
            
            # Save icon
            icon_path = self.project_root / "app_icon.ico"
            img.save(icon_path, format='ICO', sizes=[(256, 256), (128, 128), (64, 64), (32, 32), (16, 16)])
            print("✓ App icon created")
            return True
            
        except Exception as e:
            print(f"⚠️  Could not create detailed icon: {e}")
            # Create simple fallback
            return self.create_simple_icon()
    
    def create_simple_icon(self):
        """Create a simple fallback icon"""
        try:
            # Create minimal ICO file
            ico_header = b'\x00\x00\x01\x00\x01\x00\x20\x20\x00\x00\x01\x00\x20\x00\x00\x10\x00\x00\x16\x00\x00\x00'
            icon_path = self.project_root / "app_icon.ico"
            with open(icon_path, 'wb') as f:
                f.write(ico_header)
            print("✓ Simple icon created")
            return True
        except:
            print("⚠️  Could not create icon")
            return False
    
    def create_spec_file(self):
        """Create PyInstaller spec file"""
        print("📝 Creating PyInstaller spec...")
        
        spec_content = f'''# -*- mode: python ; coding: utf-8 -*-

block_cipher = None

a = Analysis(
    ['ui_demo.py'],
    pathex=[],
    binaries=[],
    datas=[
        ('finance_tracker', 'finance_tracker'),
        ('README.md', '.'),
        ('requirements.txt', '.'),
    ],
    hiddenimports=[
        'tkinter', 'tkcalendar', 'matplotlib', 'pandas', 'openpyxl', 'numpy',
        'sqlite3', 'datetime', 'pathlib', 'logging', 're'
    ],
    hookspath=[],
    hooksconfig={{}},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz, a.scripts, a.binaries, a.zipfiles, a.datas, [],
    name='PersonalFinanceTracker',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon='app_icon.ico' if Path('app_icon.ico').exists() else None,
)
'''
        
        spec_path = self.project_root / "finance_tracker.spec"
        with open(spec_path, 'w', encoding='utf-8') as f:
            f.write(spec_content)
        
        print("✓ Spec file created")
        return spec_path
    
    def build_executable(self):
        """Build the executable"""
        print("🔨 Building executable...")
        
        spec_path = self.create_spec_file()
        
        try:
            subprocess.check_call([
                sys.executable, "-m", "PyInstaller", 
                "--clean", str(spec_path)
            ])
            print("✓ Executable built successfully!")
            return True
        except subprocess.CalledProcessError as e:
            print(f"✗ Build failed: {e}")
            return False
    
    def create_distribution_files(self):
        """Create distribution support files"""
        print("📦 Creating distribution files...")
        
        # Create installer
        installer_content = '''@echo off
echo Installing Personal Finance Tracker...
echo.

REM Create application directory
if not exist "%USERPROFILE%\\PersonalFinanceTracker" (
    mkdir "%USERPROFILE%\\PersonalFinanceTracker"
)

REM Copy files
xcopy /E /I /Y "PersonalFinanceTracker.exe" "%USERPROFILE%\\PersonalFinanceTracker\\"
xcopy /E /I /Y "README.md" "%USERPROFILE%\\PersonalFinanceTracker\\" 2>nul

REM Create desktop shortcut
echo Creating desktop shortcut...
powershell "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\\Desktop\\Personal Finance Tracker.lnk'); $Shortcut.TargetPath = '%USERPROFILE%\\PersonalFinanceTracker\\PersonalFinanceTracker.exe'; $Shortcut.Save()"

REM Create start menu shortcut
if not exist "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Personal Finance Tracker" (
    mkdir "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Personal Finance Tracker"
)
powershell "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Personal Finance Tracker\\Personal Finance Tracker.lnk'); $Shortcut.TargetPath = '%USERPROFILE%\\PersonalFinanceTracker\\PersonalFinanceTracker.exe'; $Shortcut.Save()"

echo.
echo Installation complete!
echo Desktop shortcut created
echo Start menu shortcut created
echo.
pause
'''
        
        with open(self.dist_dir / "install.bat", 'w', encoding='utf-8') as f:
            f.write(installer_content)
        
        # Create uninstaller
        uninstaller_content = '''@echo off
echo Uninstalling Personal Finance Tracker...
echo.

REM Remove shortcuts
if exist "%USERPROFILE%\\Desktop\\Personal Finance Tracker.lnk" (
    del "%USERPROFILE%\\Desktop\\Personal Finance Tracker.lnk"
    echo Desktop shortcut removed
)

if exist "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Personal Finance Tracker" (
    rmdir /s /q "%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Personal Finance Tracker"
    echo Start menu shortcut removed
)

REM Ask about removing application files
echo.
set /p remove_files="Remove application files? (y/n): "
if /i "%remove_files%"=="y" (
    if exist "%USERPROFILE%\\PersonalFinanceTracker" (
        rmdir /s /q "%USERPROFILE%\\PersonalFinanceTracker"
        echo Application files removed
    )
)

echo.
echo Uninstallation complete!
pause
'''
        
        with open(self.dist_dir / "uninstall.bat", 'w', encoding='utf-8') as f:
            f.write(uninstaller_content)
        
        # Create portable version
        portable_dir = self.dist_dir / "PersonalFinanceTracker_Portable"
        portable_dir.mkdir(exist_ok=True)
        
        # Copy executable
        exe_path = self.dist_dir / "PersonalFinanceTracker.exe"
        if exe_path.exists():
            shutil.copy2(exe_path, portable_dir)
        
        # Copy README
        readme_path = self.project_root / "README.md"
        if readme_path.exists():
            shutil.copy2(readme_path, portable_dir)
        
        # Create run script for portable
        run_script = '''@echo off
echo Starting Personal Finance Tracker...
PersonalFinanceTracker.exe
'''
        
        with open(portable_dir / "Run.bat", 'w', encoding='utf-8') as f:
            f.write(run_script)
        
        print("✓ Distribution files created")
    
    def create_zip_package(self):
        """Create ZIP package for distribution"""
        print("📦 Creating ZIP package...")
        
        timestamp = datetime.now().strftime("%Y%m%d")
        zip_name = f"PersonalFinanceTracker_v1.0_{timestamp}.zip"
        zip_path = self.project_root / zip_name
        
        with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
            # Add main executable
            exe_path = self.dist_dir / "PersonalFinanceTracker.exe"
            if exe_path.exists():
                zipf.write(exe_path, "PersonalFinanceTracker.exe")
            
            # Add installer and uninstaller
            for bat_file in ["install.bat", "uninstall.bat"]:
                bat_path = self.dist_dir / bat_file
                if bat_path.exists():
                    zipf.write(bat_path, bat_file)
            
            # Add portable version
            portable_dir = self.dist_dir / "PersonalFinanceTracker_Portable"
            if portable_dir.exists():
                for file_path in portable_dir.rglob("*"):
                    if file_path.is_file():
                        arcname = f"PersonalFinanceTracker_Portable/{file_path.relative_to(portable_dir)}"
                        zipf.write(file_path, arcname)
            
            # Add documentation
            readme_path = self.project_root / "README.md"
            if readme_path.exists():
                zipf.write(readme_path, "README.md")
        
        print(f"✓ ZIP package created: {zip_name}")
        return zip_path
    
    def build_all(self, clean=True):
        """Build everything"""
        print("🚀 Building Personal Finance Tracker Distribution")
        print("=" * 60)
        
        if clean:
            self.clean_build()
        
        # Install dependencies
        self.install_dependencies()
        
        # Create icon
        self.create_app_icon()
        
        # Build executable
        if not self.build_executable():
            return False
        
        # Create distribution files
        self.create_distribution_files()
        
        # Create ZIP package
        zip_path = self.create_zip_package()
        
        print("\n" + "=" * 60)
        print("✅ Build completed successfully!")
        print(f"\n📦 Distribution files:")
        print(f"   • {zip_path.name} (Send this to friends)")
        print(f"   • dist/PersonalFinanceTracker.exe (Standalone)")
        print(f"   • dist/PersonalFinanceTracker_Portable/ (Portable version)")
        
        return True

def main():
    parser = argparse.ArgumentParser(description='Build Personal Finance Tracker')
    parser.add_argument('--no-clean', action='store_true', help='Skip cleaning previous build')
    parser.add_argument('--icon-only', action='store_true', help='Only create the app icon')
    parser.add_argument('--exe-only', action='store_true', help='Only build executable')
    
    args = parser.parse_args()
    
    builder = FinanceTrackerBuilder()
    
    if args.icon_only:
        builder.create_app_icon()
    elif args.exe_only:
        builder.install_dependencies()
        builder.create_app_icon()
        builder.build_executable()
    else:
        success = builder.build_all(clean=not args.no_clean)
        if not success:
            sys.exit(1)

if __name__ == "__main__":
    main()