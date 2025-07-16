using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Netflixx.Models;
using System.Text.RegularExpressions;

namespace Netflixx.Areas.Admin.Extensions
{
    static class UserExtenstions
    {
        private static readonly Regex NameRegex = new Regex(
      @"^[a-zA-Z\sÀÁẠẢÃĂẰẮẶẲẴÂẦẤẬẨẪĐÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸàáạảãăằắặẳẵâầấậẩẫđèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợỡùúụủũưừứựửữỳýỵỷỹ]+$",
      RegexOptions.Compiled);

        private static readonly Regex EmailRegex = new Regex(
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
            RegexOptions.Compiled);

        private static readonly Regex PhoneRegex = new Regex(
            @"^(0?(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9]))[0-9]{7}$",
            RegexOptions.Compiled);
        public static IQueryable<AppUserModel> ApplyFilters(
            IQueryable<AppUserModel> users,
            IQueryable<IdentityUserRole<string>> userRoles,
            IQueryable<IdentityRole> roles,
            FilterViewModel filterUser
        )
        {
            string search_name = filterUser.SearchName ?? string.Empty;
            string role_type = filterUser.RoleType ?? string.Empty;
            string sort_order = filterUser.SortOrder ?? "asc";
            string address = filterUser.Address ?? string.Empty;
            int minAge = filterUser.MinAge;
            int maxAge = filterUser.MaxAge;
            List<string> favoriteGenres = filterUser.FavoriteGenres ?? new List<string>();
            string status = filterUser.Status ?? "all";
            // Lọc theo role (nếu có chọn)

            if (!string.IsNullOrEmpty(role_type) && role_type != "all")
            {
                users = from u in users
                        join ur in userRoles on u.Id equals ur.UserId
                        join r in roles on ur.RoleId equals r.Id
                        where r.Name == role_type
                        select u;
            }

            // Lọc theo tên'

            if (!string.IsNullOrEmpty(search_name))
            {
                users = users.Where(u =>
                    u.FullName != null &&
                   u.FullName.ToLower().Contains(search_name.ToLower())
                );
            }


            // Lọc theo địa chỉ
            if (!string.IsNullOrEmpty(address))
            {
                users = users.Where(u => !string.IsNullOrEmpty(u.Address) &&
                                         u.Address.ToLower().Contains(address.ToLower()));
            }

            // Lọc theo tuổi (nếu age > 0)

            if (minAge <= maxAge && minAge > 0 && maxAge <= 125)
            {
                var today = DateTime.Today; // dùng Today thay vì Now để tránh tính cả thời gian
                var startDate = today.AddYears(-maxAge);
                var endDate = today.AddYears(-minAge);

                users = users.Where(u =>
                    u.DateOfBirth.HasValue &&
                    u.DateOfBirth.Value.Date >= startDate &&
                    u.DateOfBirth.Value.Date <= endDate
                );
            }




            // Lọc theo thể loại yêu thích (nếu có)
            if (favoriteGenres != null && favoriteGenres.Count > 0)
            {

                users = users
                    .Where(u => favoriteGenres.Any(genre => u.FavoriteGenres.Contains(genre)))
                    .AsEnumerable()
                    .Where(u =>
                    {
                        var userGenres = u.FavoriteGenres?
                             .Split(',')
                             .Select(g => g.Trim())
                             .ToList() ?? new List<string>();
                        return favoriteGenres.All(genre => userGenres.Contains(genre));
                    })
                .AsQueryable();

            }

            // Sắp xếp theo tên
            if (sort_order == "asc")
            {
                users = users.OrderBy(u => u.FullName);
            }
            else if (sort_order == "desc")
            {
                users = users.OrderByDescending(u => u.FullName);
            }
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                if (status == "active")
                    users = users.Where(u => !(u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow));
                else if (status == "inactive")
                    users = users.Where(u => (u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow));
            }
            return users;
        }

