using Microsoft.AspNetCore.Mvc;
using ThrdCtrl2.Data;
using ThrdCtrl2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace ThrdCtrl2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TestController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly InventoryRepository _inventoryRepo;

        public TestController(UserRepository userRepo, InventoryRepository inventoryRepo)
        {
            _userRepo = userRepo;
            _inventoryRepo = inventoryRepo;
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
            var user = _userRepo.GetUserByEmail(email);
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
                    _userRepo.UpdateUser(user);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("UserID", user.UserID.ToString()),
                    new Claim("CompanyID", user.CompanyID?.ToString() ?? ""),
                    new Claim("CompanyName", user.CompanyName ?? ""),
                    new Claim(ClaimTypes.Role, user.RoleName ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                
                if (user.RoleName == "Super Admin")
                {
                    return RedirectToAction("SuperAdminDashboard");
                }

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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost]
        public IActionResult Register(string companyName, string fullName, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }

            var existingUser = _userRepo.GetUserByEmail(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View();
            }

            // Get Company Admin RoleID
            var roles = _userRepo.GetRoles();
            var adminRole = roles.FirstOrDefault(r => r.RoleName == "Company Admin");
            if (adminRole == null)
            {
                ModelState.AddModelError("", "System error: Company Admin role not found.");
                return View();
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                RoleID = adminRole.RoleID,
                Status = "Active"
            };

            try
            {
                // 1. Create Company
                int companyId = _userRepo.CreateCompany(companyName);
                
                // 2. Link User to Company
                newUser.CompanyID = companyId;
                
                // 3. Create User
                _userRepo.CreateUser(newUser);
                
                return RedirectToAction("Login");
            }
            catch (System.Exception)
            {
                ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                // Log exception if logging is available
                return View();
            }
        }

        public IActionResult Dashboard()
        {
            if (User.IsInRole("Super Admin"))
            {
                return RedirectToAction("SuperAdminDashboard");
            }
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Auditor")]
        public IActionResult Report()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Sales Staff,Procurement Officer,Auditor")]
        public IActionResult Inventory(int? categoryId, string? status, int? warehouseId)
        {
            int companyId = 0;
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid)) companyId = cid;

            var categories = _inventoryRepo.GetCategories(companyId);
            var warehouses = _inventoryRepo.GetWarehouses(companyId);

            // AUTO-SEED: If no categories or warehouses exist, add defaults for the user
            if (!categories.Any())
            {
                _inventoryRepo.AddCategory(new Category { CompanyID = companyId, CategoryName = "Apparel", Description = "Clothing and garments" });
                _inventoryRepo.AddCategory(new Category { CompanyID = companyId, CategoryName = "Footwear", Description = "Shoes and boots" });
                categories = _inventoryRepo.GetCategories(companyId);
            }

            if (!warehouses.Any())
            {
                _inventoryRepo.AddWarehouse(new Warehouse { CompanyID = companyId, WarehouseName = "Main Warehouse", Location = "Default Location" });
                warehouses = _inventoryRepo.GetWarehouses(companyId);
            }

            var vm = new InventoryViewModel
            {
                InventoryItems = _inventoryRepo.GetInventory(companyId, categoryId, status, warehouseId),
                Categories = categories,
                Warehouses = warehouses,
                ProductVariants = _inventoryRepo.GetVariants(companyId),
                Products = _inventoryRepo.GetProducts(companyId),
                SelectedCategoryId = categoryId,
                SelectedStatus = status,
                SelectedWarehouseId = warehouseId
            };
            
            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddProduct(Product product, int warehouseId, string sku, string color, string size)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");
            
            product.CompanyID = cid;
            
            // 1. Create Product
            int productId = _inventoryRepo.AddProduct(product);
            
            // 2. Create Default Variant
            var variant = new ProductVariant
            {
                ProductID = productId,
                Size = string.IsNullOrEmpty(size) ? "Standard" : size,
                Color = string.IsNullOrEmpty(color) ? "N/A" : color,
                SKU = string.IsNullOrEmpty(sku) ? $"PROD-{productId}-{DateTime.Now.Ticks % 1000}" : sku,
                Barcode = ""
            };
            int variantId = _inventoryRepo.AddVariant(variant);
            
            // 3. Create Inventory Record
            _inventoryRepo.CreateInventoryRecord(new InventoryItem
            {
                CompanyID = cid,
                VariantID = variantId,
                WarehouseID = warehouseId,
                QuantityOnHand = 0,
                MinimumStockLevel = 5
            });

            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddVariant(ProductVariant variant, int? warehouseId, int? minStock)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                // Verify product belongs to company (Security check)
                var products = _inventoryRepo.GetProducts(cid);
                if (products.Any(p => p.ProductID == variant.ProductID))
                {
                    // Basic SKU uniqueness check (best effort)
                    var existingVariants = _inventoryRepo.GetVariants(cid);
                    if (existingVariants.Any(v => v.SKU.Equals(variant.SKU, StringComparison.OrdinalIgnoreCase)))
                    {
                        // SKUs must be unique. Simplified handling: just don't add.
                        return RedirectToAction("Inventory");
                    }

                    int variantId = _inventoryRepo.AddVariant(variant);
                    
                    // If a warehouse was selected, create the initial inventory record
                    if (warehouseId.HasValue && warehouseId > 0)
                    {
                        _inventoryRepo.CreateInventoryRecord(new InventoryItem
                        {
                            CompanyID = cid,
                            VariantID = variantId,
                            WarehouseID = warehouseId.Value,
                            QuantityOnHand = 0,
                            MinimumStockLevel = minStock ?? 5
                        });
                    }
                }
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddInventoryItem(int variantId, int warehouseId, int minStock)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                _inventoryRepo.CreateInventoryRecord(new InventoryItem
                {
                    CompanyID = cid,
                    VariantID = variantId,
                    WarehouseID = warehouseId,
                    QuantityOnHand = 0,
                    MinimumStockLevel = minStock > 0 ? minStock : 5
                });
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult UpdateStock(int inventoryId, int quantity, string reference)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;
            
            if (int.TryParse(cidStr, out int cid) && int.TryParse(uidStr, out int uid))
            {
                _inventoryRepo.UpdateStock(inventoryId, cid, quantity, uid, "Adjustment", null);
            }
            
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult TransferStock(int variantId, int fromWarehouseId, int toWarehouseId, int quantity)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;

            if (int.TryParse(cidStr, out int cid) && int.TryParse(uidStr, out int uid))
            {
                if (fromWarehouseId == toWarehouseId)
                {
                    // Source and destination are the same
                    return RedirectToAction("Inventory");
                }

                // Verify source has enough stock
                var inventory = _inventoryRepo.GetInventory(cid, warehouseId: fromWarehouseId);
                var sourceItem = inventory.FirstOrDefault(i => i.VariantID == variantId);

                if (sourceItem != null && sourceItem.QuantityOnHand >= quantity)
                {
                    _inventoryRepo.TransferStock(variantId, fromWarehouseId, toWarehouseId, quantity, cid, uid);
                }
            }

            return RedirectToAction("Inventory");
        }
        public IActionResult Sales()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult Procurement()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Auditor")]
        public IActionResult Stock()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult UserManagement(string? status)
        {
            int? companyId = null;
            if (!User.IsInRole("Super Admin"))
            {
                var cidStr = User.FindFirst("CompanyID")?.Value;
                if (int.TryParse(cidStr, out int cid)) companyId = cid;
            }

            var allUsers = _userRepo.GetAllUsers(null, companyId); // Filter by company if not SA
            var filteredUsers = string.IsNullOrEmpty(status) || status == "All" 
                                ? allUsers 
                                : allUsers.Where(u => u.Status == status).ToList();

            var vm = new UserManagementViewModel
            {
                Users = filteredUsers,
                Roles = _userRepo.GetRoles(),
                CurrentFilter = status ?? "All",
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.Status == "Active"),
                InactiveUsers = allUsers.Count(u => u.Status == "Inactive")
            };
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Auditor")]
        public IActionResult Security()
        {
            return View();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult SuperAdminDashboard()
        {
            var vm = _userRepo.GetSuperAdminDashboardStats();
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult Companies(string? status)
        {
            var companies = _userRepo.GetCompanies(status);
            var list = new List<CompanyWithAdmin>();
            foreach (var c in companies)
            {
                list.Add(new CompanyWithAdmin
                {
                    CompanyID = c.CompanyID,
                    CompanyName = c.CompanyName,
                    Status = c.Status,
                    UserCount = c.UserCount,
                    AdminEmail = _userRepo.GetCompanyAdminEmail(c.CompanyID)
                });
            }

            var vm = new CompanyManagementViewModel
            {
                Companies = list,
                CurrentFilter = status ?? "All",
                TotalCompanies = list.Count, // This is filter based, repo should provide global count
                ActiveCompanies = list.Count(x => x.Status == "Active"),
                InactiveCompanies = list.Count(x => x.Status == "Inactive")
            };
            
            // Re-fetch global counts for cards if filtered
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                var globalStats = _userRepo.GetSuperAdminDashboardStats();
                vm.TotalCompanies = globalStats.TotalCompanies;
                vm.ActiveCompanies = globalStats.ActiveCompanies;
                vm.InactiveCompanies = globalStats.InactiveCompanies;
            }

            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult ArchiveCompany(int companyId)
        {
            _userRepo.ArchiveCompany(companyId);
            return RedirectToAction("Companies");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult AllUsers(string? role)
        {
            var vm = new AllUsersViewModel
            {
                Users = _userRepo.GetAllSystemUsers(role),
                Roles = _userRepo.GetRoles(),
                SelectedRole = role ?? "All",
                Companies = _userRepo.GetCompanies()
            };
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager")]
        public IActionResult Settings()
        {
            return View();
        }
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager")]
        public IActionResult AddCategory(Category category)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                category.CompanyID = cid;
                _inventoryRepo.AddCategory(category);
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager")]
        public IActionResult AddWarehouse(Warehouse warehouse)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                warehouse.CompanyID = cid;
                _inventoryRepo.AddWarehouse(warehouse);
            }
            return RedirectToAction("Inventory");
        }
    }
}
