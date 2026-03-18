using Microsoft.AspNetCore.Mvc;
using ThrdCtrl2.Data;
using ThrdCtrl2.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using ZXing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ThrdCtrl2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class TestController : Controller
    {
        private readonly UserRepository _userRepo;


        private readonly InventoryRepository _inventoryRepo;
        private readonly AuditRepository _auditRepo;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public TestController(UserRepository userRepo, InventoryRepository inventoryRepo, AuditRepository auditRepo, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _userRepo = userRepo;
            _inventoryRepo = inventoryRepo;
            _auditRepo = auditRepo;
            _config = config;
        }

        private void LogEvent(string action, string module, string details)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;
            int? cid = int.TryParse(cidStr, out int c) ? c : null;
            int? uid = int.TryParse(uidStr, out int u) ? u : null;

            _auditRepo.Log(new AuditLog
            {
                CompanyID = cid,
                UserID = uid,
                Action = action,
                Module = module,
                Details = details,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.Now
            });
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
                _auditRepo.Log(new AuditLog { CompanyID = user.CompanyID, UserID = user.UserID, Action = "Failed Login", Module = "Auth", Details = "Inactive individual account", IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
                ModelState.AddModelError("", "Your account is inactive. Please contact your administrator.");
                return View();
            }

            // CHECK COMPANY STATUS (Skip for Super Admins who don't belong to a company)
            if (user.RoleName != "Super Admin" && user.CompanyID.HasValue)
            {
                if (user.CompanyStatus == "Pending")
                {
                    ModelState.AddModelError("", "Your company registration is still pending approval by the Super Admin.");
                    return View();
                }
                else if (user.CompanyStatus == "Inactive")
                {
                    ModelState.AddModelError("", "Your company account has been deactivated. Access is restricted.");
                    return View();
                }
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

                // Check for Company Status block (Super Admins are exempt)
                if (user.RoleName != "Super Admin")
                {
                    if (user.CompanyStatus == "Pending")
                    {
                        ModelState.AddModelError("", "Your company registration is still pending approval. Please contact support.");
                        return View();
                    }
                    if (user.CompanyStatus == "Inactive")
                    {
                        ModelState.AddModelError("", "Your account is restricted. Please contact the system administrator or check your subscription status.");
                        return View();
                    }
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
                
                _auditRepo.Log(new AuditLog { CompanyID = user.CompanyID, UserID = user.UserID, Action = "Login", Module = "Auth", Details = "User logged in successfully", IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });

                if (user.RoleName == "Super Admin")
                {
                    return RedirectToAction("SuperAdminDashboard");
                }

                return RedirectToAction("Dashboard");
            }

            if (user != null)
            {
                _auditRepo.Log(new AuditLog { CompanyID = user.CompanyID, UserID = user.UserID, Action = "Failed Login", Module = "Auth", Details = "Invalid password", IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });
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
        public IActionResult Register(string companyName, string fullName, string email, string password, string confirmPassword, string subscriptionType)
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
                int companyId = _userRepo.CreateCompany(companyName, subscriptionType);
                
                // 2. Link User to Company
                newUser.CompanyID = companyId;
                
                // 3. Create User
                _userRepo.CreateUser(newUser);
                
                _auditRepo.Log(new AuditLog { CompanyID = companyId, UserID = null, Action = "Registration", Module = "Auth", Details = $"New company {companyName} and admin {fullName} registered", IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() });

                return RedirectToAction("Login");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                return View();
            }
        }

        public IActionResult Dashboard(int page = 1)
        {
            if (User.IsInRole("Super Admin"))
            {
                return RedirectToAction("SuperAdminDashboard");
            }

            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");

            var vm = _inventoryRepo.GetDashboardStats(cid, page);
            return View(vm);
        }

        public IActionResult DashboardReport()
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");

            var vm = _inventoryRepo.GetDashboardStats(cid);
            ViewData["CompanyName"] = User.FindFirst("CompanyName")?.Value ?? "ThreadCtrl Enterprise";
            ViewData["ReportDate"] = DateTime.Now.ToString("MMMM dd, yyyy");
            return View(vm);
        }

        public IActionResult Report(int? warehouseId, int? categoryId, int page = 1)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");

            int pageSize = 15;
            var allValuation = _inventoryRepo.GetDetailedValuation(cid, warehouseId, categoryId);
            int totalItems = allValuation.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var pagedValuation = allValuation.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new ReportViewModel
            {
                Valuation = _inventoryRepo.GetValuationSummary(cid, warehouseId, categoryId),
                RecentMovements = _inventoryRepo.GetRecentMovements(cid, 10, warehouseId, categoryId),
                DetailedValuation = pagedValuation,
                ValuationHistory = _inventoryRepo.GetValuationHistory(cid, warehouseId, categoryId),
                CategoryDistribution = _inventoryRepo.GetCategoryDistribution(cid, warehouseId),
                Warehouses = _inventoryRepo.GetWarehouses(cid),
                Categories = _inventoryRepo.GetCategories(cid),
                SelectedWarehouseId = warehouseId,
                SelectedCategoryId = categoryId,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Auditor")]
        public IActionResult ValuationReport(int? warehouseId, int? categoryId)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");

            // For formal report, we usually want all data, not paged
            var vm = new ReportViewModel
            {
                Valuation = _inventoryRepo.GetValuationSummary(cid, warehouseId, categoryId),
                RecentMovements = _inventoryRepo.GetRecentMovements(cid, 50, warehouseId, categoryId),
                DetailedValuation = _inventoryRepo.GetDetailedValuation(cid, warehouseId, categoryId),
                ValuationHistory = _inventoryRepo.GetValuationHistory(cid, warehouseId, categoryId),
                CategoryDistribution = _inventoryRepo.GetCategoryDistribution(cid, warehouseId),
                Warehouses = _inventoryRepo.GetWarehouses(cid),
                Categories = _inventoryRepo.GetCategories(cid),
                SelectedWarehouseId = warehouseId,
                SelectedCategoryId = categoryId
            };

            ViewData["CompanyName"] = User.FindFirst("CompanyName")?.Value ?? "ThreadCtrl Enterprise";
            ViewData["ReportDate"] = DateTime.Now.ToString("MMMM dd, yyyy");
            
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Sales Staff,Procurement Officer,Auditor")]
        public IActionResult Inventory(int? categoryId, string? status, int? warehouseId, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

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

            var allItems = _inventoryRepo.GetInventory(companyId, categoryId, status, warehouseId);
            int totalItems = allItems.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            // Slice for current page
            var pagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new InventoryViewModel
            {
                InventoryItems = pagedItems,
                Categories = categories,
                Warehouses = warehouses,
                ProductVariants = _inventoryRepo.GetVariants(companyId),
                Products = _inventoryRepo.GetProducts(companyId),
                SelectedCategoryId = categoryId,
                SelectedStatus = status,
                SelectedWarehouseId = warehouseId,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };
            
            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddProduct(Product product)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");
            
            product.CompanyID = cid;
            
            // 1. Create Product only. Variants will be added separately.
            _inventoryRepo.AddProduct(product);
            LogEvent("Add Product", "Inventory", $"Created new product: {product.ProductName}");
            
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddVariant(ProductVariant variant)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                var products = _inventoryRepo.GetProducts(cid);
                var product = products.FirstOrDefault(p => p.ProductID == variant.ProductID);
                if (product != null)
                {
                    string cleanProdName = product.ProductName.Replace(" ", "-").ToUpper();
                    string cleanColor = (variant.Color ?? "NA").Replace(" ", "-").ToUpper();
                    string cleanSize = (variant.Size ?? "STD").Replace(" ", "-").ToUpper();
                    string generatedSku = $"{cleanProdName}-{cleanColor}-{cleanSize}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
                    variant.SKU = generatedSku;
                    variant.Barcode = generatedSku;
                    variant.Price = variant.Price > 0 ? variant.Price : 0;

                    _inventoryRepo.AddVariant(variant);
                    LogEvent("Add Variant", "Inventory", $"Added variant {variant.SKU} for Product ID {variant.ProductID}");
                }
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        public IActionResult GetBarcodeImage(string sku)
        {
            if (string.IsNullOrEmpty(sku)) return BadRequest();

            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 400,
                    Height = 400,
                    Margin = 2
                }
            };

            using var image = writer.Write(sku);
            using var ms = new System.IO.MemoryStream();
            image.SaveAsPng(ms);
            return File(ms.ToArray(), "image/png");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult AddInventoryItem(int variantId, int warehouseId, int minStock, int quantity)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                _inventoryRepo.CreateInventoryRecord(new InventoryItem
                {
                    CompanyID = cid,
                    VariantID = variantId,
                    WarehouseID = warehouseId,
                    QuantityOnHand = quantity,
                    MinimumStockLevel = minStock > 0 ? minStock : 5
                });
                LogEvent("Inventory Setup", "Inventory", $"Assigned Variant ID {variantId} to Warehouse {warehouseId} with {quantity} units");
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Procurement Officer")]
        public IActionResult UpdateInventoryItem(int inventoryId, int minStock, decimal price)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                // We no longer allow updating quantity directly from the Inventory view
                // Fetch the current quantity to pass to the repo, or update the repo method
                _inventoryRepo.UpdateInventorySettings(inventoryId, cid, minStock, price);
                LogEvent("Inventory Settings Update", "Inventory", $"Updated settings for InventoryID {inventoryId}: MinStock={minStock}, Price={price}");
                return Json(new { success = true });
            }
            return BadRequest();
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
                LogEvent("Stock Update", "Inventory", $"Manual update: InventoryID {inventoryId}, Qty {quantity}");
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
                    LogEvent("Stock Transfer", "Inventory", $"Transferred {quantity} of Variant {variantId} from Warehouse {fromWarehouseId} to {toWarehouseId}");
                }
            }

            return RedirectToAction("Inventory");
        }
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Sales Staff,Auditor")]
        public IActionResult Sales(string? status, DateTime? date, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            int companyId = 0;
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid)) companyId = cid;

            var allOrders = _inventoryRepo.GetSalesOrders(companyId, status, date);
            int totalItems = allOrders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            var pagedOrders = allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new SalesViewModel
            {
                Orders = pagedOrders,
                SelectedStatus = status,
                SelectedDate = date,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            ViewBag.Warehouses = _inventoryRepo.GetWarehouses(companyId);
            ViewBag.Variants = _inventoryRepo.GetVariants(companyId);
            
            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Sales Staff")]
        public IActionResult CreateSalesOrder(string customerName, int warehouseId, int variantId, int quantity)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;

            if (int.TryParse(cidStr, out int cid) && int.TryParse(uidStr, out int uid))
            {
                var variants = _inventoryRepo.GetVariants(cid);
                var variant = variants.FirstOrDefault(v => v.VariantID == variantId);
                decimal unitPrice = variant != null ? variant.Price : 0;

                var order = new SalesOrder
                {
                    CompanyID = cid,
                    CreatedBy = uid,
                    CustomerName = customerName,
                    TotalAmount = quantity * unitPrice
                };

                var items = new List<SalesOrderItem>
                {
                    new SalesOrderItem
                    {
                        VariantID = variantId,
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    }
                };

                try
                {
                    _inventoryRepo.CreateSalesOrder(order, items, warehouseId);
                    LogEvent("Sales Order", "Sales", $"Created order for customer {customerName}, Total: ₱{order.TotalAmount}");
                    return Json(new { success = true, message = "Sales Order created successfully!" });
                }
                catch (System.Exception ex)
                {
                    string msg = "An error occurred while creating the sales order.";
                    if (ex.Message.Contains("stock")) msg = ex.Message;
                    return Json(new { success = false, message = msg });
                }
            }

            return Json(new { success = false, message = "Authentication error." });
        }

        [HttpGet]
        public IActionResult GetVariantsByWarehouse(int warehouseId)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                var inventory = _inventoryRepo.GetInventory(cid, warehouseId: warehouseId);
                // We only want items that are actually in stock or at least assigned to this warehouse
                var variants = inventory.Select(i => new {
                    i.VariantID,
                    DisplayText = $"{i.ProductName} - {i.Size} | {i.Color} ({i.SKU})",
                    i.QuantityOnHand,
                    i.Barcode,
                    price = i.VariantPrice
                }).ToList();

                return Json(variants);
            }
            return BadRequest();
        }

        [HttpGet]
        public IActionResult GetWarehousesByVariant(int variantId)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                var inventory = _inventoryRepo.GetInventory(cid, variantId: variantId);
                // Filter out items where InventoryID is 0 (not truly assigned yet) if needed, 
                // but usually GetInventory returns established records if they exist.
                var warehouses = inventory
                    .Where(i => i.InventoryID > 0)
                    .Select(i => new {
                        i.WarehouseID,
                        i.WarehouseName,
                        i.QuantityOnHand
                    }).ToList();

                return Json(warehouses);
            }
            return BadRequest();
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult Procurement(string tab = "PurchaseOrders", string status = "All Statuses", int? supplierId = null, string supplierStatus = "Active", int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                var suppliers = _inventoryRepo.GetSuppliers(cid, supplierStatus);
                var activeSuppliers = (supplierStatus == "Active") ? suppliers : _inventoryRepo.GetSuppliers(cid, "Active");
                
                var pos = _inventoryRepo.GetPurchaseOrders(cid, status, supplierId);
                int totalItems = pos.Count;
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedPos = pos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var inventory = _inventoryRepo.GetInventory(cid);
                var alerts = inventory.Where(i => i.QuantityOnHand <= i.MinimumStockLevel).ToList();

                var vm = new ProcurementViewModel
                {
                    PurchaseOrders = pagedPos,
                    Suppliers = suppliers,
                    ActiveSuppliers = activeSuppliers,
                    ReorderAlerts = alerts,
                    PendingApprovalCount = pos.Count(p => p.Status == "Pending"),
                    ReceivingDueCount = pos.Count(p => p.Status == "Ordered"),
                    ActiveSuppliersCount = activeSuppliers.Count,
                    ActiveTab = tab,
                    SelectedStatus = status,
                    SelectedSupplierId = supplierId,
                    SelectedSupplierStatus = supplierStatus,
                    AllVariants = _inventoryRepo.GetVariants(cid),
                    AllWarehouses = _inventoryRepo.GetWarehouses(cid),
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems
                };

                return View(vm);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult UpdateSupplierStatus(int supplierId, bool isActive)
        {
            try
            {
                _inventoryRepo.UpdateSupplierStatus(supplierId, isActive);
                LogEvent("Supplier Status Update", "Procurement", $"Updated SupplierID {supplierId} status to {(isActive ? "Active" : "Inactive")}");
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while updating the supplier status." });
            }
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult AddSupplier(string name, string contact)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                _inventoryRepo.AddSupplier(new Supplier { CompanyID = cid, SupplierName = name, ContactInfo = contact });
                LogEvent("Add Supplier", "Procurement", $"Added new supplier: {name}");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Session error" });
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult CreatePurchaseOrder(int supplierId, int variantId, int qty, decimal cost, int? warehouseId)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;

            if (int.TryParse(cidStr, out int cid) && int.TryParse(uidStr, out int uid))
            {
                var po = new PurchaseOrder
                {
                    CompanyID = cid,
                    SupplierID = supplierId,
                    WarehouseID = warehouseId,
                    CreatedBy = uid,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    TotalCost = qty * cost
                };

                var items = new List<PurchaseOrderItem>
                {
                    new PurchaseOrderItem
                    {
                        VariantID = variantId,
                        Quantity = qty,
                        CostPerUnit = cost,
                        Subtotal = qty * cost
                    }
                };

                try
                {
                    _inventoryRepo.AddPurchaseOrder(po, items);
                    LogEvent("Purchase Order", "Procurement", $"Created PO for SupplierID {supplierId}, Total: ₱{po.TotalCost}");
                    return Json(new { success = true });
                }
                catch (Exception)
                {
                    return Json(new { success = false, message = "An error occurred while creating the purchase order." });
                }
            }
            return Json(new { success = false, message = "Session error" });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Auditor")]
        public IActionResult Stock(string? tab = "All", string? type = "All Types", int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                string? statusFilter = tab;
                if (tab == "All" || tab == "All Adjustments") statusFilter = null;

                var allMatchingAdjustments = _inventoryRepo.GetStockAdjustments(cid, statusFilter, type);
                int totalItems = allMatchingAdjustments.Count;
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedAdjustments = allMatchingAdjustments.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var allAdjustments = _inventoryRepo.GetStockAdjustments(cid); // For stats

                var vm = new StockAdjustmentViewModel
                {
                    Adjustments = pagedAdjustments,
                    AllVariants = _inventoryRepo.GetVariants(cid),
                    AllWarehouses = _inventoryRepo.GetWarehouses(cid),
                    ActiveTab = tab ?? "All",
                    SelectedType = type,
                    PendingCount = allAdjustments.Count(a => a.Status == "Pending"),
                    ThisMonthDamages = allAdjustments
                        .Where(a => a.Type == "Damage" && a.DateRequested.Month == DateTime.Now.Month && a.DateRequested.Year == DateTime.Now.Year)
                        .Sum(a => Math.Abs(a.ChangeQuantity)),
                    NetCorrections = allAdjustments
                        .Where(a => a.Type == "Correction")
                        .Sum(a => a.ChangeQuantity),
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems
                };

                return View(vm);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager")]
        public IActionResult RequestAdjustment(int variantId, int warehouseId, string type, int qty, string reason)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            var uidStr = User.FindFirst("UserID")?.Value;

            if (int.TryParse(cidStr, out int cid) && int.TryParse(uidStr, out int uid))
            {
                // Auto-invert if user entered positive number for Damage/Write-off
                int finalQty = qty;
                if ((type == "Damage" || type == "Write-off") && qty > 0)
                {
                    finalQty = -qty;
                }

                var adj = new StockAdjustment
                {
                    CompanyID = cid,
                    VariantID = variantId,
                    WarehouseID = warehouseId,
                    Type = type,
                    ChangeQuantity = finalQty,
                    RequestedBy = uid,
                    Status = "Pending",
                    Reason = reason,
                    DateRequested = DateTime.Now
                };

                _inventoryRepo.AddStockAdjustment(adj);
                LogEvent("Adjustment Request", "Inventory", $"Requested {type} for VariantID {variantId}, Qty: {finalQty}");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Session error." });
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult ApproveAdjustment(int id)
        {
            var uidStr = User.FindFirst("UserID")?.Value;
            if (int.TryParse(uidStr, out int uid))
            {
                try
                {
                    _inventoryRepo.ApproveStockAdjustment(id, uid);
                    LogEvent("Approve Adjustment", "Inventory", $"Approved adjustment ID {id}");
                    return Json(new { success = true });
                }
                catch (Exception)
                {
                    return Json(new { success = false, message = "An error occurred while approving the stock adjustment." });
                }
            }
            return Json(new { success = false, message = "Session error." });
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult RejectAdjustment(int id)
        {
            var uidStr = User.FindFirst("UserID")?.Value;
            if (int.TryParse(uidStr, out int uid))
            {
                _inventoryRepo.RejectStockAdjustment(id, uid);
                LogEvent("Reject Adjustment", "Inventory", $"Rejected adjustment ID {id}");
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Session error." });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin")]
        public IActionResult UserManagement(string? status, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            int? companyId = null;
            if (!User.IsInRole("Super Admin"))
            {
                var cidStr = User.FindFirst("CompanyID")?.Value;
                if (int.TryParse(cidStr, out int cid)) companyId = cid;
            }

            var allUsers = _userRepo.GetAllUsers(null, companyId); // Filter by company if not SA
            var filteredUsersList = string.IsNullOrEmpty(status) || status == "All" 
                                 ? allUsers 
                                 : allUsers.Where(u => u.Status == status).ToList();

            int totalItems = filteredUsersList.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedUsers = filteredUsersList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new UserManagementViewModel
            {
                Users = pagedUsers,
                Roles = _userRepo.GetRoles(),
                CurrentFilter = status ?? "All",
                TotalUsers = allUsers.Count,
                ActiveUsers = allUsers.Count(u => u.Status == "Active"),
                InactiveUsers = allUsers.Count(u => u.Status == "Inactive"),
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            if (!User.IsInRole("Super Admin"))
            {
                vm.Roles = vm.Roles.Where(r => r.RoleName != "Super Admin").ToList();
                vm.Users = vm.Users.Where(u => u.RoleName != "Super Admin").ToList();
                // Re-count stats based on filtered users if necessary, but usually Company Admin only sees their company anyway
            }
            
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Auditor")]
        public IActionResult Security(int? userId, string? module, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid))
            {
                var allLogs = _auditRepo.GetLogs(cid, userId, module, 1000); // Get more for filtering/summary if needed, or just paged
                int totalItems = allLogs.Count;
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                var pagedLogs = allLogs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var vm = new SecurityViewModel
                {
                    Logs = pagedLogs,
                    Stats = _auditRepo.GetSecurityStats(cid),
                    Users = _userRepo.GetAllUsers(null, cid),
                    SelectedUserId = userId,
                    SelectedModule = module,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    PageSize = pageSize
                };

                return View(vm);
            }

            return RedirectToAction("Login");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Auditor")]
        public IActionResult SecurityReport(int? userId, string? module)
        {
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (!int.TryParse(cidStr, out int cid)) return RedirectToAction("Login");

            // For formal report, we want the logs (up to 2000 for visibility)
            var allLogs = _auditRepo.GetLogs(cid, userId, module, 2000);
            
            var vm = new SecurityViewModel
            {
                Logs = allLogs,
                Stats = _auditRepo.GetSecurityStats(cid),
                Users = _userRepo.GetAllUsers(null, cid),
                SelectedUserId = userId,
                SelectedModule = module
            };

            ViewData["CompanyName"] = User.FindFirst("CompanyName")?.Value ?? "ThreadCtrl Enterprise";
            ViewData["ReportDate"] = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
            
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult SuperAdminDashboard()
        {
            var vm = _userRepo.GetSuperAdminDashboardStats();
            return View(vm);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult Companies(string? status, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            var allMatchingCompanies = _userRepo.GetCompanies(status);
            var list = new List<CompanyWithAdmin>();
            foreach (var c in allMatchingCompanies)
            {
                list.Add(new CompanyWithAdmin
                {
                    CompanyID = c.CompanyID,
                    CompanyName = c.CompanyName,
                    Status = c.Status,
                    UserCount = c.UserCount,
                    AdminEmail = _userRepo.GetCompanyAdminEmail(c.CompanyID),
                    SubscriptionType = c.SubscriptionType
                });
            }

            int totalItems = list.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedList = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var stats = _userRepo.GetSuperAdminDashboardStats();
            var vm = new CompanyManagementViewModel
            {
                Companies = pagedList,
                CurrentFilter = status ?? "All",
                TotalCompanies = stats.TotalCompanies,
                ActiveCompanies = stats.ActiveCompanies,
                InactiveCompanies = stats.InactiveCompanies,
                PendingCompanies = stats.PendingCompanies,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };
            


            return View(vm);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult ApproveCompany(int companyId)
        {
            _userRepo.UpdateCompanyStatus(companyId, "Active");
            LogEvent("Approve Company", "Auth", $"Super Admin approved Company ID: {companyId}");
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult ArchiveCompany(int companyId)
        {
            _userRepo.UpdateCompanyStatus(companyId, "Inactive");
            LogEvent("Archive Company", "Auth", $"Super Admin archived Company ID: {companyId}");
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult ActivateCompany(int companyId)
        {
            _userRepo.UpdateCompanyStatus(companyId, "Active");
            LogEvent("Activate Company", "Auth", $"Super Admin activated Company ID: {companyId}");
            return RedirectToAction("Companies");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin")]
        public IActionResult AllUsers(string? role, int page = 1)
        {
            if (page < 1) page = 1;
            int pageSize = 15;

            var allUsers = _userRepo.GetAllSystemUsers(role);
            int totalItems = allUsers.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedUsers = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new AllUsersViewModel
            {
                Users = pagedUsers,
                Roles = _userRepo.GetRoles(),
                SelectedRole = role ?? "All",
                Companies = _userRepo.GetCompanies(),
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };
            return View(vm);
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
                LogEvent("Add Category", "Inventory", $"Added category: {category.CategoryName}");
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
                LogEvent("Add Warehouse", "Inventory", $"Added warehouse: {warehouse.WarehouseName}");
            }
            return RedirectToAction("Inventory");
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Inventory Manager,Sales Staff,Procurement Officer,Auditor")]
        public IActionResult PrintLabels()
        {
            int companyId = 0;
            var cidStr = User.FindFirst("CompanyID")?.Value;
            if (int.TryParse(cidStr, out int cid)) companyId = cid;

            var variants = _inventoryRepo.GetVariants(companyId);
            return View(variants);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult ReceivePO(int poId)
        {
            var uidStr = User.FindFirst("UserID")?.Value;
            if (int.TryParse(uidStr, out int uid))
            {
                try
                {
                    _inventoryRepo.ReceivePurchaseOrder(poId, uid);
                    LogEvent("Receive PO", "Procurement", $"Received inventory from PurchaseOrderID {poId}");
                    return Json(new { success = true });
                }
                catch (Exception)
                {
                    return Json(new { success = false, message = "An error occurred while receiving the purchase order." });
                }
            }
            return Json(new { success = false, message = "Session error" });
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Super Admin,Company Admin,Procurement Officer")]
        public IActionResult ApprovePO(int poId)
        {
            try
            {
                _inventoryRepo.ApprovePurchaseOrder(poId);
                LogEvent("Approve PO", "Procurement", $"Approved PurchaseOrderID {poId}");
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while approving the purchase order." });
            }
        }
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Migrate()
        {
            try
            {
                _userRepo.MigrateDatabase();
                return Content("Migration Successful");
            }
            catch (Exception ex)
            {
                return Content("Migration Failed: " + ex.Message);
            }
        }
    }
}