        public static (bool IsValid, List<string> Errors) ValidateUser(UserViewModel user)
        {
            var errors = new List<string>();

            // Trim all string inputs
            string role = user.Role?.Trim() ?? "";
            string name = user.Name?.Trim() ?? "";
            string email = user.Email?.Trim() ?? "";
            string pwd = user.Password?.Trim() ?? "";
            string phone = user.PhoneNumber?.Trim() ?? "";
            string address = user.Address?.Trim() ?? "";
            DateTime? dob = user.dateOfBirth;

            // 1. Role phải có
            if (string.IsNullOrEmpty(role))
                errors.Add("Vui lòng chọn vai trò của bạn.");

            bool isManager = role.Equals("manager", StringComparison.OrdinalIgnoreCase);

            // 2. Nếu Manager, các trường bắt buộc
            if (isManager)
            {
                if (string.IsNullOrEmpty(name))
                    errors.Add("Họ và tên không được để trống.");

                if (string.IsNullOrEmpty(email))
                    errors.Add("Email không được để trống.");

                if (string.IsNullOrEmpty(pwd))
                    errors.Add("Mật khẩu không được để trống.");

                if (!dob.HasValue)
                    errors.Add("Ngày sinh không được để trống.");

                if (string.IsNullOrEmpty(address))
                    errors.Add("Địa chỉ chi tiết không được để trống.");

                if (string.IsNullOrEmpty(phone))
                    errors.Add("Số điện thoại không được để trống.");
                if (string.IsNullOrEmpty(address))
                    errors.Add("Địa chỉ không được để trống");
            
            }

            // 3. Nếu User (hoặc Manager), tiếp tục validate format khi có giá trị
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Length < 2 || name.Length > 64 || !NameRegex.IsMatch(name))
                    errors.Add("Họ và tên phải 2–64 ký tự và chỉ bao gồm chữ cái, khoảng trắng.");
            }

            if (!string.IsNullOrEmpty(email) && !EmailRegex.IsMatch(email))
                errors.Add("Địa chỉ email không hợp lệ.");

            if (!string.IsNullOrEmpty(pwd))
            {
                if (pwd.Length < 8)
                    errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
                if (!pwd.Any(char.IsUpper))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ cái viết hoa.");
                if (!pwd.Any(char.IsLower))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ cái viết thường.");
                if (!pwd.Any(char.IsDigit))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ số.");
                if (pwd.All(char.IsLetterOrDigit))
                    errors.Add("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");
            }

     

            if (!string.IsNullOrEmpty(phone) && !PhoneRegex.IsMatch(phone))
                errors.Add("Số điện thoại không hợp lệ (ví dụ: 0xxxxxxxxx, 10 số).");

            // 4. Validate DOB
            if (dob.HasValue)
            {
                var d = dob.Value.Date;
                var today = DateTime.Today;
                if (d > today)
                    errors.Add("Ngày sinh không được sau ngày hiện tại.");

                int age = today.Year - d.Year;
                if (d > today.AddYears(-age)) age--;
                if (age < 6)
                    errors.Add("Người dùng phải đủ 6 tuổi trở lên.");
            }

            bool isValid = !errors.Any();
            return (isValid, errors);
        }
        public static (bool IsValid, List<string> Errors) ValidateUserForUpdate(UserViewModel user)
        {
            var errors = new List<string>();

            // Trim inputs
            string role = user.Role?.Trim() ?? "";
            string name = user.Name?.Trim() ?? "";
            string email = user.Email?.Trim() ?? "";
            string pwd = user.Password?.Trim() ?? "";
            string phone = user.PhoneNumber?.Trim() ?? "";
            string address = user.Address?.Trim() ?? "";
            DateTime? dob = user.dateOfBirth;

            if (string.IsNullOrEmpty(role))
                errors.Add("Vui lòng chọn vai trò của người dùng.");

            bool isManager = role.Equals("manager", StringComparison.OrdinalIgnoreCase);

            // 2. Với Manager, yêu cầu các trường cơ bản (trừ mật khẩu)
            if (isManager)
            {
                if (string.IsNullOrEmpty(name))
                    errors.Add("Họ và tên không được để trống.");

                if (string.IsNullOrEmpty(email))
                    errors.Add("Email không được để trống.");

                if (!dob.HasValue)
                    errors.Add("Ngày sinh không được để trống.");

                if (string.IsNullOrEmpty(address))
                    errors.Add("Địa chỉ không được để trống.");

                if (string.IsNullOrEmpty(phone))
                    errors.Add("Số điện thoại không được để trống.");
            }

            // 3. Kiểm tra định dạng chung (nếu có giá trị)
            if (!string.IsNullOrEmpty(name)
                && (name.Length < 2 || name.Length > 64 || !NameRegex.IsMatch(name)))
            {
                errors.Add("Họ và tên phải 2–64 ký tự và chỉ bao gồm chữ cái, khoảng trắng.");
            }

            if (!string.IsNullOrEmpty(email)
                && !EmailRegex.IsMatch(email))
            {
                errors.Add("Địa chỉ email không hợp lệ.");
            }

            // 4. Mật khẩu: CHỈ kiểm tra khi user muốn đổi (pwd không rỗng)
            if (!string.IsNullOrEmpty(pwd))
            {
                if (pwd.Length < 8)
                    errors.Add("Mật khẩu phải có ít nhất 8 ký tự.");
                if (!pwd.Any(char.IsUpper))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ cái viết hoa.");
                if (!pwd.Any(char.IsLower))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ cái viết thường.");
                if (!pwd.Any(char.IsDigit))
                    errors.Add("Mật khẩu phải chứa ít nhất một chữ số.");
                if (pwd.All(char.IsLetterOrDigit))
                    errors.Add("Mật khẩu phải chứa ít nhất một ký tự đặc biệt.");
            }

            // 5. SĐT
            if (!string.IsNullOrEmpty(phone)
                && !PhoneRegex.IsMatch(phone))
            {
                errors.Add("Số điện thoại không hợp lệ (ví dụ: 0xxxxxxxxx).");
            }

            // 6. Ngày sinh
            if (dob.HasValue)
            {
                var d = dob.Value.Date;
                var today = DateTime.Today;
                if (d > today)
                    errors.Add("Ngày sinh không được sau ngày hiện tại.");

                int age = today.Year - d.Year;
                if (d > today.AddYears(-age)) age--;
                if (age < 6)
                    errors.Add("Người dùng phải đủ 6 tuổi trở lên.");
            }

            bool isValid = !errors.Any();
            return (isValid, errors);
        }

    }

}
