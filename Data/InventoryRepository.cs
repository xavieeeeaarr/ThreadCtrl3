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
                INSERT INTO ProductVariant (ProductID, Size, Color, SKU, Barcode)
                OUTPUT INSERTED.VariantID
                VALUES (@pid, @size, @color, @sku, @barcode)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", variant.ProductID);
            cmd.Parameters.AddWithValue("@size", (object)variant.Size ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@color", (object)variant.Color ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sku", (object)variant.SKU ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@barcode", (object)variant.Barcode ?? DBNull.Value);
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

        public List<InventoryItem> GetInventory(int companyId, int? categoryId = null, string? status = null, int? warehouseId = null)
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
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "In Stock") sql += " AND i.QuantityOnHand > i.MinimumStockLevel";
                else if (status == "Low Stock") sql += " AND i.QuantityOnHand <= i.MinimumStockLevel AND i.QuantityOnHand > 0";
                else if (status == "Out of Stock") sql += " AND (i.QuantityOnHand IS NULL OR i.QuantityOnHand <= 0)";
            }
                
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catid", categoryId.Value);
            if (warehouseId.HasValue) cmd.Parameters.AddWithValue("@whid", warehouseId.Value);

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
                    INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate)
                    VALUES (@cid, @iid, @uid, @type, @refid, GETDATE())";
                using var logCmd = new SqlCommand(logSql, conn, trans);
                logCmd.Parameters.AddWithValue("@cid", companyId);
                logCmd.Parameters.AddWithValue("@iid", inventoryId);
                logCmd.Parameters.AddWithValue("@uid", userId);
                logCmd.Parameters.AddWithValue("@type", type);
                logCmd.Parameters.AddWithValue("@refid", (object?)refId ?? DBNull.Value);
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
            string sql = @"
                INSERT INTO Inventory (CompanyID, VariantID, WarehouseID, QuantityOnHand, MinimumStockLevel, LastUpdated)
                VALUES (@cid, @vid, @wid, @qty, @min, GETDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", item.CompanyID);
            cmd.Parameters.AddWithValue("@vid", item.VariantID);
            cmd.Parameters.AddWithValue("@wid", item.WarehouseID);
            cmd.Parameters.AddWithValue("@qty", item.QuantityOnHand);
            cmd.Parameters.AddWithValue("@min", item.MinimumStockLevel);
            conn.Open();
            cmd.ExecuteNonQuery();
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
                string logSql = @"INSERT INTO InventoryTransaction (CompanyID, InventoryID, UserID, TransactionType, ReferenceID, TransactionDate)
                                  SELECT @cid, InventoryID, @uid, 'Transfer', @vid, GETDATE() 
                                  FROM Inventory WHERE VariantID = @vid AND (WarehouseID = @fwh OR WarehouseID = @twh)";
                using var logCmd = new SqlCommand(logSql, conn, trans);
                logCmd.Parameters.AddWithValue("@cid", companyId);
                logCmd.Parameters.AddWithValue("@uid", userId);
                logCmd.Parameters.AddWithValue("@vid", variantId);
                logCmd.Parameters.AddWithValue("@fwh", fromWhId);
                logCmd.Parameters.AddWithValue("@twh", toWhId);
                logCmd.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}
