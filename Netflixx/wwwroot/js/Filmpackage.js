let filmpackage = [
    {
        packageId: "package01",
        packageName: "Cơ Bản",
        packagePrice: 0,
        features: [
            "Xem một số phim và chương trình miễn phí",
            "Chất lượng video HD (720p)",
            "× Tải xuống",
            "× Không quảng cáo",
            "× Không giới hạn thiết bị"
        ],
        btnClass: "basic-btn"
    },
    {
        packageId: "package02",
        packageName: "Tiêu Chuẩn",
        packagePrice: 99000,
        features: [
            "Tất cả tính năng của gói Cơ Bản",
            "Thoải mái xem tất cả các phim",
            "Chất lượng video Full HD (1080p)",
            "Xem trên 2 thiết bị cùng lúc",
            "Không quảng cáo",
            "Tải xuống"
        ],
        btnClass: "standard-btn"
    },
    {
        packageId: "package03",
        packageName: "Cao Cấp",
        packagePrice: 149000,
        features: [
            "Tất cả tính năng của gói Tiêu Chuẩn",
            "Xem trên 4 thiết bị cùng lúc",
            "Chất lượng video Ultra HD (4K) và HDR",
            "Âm thanh Dolby Atmos",
            "Hỗ trợ khách hàng nhanh (ưu tiên)"
        ],
        btnClass: "premium-btn"
    }
];

document.addEventListener('DOMContentLoaded', () => {
    const packageContainer = document.querySelector('.package-container');

    filmpackage.forEach(pkg => {
        // Tạo HTML cho mỗi gói
        const card = document.createElement('div');
        card.classList.add('package-card');

        const headerClass = pkg.packageName === "Cơ Bản" ? "basic" :
            pkg.packageName === "Tiêu Chuẩn" ? "standard" : "premium";

        // Tạo phần header
        const header = `
            <div class="package-header ${headerClass}">
                <h2>${pkg.packageName}</h2>
                <div class="price">${pkg.packagePrice.toLocaleString()} coins <span>/tháng</span></div>
            </div>
        `;

        // Tạo danh sách tính năng
        const features = pkg.features.map(feature => {
            const icon = feature.startsWith("×") ? "×" : "✓";
            const text = feature.replace(/^✓ |^× /, '');
            return `
                <div class="feature">
                    <div class="feature-icon">${icon}</div>
                    <div class="feature-text">${text}</div>
                </div>
            `;
        }).join("");

        const featuresHTML = `<div class="package-features">${features}</div>`;

        // Thêm data attributes để truyền dữ liệu cho trang thanh toán
        const buttonHTML = `<button class="${pkg.btnClass}" data-id="${pkg.packageId}" data-name="${pkg.packageName}" data-price="${pkg.packagePrice}">Đăng ký ngay</button>`;

        card.innerHTML = header + featuresHTML + buttonHTML;

        // Thêm thẻ vào container
        packageContainer.appendChild(card);
    });

    // Sự kiện click cho toàn bộ container (event delegation)
    packageContainer.addEventListener('click', (e) => {
        if (e.target.tagName === 'BUTTON') {
            const btn = e.target;

            const packageId = btn.dataset.id;
            const packageName = encodeURIComponent(btn.dataset.name);
            const packagePrice = btn.dataset.price;

            // Điều hướng đến trang thanh toán với thông tin gói
            window.location.href = `/Filmpackage/Buy?packageId=${packageId}&packageName=${packageName}&packagePrice=${packagePrice}`;
        }
    });
});
