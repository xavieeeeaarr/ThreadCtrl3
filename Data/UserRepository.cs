using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System;
using ThrdCtrl2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ThrdCtrl2.Data
{
    public class UserRepository
    {
        private readonly string _connectionString;
        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");
        }

        private Microsoft.Data.SqlClient.SqlConnection GetConnection()
        {
            return new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        }

        public List<Role> GetRoles()
        {
            var list = new List<Role>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT RoleID, RoleName, Description FROM dbo.Roles ORDER BY RoleName";
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Role
                {
                    RoleID = rdr.GetInt32(0),
                    RoleName = rdr.GetString(1),
                    Description = rdr.IsDBNull(2) ? null : rdr.GetString(2)
                });
            }
            return list;
        }

        public List<User> GetAllUsers(string? status = null, int? companyId = null)
        {
            var list = new List<User>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string query = @"SELECT u.UserID, u.RoleID, u.FullName, u.CompanyID, c.CompanyName, u.Email, u.Password, u.Status, r.RoleName
                             FROM dbo.Users u
                             LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                             LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID
                             WHERE 1=1";
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " AND u.Status = @status";
                var pStat = cmd.CreateParameter();
                pStat.ParameterName = "@status";
                pStat.Value = status;
                cmd.Parameters.Add(pStat);
            }

            if (companyId.HasValue)
            {
                query += " AND u.CompanyID = @companyId";
                var pCid = cmd.CreateParameter();
                pCid.ParameterName = "@companyId";
                pCid.Value = companyId.Value;
                cmd.Parameters.Add(pCid);
            }
            
            query += " ORDER BY u.FullName";
            cmd.CommandText = query;

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new User
                {
                    UserID = rdr.GetInt32(0),
                    RoleID = rdr.GetInt32(1),
                    FullName = rdr.GetString(2),
                    CompanyID = rdr.IsDBNull(3) ? null : (int?)rdr.GetInt32(3),
                    CompanyName = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    Email = rdr.GetString(5),
                    Password = rdr.GetString(6),
                    Status = rdr.GetString(7),
                    RoleName = rdr.IsDBNull(8) ? null : rdr.GetString(8)
                });
            }
            return list;
        }

        public User? GetUserById(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT u.*, r.RoleName, c.CompanyName, c.Status as CompanyStatus
                                FROM dbo.Users u 
                                LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                                LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID 
                                WHERE u.UserID = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new User
                {
                    UserID = rdr.GetInt32(rdr.GetOrdinal("UserID")),
                    RoleID = rdr.GetInt32(rdr.GetOrdinal("RoleID")),
                    FullName = rdr.GetString(rdr.GetOrdinal("FullName")),
                    CompanyID = rdr.IsDBNull(rdr.GetOrdinal("CompanyID")) ? null : (int?)rdr.GetInt32(rdr.GetOrdinal("CompanyID")),
                    CompanyName = rdr.IsDBNull(rdr.GetOrdinal("CompanyName")) ? null : rdr.GetString(rdr.GetOrdinal("CompanyName")),
                    Email = rdr.GetString(rdr.GetOrdinal("Email")),
                    Password = rdr.GetString(rdr.GetOrdinal("Password")),
                    Status = rdr.GetString(rdr.GetOrdinal("Status")),
                    RoleName = rdr.IsDBNull(rdr.GetOrdinal("RoleName")) ? null : rdr.GetString(rdr.GetOrdinal("RoleName")),
                    CompanyStatus = rdr.IsDBNull(rdr.GetOrdinal("CompanyStatus")) ? null : rdr.GetString(rdr.GetOrdinal("CompanyStatus")),
                    AccessFailedCount = rdr.IsDBNull(rdr.GetOrdinal("AccessFailedCount")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("AccessFailedCount")),
                    LockoutEnd = rdr.IsDBNull(rdr.GetOrdinal("LockoutEnd")) ? null : (DateTime?)rdr.GetDateTime(rdr.GetOrdinal("LockoutEnd"))
                };
            }
            return null;
        }

        public int CreateUser(User user)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.Users (RoleID, FullName, CompanyID, Email, Password, Status)
                                VALUES (@RoleID, @FullName, @CompanyID, @Email, @Password, @Status);
                                SELECT SCOPE_IDENTITY();";
            var pRole = cmd.CreateParameter(); pRole.ParameterName = "@RoleID"; pRole.Value = user.RoleID; cmd.Parameters.Add(pRole);
            var pFull = cmd.CreateParameter(); pFull.ParameterName = "@FullName"; pFull.Value = user.FullName; cmd.Parameters.Add(pFull);
            var pComp = cmd.CreateParameter(); pComp.ParameterName = "@CompanyID"; pComp.Value = (object?)user.CompanyID ?? DBNull.Value; cmd.Parameters.Add(pComp);
            var pEmail = cmd.CreateParameter(); pEmail.ParameterName = "@Email"; pEmail.Value = user.Email; cmd.Parameters.Add(pEmail);
            var pPass = cmd.CreateParameter(); pPass.ParameterName = "@Password"; pPass.Value = user.Password; cmd.Parameters.Add(pPass);
            var pStat = cmd.CreateParameter(); pStat.ParameterName = "@Status"; pStat.Value = user.Status; cmd.Parameters.Add(pStat);
            conn.Open();
            var id = cmd.ExecuteScalar();
            
            // Increment UserCount in Companies table
            if (user.CompanyID.HasValue)
            {
                using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies') AND name = 'UserCount'";
                if ((int)(checkCmd.ExecuteScalar() ?? 0) > 0)
                {
                    using var cmdMsg = conn.CreateCommand();
                    cmdMsg.CommandText = "UPDATE dbo.Companies SET UserCount = UserCount + 1 WHERE CompanyID = @cid";
                    var pCid = cmdMsg.CreateParameter(); pCid.ParameterName = "@cid"; pCid.Value = user.CompanyID.Value; cmdMsg.Parameters.Add(pCid);
                    cmdMsg.ExecuteNonQuery();
                }
            }

            return Convert.ToInt32(id);
        }

        public void UpdateUser(User user)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE dbo.Users SET RoleID = @RoleID, FullName = @FullName, CompanyID = @CompanyID, Email = @Email, Password = @Password, Status = @Status
                                WHERE UserID = @UserID";
            var pRole = cmd.CreateParameter(); pRole.ParameterName = "@RoleID"; pRole.Value = user.RoleID; cmd.Parameters.Add(pRole);
            var pFull = cmd.CreateParameter(); pFull.ParameterName = "@FullName"; pFull.Value = user.FullName; cmd.Parameters.Add(pFull);
            var pComp = cmd.CreateParameter(); pComp.ParameterName = "@CompanyID"; pComp.Value = (object?)user.CompanyID ?? DBNull.Value; cmd.Parameters.Add(pComp);
            var pEmail = cmd.CreateParameter(); pEmail.ParameterName = "@Email"; pEmail.Value = user.Email; cmd.Parameters.Add(pEmail);
            var pPass = cmd.CreateParameter(); pPass.ParameterName = "@Password"; pPass.Value = user.Password; cmd.Parameters.Add(pPass);
            var pStat = cmd.CreateParameter(); pStat.ParameterName = "@Status"; pStat.Value = user.Status; cmd.Parameters.Add(pStat);
            var pId = cmd.CreateParameter(); pId.ParameterName = "@UserID"; pId.Value = user.UserID; cmd.Parameters.Add(pId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public User? GetUserByEmail(string email)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT u.*, r.RoleName, c.CompanyName, c.Status as CompanyStatus
                                FROM dbo.Users u
                                LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                                LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID
                                WHERE u.Email = @email";
            var pEmail = cmd.CreateParameter(); pEmail.ParameterName = "@email"; pEmail.Value = email; cmd.Parameters.Add(pEmail);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                var user = new User
                {
                    UserID = rdr.GetInt32(rdr.GetOrdinal("UserID")),
                    RoleID = rdr.GetInt32(rdr.GetOrdinal("RoleID")),
                    FullName = rdr.GetString(rdr.GetOrdinal("FullName")),
                    CompanyID = rdr.IsDBNull(rdr.GetOrdinal("CompanyID")) ? null : (int?)rdr.GetInt32(rdr.GetOrdinal("CompanyID")),
                    CompanyName = rdr.IsDBNull(rdr.GetOrdinal("CompanyName")) ? null : rdr.GetString(rdr.GetOrdinal("CompanyName")),
                    Email = rdr.GetString(rdr.GetOrdinal("Email")),
                    Password = rdr.GetString(rdr.GetOrdinal("Password")),
                    Status = rdr.GetString(rdr.GetOrdinal("Status")),
                    RoleName = rdr.IsDBNull(rdr.GetOrdinal("RoleName")) ? null : rdr.GetString(rdr.GetOrdinal("RoleName")),
                    AccessFailedCount = rdr.IsDBNull(rdr.GetOrdinal("AccessFailedCount")) ? 0 : rdr.GetInt32(rdr.GetOrdinal("AccessFailedCount")),
                    LockoutEnd = rdr.IsDBNull(rdr.GetOrdinal("LockoutEnd")) ? null : (DateTime?)rdr.GetDateTime(rdr.GetOrdinal("LockoutEnd"))
                };

                try {
                    user.CompanyStatus = rdr.IsDBNull(rdr.GetOrdinal("CompanyStatus")) ? null : rdr.GetString(rdr.GetOrdinal("CompanyStatus"));
                } catch {
                    user.CompanyStatus = "Active"; // Fallback if column missing
                }
                
                return user;
            }
            return null;
        }

        public int CreateCompany(string companyName, string subscriptionType)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            conn.Open();

            // Detect available columns
            using var schemaCmd = conn.CreateCommand();
            schemaCmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies')";
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var rdr = schemaCmd.ExecuteReader()) {
                while (rdr.Read()) columns.Add(rdr.GetString(0));
            }

            var cols = new List<string> { "CompanyName" };
            var vals = new List<string> { "@CompanyName" };
            
            if (columns.Contains("Status")) { cols.Add("Status"); vals.Add("'Pending'"); }
            if (columns.Contains("UserCount")) { cols.Add("UserCount"); vals.Add("0"); }
            if (columns.Contains("SubscriptionType")) { cols.Add("SubscriptionType"); vals.Add("@SubscriptionType"); }

            cmd.CommandText = $"INSERT INTO dbo.Companies ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)}); SELECT SCOPE_IDENTITY();";
            
            var pComp = cmd.CreateParameter(); pComp.ParameterName = "@CompanyName"; pComp.Value = companyName; cmd.Parameters.Add(pComp);
            if (columns.Contains("SubscriptionType")) {
                var pSub = cmd.CreateParameter(); pSub.ParameterName = "@SubscriptionType"; pSub.Value = subscriptionType; cmd.Parameters.Add(pSub);
            }

            var id = cmd.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        public string? GetCompanyAdminEmail(int companyId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TOP 1 Email FROM dbo.Users 
                                WHERE CompanyID = @cid AND RoleID = (SELECT RoleID FROM Roles WHERE RoleName = 'Company Admin')";
            var pCid = cmd.CreateParameter(); pCid.ParameterName = "@cid"; pCid.Value = companyId; cmd.Parameters.Add(pCid);
            conn.Open();
            return cmd.ExecuteScalar()?.ToString();
        }

        public List<Company> GetCompanies(string? status = null)
        {
            var list = new List<Company>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            string query = "SELECT * FROM dbo.Companies";
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = "SELECT * FROM dbo.Companies WHERE Status = @status";
                var pStat = cmd.CreateParameter(); pStat.ParameterName = "@status"; pStat.Value = status; cmd.Parameters.Add(pStat);
            }
            query += " ORDER BY CompanyName";
            cmd.CommandText = query;
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            var hasSubCol = false;
            try { rdr.GetOrdinal("SubscriptionType"); hasSubCol = true; } catch { }
            var hasStatusCol = false;
            try { rdr.GetOrdinal("Status"); hasStatusCol = true; } catch { }

            while (rdr.Read())
            {
                list.Add(new Company
                {
                    CompanyID = rdr.GetInt32(rdr.GetOrdinal("CompanyID")),
                    CompanyName = rdr.GetString(rdr.GetOrdinal("CompanyName")),
                    Status = hasStatusCol ? rdr.GetString(rdr.GetOrdinal("Status")) : "Active",
                    UserCount = rdr.GetInt32(rdr.GetOrdinal("UserCount")),
                    SubscriptionType = (hasSubCol && !rdr.IsDBNull(rdr.GetOrdinal("SubscriptionType"))) 
                                       ? rdr.GetString(rdr.GetOrdinal("SubscriptionType")) 
                                       : "Monthly"
                });
            }
            return list;
        }

        public void UpdateCompanyStatus(int id, string status)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Companies SET Status = @status WHERE CompanyID = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            var pStatus = cmd.CreateParameter(); pStatus.ParameterName = "@status"; pStatus.Value = status; cmd.Parameters.Add(pStatus);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            cmd.Transaction = transaction;
            try {
                // Update Company
                cmd.CommandText = "UPDATE dbo.Companies SET Status = @status WHERE CompanyID = @id";
                cmd.ExecuteNonQuery();

                // Update all Users of that company
                using var userCmd = (Microsoft.Data.SqlClient.SqlCommand)conn.CreateCommand();
                userCmd.Transaction = (Microsoft.Data.SqlClient.SqlTransaction)transaction;
                userCmd.CommandText = "UPDATE dbo.Users SET Status = @status WHERE CompanyID = @id";
                userCmd.Parameters.AddWithValue("@id", id);
                userCmd.Parameters.AddWithValue("@status", status);
                userCmd.ExecuteNonQuery();

                transaction.Commit();
            } catch {
                transaction.Rollback();
                throw;
            }
        }

        public SuperAdminDashboardViewModel GetSuperAdminDashboardStats()
        {
            var vm = new SuperAdminDashboardViewModel();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies";
            vm.TotalCompanies = (int)(cmd.ExecuteScalar() ?? 0);

            // Detection
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var schemaCmd = conn.CreateCommand()) {
                schemaCmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Companies')";
                using (var rdr = schemaCmd.ExecuteReader()) {
                    while (rdr.Read()) columns.Add(rdr.GetString(0));
                }
            }

            bool hasStatus = columns.Contains("Status");
            bool hasSub = columns.Contains("SubscriptionType");

            if (hasStatus) {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE Status = 'Active'";
                vm.ActiveCompanies = (int)(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE Status = 'Inactive'";
                vm.InactiveCompanies = (int)(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE Status = 'Pending'";
                vm.PendingCompanies = (int)(cmd.ExecuteScalar() ?? 0);
            } else {
                // Fallback: All companies are active if status column missing
                vm.ActiveCompanies = vm.TotalCompanies;
                vm.PendingCompanies = 0;
                vm.InactiveCompanies = 0;
            }

            if (hasSub) {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE SubscriptionType = 'Monthly'";
                vm.MonthlyCount = (int)(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE SubscriptionType = 'Yearly'";
                vm.YearlyCount = (int)(cmd.ExecuteScalar() ?? 0);

                // Revenue calculation
                string statusFilter = hasStatus ? "WHERE Status = 'Active'" : "";
                
                cmd.CommandText = $"SELECT COUNT(*) FROM dbo.Companies {statusFilter} AND SubscriptionType = 'Monthly'";
                int activeMonthly = (int)(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = $"SELECT COUNT(*) FROM dbo.Companies {statusFilter} AND SubscriptionType = 'Yearly'";
                int activeYearly = (int)(cmd.ExecuteScalar() ?? 0);

                vm.TotalRevenue = (activeMonthly * 20000m) + (activeYearly * 500000m);
            } else {
                // Fallback: All are monthly if sub column missing
                vm.MonthlyCount = vm.TotalCompanies;
                vm.YearlyCount = 0;
                vm.TotalRevenue = vm.ActiveCompanies * 20000m;
            }

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Users";
            vm.TotalUsers = (int)(cmd.ExecuteScalar() ?? 0);
            
            // Fetch Recent Companies
            vm.RecentCompanies = new List<Company>();
            cmd.CommandText = hasStatus 
                ? "SELECT TOP 5 CompanyID, CompanyName, Status FROM dbo.Companies ORDER BY CompanyID DESC"
                : "SELECT TOP 5 CompanyID, CompanyName, 'Active' as Status FROM dbo.Companies ORDER BY CompanyID DESC";
            
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    vm.RecentCompanies.Add(new Company { 
                        CompanyID = rdr.GetInt32(0),
                        CompanyName = rdr.GetString(1),
                        Status = rdr.GetString(2)
                    });
                }
            }

            return vm;
        }

        public List<User> GetAllSystemUsers(string? roleName = null)
        {
            var list = new List<User>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string query = @"SELECT u.UserID, u.RoleID, u.FullName, u.CompanyID, c.CompanyName, u.Email, u.Password, u.Status, r.RoleName
                             FROM dbo.Users u
                             LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                             LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID";
            
            if (!string.IsNullOrEmpty(roleName) && roleName != "All")
            {
                query += " WHERE r.RoleName = @role";
                var pRole = cmd.CreateParameter();
                pRole.ParameterName = "@role";
                pRole.Value = roleName;
                cmd.Parameters.Add(pRole);
            }
            
            query += " ORDER BY u.FullName";
            cmd.CommandText = query;

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new User
                {
                    UserID = rdr.GetInt32(0),
                    RoleID = rdr.GetInt32(1),
                    FullName = rdr.GetString(2),
                    CompanyID = rdr.IsDBNull(3) ? null : (int?)rdr.GetInt32(3),
                    CompanyName = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    Email = rdr.GetString(5),
                    Password = rdr.GetString(6),
                    Status = rdr.GetString(7),
                    RoleName = rdr.IsDBNull(8) ? null : rdr.GetString(8)
                });
            }
            return list;
        }

        public void ArchiveUser(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET Status = 'Inactive' WHERE UserID = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void CreateOTP(int userId, string code, string type)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO UserOTPs (UserID, OTPCode, Type, ExpiryTime) VALUES (@uid, @code, @type, @expiry)";
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddMinutes(10));
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public bool VerifyOTP(int userId, string code, string type)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 OTPID FROM UserOTPs WHERE UserID = @uid AND OTPCode = @code AND Type = @type AND IsUsed = 0 AND ExpiryTime > GETDATE() ORDER BY ExpiryTime DESC";
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@type", type);
            conn.Open();
            var result = cmd.ExecuteScalar();
            if (result != null)
            {
                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE UserOTPs SET IsUsed = 1 WHERE OTPID = @id";
                updateCmd.Parameters.AddWithValue("@id", result);
                updateCmd.ExecuteNonQuery();
                return true;
            }
            return false;
        }

        public void CreatePasswordResetToken(int userId, string token)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO PasswordResetTokens (UserID, Token, ExpiryTime) VALUES (@uid, @token, @expiry)";
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddHours(1));
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public int? VerifyPasswordResetToken(string token)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT UserID FROM PasswordResetTokens WHERE Token = @token AND IsUsed = 0 AND ExpiryTime > GETDATE()";
            cmd.Parameters.AddWithValue("@token", token);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result != null ? (int?)result : null;
        }

        public void MarkTokenAsUsed(string token)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE PasswordResetTokens SET IsUsed = 1 WHERE Token = @token";
            cmd.Parameters.AddWithValue("@token", token);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void UpdatePassword(int userId, string hashedPassword)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET Password = @pass WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@pass", hashedPassword);
            cmd.Parameters.AddWithValue("@uid", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void SetUserStatus(int userId, string status)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET Status = @status WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@uid", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void IncrementAccessFailedCount(int userId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET AccessFailedCount = AccessFailedCount + 1 WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@uid", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void ResetAccessFailedCount(int userId)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET AccessFailedCount = 0, LockoutEnd = NULL WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@uid", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void SetLockout(int userId, DateTime lockoutEnd)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Users SET LockoutEnd = @end WHERE UserID = @uid";
            cmd.Parameters.AddWithValue("@end", lockoutEnd);
            cmd.Parameters.AddWithValue("@uid", userId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void MigrateDatabase()
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            conn.Open();

            // Check for Status column in Companies
            if (!ColumnExists(conn, "dbo.Companies", "Status")) {
                cmd.CommandText = "ALTER TABLE dbo.Companies ADD Status NVARCHAR(20) DEFAULT 'Pending' WITH VALUES;";
                cmd.ExecuteNonQuery();
                // Set existing ones to Active
                cmd.CommandText = "UPDATE dbo.Companies SET Status = 'Active'";
                cmd.ExecuteNonQuery();
            }

            // Check for SubscriptionType column in Companies
            if (!ColumnExists(conn, "dbo.Companies", "SubscriptionType")) {
                cmd.CommandText = "ALTER TABLE dbo.Companies ADD SubscriptionType NVARCHAR(50) DEFAULT 'Monthly' WITH VALUES;";
                cmd.ExecuteNonQuery();
            }

            // Check for UserCount column
            if (!ColumnExists(conn, "dbo.Companies", "UserCount")) {
                cmd.CommandText = "ALTER TABLE dbo.Companies ADD UserCount INT DEFAULT 0 WITH VALUES;";
                cmd.ExecuteNonQuery();
            }

            // Check for Lockout columns
            if (!ColumnExists(conn, "dbo.Users", "AccessFailedCount")) {
                cmd.CommandText = "ALTER TABLE dbo.Users ADD AccessFailedCount INT DEFAULT 0 WITH VALUES;";
                cmd.ExecuteNonQuery();
            }
            if (!ColumnExists(conn, "dbo.Users", "LockoutEnd")) {
                cmd.CommandText = "ALTER TABLE dbo.Users ADD LockoutEnd DATETIME NULL;";
                cmd.ExecuteNonQuery();
            }
        }

        private bool ColumnExists(DbConnection conn, string tableName, string columnName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@table) AND name = @col";
            
            var pTable = cmd.CreateParameter();
            pTable.ParameterName = "@table";
            pTable.Value = tableName;
            cmd.Parameters.Add(pTable);

            var pCol = cmd.CreateParameter();
            pCol.ParameterName = "@col";
            pCol.Value = columnName;
            cmd.Parameters.Add(pCol);

            return (int)(cmd.ExecuteScalar() ?? 0) > 0;
        }
    }
}
