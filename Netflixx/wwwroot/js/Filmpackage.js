document.addEventListener('DOMContentLoaded', () => {
    const packageContainer = document.querySelector('.package-container');

    fetch('/Filmpackage/GetPackages')
        .then(res => res.json())
        .then(data => {
            data.forEach(pkg => {
                const card = document.createElement('div');
                card.classList.add('package-card');
                const headerClass = pkg.name === 'Cơ Bản' ? 'basic' :
                    pkg.name === 'Tiêu Chuẩn' ? 'standard' : 'premium';
                const header = `
                    <div class="package-header ${headerClass}">
                        <h2>${pkg.name}</h2>
                        <div class="price">${Number(pkg.price).toLocaleString()}đ <span>/tháng</span></div>
                    </div>`;
                const description = pkg.description ? `<p>${pkg.description}</p>` : '';
                const btnClass = headerClass + '-btn';
                const buttonHTML = `<button class="${btnClass}" data-id="${pkg.id}" data-name="${pkg.name}" data-price="${pkg.price}">Đăng ký ngay</button>`;
                card.innerHTML = header + description + buttonHTML;
                packageContainer.appendChild(card);
            });
        });

    packageContainer.addEventListener('click', (e) => {
        if (e.target.tagName === 'BUTTON') {
            const btn = e.target;
            const packageId = btn.dataset.id;
            const packageName = encodeURIComponent(btn.dataset.name);
            const packagePrice = btn.dataset.price;
            window.location.href = `/Filmpackage/Buy?packageId=${packageId}&packageName=${packageName}&packagePrice=${packagePrice}`;
        }
    });
});
