// Send OTP functionality
document.addEventListener('DOMContentLoaded', function () {
    const sendOtpBtn = document.getElementById('sendOtpBtn');
    if (sendOtpBtn) {
        sendOtpBtn.addEventListener('click', async function () {
            const email = document.getElementById('emailInput').value;
            if (!email) {
                alert('Vui lòng nhập email');
                return;
            }

            try {
                const response = await fetch('/Login/SendSignUpOtp', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email: email })
                });

                const result = await response.json();
                if (result.success) {
                    alert('Mã OTP đã được gửi!');
                } else {
                    alert(result.message || 'Lỗi gửi OTP');
                }
            } catch (error) {
                console.error('Error:', error);
                alert('Đã xảy ra lỗi khi gửi OTP');
            }
        });
    }

    // Password validation
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
        });

        // Trigger validation on page load
        passwordInput.dispatchEvent(new Event('input'));
    }

    function updateRequirement(elementId, isValid) {
        const element = document.getElementById(elementId);
        if (element) {
            element.classList.remove('requirement-valid', 'requirement-invalid');
            element.classList.add(isValid ? 'requirement-valid' : 'requirement-invalid');
        }
    }
});

document.addEventListener('DOMContentLoaded', function () {
    const sendOtpBtn = document.getElementById('sendOtpBtn');
    const emailInput = document.getElementById('emailInput');
    const attemptsLeftSpan = document.getElementById('attemptsLeft');
    const otpAttemptsMessage = document.getElementById('otpAttemptsMessage');
    const MAX_OTP_ATTEMPTS = 5;

    sendOtpBtn.addEventListener('click', async function () {
        const email = emailInput.value.trim();

        if (!email) {
            Swal.fire({
                icon: 'error',
                title: 'Lỗi',
                text: 'Vui lòng nhập email',
                confirmButtonColor: '#e50914'
            });
            return;
        }

        try {
            const response = await fetch('/Login/SendSignUpOtp', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({ email: email })
            });

            const data = await response.json();

            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Thành công',
                    text: 'Mã OTP đã được gửi đến email của bạn',
                    confirmButtonColor: '#e50914'
                });

                // Cập nhật số lần còn lại
                attemptsLeftSpan.textContent = data.attemptsLeft;
                otpAttemptsMessage.style.display = 'block';

                // Kiểm tra nếu đạt giới hạn
                if (data.attemptsLeft <= 0) {
                    disableOtpButton();
                }
            } else {
                if (data.isLocked) {
                    disableOtpButton();
                }
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: data.message,
                    confirmButtonColor: '#e50914'
                });
            }
        } catch (error) {
            console.error('Error:', error);
            
        }
    });

    function disableOtpButton() {
        sendOtpBtn.disabled = true;
        sendOtpBtn.textContent = 'Đã vượt quá giới hạn';
        sendOtpBtn.style.backgroundColor = '#ccc';
        sendOtpBtn.style.cursor = 'not-allowed';

        // Kích hoạt lại sau 30 phút
        setTimeout(() => {
            sendOtpBtn.disabled = false;
            sendOtpBtn.textContent = 'Gửi mã OTP';
            sendOtpBtn.style.backgroundColor = '';
            sendOtpBtn.style.cursor = '';
            attemptsLeftSpan.textContent = MAX_OTP_ATTEMPTS;
        }, 30 * 60 * 1000); // 30 phút
    }

    // Kiểm tra email hợp lệ
    function isValidEmail(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    }
});