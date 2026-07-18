class PasswordGenerator {
    constructor() {
        this.initializeElements();
        this.bindEvents();
    }

    initializeElements() {
        this.uppercaseCheckbox = document.getElementById('uppercase');
        this.lowercaseCheckbox = document.getElementById('lowercase');
        this.numbersCheckbox = document.getElementById('numbers');
        this.symbolsCheckbox = document.getElementById('symbols');
        this.lengthSlider = document.getElementById('lengthSlider');
        this.lengthInput = document.getElementById('lengthInput');
        this.avoidConsecutiveCheckbox = document.getElementById('avoidConsecutive');
        this.generateBtn = document.getElementById('generateBtn');
        this.passwordOutput = document.getElementById('passwordOutput');
        this.copyBtn = document.getElementById('copyBtn');
        this.strengthFill = document.getElementById('strengthFill');
        this.strengthText = document.getElementById('strengthText');
    }

    bindEvents() {
        // Sync slider and input
        this.lengthSlider.addEventListener('input', (e) => {
            this.lengthInput.value = e.target.value;
        });

        this.lengthInput.addEventListener('input', (e) => {
            const value = Math.max(4, Math.min(50, parseInt(e.target.value) || 4));
            this.lengthInput.value = value;
            this.lengthSlider.value = value;
        });

        // Generate password
        this.generateBtn.addEventListener('click', () => {
            this.generatePassword();
        });

        // Copy to clipboard
        this.copyBtn.addEventListener('click', () => {
            this.copyToClipboard();
        });

        // Generate initial password
        this.generatePassword();
    }

    getCharacterSets() {
        const sets = {};
        
        if (this.uppercaseCheckbox.checked) {
            sets.uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
        }
        
        if (this.lowercaseCheckbox.checked) {
            sets.lowercase = 'abcdefghijklmnopqrstuvwxyz';
        }
        
        if (this.numbersCheckbox.checked) {
            sets.numbers = '0123456789';
        }
        
        if (this.symbolsCheckbox.checked) {
            sets.symbols = '!@#$%^&*()_+-=[]{}|;:,.<>?';
        }

        return sets;
    }

    generatePassword() {
        const characterSets = this.getCharacterSets();
        const setKeys = Object.keys(characterSets);
        
        if (setKeys.length === 0) {
            this.passwordOutput.value = 'Please select at least one character type';
            this.updateStrengthMeter(0);
            return;
        }

        const length = parseInt(this.lengthInput.value);
        const avoidConsecutive = this.avoidConsecutiveCheckbox.checked;
        
        let password = '';
        let consecutiveCount = 0;
        let lastType = null;

        // Create combined character set for random selection
        const allChars = Object.values(characterSets).join('');

        for (let i = 0; i < length; i++) {
            let char, currentType;
            let attempts = 0;
            
            do {
                // Pick random character set
                const randomSetKey = setKeys[Math.floor(Math.random() * setKeys.length)];
                const randomSet = characterSets[randomSetKey];
                char = randomSet[Math.floor(Math.random() * randomSet.length)];
                currentType = randomSetKey;
                attempts++;
                
                // Avoid infinite loops
                if (attempts > 50) break;
                
            } while (avoidConsecutive && 
                     currentType === lastType && 
                     consecutiveCount >= 2 && 
                     setKeys.length > 1);

            password += char;

            // Track consecutive types
            if (currentType === lastType) {
                consecutiveCount++;
            } else {
                consecutiveCount = 1;
                lastType = currentType;
            }
        }

        this.passwordOutput.value = password;
        this.updateStrengthMeter(this.calculateStrength(password, characterSets));
    }

    calculateStrength(password, characterSets) {
        let score = 0;
        const length = password.length;
        
        // Length scoring
        if (length >= 8) score += 25;
        if (length >= 12) score += 25;
        if (length >= 16) score += 25;
        
        // Character variety scoring
        const setCount = Object.keys(characterSets).length;
        score += setCount * 6.25; // Max 25 points for all 4 types
        
        // Bonus for very long passwords
        if (length >= 20) score += 10;
        
        return Math.min(100, score);
    }

    updateStrengthMeter(strength) {
        this.strengthFill.style.width = strength + '%';
        
        let color, text;
        if (strength < 30) {
            color = '#dc3545';
            text = 'Weak';
        } else if (strength < 60) {
            color = '#ffc107';
            text = 'Fair';
        } else if (strength < 80) {
            color = '#fd7e14';
            text = 'Good';
        } else {
            color = '#28a745';
            text = 'Strong';
        }
        
        this.strengthFill.style.backgroundColor = color;
        this.strengthText.textContent = `Password Strength: ${text} (${Math.round(strength)}%)`;
        this.strengthText.style.color = color;
    }

    async copyToClipboard() {
        const password = this.passwordOutput.value;
        
        if (!password || password === 'Please select at least one character type') {
            return;
        }

        try {
            await navigator.clipboard.writeText(password);
            
            // Visual feedback
            const originalText = this.copyBtn.textContent;
            this.copyBtn.textContent = '✓';
            this.copyBtn.classList.add('copied');
            
            setTimeout(() => {
                this.copyBtn.textContent = originalText;
                this.copyBtn.classList.remove('copied');
            }, 2000);
            
        } catch (err) {
            // Fallback for older browsers
            this.passwordOutput.select();
            document.execCommand('copy');
            
            const originalText = this.copyBtn.textContent;
            this.copyBtn.textContent = '✓';
            this.copyBtn.classList.add('copied');
            
            setTimeout(() => {
                this.copyBtn.textContent = originalText;
                this.copyBtn.classList.remove('copied');
            }, 2000);
        }
    }
}

// Initialize the password generator when the page loads
document.addEventListener('DOMContentLoaded', () => {
    new PasswordGenerator();
});