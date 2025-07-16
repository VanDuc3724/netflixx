let lastScrollTop = 0;
const header = document.querySelector('header');
let isTicking = false;
const hideThreshold =   100; // Ngưỡng bắt đầu ẩn

window.addEventListener('scroll', function () {
    const currentScroll = window.pageYOffset || document.documentElement.scrollTop;

    if (!isTicking) {
        window.requestAnimationFrame(() => {
            if (currentScroll > lastScrollTop + 10 && currentScroll > hideThreshold) {
                // Cuộn xuống và vượt ngưỡng → ẩn
                header.classList.add('hide');
            } else if (currentScroll < lastScrollTop - 10) {
                // Cuộn lên → hiện lại
                header.classList.remove('hide');
            }
            lastScrollTop = currentScroll <= 0 ? 0 : currentScroll;
            isTicking = false;
        });

        isTicking = true;
    }
});
