using Microsoft.AspNetCore.Mvc;
using ThrdCtrl2.Data;
using ThrdCtrl2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;

namespace ThrdCtrl2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TestController : Controller
    {
        private readonly UserRepository _repo;
        public TestController(UserRepository repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Login(string email, string password)
        {
            var user = _repo.GetUserByEmail(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            if (user.Status != "Active")
            {
                ModelState.AddModelError("", "Your account is inactive. Please contact your administrator.");
                return View();
            }

            bool isValid = false;
            bool needsUpgrade = false;

            try
            {
                // Check if it's a valid BCrypt hash and verify
                if (user.Password.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    isValid = true;
                }
                else if (user.Password == password) // Fallback for plain-text legacy passwords
                {
                    isValid = true;
                    needsUpgrade = true;
                }
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // If it wasn't a valid hash, check as plain text
                if (user.Password == password)
                {
                    isValid = true;
                    needsUpgrade = true;
                }
            }

            if (isValid)
            {
                if (needsUpgrade)
                {
                    // Update legacy plain-text password to hash
                    user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                    _repo.UpdateUser(user);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserID", user.UserID.ToString()),
                    new Claim(ClaimTypes.Role, user.RoleName ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Dashboard");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }

        public async System.Threading.Tasks.Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Inventory Manager,Auditor")]
        public IActionResult Report()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Inventory Manager,Sales Staff,Procurement Officer,Auditor")]
        public IActionResult Inventory()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Sales Staff")]
        public IActionResult Sales()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Procurement Officer")]
        public IActionResult Procurement()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Inventory Manager,Auditor")]
        public IActionResult Stock()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult UserManagement(string? status)
        {
            var allUsers = _repo.GetAllUsers(); // Get all to calculate stats
            var filteredUsers = string.IsNullOrEmpty(status) || status == "All" 
                                ? allUsers 
                                : allUsers.Where(u => u.Status == status).ToList();

            var vm = new UserManagementViewModel
            {
                Users = filteredUsers,
                Roles = _repo.GetRoles(),
                CurrentFilter = status ?? "All",
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.Status == "Active"),
                InactiveUsers = allUsers.Count(u => u.Status == "Inactive")
            };
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Auditor")]
        public IActionResult Security()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Inventory Manager")]
        public IActionResult Settings()
        {
            return View();
        }
    }
}
