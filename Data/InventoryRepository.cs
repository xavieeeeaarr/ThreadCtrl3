using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ThrdCtrl2.Models;

namespace ThrdCtrl2.Data
{
    public class InventoryRepository
    {
        private readonly string _connectionString;

        public InventoryRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");
            
            EnsureReportingSchema();
        }

        private void EnsureReportingSchema()
        {
            try {
                using var conn = GetConnection();
                conn.Open();
                string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryTransaction') AND name = 'ChangeQuantity')
                    BEGIN
                        ALTER TABLE InventoryTransaction ADD ChangeQuantity INT NOT NULL DEFAULT 0;
                    END
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryTransaction') AND name = 'Status')
                    BEGIN
                        ALTER TABLE InventoryTransaction ADD Status VARCHAR(20) NOT NULL DEFAULT 'Completed';
                    END";
                using var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            } catch { /* Silent fail if schema already exists or permission issues */ }
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // --- CATEGORIES ---

        public List<Category> GetCategories(int companyId)
        {
            var list = new List<Category>();
            using var conn = GetConnection();
            string sql = "SELECT CategoryID, CompanyID, CategoryName, Description FROM Category WHERE CompanyID = @cid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Category
                {
                    CategoryID = (int)rdr["CategoryID"],
                    CompanyID = (int)rdr["CompanyID"],
                    CategoryName = rdr["CategoryName"] as string ?? "",
                    Description = rdr["Description"] as string ?? ""
                });
            }
            return list;
        }

        public void AddCategory(Category category)
        {
            using var conn = GetConnection();
            string sql = "INSERT INTO Category (CompanyID, CategoryName, Description) VALUES (@cid, @name, @desc)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", category.CompanyID);
            cmd.Parameters.AddWithValue("@name", category.CategoryName);
            cmd.Parameters.AddWithValue("@desc", (object)category.Description ?? DBNull.Value);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // --- PRODUCTS ---

        public List<Product> GetProducts(int companyId)
        {
            var list = new List<Product>();
            using var conn = GetConnection();
            string sql = @"
                SELECT p.ProductID, p.CompanyID, p.CategoryID, p.ProductName, p.Description, p.IsActive, c.CategoryName 
                FROM Product p
                INNER JOIN Category c ON p.CategoryID = c.CategoryID
                WHERE p.CompanyID = @cid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Product
                {
                    ProductID = (int)rdr["ProductID"],
                    CompanyID = (int)rdr["CompanyID"],
                    CategoryID = (int)rdr["CategoryID"],
                    ProductName = rdr["ProductName"] as string ?? "",
                    Description = rdr["Description"] as string ?? "",
                    IsActive = (bool)rdr["IsActive"],
                    CategoryName = rdr["CategoryName"] as string ?? ""
                });
            }
            return list;
        }

        public List<ProductVariant> GetVariants(int companyId)
        {
            var list = new List<ProductVariant>();
            using var conn = GetConnection();
            string sql = @"
                SELECT v.*, p.ProductName 
                FROM ProductVariant v
                INNER JOIN Product p ON v.ProductID = p.ProductID
                WHERE p.CompanyID = @cid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new ProductVariant
                {
                    VariantID = (int)rdr["VariantID"],
                    ProductID = (int)rdr["ProductID"],
                    Size = rdr["Size"] as string ?? "Standard",
                    Color = rdr["Color"] as string ?? "N/A",
                    SKU = rdr["SKU"] as string ?? "",
                    Barcode = rdr["Barcode"] as string ?? "",
                    Price = rdr["Price"] != DBNull.Value ? (decimal)rdr["Price"] : 0m,
                    ProductName = rdr["ProductName"] as string ?? ""
                });
            }
            return list;
        }

        public int AddProduct(Product product)
        {
            using var conn = GetConnection();
            string sql = @"
                INSERT INTO Product (CompanyID, CategoryID, ProductName, Description, IsActive)
                OUTPUT INSERTED.ProductID
                VALUES (@cid, @catid, @name, @desc, @active)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", product.CompanyID);
            cmd.Parameters.AddWithValue("@catid", product.CategoryID);
            cmd.Parameters.AddWithValue("@name", product.ProductName);
            cmd.Parameters.AddWithValue("@desc", (object)product.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active", product.IsActive);
            conn.Open();
            return (int)cmd.ExecuteScalar();
        }

        public int AddVariant(ProductVariant variant)
        {
            using var conn = GetConnection();
            string sql = @"
                INSERT INTO ProductVariant (ProductID, Size, Color, SKU, Barcode, Price)
                OUTPUT INSERTED.VariantID
                VALUES (@pid, @size, @color, @sku, @barcode, @price)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", variant.ProductID);
            cmd.Parameters.AddWithValue("@size", (object)variant.Size ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@color", (object)variant.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sku", (object)variant.SKU ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@barcode", (object)variant.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@price", variant.Price);
            conn.Open();
            return (int)cmd.ExecuteScalar();
        }


        // --- WAREHOUSES ---

        public List<Warehouse> GetWarehouses(int companyId)
        {
            var list = new List<Warehouse>();
            using var conn = GetConnection();
            string sql = "SELECT WarehouseID, CompanyID, WarehouseName, Location FROM Warehouse WHERE CompanyID = @cid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Warehouse
                {
                    WarehouseID = (int)rdr["WarehouseID"],
                    CompanyID = (int)rdr["CompanyID"],
                    WarehouseName = rdr["WarehouseName"] as string ?? "",
                    Location = rdr["Location"] as string ?? ""
                });
            }
            return list;
        }

        public void AddWarehouse(Warehouse warehouse)
        {
            using var conn = GetConnection();
            string sql = "INSERT INTO Warehouse (CompanyID, WarehouseName, Location) VALUES (@cid, @name, @loc)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", warehouse.CompanyID);
            cmd.Parameters.AddWithValue("@name", warehouse.WarehouseName);
            cmd.Parameters.AddWithValue("@loc", (object)warehouse.Location ?? DBNull.Value);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // --- INVENTORY ---

        public List<InventoryItem> GetInventory(int companyId, int? categoryId = null, string? status = null, int? warehouseId = null, int? variantId = null)
        {
            var list = new List<InventoryItem>();
            using var conn = GetConnection();
            string sql = @"
                SELECT 
                    ISNULL(i.InventoryID, 0) as InventoryID, 
                    p.CompanyID, 
                    v.VariantID, 
                    ISNULL(i.WarehouseID, 0) as WarehouseID, 
                    ISNULL(i.QuantityOnHand, 0) as QuantityOnHand, 
                    ISNULL(i.MinimumStockLevel, 0) as MinimumStockLevel, 
                    ISNULL(i.LastUpdated, GETDATE()) as LastUpdated,
                    p.ProductName, 
                    v.Color, 
                    v.Size, 
                    v.SKU, 
                    v.Barcode,
                    v.Price as VariantPrice,
                    ISNULL(w.WarehouseName, 'Unassigned') as WarehouseName
                FROM Product p
                JOIN ProductVariant v ON p.ProductID = v.ProductID
                LEFT JOIN Inventory i ON v.VariantID = i.VariantID
                LEFT JOIN Warehouse w ON i.WarehouseID = w.WarehouseID
                WHERE p.CompanyID = @cid";

            if (categoryId.HasValue)
            {
                sql += " AND p.CategoryID = @catid";
            }
            if (warehouseId.HasValue)
            {
                sql += " AND i.WarehouseID = @whid";
            }
            if (variantId.HasValue)
            {
                sql += " AND v.VariantID = @vid";
            }
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "In Stock") sql += " AND i.QuantityOnHand > i.MinimumStockLevel";
                else if (status == "Low Stock") sql += " AND i.QuantityOnHand <= i.MinimumStockLevel AND i.QuantityOnHand > 0";
                else if (status == "Out of Stock") sql += " AND (i.QuantityOnHand IS NULL OR i.QuantityOnHand <= 0)";
            }
                
            sql += " ORDER BY i.LastUpdated DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
            if (variantId.HasValue) cmd.Parameters.AddWithValue("@vid", variantId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new InventoryItem
                {
                    InventoryID = (int)rdr["InventoryID"],
                    CompanyID = (int)rdr["CompanyID"],
                    VariantID = (int)rdr["VariantID"],
                    WarehouseID = (int)rdr["WarehouseID"],
                    QuantityOnHand = (int)rdr["QuantityOnHand"],
                    MinimumStockLevel = (int)rdr["MinimumStockLevel"],
                    LastUpdated = (DateTime)rdr["LastUpdated"],
                    ProductName = rdr["ProductName"] as string ?? "",
                    Color = rdr["Color"] as string ?? "",
                    Size = rdr["Size"] as string ?? "",
                    SKU = rdr["SKU"] as string ?? "",
                    Barcode = rdr["Barcode"] as string ?? "",
                    VariantPrice = rdr["VariantPrice"] != DBNull.Value ? (decimal)rdr["VariantPrice"] : 0m,
                    WarehouseName = rdr["WarehouseName"] as string ?? ""
                });
            }
            return list;
        }

        public void UpdateStock(int inventoryId, int companyId, int change, int userId, string type, int? refId = null)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Update Inventory
                string updateSql = "UPDATE Inventory SET QuantityOnHand = QuantityOnHand + @change, LastUpdated = GETDATE() WHERE InventoryID = @iid AND CompanyID = @cid";
                using var updateCmd = new SqlCommand(updateSql, conn, trans);
                updateCmd.Parameters.AddWithValue("@change", change);
                updateCmd.Parameters.AddWithValue("@iid", inventoryId);
                updateCmd.Parameters.AddWithValue("@cid", companyId);
                updateCmd.ExecuteNonQuery();

                // 2. Log Transaction
                string logSql = @"
                    INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate, ChangeQuantity, Status)
                    VALUES (@cid, @iid, @uid, @type, @refid, GETDATE(), @change, 'Completed')";
                using var logCmd = new SqlCommand(logSql, conn, trans);
                logCmd.Parameters.AddWithValue("@cid", companyId);
                logCmd.Parameters.AddWithValue("@iid", inventoryId);
                logCmd.Parameters.AddWithValue("@uid", userId);
                logCmd.Parameters.AddWithValue("@type", type);
                logCmd.Parameters.AddWithValue("@refid", (object?)refId ?? DBNull.Value);
                logCmd.Parameters.AddWithValue("@change", change);
                logCmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void CreateInventoryRecord(InventoryItem item)
        {
            using var conn = GetConnection();
            conn.Open();
            
            // Check if record exists
            string checkSql = "SELECT InventoryID FROM Inventory WHERE VariantID = @vid AND WarehouseID = @wid AND CompanyID = @cid";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@cid", item.CompanyID);
            checkCmd.Parameters.AddWithValue("@vid", item.VariantID);
            checkCmd.Parameters.AddWithValue("@wid", item.WarehouseID);
            
            var existingId = checkCmd.ExecuteScalar();
            
            if (existingId != null)
            {
                // Update existing record (Add to current quantity)
                string updateSql = @"
                    UPDATE Inventory 
                    SET QuantityOnHand = QuantityOnHand + @qty, 
                        MinimumStockLevel = @min, 
                        LastUpdated = GETDATE() 
                    WHERE InventoryID = @iid";
                using var updateCmd = new SqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@qty", item.QuantityOnHand);
                updateCmd.Parameters.AddWithValue("@min", item.MinimumStockLevel);
                updateCmd.Parameters.AddWithValue("@iid", (int)existingId);
                updateCmd.ExecuteNonQuery();
            }
            else
            {
                // Insert new record
                string insertSql = @"
                    INSERT INTO Inventory (CompanyID, VariantID, WarehouseID, QuantityOnHand, MinimumStockLevel, LastUpdated)
                    VALUES (@cid, @vid, @wid, @qty, @min, GETDATE())";
                using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@cid", item.CompanyID);
                insertCmd.Parameters.AddWithValue("@vid", item.VariantID);
                insertCmd.Parameters.AddWithValue("@wid", item.WarehouseID);
                insertCmd.Parameters.AddWithValue("@qty", item.QuantityOnHand);
                insertCmd.Parameters.AddWithValue("@min", item.MinimumStockLevel);
                insertCmd.ExecuteNonQuery();
            }
        }

        public void UpdateVariantPrice(int variantId, decimal price)
        {
            using var conn = GetConnection();
            string sql = "UPDATE ProductVariant SET Price = @price WHERE VariantID = @vid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@vid", variantId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdateInventorySettings(int inventoryId, int companyId, int minStock, decimal price)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Get VariantID
                string getVidSql = "SELECT VariantID FROM Inventory WHERE InventoryID = @iid AND CompanyID = @cid";
                using var getVidCmd = new SqlCommand(getVidSql, conn, trans);
                getVidCmd.Parameters.AddWithValue("@iid", inventoryId);
                getVidCmd.Parameters.AddWithValue("@cid", companyId);
                var variantIdObj = getVidCmd.ExecuteScalar();

                if (variantIdObj != null && variantIdObj != DBNull.Value)
                {
                    int variantId = (int)variantIdObj;

                    // 2. Update Inventory (Settings only, not Qty)
                    string invSql = @"UPDATE Inventory 
                                      SET MinimumStockLevel = @minStock, LastUpdated = GETDATE()
                                      WHERE InventoryID = @iid AND CompanyID = @cid";
                    using var invCmd = new SqlCommand(invSql, conn, trans);
                    invCmd.Parameters.AddWithValue("@minStock", minStock);
                    invCmd.Parameters.AddWithValue("@iid", inventoryId);
                    invCmd.Parameters.AddWithValue("@cid", companyId);
                    invCmd.ExecuteNonQuery();

                    // 3. Update Price in Variant table
                    string priceSql = "UPDATE ProductVariant SET Price = @price WHERE VariantID = @vid";
                    using var priceCmd = new SqlCommand(priceSql, conn, trans);
                    priceCmd.Parameters.AddWithValue("@price", price);
                    priceCmd.Parameters.AddWithValue("@vid", variantId);
                    priceCmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void UpdateInventoryItem(int inventoryId, int companyId, int quantity, int minStock, decimal price)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // 1. Get VariantID
                string getVidSql = "SELECT VariantID FROM Inventory WHERE InventoryID = @iid AND CompanyID = @cid";
                using var getVidCmd = new SqlCommand(getVidSql, conn, trans);
                getVidCmd.Parameters.AddWithValue("@iid", inventoryId);
                getVidCmd.Parameters.AddWithValue("@cid", companyId);
                var variantIdObj = getVidCmd.ExecuteScalar();

                if (variantIdObj != null && variantIdObj != DBNull.Value)
                {
                    int variantId = (int)variantIdObj;

                    // 2. Update Inventory
                    string invSql = @"UPDATE Inventory 
                                      SET QuantityOnHand = @qty, MinimumStockLevel = @minStock, LastUpdated = GETDATE()
                                      WHERE InventoryID = @iid AND CompanyID = @cid";
                    using var invCmd = new SqlCommand(invSql, conn, trans);
                    invCmd.Parameters.AddWithValue("@qty", quantity);
                    invCmd.Parameters.AddWithValue("@minStock", minStock);
                    invCmd.Parameters.AddWithValue("@iid", inventoryId);
                    invCmd.Parameters.AddWithValue("@cid", companyId);
                    invCmd.ExecuteNonQuery();

                    // 3. Update Price
                    string varSql = "UPDATE ProductVariant SET Price = @price WHERE VariantID = @vid";
                    using var varCmd = new SqlCommand(varSql, conn, trans);
                    varCmd.Parameters.AddWithValue("@price", price);
                    varCmd.Parameters.AddWithValue("@vid", variantId);
                    varCmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }


        public void TransferStock(int variantId, int fromWhId, int toWhId, int qty, int companyId, int userId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Deduct from source
                string deductSql = "UPDATE Inventory SET QuantityOnHand = QuantityOnHand - @qty, LastUpdated = GETDATE() WHERE VariantID = @vid AND WarehouseID = @fwh AND CompanyID = @cid";
                using var deductCmd = new SqlCommand(deductSql, conn, trans);
                deductCmd.Parameters.AddWithValue("@qty", qty);
                deductCmd.Parameters.AddWithValue("@vid", variantId);
                deductCmd.Parameters.AddWithValue("@fwh", fromWhId);
                deductCmd.Parameters.AddWithValue("@cid", companyId);
                deductCmd.ExecuteNonQuery();

                // 2. Check if destination exists
                string checkSql = "SELECT InventoryID FROM Inventory WHERE VariantID = @vid AND WarehouseID = @twh AND CompanyID = @cid";
                using var checkCmd = new SqlCommand(checkSql, conn, trans);
                checkCmd.Parameters.AddWithValue("@vid", variantId);
                checkCmd.Parameters.AddWithValue("@twh", toWhId);
                checkCmd.Parameters.AddWithValue("@cid", companyId);
                var destInvId = checkCmd.ExecuteScalar();

                if (destInvId != null)
                {
                    // Update existing destination
                    string addSql = "UPDATE Inventory SET QuantityOnHand = QuantityOnHand + @qty, LastUpdated = GETDATE() WHERE InventoryID = @iid";
                    using var addCmd = new SqlCommand(addSql, conn, trans);
                    addCmd.Parameters.AddWithValue("@qty", qty);
                    addCmd.Parameters.AddWithValue("@iid", (int)destInvId);
                    addCmd.ExecuteNonQuery();
                }
                else
                {
                    // Create new destination record
                    string createSql = "INSERT INTO Inventory (CompanyID, VariantID, WarehouseID, QuantityOnHand, MinimumStockLevel, LastUpdated) VALUES (@cid, @vid, @twh, @qty, 5, GETDATE())";
                    using var createCmd = new SqlCommand(createSql, conn, trans);
                    createCmd.Parameters.AddWithValue("@cid", companyId);
                    createCmd.Parameters.AddWithValue("@vid", variantId);
                    createCmd.Parameters.AddWithValue("@twh", toWhId);
                    createCmd.Parameters.AddWithValue("@qty", qty);
                    createCmd.ExecuteNonQuery();
                }

                // 3. Log transactions
                string logSql = @"INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate, ChangeQuantity, Status)
                                  SELECT @cid, InventoryID, @uid, 'Transfer', @vid, GETDATE(), 
                                         CASE WHEN WarehouseID = @fwh THEN -@qty ELSE @qty END, 'Completed'
                                  FROM Inventory WHERE VariantID = @vid AND (WarehouseID = @fwh OR WarehouseID = @twh)";
                using var logCmd = new SqlCommand(logSql, conn, trans);
                logCmd.Parameters.AddWithValue("@cid", companyId);
                logCmd.Parameters.AddWithValue("@uid", userId);
                logCmd.Parameters.AddWithValue("@vid", variantId);
                logCmd.Parameters.AddWithValue("@fwh", fromWhId);
                logCmd.Parameters.AddWithValue("@twh", toWhId);
                logCmd.Parameters.AddWithValue("@qty", qty);
                logCmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        // --- SALES ORDERS ---

        public List<SalesOrder> GetSalesOrders(int companyId, string? status = null, DateTime? date = null)
        {
            var list = new List<SalesOrder>();
            using var conn = GetConnection();
            string sql = @"
                SELECT s.*, u.FullName as CreatedByName,
                       ISNULL((SELECT SUM(Quantity) FROM SalesOrderItem WHERE SalesOrderID = s.SalesOrderID), 0) as TotalQuantity
                FROM SalesOrder s
                LEFT JOIN Users u ON s.CreatedBy = u.UserID
                WHERE s.CompanyID = @cid";

            if (!string.IsNullOrEmpty(status) && status != "All Statuses")
            {
                sql += " AND s.Status = @status";
            }
            if (date.HasValue)
            {
                sql += " AND CAST(s.OrderDate AS DATE) = CAST(@date AS DATE)";
            }

            sql += " ORDER BY s.OrderDate DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (!string.IsNullOrEmpty(status) && status != "All Statuses") cmd.Parameters.AddWithValue("@status", status);
            if (date.HasValue) cmd.Parameters.AddWithValue("@date", date.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new SalesOrder
                {
                    SalesOrderID = (int)rdr["SalesOrderID"],
                    CompanyID = (int)rdr["CompanyID"],
                    CreatedBy = (int)rdr["CreatedBy"],
                    OrderDate = (DateTime)rdr["OrderDate"],
                    Status = rdr["Status"] as string ?? "Pending",
                    TotalAmount = (decimal)rdr["TotalAmount"],
                    TotalQuantity = (int)rdr["TotalQuantity"],
                    CreatedByName = rdr["CreatedByName"] as string ?? "Unknown",
                    CustomerName = rdr["CustomerName"] as string ?? ""
                });
            }
            return list;
        }

        public void CreateSalesOrder(SalesOrder order, List<SalesOrderItem> items, int warehouseId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Validate Stock for all items
                foreach (var item in items)
                {
                    string stockSql = @"
                        SELECT ISNULL(QuantityOnHand, 0) 
                        FROM Inventory 
                        WHERE VariantID = @vid AND WarehouseID = @wid AND CompanyID = @cid";
                    using var stockCmd = new SqlCommand(stockSql, conn, trans);
                    stockCmd.Parameters.AddWithValue("@vid", item.VariantID);
                    stockCmd.Parameters.AddWithValue("@wid", warehouseId);
                    stockCmd.Parameters.AddWithValue("@cid", order.CompanyID);
                    
                    var available = (int?)stockCmd.ExecuteScalar() ?? 0;
                    if (available < item.Quantity)
                    {
                        throw new Exception($"Insufficient stock for variant ID {item.VariantID}. Available: {available}, Requested: {item.Quantity}");
                    }
                }

                // 2. Insert SalesOrder
                string orderSql = @"
                    INSERT INTO SalesOrder (CompanyID, CreatedBy, OrderDate, Status, TotalAmount, CustomerName)
                    OUTPUT INSERTED.SalesOrderID
                    VALUES (@cid, @uid, GETDATE(), 'Paid', @total, @customer)";
                using var orderCmd = new SqlCommand(orderSql, conn, trans);
                orderCmd.Parameters.AddWithValue("@cid", order.CompanyID);
                orderCmd.Parameters.AddWithValue("@uid", order.CreatedBy);
                orderCmd.Parameters.AddWithValue("@total", order.TotalAmount);
                orderCmd.Parameters.AddWithValue("@customer", (object?)order.CustomerName ?? DBNull.Value);
                int orderId = (int)orderCmd.ExecuteScalar();

                // 3. Insert Items and Update Inventory
                foreach (var item in items)
                {
                    // Insert Item
                    string itemSql = @"
                        INSERT INTO SalesOrderItem (SalesOrderID, VariantID, Quantity, UnitPrice)
                        VALUES (@oid, @vid, @qty, @price)";
                    using var itemCmd = new SqlCommand(itemSql, conn, trans);
                    itemCmd.Parameters.AddWithValue("@oid", orderId);
                    itemCmd.Parameters.AddWithValue("@vid", item.VariantID);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@price", item.UnitPrice);
                    itemCmd.ExecuteNonQuery();

                    // Update Inventory
                    string invSql = @"
                        UPDATE Inventory 
                        SET QuantityOnHand = QuantityOnHand - @qty, LastUpdated = GETDATE()
                        WHERE VariantID = @vid AND WarehouseID = @wid AND CompanyID = @cid";
                    using var invCmd = new SqlCommand(invSql, conn, trans);
                    invCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    invCmd.Parameters.AddWithValue("@vid", item.VariantID);
                    invCmd.Parameters.AddWithValue("@wid", warehouseId);
                    invCmd.Parameters.AddWithValue("@cid", order.CompanyID);
                    invCmd.ExecuteNonQuery();

                    // Log Transaction
                    string logSql = @"
                        INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate, ChangeQuantity, Status)
                        SELECT @cid, InventoryID, @uid, 'Sale', @oid, GETDATE(), -@qty, 'Completed'
                        FROM Inventory WHERE VariantID = @vid AND WarehouseID = @wid";
                    using var logCmd = new SqlCommand(logSql, conn, trans);
                    logCmd.Parameters.AddWithValue("@cid", order.CompanyID);
                    logCmd.Parameters.AddWithValue("@uid", order.CreatedBy);
                    logCmd.Parameters.AddWithValue("@oid", orderId);
                    logCmd.Parameters.AddWithValue("@vid", item.VariantID);
                    logCmd.Parameters.AddWithValue("@wid", warehouseId);
                    logCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    logCmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        // --- STOCK ADJUSTMENTS ---

        public List<StockAdjustment> GetStockAdjustments(int companyId, string? status = null, string? type = null)
        {
            var list = new List<StockAdjustment>();
            using var conn = GetConnection();
            string sql = @"
                SELECT sa.*, 
                       v.SKU, v.Color, v.Size, 
                       p.ProductName, 
                       w.WarehouseName, 
                       u.FullName as RequestedByName
                FROM StockAdjustment sa
                INNER JOIN ProductVariant v ON sa.VariantID = v.VariantID
                INNER JOIN Product p ON v.ProductID = p.ProductID
                INNER JOIN Warehouse w ON sa.WarehouseID = w.WarehouseID
                INNER JOIN Users u ON sa.RequestedBy = u.UserID
                WHERE sa.CompanyID = @cid";

            if (!string.IsNullOrEmpty(status) && status != "All")
                sql += " AND sa.Status = @status";
            if (!string.IsNullOrEmpty(type) && type != "All Types")
                sql += " AND sa.Type = @type";

            sql += " ORDER BY sa.DateRequested DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (!string.IsNullOrEmpty(status) && status != "All")
                cmd.Parameters.AddWithValue("@status", status);
            if (!string.IsNullOrEmpty(type) && type != "All Types")
                cmd.Parameters.AddWithValue("@type", type);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new StockAdjustment
                {
                    AdjustmentID = (int)rdr["AdjustmentID"],
                    CompanyID = (int)rdr["CompanyID"],
                    VariantID = (int)rdr["VariantID"],
                    WarehouseID = (int)rdr["WarehouseID"],
                    Type = rdr["Type"] as string ?? "Correction",
                    ChangeQuantity = (int)rdr["ChangeQuantity"],
                    RequestedBy = (int)rdr["RequestedBy"],
                    Status = rdr["Status"] as string ?? "Pending",
                    Reason = rdr["Reason"] as string ?? "N/A",
                    DateRequested = (DateTime)rdr["DateRequested"],
                    ApprovedBy = rdr["ApprovedBy"] == DBNull.Value ? null : (int?)rdr["ApprovedBy"],
                    DateProcessed = rdr["DateProcessed"] == DBNull.Value ? null : (DateTime?)rdr["DateProcessed"],
                    ProductName = rdr["ProductName"] as string ?? "Unknown",
                    VariantInfo = $"{rdr["Color"]} / {rdr["Size"]} ({rdr["SKU"]})",
                    WarehouseName = rdr["WarehouseName"] as string ?? "N/A",
                    RequestedByName = rdr["RequestedByName"] as string ?? "System"
                });
            }
            return list;
        }

        public void AddStockAdjustment(StockAdjustment adj)
        {
            using var conn = GetConnection();
            string sql = @"
                INSERT INTO StockAdjustment 
                (CompanyID, VariantID, WarehouseID, Type, ChangeQuantity, RequestedBy, Status, Reason, DateRequested)
                VALUES 
                (@cid, @vid, @wid, @type, @qty, @req, @status, @reason, @date)";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", adj.CompanyID);
            cmd.Parameters.AddWithValue("@vid", adj.VariantID);
            cmd.Parameters.AddWithValue("@wid", adj.WarehouseID);
            cmd.Parameters.AddWithValue("@type", adj.Type);
            cmd.Parameters.AddWithValue("@qty", adj.ChangeQuantity);
            cmd.Parameters.AddWithValue("@req", adj.RequestedBy);
            cmd.Parameters.AddWithValue("@status", adj.Status);
            cmd.Parameters.AddWithValue("@reason", (object?)adj.Reason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@date", adj.DateRequested);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        public void ApproveStockAdjustment(int adjustmentId, int approvedByUserId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Get adjustment details
                string getSql = "SELECT * FROM StockAdjustment WHERE AdjustmentID = @id";
                using var getCmd = new SqlCommand(getSql, conn, trans);
                getCmd.Parameters.AddWithValue("@id", adjustmentId);
                StockAdjustment? adj = null;
                using (var rdr = getCmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        adj = new StockAdjustment
                        {
                            AdjustmentID = (int)rdr["AdjustmentID"],
                            CompanyID = (int)rdr["CompanyID"],
                            VariantID = (int)rdr["VariantID"],
                            WarehouseID = (int)rdr["WarehouseID"],
                            ChangeQuantity = (int)rdr["ChangeQuantity"],
                            Type = rdr["Type"] as string ?? "Correction",
                            Status = rdr["Status"] as string ?? "Pending"
                        };
                    }
                }

                if (adj == null || adj.Status != "Pending") return;

                // 2. Find InventoryID
                string invSql = "SELECT InventoryID FROM Inventory WHERE CompanyID = @cid AND VariantID = @vid AND WarehouseID = @wid";
                using var invCmd = new SqlCommand(invSql, conn, trans);
                invCmd.Parameters.AddWithValue("@cid", adj.CompanyID);
                invCmd.Parameters.AddWithValue("@vid", adj.VariantID);
                invCmd.Parameters.AddWithValue("@wid", adj.WarehouseID);
                object? invIdObj = invCmd.ExecuteScalar();
                
                int inventoryId = 0;
                if (invIdObj != null)
                {
                    inventoryId = (int)invIdObj;
                }
                else
                {
                    // If doesn't exist, create it (e.g. adding stock to a new warehouse)
                    string insertInvSql = "INSERT INTO Inventory (CompanyID, VariantID, WarehouseID, QuantityOnHand, MinimumStockLevel) VALUES (@cid, @vid, @wid, 0, 0); SELECT SCOPE_IDENTITY();";
                    using var insertInvCmd = new SqlCommand(insertInvSql, conn, trans);
                    insertInvCmd.Parameters.AddWithValue("@cid", adj.CompanyID);
                    insertInvCmd.Parameters.AddWithValue("@vid", adj.VariantID);
                    insertInvCmd.Parameters.AddWithValue("@wid", adj.WarehouseID);
                    inventoryId = Convert.ToInt32(insertInvCmd.ExecuteScalar());
                }

                // 3. Update Inventory Stock
                string updateInvSql = "UPDATE Inventory SET QuantityOnHand = QuantityOnHand + @change, LastUpdated = GETDATE() WHERE InventoryID = @iid";
                using var updateInvCmd = new SqlCommand(updateInvSql, conn, trans);
                updateInvCmd.Parameters.AddWithValue("@change", adj.ChangeQuantity);
                updateInvCmd.Parameters.AddWithValue("@iid", inventoryId);
                updateInvCmd.ExecuteNonQuery();

                // 4. Log Transaction
                string logSql = @"
                    INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate, ChangeQuantity, Status)
                    VALUES (@cid, @iid, @uid, @type, @refid, GETDATE(), @change, 'Completed')";
                using var logCmd = new SqlCommand(logSql, conn, trans);
                logCmd.Parameters.AddWithValue("@cid", adj.CompanyID);
                logCmd.Parameters.AddWithValue("@iid", inventoryId);
                logCmd.Parameters.AddWithValue("@uid", approvedByUserId);
                logCmd.Parameters.AddWithValue("@type", "Adjustment: " + adj.Type);
                logCmd.Parameters.AddWithValue("@refid", adj.AdjustmentID);
                logCmd.Parameters.AddWithValue("@change", adj.ChangeQuantity);
                logCmd.ExecuteNonQuery();

                // 5. Mark Adjustment as Approved
                string statusSql = "UPDATE StockAdjustment SET Status = 'Approved', ApprovedBy = @uid, DateProcessed = GETDATE() WHERE AdjustmentID = @id";
                using var statusCmd = new SqlCommand(statusSql, conn, trans);
                statusCmd.Parameters.AddWithValue("@uid", approvedByUserId);
                statusCmd.Parameters.AddWithValue("@id", adj.AdjustmentID);
                statusCmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void RejectStockAdjustment(int adjustmentId, int rejectedByUserId)
        {
            using var conn = GetConnection();
            string sql = "UPDATE StockAdjustment SET Status = 'Rejected', ApprovedBy = @uid, DateProcessed = GETDATE() WHERE AdjustmentID = @id AND Status = 'Pending'";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", adjustmentId);
            cmd.Parameters.AddWithValue("@uid", rejectedByUserId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // --- PROCUREMENT ---

        public List<Supplier> GetSuppliers(int companyId, string status = "Active")
        {
            var list = new List<Supplier>();
            using var conn = GetConnection();
            string sql = "SELECT * FROM Supplier WHERE CompanyID = @cid";
            
            if (status == "Active") sql += " AND IsActive = 1";
            else if (status == "Inactive") sql += " AND IsActive = 0";
            
            sql += " ORDER BY SupplierName";
            
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Supplier
                {
                    SupplierID = (int)rdr["SupplierID"],
                    CompanyID = (int)rdr["CompanyID"],
                    SupplierName = rdr["SupplierName"] as string ?? "Unknown",
                    ContactInfo = rdr["ContactInfo"] as string ?? "",
                    IsActive = (bool)rdr["IsActive"]
                });
            }
            return list;
        }

        public void UpdateSupplierStatus(int supplierId, bool isActive)
        {
            using var conn = GetConnection();
            string sql = "UPDATE Supplier SET IsActive = @active WHERE SupplierID = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@active", isActive);
            cmd.Parameters.AddWithValue("@id", supplierId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void AddSupplier(Supplier s)
        {
            using var conn = GetConnection();
            string sql = "INSERT INTO Supplier (CompanyID, SupplierName, ContactInfo) VALUES (@cid, @name, @info)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", s.CompanyID);
            cmd.Parameters.AddWithValue("@name", s.SupplierName);
            cmd.Parameters.AddWithValue("@info", s.ContactInfo ?? "");
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<PurchaseOrder> GetPurchaseOrders(int companyId, string? status = null, int? supplierId = null)
        {
            var list = new List<PurchaseOrder>();
            using var conn = GetConnection();
            string sql = @"
                SELECT po.*, s.SupplierName,
                    (SELECT ISNULL(SUM(Quantity), 0) FROM PurchaseOrderItem WHERE PurchaseOrderID = po.PurchaseOrderID) as TotalQty,
                    (SELECT COUNT(*) FROM PurchaseOrderItem WHERE PurchaseOrderID = po.PurchaseOrderID) as LineItemCount,
                    (SELECT TOP 1 p.ProductName + ' (' + v.Color + ' / ' + v.Size + ')' 
                     FROM PurchaseOrderItem poi
                     JOIN ProductVariant v ON poi.VariantID = v.VariantID
                     JOIN Product p ON v.ProductID = p.ProductID
                     WHERE poi.PurchaseOrderID = po.PurchaseOrderID) as FirstItem
                FROM PurchaseOrder po 
                JOIN Supplier s ON po.SupplierID = s.SupplierID 
                WHERE po.CompanyID = @cid";
            
            if (!string.IsNullOrEmpty(status) && status != "All Statuses")
                sql += " AND po.Status = @status";
            if (supplierId.HasValue)
                sql += " AND po.SupplierID = @sid";
            
            sql += " ORDER BY po.OrderDate DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (!string.IsNullOrEmpty(status) && status != "All Statuses")
                cmd.Parameters.AddWithValue("@status", status);
            if (supplierId.HasValue)
                cmd.Parameters.AddWithValue("@sid", supplierId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                int totalQty = (int)rdr["TotalQty"];
                int lineItemCount = (int)rdr["LineItemCount"];
                string firstItem = rdr["FirstItem"] as string ?? "No items";
                
                list.Add(new PurchaseOrder
                {
                    PurchaseOrderID = (int)rdr["PurchaseOrderID"],
                    CompanyID = (int)rdr["CompanyID"],
                    SupplierID = (int)rdr["SupplierID"],
                    WarehouseID = rdr["WarehouseID"] != DBNull.Value ? (int)rdr["WarehouseID"] : (int?)null,
                    CreatedBy = (int)rdr["CreatedBy"],
                    OrderDate = (DateTime)rdr["OrderDate"],
                    Status = rdr["Status"] as string ?? "Pending",
                    TotalCost = (decimal)rdr["TotalCost"],
                    SupplierName = rdr["SupplierName"] as string ?? "Unknown",
                    TotalQuantity = totalQty,
                    ItemSummary = lineItemCount > 1 ? $"{firstItem} + {lineItemCount - 1} more" : firstItem
                });
            }
            return list;
        }

        public void AddPurchaseOrder(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                string sqlPO = @"
                    INSERT INTO PurchaseOrder (CompanyID, SupplierID, WarehouseID, CreatedBy, OrderDate, Status, TotalCost) 
                    OUTPUT INSERTED.PurchaseOrderID
                    VALUES (@cid, @sid, @wid, @uid, @date, @status, @total)";
                
                using var cmdPO = new SqlCommand(sqlPO, conn, trans);
                cmdPO.Parameters.AddWithValue("@cid", po.CompanyID);
                cmdPO.Parameters.AddWithValue("@sid", po.SupplierID);
                cmdPO.Parameters.AddWithValue("@wid", (object?)po.WarehouseID ?? DBNull.Value);
                cmdPO.Parameters.AddWithValue("@uid", po.CreatedBy);
                cmdPO.Parameters.AddWithValue("@date", po.OrderDate);
                cmdPO.Parameters.AddWithValue("@status", po.Status);
                cmdPO.Parameters.AddWithValue("@total", po.TotalCost);
                int poId = (int)cmdPO.ExecuteScalar();

                string sqlItem = @"
                    INSERT INTO PurchaseOrderItem (PurchaseOrderID, VariantID, Quantity, CostPerUnit)
                    VALUES (@poid, @vid, @qty, @cost)";
                
                foreach (var item in items)
                {
                    using var cmdItem = new SqlCommand(sqlItem, conn, trans);
                    cmdItem.Parameters.AddWithValue("@poid", poId);
                    cmdItem.Parameters.AddWithValue("@vid", item.VariantID);
                    cmdItem.Parameters.AddWithValue("@qty", item.Quantity);
                    cmdItem.Parameters.AddWithValue("@cost", item.CostPerUnit);
                    cmdItem.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void ReceivePurchaseOrder(int poId, int receivedByUserId)
        {
            using var conn = GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Get PO details
                string getPOSql = "SELECT CompanyID, WarehouseID, Status FROM PurchaseOrder WHERE PurchaseOrderID = @id";
                using var getPOCmd = new SqlCommand(getPOSql, conn, trans);
                getPOCmd.Parameters.AddWithValue("@id", poId);
                using var rdr = getPOCmd.ExecuteReader();
                if (!rdr.Read()) throw new Exception("Purchase Order not found.");
                if (rdr["Status"].ToString() == "Received") throw new Exception("PO already received.");
                
                int cid = (int)rdr["CompanyID"];
                int? whId = rdr["WarehouseID"] != DBNull.Value ? (int)rdr["WarehouseID"] : (int?)null;
                rdr.Close();

                if (!whId.HasValue) throw new Exception("No target warehouse assigned to this PO.");

                // 2. Get Items
                var items = new List<PurchaseOrderItem>();
                string getItemsSql = "SELECT * FROM PurchaseOrderItem WHERE PurchaseOrderID = @poid";
                using var getItemsCmd = new SqlCommand(getItemsSql, conn, trans);
                getItemsCmd.Parameters.AddWithValue("@poid", poId);
                using var rdr2 = getItemsCmd.ExecuteReader();
                while (rdr2.Read())
                {
                    items.Add(new PurchaseOrderItem
                    {
                        VariantID = (int)rdr2["VariantID"],
                        Quantity = (int)rdr2["Quantity"]
                    });
                }
                rdr2.Close();

                // 3. Update Status
                string updateStatusSql = "UPDATE PurchaseOrder SET Status = 'Received' WHERE PurchaseOrderID = @id";
                using var statusCmd = new SqlCommand(updateStatusSql, conn, trans);
                statusCmd.Parameters.AddWithValue("@id", poId);
                statusCmd.ExecuteNonQuery();

                // 4. Update Inventory
                foreach (var item in items)
                {
                    // Check if record exists
                    string checkSql = "SELECT InventoryID FROM Inventory WHERE VariantID = @vid AND WarehouseID = @wid AND CompanyID = @cid";
                    using var checkCmd = new SqlCommand(checkSql, conn, trans);
                    checkCmd.Parameters.AddWithValue("@vid", item.VariantID);
                    checkCmd.Parameters.AddWithValue("@wid", whId.Value);
                    checkCmd.Parameters.AddWithValue("@cid", cid);
                    var invId = checkCmd.ExecuteScalar();

                    int actualInvId;
                    if (invId != null)
                    {
                        actualInvId = (int)invId;
                        string updateInv = "UPDATE Inventory SET QuantityOnHand = QuantityOnHand + @qty, LastUpdated = GETDATE() WHERE InventoryID = @iid";
                        using var updateCmd = new SqlCommand(updateInv, conn, trans);
                        updateCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        updateCmd.Parameters.AddWithValue("@iid", actualInvId);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        string insertInv = "INSERT INTO Inventory (CompanyID, VariantID, WarehouseID, QuantityOnHand, MinimumStockLevel, LastUpdated) OUTPUT INSERTED.InventoryID VALUES (@cid, @vid, @wid, @qty, 0, GETDATE())";
                        using var insertCmd = new SqlCommand(insertInv, conn, trans);
                        insertCmd.Parameters.AddWithValue("@cid", cid);
                        insertCmd.Parameters.AddWithValue("@vid", item.VariantID);
                        insertCmd.Parameters.AddWithValue("@wid", whId.Value);
                        insertCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        actualInvId = (int)insertCmd.ExecuteScalar();
                    }

                    // Log Transaction
                    string logSql = "INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate, ChangeQuantity, Status) VALUES (@cid, @iid, @uid, 'Procurement', @poid, GETDATE(), @qty, 'Received')";
                    using var logCmd = new SqlCommand(logSql, conn, trans);
                    logCmd.Parameters.AddWithValue("@cid", cid);
                    logCmd.Parameters.AddWithValue("@iid", actualInvId);
                    logCmd.Parameters.AddWithValue("@uid", receivedByUserId);
                    logCmd.Parameters.AddWithValue("@poid", poId);
                    logCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    logCmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        public void ApprovePurchaseOrder(int poId)
        {
            using var conn = GetConnection();
            string sql = "UPDATE PurchaseOrder SET Status = 'Ordered' WHERE PurchaseOrderID = @id AND Status = 'Pending'";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", poId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // --- REPORTING ---

        public InventoryValuationSummary GetValuationSummary(int companyId, int? warehouseId = null, int? categoryId = null)
        {
            var summary = new InventoryValuationSummary();
            using var conn = GetConnection();
            
            string whClause = warehouseId.HasValue ? " AND i.WarehouseID = @whid " : "";
            string catClause = categoryId.HasValue ? " AND p.CategoryID = @catid " : "";

            string sql = $@"
                SELECT 
                    SUM(CAST(i.QuantityOnHand AS DECIMAL(18,2)) * v.Price) as TotalAssetValue,
                    AVG(NULLIF(v.Price, 0)) as AvgUnitCost,
                    COUNT(DISTINCT v.VariantID) as TotalSKUs
                FROM Inventory i
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                WHERE p.CompanyID = @cid {whClause} {catClause}";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                summary.TotalAssetValue = rdr["TotalAssetValue"] != DBNull.Value ? (decimal)rdr["TotalAssetValue"] : 0m;
                summary.AvgUnitCost = rdr["AvgUnitCost"] != DBNull.Value ? Convert.ToDecimal(rdr["AvgUnitCost"]) : 0m;
                summary.TotalSKUs = rdr["TotalSKUs"] != DBNull.Value ? (int)rdr["TotalSKUs"] : 0;
            }
            rdr.Close();

            // Top Category
            string topCatSql = $@"
                SELECT TOP 1 c.CategoryName, SUM(CAST(i.QuantityOnHand AS DECIMAL(18,2)) * v.Price) as CatValue
                FROM Inventory i
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                JOIN Category c ON p.CategoryID = c.CategoryID
                WHERE p.CompanyID = @cid {whClause} {catClause}
                GROUP BY c.CategoryName
                ORDER BY CatValue DESC";
            
            using var cmd2 = new SqlCommand(topCatSql, conn);
            cmd2.Parameters.AddWithValue("@cid", companyId);
            if (warehouseId.HasValue) cmd2.Parameters.AddWithValue("@whid", warehouseId.Value);
            if (categoryId.HasValue) cmd2.Parameters.AddWithValue("@catid", categoryId.Value);

            using var rdr2 = cmd2.ExecuteReader();
            if (rdr2.Read())
            {
                summary.TopCategoryName = rdr2["CategoryName"]?.ToString() ?? "N/A";
                summary.TopCategoryValue = (decimal)rdr2["CatValue"];
            }

            return summary;
        }

        public List<StockMovementViewModel> GetRecentMovements(int companyId, int limit = 15, int? warehouseId = null, int? categoryId = null)
        {
            var list = new List<StockMovementViewModel>();
            using var conn = GetConnection();

            string whClause = warehouseId.HasValue ? " AND i.WarehouseID = @whid " : "";
            string catClause = categoryId.HasValue ? " AND p.CategoryID = @catid " : "";

            string sql = $@"
                SELECT TOP (@limit)
                    t.InventoryTransactionID,
                    t.TransactionType,
                    t.TransactionDate,
                    t.ChangeQuantity,
                    t.Status,
                    p.ProductName + ' (' + v.Color + ' / ' + v.Size + ')' as ProductVariant
                FROM InventoryTransaction t
                JOIN Inventory i ON t.InventoryID = i.InventoryID
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                WHERE t.CompanyID = @cid {whClause} {catClause}
                ORDER BY t.TransactionDate DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@limit", limit);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new StockMovementViewModel
                {
                    TransactionID = "TRX-" + (rdr["InventoryTransactionID"]?.ToString() ?? "0").PadLeft(4, '0'),
                    Type = rdr["TransactionType"]?.ToString() ?? "Unknown",
                    ProductVariant = rdr["ProductVariant"]?.ToString() ?? "N/A",
                    Quantity = rdr["ChangeQuantity"] != DBNull.Value ? (int)rdr["ChangeQuantity"] : 0,
                    Date = rdr["TransactionDate"] != DBNull.Value ? (DateTime)rdr["TransactionDate"] : DateTime.Now,
                    Status = rdr["Status"]?.ToString() ?? "Completed"
                });
            }
            return list;
        }

        public List<DetailedValuationViewModel> GetDetailedValuation(int companyId, int? warehouseId = null, int? categoryId = null)
        {
            var list = new List<DetailedValuationViewModel>();
            using var conn = GetConnection();

            string whClause = warehouseId.HasValue ? " AND i.WarehouseID = @whid " : "";
            string catClause = categoryId.HasValue ? " AND p.CategoryID = @catid " : "";

            string sql = $@"
                SELECT 
                    p.ProductName,
                    v.SKU,
                    c.CategoryName,
                    wh.WarehouseName,
                    i.QuantityOnHand,
                    v.Price as UnitCost,
                    (CAST(i.QuantityOnHand AS DECIMAL(18,2)) * v.Price) as TotalValue,
                    i.LastUpdated
                FROM Inventory i
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                JOIN Category c ON p.CategoryID = c.CategoryID
                JOIN Warehouse wh ON i.WarehouseID = wh.WarehouseID
                WHERE p.CompanyID = @cid {whClause} {catClause}
                ORDER BY TotalValue DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new DetailedValuationViewModel
                {
                    ProductName = rdr["ProductName"]?.ToString() ?? "N/A",
                    SKU = rdr["SKU"]?.ToString() ?? "N/A",
                    Category = rdr["CategoryName"]?.ToString() ?? "Uncategorized",
                    Warehouse = rdr["WarehouseName"]?.ToString() ?? "Unknown",
                    QtyOnHand = rdr["QuantityOnHand"] != DBNull.Value ? (int)rdr["QuantityOnHand"] : 0,
                    UnitCost = rdr["UnitCost"] != DBNull.Value ? (decimal)rdr["UnitCost"] : 0,
                    TotalValue = rdr["TotalValue"] != DBNull.Value ? (decimal)rdr["TotalValue"] : 0,
                    LastRevalDate = rdr["LastUpdated"] != DBNull.Value ? (DateTime)rdr["LastUpdated"] : DateTime.Now
                });
            }
            return list;
        }

        public List<CategoryValuePoint> GetCategoryDistribution(int companyId, int? warehouseId = null)
        {
            var list = new List<CategoryValuePoint>();
            using var conn = GetConnection();

            string whClause = warehouseId.HasValue ? " AND i.WarehouseID = @whid " : "";

            string sql = $@"
                SELECT 
                    c.CategoryName,
                    SUM(CAST(i.QuantityOnHand AS DECIMAL(18,2)) * v.Price) as TotalValue
                FROM Inventory i
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                JOIN Category c ON p.CategoryID = c.CategoryID
                WHERE p.CompanyID = @cid {whClause}
                GROUP BY c.CategoryName
                ORDER BY TotalValue DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new CategoryValuePoint
                {
                    CategoryName = rdr["CategoryName"]?.ToString() ?? "Uncategorized",
                    Value = rdr["TotalValue"] != DBNull.Value ? (decimal)rdr["TotalValue"] : 0
                });
            }
            rdr.Close();

            decimal total = list.Sum(x => x.Value);
            if (total > 0)
            {
                foreach (var item in list)
                {
                    item.Percentage = (double)(item.Value / total * 100);
                }
            }

            return list;
        }

        public List<ValuationHistoryPoint> GetValuationHistory(int companyId, int? warehouseId = null, int? categoryId = null)
        {
            var history = new List<ValuationHistoryPoint>();
            using var conn = GetConnection();

            // 1. Get current valuation per variant
            string whClause = warehouseId.HasValue ? " AND i.WarehouseID = @whid " : "";
            string catClause = categoryId.HasValue ? " AND p.CategoryID = @catid " : "";

            string sqlCurrent = $@"
                SELECT 
                    i.VariantID, 
                    i.QuantityOnHand, 
                    v.Price
                FROM Inventory i
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                JOIN Product p ON v.ProductID = p.ProductID
                WHERE i.CompanyID = @cid {whClause} {catClause}";

            var currentStock = new Dictionary<int, (int qty, decimal price)>();
            using (var cmd = new SqlCommand(sqlCurrent, conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
                if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);
                
                conn.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    int vId = rdr["VariantID"] != DBNull.Value ? (int)rdr["VariantID"] : 0;
                    int qty = rdr["QuantityOnHand"] != DBNull.Value ? (int)rdr["QuantityOnHand"] : 0;
                    decimal price = rdr["Price"] != DBNull.Value ? (decimal)rdr["Price"] : 0;
                    currentStock[vId] = (qty, price);
                }
            }

            // 2. Get all transactions for these months
            string sqlTrans = $@"
                SELECT 
                    i.VariantID, 
                    t.ChangeQuantity, 
                    t.TransactionDate
                FROM InventoryTransaction t
                JOIN Inventory i ON t.InventoryID = i.InventoryID
                JOIN Product p ON i.CompanyID = p.CompanyID
                JOIN ProductVariant v ON i.VariantID = v.VariantID
                WHERE t.CompanyID = @cid {whClause} {catClause}
                AND t.TransactionDate >= DATEADD(MONTH, -12, GETDATE())
                ORDER BY t.TransactionDate DESC";

            var transactions = new List<(int vId, int change, DateTime date)>();
            using (var cmd = new SqlCommand(sqlTrans, conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);
                if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);
                
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    int vId = rdr["VariantID"] != DBNull.Value ? (int)rdr["VariantID"] : 0;
                    int change = rdr["ChangeQuantity"] != DBNull.Value ? (int)rdr["ChangeQuantity"] : 0;
                    DateTime date = rdr["TransactionDate"] != DBNull.Value ? (DateTime)rdr["TransactionDate"] : DateTime.Now;
                    transactions.Add((vId, change, date));
                }
            }

            // 3. Backtrack for last 12 months
            var now = DateTime.Now;
            for (int i = 0; i < 12; i++)
            {
                var monthDate = now.AddMonths(-i);
                var monthLabel = monthDate.ToString("MMM yyyy");
                
                // Valuation at end of this month
                decimal valuation = 0;
                foreach (var variant in currentStock)
                {
                    int vId = variant.Key;
                    int qtyAtEnd = variant.Value.qty;
                    decimal price = variant.Value.price;

                    // Subtract changes that happened AFTER this month
                    var endOfMonth = new DateTime(monthDate.Year, monthDate.Month, DateTime.DaysInMonth(monthDate.Year, monthDate.Month), 23, 59, 59);
                    var futureChanges = transactions.Where(t => t.vId == vId && t.date > endOfMonth).Sum(t => t.change);
                    
                    qtyAtEnd -= futureChanges;
                    if (qtyAtEnd < 0) qtyAtEnd = 0;

                    valuation += qtyAtEnd * price;
                }

                history.Add(new ValuationHistoryPoint { Month = monthLabel, Value = valuation });
            }

            history.Reverse(); // Chronological order
            return history;
        }

        public DashboardViewModel GetDashboardStats(int companyId, int page = 1, int pageSize = 5)
    {
        var vm = new DashboardViewModel
        {
            CurrentPage = page,
            PageSize = pageSize
        };
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)GetConnection();
        conn.Open();

        // 1. Total Inventory Value
        string sqlVal = "SELECT SUM(CAST(i.QuantityOnHand AS DECIMAL(18,2)) * v.Price) FROM Inventory i JOIN ProductVariant v ON i.VariantID = v.VariantID WHERE i.CompanyID = @cid";
        using (var cmd = new SqlCommand(sqlVal, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            var val = cmd.ExecuteScalar();
            vm.TotalInventoryValue = (val != DBNull.Value && val != null) ? (decimal)val : 0m;
        }

        // 2. Low Stock Count
        string sqlLow = "SELECT COUNT(*) FROM Inventory WHERE CompanyID = @cid AND QuantityOnHand < MinimumStockLevel";
        using (var cmd = new SqlCommand(sqlLow, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            vm.LowStockItemsCount = (int)cmd.ExecuteScalar();
        }

        // 3. Active Sales Orders
        string sqlSales = "SELECT COUNT(*) FROM SalesOrder WHERE CompanyID = @cid AND Status != 'Cancelled'";
        using (var cmd = new SqlCommand(sqlSales, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            vm.ActiveSalesOrdersCount = (int)cmd.ExecuteScalar();
        }

        // 4. Pending Adjustments Count
        string sqlAdj = "SELECT COUNT(*) FROM StockAdjustment WHERE CompanyID = @cid AND Status = 'Pending'";
        using (var cmd = new SqlCommand(sqlAdj, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            vm.TotalItems = (int)cmd.ExecuteScalar();
            vm.PendingAdjustmentsCount = vm.TotalItems;
            vm.TotalPages = (int)Math.Ceiling((double)vm.TotalItems / pageSize);
        }

        // 5. Recent Movements
        vm.RecentMovements = GetRecentMovements(companyId, 5);

        // 6. Valuation Trend (Last 6 months for the dashboard line chart)
        vm.ValuationTrend = GetValuationHistory(companyId).TakeLast(6).ToList();

        // 7. Recent Activity (Audit Log)
        vm.RecentActivity = new List<AuditLog>();
        string sqlAudit = @"
            SELECT TOP 5 l.LogID, l.UserID, l.Action, l.Module, l.Timestamp, u.FullName as UserFullName
            FROM AuditLogs l
            LEFT JOIN Users u ON l.UserID = u.UserID
            WHERE l.CompanyID = @cid
            ORDER BY l.Timestamp DESC";
        using (var cmd = new SqlCommand(sqlAudit, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                vm.RecentActivity.Add(new AuditLog
                {
                    LogID = (int)rdr["LogID"],
                    UserID = rdr["UserID"] == DBNull.Value ? null : (int)rdr["UserID"],
                    UserFullName = rdr["UserFullName"] == DBNull.Value ? "System" : rdr["UserFullName"].ToString(),
                    Module = rdr["Module"].ToString() ?? "System",
                    Action = rdr["Action"].ToString() ?? "",
                    Timestamp = (DateTime)rdr["Timestamp"]
                });
            }
        }

        // 8. Pending Adjustments for Approval (PAGED)
        vm.PendingAdjustments = new List<StockAdjustment>();
        string sqlPending = @"
            SELECT a.*, p.ProductName, v.Color, v.Size, v.Price as UnitCost, u.FullName as RequestedByName
            FROM StockAdjustment a
            JOIN ProductVariant v ON a.VariantID = v.VariantID
            JOIN Product p ON v.ProductID = p.ProductID
            JOIN Users u ON a.RequestedBy = u.UserID
            WHERE a.CompanyID = @cid AND a.Status = 'Pending'
            ORDER BY a.DateRequested DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";
        using (var cmd = new SqlCommand(sqlPending, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                vm.PendingAdjustments.Add(new StockAdjustment
                {
                    AdjustmentID = (int)rdr["AdjustmentID"],
                    RequestedBy = (int)rdr["RequestedBy"],
                    RequestedByName = rdr["RequestedByName"].ToString() ?? "",
                    Type = rdr["Type"].ToString() ?? "",
                    ProductName = rdr["ProductName"].ToString() ?? "",
                    VariantInfo = $"{rdr["Color"]} / {rdr["Size"]}",
                    ChangeQuantity = (int)rdr["ChangeQuantity"],
                    DateRequested = (DateTime)rdr["DateRequested"]
                });
            }
        }

        return vm;
    }
}
}
