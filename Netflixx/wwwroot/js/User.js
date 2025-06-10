document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("userForm");
    if (!form) return;

    const inputs = form.querySelectorAll("input[readonly], input[type='hidden']");
    const radioButtons = form.querySelectorAll("input[name='IsActive']");
    const saveBtn = document.getElementById("saveBtn");
    const cancelBtn = document.getElementById("cancelBtn");

    // Lưu giá trị ban đầu
    let originalValues = {};
    inputs.forEach(input => originalValues[input.name] = input.value);

    const initialRadio = form.querySelector("input[name='IsActive']:checked");
    if (initialRadio) {
        originalValues["IsActive"] = initialRadio.value;
    }

    // Cho phép chỉnh sửa input khi focus (các input text)
    inputs.forEach(input => {
        if (input.hasAttribute("readonly")) {
            input.addEventListener("focus", () => {
                input.removeAttribute("readonly");
            });
            input.addEventListener("input", checkChanges);
        }
    });

    // Lắng nghe radio button
    radioButtons.forEach(radio => {
        radio.addEventListener("change", checkChanges);
    });

    // Chọn role từ dropdown
    window.selectRole = function (role) {
        document.getElementById("dropdownRoleButton").innerText = role;
        const hidden = document.getElementById("SelectedRole");
        hidden.value = role;
        checkChanges();
    };

    // Kiểm tra xem có thay đổi dữ liệu không
    function checkChanges() {
        let changed = Array.from(inputs).some(input => input.value !== originalValues[input.name]);

        const selectedRadio = form.querySelector("input[name='IsActive']:checked");
        if (selectedRadio && selectedRadio.value !== originalValues["IsActive"]) {
            changed = true;
        }

        if (changed) {
            saveBtn.classList.remove("d-none");
            cancelBtn.classList.remove("d-none");
        } else {
            saveBtn.classList.add("d-none");
            cancelBtn.classList.add("d-none");
        }
    }

    // Nút Cancel
    cancelBtn.addEventListener("click", () => {
        inputs.forEach(input => {
            input.value = originalValues[input.name];
            if (input.hasAttribute("readonly")) {
                input.setAttribute("readonly", "readonly");
            }
        });

        document.getElementById("dropdownRoleButton").innerText = originalValues["Role"] || "Select Role";

        radioButtons.forEach(radio => {
            radio.checked = radio.value === originalValues["IsActive"];
        });

        saveBtn.classList.add("d-none");
        cancelBtn.classList.add("d-none");
    });
});