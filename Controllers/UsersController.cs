using Microsoft.AspNetCore.Mvc;
using ThrdCtrl2.Data;
using ThrdCtrl2.Models;

namespace ThrdCtrl2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class UsersController : Controller
    {
        private readonly UserRepository _repo;
        public UsersController(UserRepository repo)
        {
            _repo = repo;
        }

        private int? GetCurrentCompanyId()
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid)) return cid;
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult Create(User user)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("UserManagement", "Test");
            }

            // Automate CompanyID for Company Admin
            if (!User.IsInRole("Super Admin"))
            {
                user.CompanyID = GetCurrentCompanyId();
            }

            // Hash password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _repo.CreateUser(user);
            return RedirectToAction("UserManagement", "Test");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult Edit(User user)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("UserManagement", "Test");
            }

            // Security Check for Company Admin
            if (!User.IsInRole("Super Admin"))
            {
                var existingUser = _repo.GetUserById(user.UserID);
                if (existingUser == null || existingUser.CompanyID != GetCurrentCompanyId())
                {
                    return Forbid();
                }
                user.CompanyID = existingUser.CompanyID; // Ensure company doesn't change
            }

            _repo.UpdateUser(user);
            return RedirectToAction("UserManagement", "Test");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult Archive(int userId)
        {
            // Security Check for Company Admin
            if (!User.IsInRole("Super Admin"))
            {
                var existingUser = _repo.GetUserById(userId);
                if (existingUser == null || existingUser.CompanyID != GetCurrentCompanyId())
                {
                    return Forbid();
                }
            }

            _repo.ArchiveUser(userId);
            return RedirectToAction("UserManagement", "Test");
        }
    }
}
