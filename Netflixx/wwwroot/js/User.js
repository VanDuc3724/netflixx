// =====================================
// Utility Functions (Các hàm tiện ích)
// =====================================

// Utility: fetch JSON with error handling and fallback data
// Hàm này được dùng để gửi yêu cầu HTTP GET đến một URL API
// và mong đợi nhận lại dữ liệu JSON.
// Nó bao gồm xử lý lỗi và cung cấp dữ liệu mẫu (mock data)
// trong trường hợp fetch API thất bại, giúp ứng dụng không bị lỗi
// khi backend không phản hồi hoặc có vấn đề.
async function fetchJson(url) {
    try {
        const resp = await fetch(url); // Gửi yêu cầu fetch đến URL
        if (!resp.ok) {
            // Nếu phản hồi không thành công (ví dụ: status 404, 500), ném lỗi
            throw new Error(resp.statusText);
        }
        return await resp.json(); // Phân tích phản hồi thành JSON và trả về
    } catch (err) {
        console.error('Fetch error:', err); // Ghi lỗi ra console để debug

        // --- Cung cấp dữ liệu mẫu (Mock data) khi fetch thất bại ---
        // Điều này rất hữu ích cho việc phát triển và thử nghiệm
        // khi API backend chưa sẵn sàng hoặc gặp sự cố.
        if (url.includes('/api/users/totalCount')) return { count: 482 };
        if (url.includes('/api/users/activeCount')) return { count: 350 };
        if (url.includes('/api/users/newThisMonth')) return { count: 45 };
        if (url.includes('/api/users/inactiveCount')) return { count: 142 };
        if (url.includes('/api/users/totalByMonth')) return { labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'], data: [50, 60, 75, 90, 100, 120] };
        if (url.includes('/api/users/countByRole')) return { labels: ['Admin', 'Editor', 'User'], data: [10, 30, 60] };
        if (url.includes('/api/users/newByMonth')) return { labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'], data: [5, 8, 12, 10, 15, 20] };
        if (url.includes('/api/users/averageUsageByDay')) return { labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'], data: [45, 50, 48, 55, 60, 52, 40] };
        if (url.includes('/api/users/activeInactiveByMonth')) return { labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'], active: [40, 50, 60, 70, 80, 90], inactive: [10, 10, 15, 20, 20, 30] };
        // Dữ liệu mẫu cho danh sách người dùng khi '/api/users' bị lỗi
        if (url.includes('/api/users')) return {
            items: [
                { id: 'U001', fullName: 'Nguyễn Văn A', email: 'nguyenvana@example.com', role: 'Admin', status: 'Active', statusColor: '#10B981', createdAt: '2025-01-15' },
                { id: 'U002', fullName: 'Trần Thị B', email: 'tranb@example.com', role: 'Editor', status: 'Active', statusColor: '#10B981', createdAt: '2025-02-20' },
                { id: 'U003', fullName: 'Lê Văn C', email: 'levanc@example.com', role: 'User', status: 'Inactive', statusColor: '#EF4444', createdAt: '2025-03-10' },
                { id: 'U004', fullName: 'Phạm Thị D', email: 'phamd@example.com', role: 'User', status: 'Active', statusColor: '#10B981', createdAt: '2025-04-01' },
                { id: 'U005', fullName: 'Hoàng Văn E', email: 'hoange@example.com', role: 'Admin', status: 'Active', statusColor: '#10B981', createdAt: '2025-05-05' },
                { id: 'U006', fullName: 'Nguyễn Thị F', email: 'nguyenf@example.com', role: 'Editor', status: 'Inactive', statusColor: '#EF4444', createdAt: '2025-06-10' },
                { id: 'U007', fullName: 'Bùi Văn G', email: 'buig@example.com', role: 'User', status: 'Active', statusColor: '#10B981', createdAt: '2025-07-18' },
                { id: 'U008', fullName: 'Đỗ Thị H', email: 'doh@example.com', role: 'User', status: 'Active', statusColor: '#10B981', createdAt: '2025-08-22' },
                { id: 'U009', fullName: 'Vũ Văn I', email: 'vui@example.com', role: 'Admin', status: 'Inactive', statusColor: '#EF4444', createdAt: '2025-09-01' },
                { id: 'U010', fullName: 'Trịnh Thị K', email: 'trinhk@example.com', role: 'User', status: 'Active', statusColor: '#10B981', createdAt: '2025-10-10' },
            ],
            page: 1,
            pageSize: 10,
            totalPages: 5,
            totalCount: 42
        };
        return null; // Trả về null nếu không có dữ liệu mẫu phù hợp
    }
}

// =====================================
// Metrics Initialization (Khởi tạo các chỉ số thống kê)
// =====================================

// Hàm này lấy các chỉ số tổng hợp về người dùng từ API
// và cập nhật chúng vào các phần tử HTML tương ứng trên giao diện.
async function initMetrics() {
    // Gửi các yêu cầu fetch đồng thời để lấy các số liệu
    const item = await fetchJson('/admin/user/StaticCount');
    const totalRes = item.totalCount ?? 0;
    const activeRes = item.activeCount ?? 0;
    const newRes = item.newThisWeekCount ?? 0;
    const totalpaid = item.totalPaid ?? 0;

    // Cập nhật nội dung của các phần tử HTML (có id tương ứng)
    document.getElementById('totalUsersMetricLine1').textContent = totalRes;
    document.getElementById('newUsersMetricLine1').textContent = newRes;
    document.getElementById('PaidUsersMetricLine1').textContent = totalpaid;
    document.getElementById('activeUsersMetricLine1').textContent = activeRes;

    document.getElementById('totalUsersMetricLine2').textContent = '20%';
    document.getElementById('newUsersMetricLine2').textContent = '30%';
    document.getElementById('PaidUsersMetricLine2').textContent = '40%';
    document.getElementById('activeUsersMetricLine2').textContent = '50%';

    // Các dòng bổ sung có thể được thiết lập tương tự nếu API cung cấp thêm dữ liệu
}
//
function activeSidebar() {
    document.getElementById('sidebar-user').classList.add('active'); // Thêm class 'active' vào sidebar người dùng
}
// =====================================
// Chart Initialization (Khởi tạo biểu đồ)
// =====================================

// Hàm này lấy dữ liệu từ API và sử dụng thư viện Chart.js
// để vẽ các biểu đồ khác nhau trên dashboard.
async function initCharts() {
    // 1. Biểu đồ tổng số user theo tháng (Line Chart)
    const totalRes = await fetchJson('/api/users/totalByMonth');
    const ctx1 = document.getElementById('totalUsersChart').getContext('2d');
    const labelsMonths = totalRes?.labels || []; // Lấy nhãn tháng, nếu không có thì là mảng rỗng
    const totalData = totalRes?.data || []; // Lấy dữ liệu tổng, nếu không có thì là mảng rỗng
    new Chart(ctx1, {
        type: 'line', // Loại biểu đồ: đường
        data: {
            labels: labelsMonths, // Nhãn cho trục X (tháng)
            datasets: [{
                label: 'Tổng số user',
                data: totalData,
                borderColor: 'rgba(75,192,192,1)',
                backgroundColor: 'rgba(75,192,192,0.2)',
                tension: 0.3, // Độ cong của đường
                fill: true, // Tô màu bên dưới đường
                pointRadius: 4 // Kích thước điểm dữ liệu
            }]
        },
        options: {
            responsive: true, // Biểu đồ sẽ tự điều chỉnh kích thước theo container
            maintainAspectRatio: false, // Không duy trì tỷ lệ khung hình cố định
            plugins: {
                tooltip: { // Cấu hình tooltip khi hover
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    display: false // Không hiển thị chú giải
                }
            },
            interaction: {
                mode: 'nearest', // Chế độ tương tác khi hover
                axis: 'x', // Chỉ tương tác trên trục X
                intersect: false
            },
            scales: { // Cấu hình trục X và Y
                y: {
                    beginAtZero: true, // Bắt đầu trục Y từ 0
                    title: {
                        display: true,
                        text: 'Tổng số người dùng' // Tiêu đề trục Y
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Tháng' // Tiêu đề trục X
                    }
                }
            }
        }
    });

    // 2. Biểu đồ phân bố theo vai trò (Pie Chart)
    const roleRes = await fetchJson('/api/users/countByRole');
    const ctx2 = document.getElementById('rolePieChart').getContext('2d');
    const roleLabels = roleRes?.labels || [];
    const roleData = roleRes?.data || [];
    new Chart(ctx2, {
        type: 'pie', // Loại biểu đồ: hình tròn
        data: {
            labels: roleLabels, // Nhãn cho các lát cắt (vai trò)
            datasets: [{
                data: roleData,
                backgroundColor: ['rgba(255,99,132,0.7)', 'rgba(54,162,235,0.7)', 'rgba(255,206,86,0.7)'], // Màu nền các lát cắt
                borderColor: ['rgba(255,99,132,1)', 'rgba(54,162,235,1)', 'rgba(255,206,86,1)'], // Màu viền các lát cắt
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom' // Vị trí chú giải: dưới cùng
                },
                tooltip: {
                    callbacks: {
                        label: function (ctx) { // Tùy chỉnh nội dung tooltip
                            const label = ctx.label || '';
                            const val = ctx.raw;
                            const sum = ctx.chart._metasets[ctx.datasetIndex]?.total || 0;
                            const pct = sum ? ((val / sum * 100).toFixed(1)) + '%' : '';
                            return label + ': ' + val + ': ' + pct + '%'; // Hiển thị nhãn, giá trị và phần trăm
                        }
                    }
                }
            }
        }
    });

    // 3. Biểu đồ số user mới theo tháng (Bar Chart)
    const newMonthRes = await fetchJson('/api/users/newByMonth');
    const ctx3 = document.getElementById('newUsersChart').getContext('2d');
    const newLabels = newMonthRes?.labels || labelsMonths;
    const newData = newMonthRes?.data || [];
    new Chart(ctx3, {
        type: 'bar', // Loại biểu đồ: cột
        data: {
            labels: newLabels,
            datasets: [{
                label: '    Số user mới',
                data: newData,
                backgroundColor: 'rgba(54,162,235,0.7)',
                borderColor: 'rgba(54,162,235,1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Số user mới'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Tháng'
                    }
                }
            }
        }
    });

    // 4. Biểu đồ thời gian sử dụng trung bình/ngày (Line Chart)
    const usageRes = await fetchJson('/api/users/averageUsageByDay');
    const ctx4 = document.getElementById('usageTimeChart').getContext('2d');
    const usageLabels = usageRes?.labels || [];
    const usageData = usageRes?.data || [];
    new Chart(ctx4, {
        type: 'line',
        data: {
            labels: usageLabels,
            datasets: [{
                label: 'Thời gian sử dụng trung bình (phút)',
                data: usageData,
                borderColor: 'rgba(255,159,64,1)',
                backgroundColor: 'rgba(255,159,64,0.2)',
                tension: 0.3,
                fill: true,
                pointRadius: 5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function (c) {
                            return c.dataset.label + ': ' + c.parsed.y + ' phút'; // Tùy chỉnh tooltip hiển thị "phút"
                        }
                    }
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Thời gian (phút)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Ngày'
                    }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });

    // 5. Biểu đồ Active vs Inactive Users theo tháng (Bar Chart - Stacked)
    const aiRes = await fetchJson('/api/users/activeInactiveByMonth');
    const ctx5 = document.getElementById('activeInactiveChart').getContext('2d');
    const aiLabels = aiRes?.labels || [];
    const active = aiRes?.active || [];
    const inactive = aiRes?.inactive || [];
    new Chart(ctx5, {
        type: 'bar',
        data: {
            labels: aiLabels,
            datasets: [{
                label: 'Active',
                data: active,
                backgroundColor: 'rgba(75,192,192,0.7)'
            }, {
                label: 'Inactive',
                data: inactive,
                backgroundColor: 'rgba(255,99,132,0.7)'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                x: {
                    stacked: true // Xếp chồng các cột trên trục X
                },
                y: {
                    stacked: true, // Xếp chồng các cột trên trục Y
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Số lượng Users'
                    }
                }
            }
        }
    });
}

// =====================================
// Table Data Loading (Tải dữ liệu bảng)
// =====================================

// Hàm này tải dữ liệu người dùng từ API và hiển thị chúng trong một bảng HTML,
// bao gồm cả chức năng phân trang.
async function loadUsersTable(page = 1, search = null) { // Mặc định tải trang 1
    const res = await fetchJson(`/admin/user/getUsers?page=${page}&pageSize=10&search=${search}`); // Gửi yêu cầu fetch
    if (!res) return; // Nếu không có dữ liệu trả về, dừng hàm

    const tbody = document.getElementById('usersTbody'); // Lấy phần tử tbody của bảng
    tbody.innerHTML = ''; // Xóa sạch nội dung cũ của bảng

    // Duyệt qua từng người dùng trong dữ liệu nhận được và thêm vào bảng
    res.items.forEach(user => {
        const tr = document.createElement('tr'); // Tạo một hàng mới (<tr>)
        // Đặt nội dung HTML cho hàng, bao gồm dữ liệu người dùng
        // và các nút action (chỉnh sửa, xóa) với SVG icon
        tr.innerHTML = `
          
            <td>${user.fullName}</td>
            <td>${user.email}</td>
            <td>${user.role}</td>
            <td><span class="status-pill" style="background:${user.statusColor};">${user.status}</span></td>
            <td>${user.createdAt}</td>
            <td>
                <button class="action-icon edit" title="Edit">
                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="white" viewBox="0 0 24 24">
                        <path d="M19.14 12.94c.04-.3.06-.61.06-.94s-.02-.64-.06-.94l2.03-1.58a.5.5 0 0 0 .12-.66l-1.92-3.32a.5.5 0 0 0-.61-.22l-2.39.96a6.98 6.98 0 0 0-1.62-.94l-.36-2.54A.5.5 0 0 0 14.3 2h-4.6a.5.5 0 0 0-.49.42l-.36 2.54a7.03 7.03 0 0 0-1.62.94l-2.39-.96a.5.5 0 0 0-.61.22L2.7 8.48a.5.5 0 0 0 .12.66l2.03 1.58c-.04.3-.06.61-.06.94s.02.64.06.94l-2.03 1.58a.5.5 0 0 0-.12.66l1.92 3.32c.14.25.43.34.68.22l2.39-.96c.5.38 1.04.7 1.62.94l.36 2.54c.05.27.26.46.49.46h4.6c.24 0 .44-.19.49-.46l.36-2.54c.58-.24 1.12-.56 1.62-.94l2.39.96c.25.12.54.03.68-.22l1.92-3.32a.5.5 0 0 0-.12-.66l-2.03-1.58zM12 15.5a3.5 3.5 0 1 1 0-7 3.5 3.5 0 0 1 0 7z"/>
                    </svg>
                </button>
                <button class="action-icon delete" title="Delete">
                    <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M15 7V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3" />
                    </svg>
                </button>
            </td>
        `;
        tbody.appendChild(tr); // Thêm hàng vào tbody
    });

    setupTableActions(); // Gọi hàm để thiết lập các sự kiện click cho các nút action
    setupPagination(res.page, res.totalPage, res.totalCount, search); // Cập nhật và thiết lập phân trang
}

// Hàm này thiết lập các sự kiện click cho các nút "Edit" và "Delete"
// trong mỗi hàng của bảng người dùng.
function setupTableActions() {
    // Lấy tất cả các nút có class 'action-icon edit' và gán sự kiện click
    document.querySelectorAll('.action-icon.edit').forEach(btn => {
        btn.onclick = () => {
            // Thay thế alert này bằng logic chỉnh sửa thực tế,
            // ví dụ: mở modal chỉnh sửa, chuyển hướng đến trang chỉnh sửa.
            alert('Implement edit for user');
        };
    });
    // Lấy tất cả các nút có class 'action-icon delete' và gán sự kiện click
    document.querySelectorAll('.action-icon.delete').forEach(btn => {
        btn.onclick = () => {
            if (confirm('Xác nhận xóa người dùng này?')) {
                // Thay thế alert này bằng logic xóa người dùng thực tế
                // thông qua API (ví dụ: gửi yêu cầu DELETE đến backend)
                alert('Implement delete via API');
            }
        };
    });
}

// Hàm này tạo và cập nhật các nút phân trang
// dựa trên trang hiện tại, tổng số trang và tổng số người dùng.
function setupPagination(currentPage, totalPages, totalCount, search = null) {
    const paginationEl = document.getElementById('pagination'); // Lấy phần tử chứa các nút phân trang
    const tableInfo = document.getElementById('tableInfo');     // Lấy phần tử hiển thị thông tin bảng
    paginationEl.innerHTML = ''; // Xóa sạch các nút phân trang cũ
    console.log(totalPages);
    // Nút "Trang trước" (Prev)
    const prevBtn = document.createElement('button');
    prevBtn.textContent = '←'; // Dùng ký tự mũi tên
    prevBtn.disabled = currentPage <= 1; // Vô hiệu hóa nếu đang ở trang đầu tiên
    prevBtn.onclick = () => loadUsersTable(currentPage - 1, search); // Tải trang trước đó khi click
    paginationEl.appendChild(prevBtn);

    // Hiển thị các nút số trang (1, 2, 3...)
    for (let i = 1; i <= totalPages; i++) {
        const btn = document.createElement('button');
        btn.textContent = i;
        if (i === currentPage) btn.classList.add('current'); // Đánh dấu trang hiện tại
        btn.onclick = () => loadUsersTable(i, search); // Tải trang tương ứng khi click
        paginationEl.appendChild(btn);
    }

    // Nút "Trang sau" (Next)
    const nextBtn = document.createElement('button');
    nextBtn.textContent = '→'; // Dùng ký tự mũi tên
    nextBtn.disabled = currentPage >= totalPages; // Vô hiệu hóa nếu đang ở trang cuối cùng
    nextBtn.onclick = () => loadUsersTable(currentPage + 1, search); // Tải trang kế tiếp khi click
    paginationEl.appendChild(nextBtn);

    // Cập nhật thông tin hiển thị số lượng người dùng
    const start = (currentPage - 1) * 10 + 1; // Ví dụ: trang 1 (1-10), trang 2 (11-20)
    const end = Math.min(currentPage * 10, totalCount); // Đảm bảo không vượt quá tổng số lượng
    tableInfo.textContent = `Showing ${start} to ${end} of ${totalCount} users`; // Hiển thị "Showing X to Y of Z users"
}

// =====================================
// Search Functionality (Chức năng tìm kiếm)
// =====================================

// Đảm bảo element với id 'searchInput' tồn tại trước khi thêm event listener
// Listener này chờ cho DOM được tải hoàn toàn rồi mới cố gắng lấy phần tử.
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput'); // Lấy ô input tìm kiếm
    if (searchInput) { // Kiểm tra xem ô input có tồn tại không
        searchInput.addEventListener('input', function () { // Lắng nghe sự kiện 'input' (mỗi khi giá trị thay đổi)
            const q = this.value; // Lấy giá trị hiện tại của ô input

            loadUsersTable(1, q); // Load lại trang 1 với bộ lọc tìm kiếm (cần truyền tham số tìm kiếm)
        });
    }
});

// =====================================
// Add User Functionality (Chức năng thêm người dùng)
// =====================================

// Đảm bảo element với id 'addUserBtn' tồn tại trước khi thêm event listener
document.addEventListener('DOMContentLoaded', () => {
    const addUserBtn = document.getElementById('addUserBtn'); // Lấy nút "Add User"
    if (addUserBtn) { // Kiểm tra xem nút có tồn tại không
        addUserBtn.addEventListener('click', function () { // Lắng nghe sự kiện 'click'
            window.location.href = '/users/create'; // Giả lập chuyển hướng đến trang tạo người dùng mới
        });
    }
});




// =====================================
// Fullscreen Chart Toggle (Chức năng phóng to biểu đồ)
// =====================================

// Duyệt qua tất cả các nút có class 'btn-maximize'
document.querySelectorAll('.btn-maximize').forEach(btn => {
    btn.addEventListener('click', () => {
        const card = btn.closest('.chart-card'); // Tìm phần tử cha gần nhất có class 'chart-card'
        const overlay = document.getElementById('overlay'); // Lấy lớp phủ (overlay)
        const canvasId = card.querySelector('canvas').id; // Lấy ID của canvas biểu đồ bên trong card

        if (!card.classList.contains('fullscreen')) {
            // Nếu card chưa ở chế độ toàn màn hình
            card.classList.add('fullscreen'); // Thêm class 'fullscreen' để áp dụng CSS toàn màn hình
            overlay.style.display = 'block'; // Hiển thị lớp phủ mờ
            const chart = Chart.getChart(canvasId); // Lấy đối tượng biểu đồ Chart.js
            if (chart) chart.resize(); // Quan trọng: Cập nhật lại kích thước biểu đồ để nó vẽ lại đúng cách
        } else {
            // Nếu card đang ở chế độ toàn màn hình
            card.classList.remove('fullscreen'); // Xóa class 'fullscreen'
            overlay.style.display = 'none'; // Ẩn lớp phủ
            const chart = Chart.getChart(canvasId);
            if (chart) chart.resize(); // Quan trọng: Cập nhật lại kích thước biểu đồ về ban đầu
        }
    });
});

// Sự kiện click vào lớp phủ để thoát chế độ toàn màn hình
document.getElementById('overlay').addEventListener('click', () => {
    document.querySelectorAll('.chart-card.fullscreen').forEach(card => {
        card.classList.remove('fullscreen'); // Xóa class fullscreen khỏi tất cả các card đang ở chế độ này
        const chart = Chart.getChart(card.querySelector('canvas').id);
        if (chart) chart.resize(); // Cập nhật kích thước biểu đồ
    });
    document.getElementById('overlay').style.display = 'none'; // Ẩn lớp phủ
});


// =====================================
// Initial Load (Tải trang ban đầu)
// =====================================

// Sự kiện này đảm bảo rằng tất cả HTML và DOM đã được tải hoàn chỉnh
// trước khi cố gắng truy cập các phần tử và khởi tạo các chức năng.
window.addEventListener('DOMContentLoaded', () => {
    initMetrics();    // Khởi tạo các chỉ số thống kê
    initCharts();     // Khởi tạo và vẽ tất cả các biểu đồ
    loadUsersTable(1, ''); // Tải dữ liệu bảng người dùng trang đầu tiên
    activeSidebar();
});