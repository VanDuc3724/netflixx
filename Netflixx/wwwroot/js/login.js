document.addEventListener('DOMContentLoaded', function () {
    const passwordInput = document.getElementById('passwordInput');
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            const password = this.value;
            const requirements = {
                length: password.length >= 8,
                uppercase: /[A-Z]/.test(password),
                number: /[0-9]/.test(password),
                special: /[^A-Za-z0-9]/.test(password)
            };

            updateRequirement('lengthRequirement', requirements.length);
            updateRequirement('uppercaseRequirement', requirements.uppercase);
            updateRequirement('numberRequirement', requirements.number);
            updateRequirement('specialRequirement', requirements.special);

            function updateRequirement(elementId, isValid) {
                const element = document.getElementById(elementId);
                element.classList.remove('requirement-valid', 'requirement-invalid');
                element.classList.add(isValid ? 'requirement-valid' : 'requirement-invalid');
            }
        });

        // Trigger once on load
        passwordInput.dispatchEvent(new Event('input'));
    }
});
