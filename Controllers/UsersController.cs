using Microsoft.AspNetCore.Mvc;
using ThrdCtrl2.Data;
using ThrdCtrl2.Models;

namespace ThrdCtrl2.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserRepository _repo;
        public UsersController(UserRepository repo)
        {
            _repo = repo;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("UserManagement", "Test");
            }
            // Hash password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _repo.CreateUser(user);
            return RedirectToAction("UserManagement", "Test");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User user)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("UserManagement", "Test");
            }
            _repo.UpdateUser(user);
            return RedirectToAction("UserManagement", "Test");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Archive(int userId)
        {
            _repo.ArchiveUser(userId);
            return RedirectToAction("UserManagement", "Test");
        }
    }
}
