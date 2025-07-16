using Microsoft.AspNetCore.Identity;
using Netflixx.Models;

namespace Netflixx.Areas.Admin.Extensions
{
    static class UsersFilterExtensions
    {
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
    }
}
