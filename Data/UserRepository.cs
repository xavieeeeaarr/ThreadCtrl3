using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System;
using ThrdCtrl2.Models;
using Microsoft.Extensions.Configuration;

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

        private DbConnection GetConnection()
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
            cmd.CommandText = @"SELECT u.UserID, u.RoleID, u.FullName, u.CompanyID, c.CompanyName, u.Email, u.Password, u.Status 
                                FROM dbo.Users u 
                                LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID 
                                WHERE u.UserID = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new User
                {
                    UserID = rdr.GetInt32(0),
                    RoleID = rdr.GetInt32(1),
                    FullName = rdr.GetString(2),
                    CompanyID = rdr.IsDBNull(3) ? null : (int?)rdr.GetInt32(3),
                    CompanyName = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    Email = rdr.GetString(5),
                    Password = rdr.GetString(6),
                    Status = rdr.GetString(7)
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
                using var cmdMsg = conn.CreateCommand();
                cmdMsg.CommandText = "UPDATE dbo.Companies SET UserCount = UserCount + 1 WHERE CompanyID = @cid";
                var pCid = cmdMsg.CreateParameter(); pCid.ParameterName = "@cid"; pCid.Value = user.CompanyID.Value; cmdMsg.Parameters.Add(pCid);
                cmdMsg.ExecuteNonQuery();
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
            cmd.CommandText = @"SELECT u.UserID, u.RoleID, u.FullName, u.CompanyID, c.CompanyName, u.Email, u.Password, u.Status, r.RoleName
                                FROM dbo.Users u
                                LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                                LEFT JOIN dbo.Companies c ON u.CompanyID = c.CompanyID
                                WHERE u.Email = @email";
            var pEmail = cmd.CreateParameter(); pEmail.ParameterName = "@email"; pEmail.Value = email; cmd.Parameters.Add(pEmail);
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            if (rdr.Read())
            {
                return new User
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
                };
            }
            return null;
        }

        public int CreateCompany(string companyName)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.Companies (CompanyName, Status, UserCount)
                                VALUES (@CompanyName, 'Active', 0);
                                SELECT SCOPE_IDENTITY();";
            var pComp = cmd.CreateParameter(); pComp.ParameterName = "@CompanyName"; pComp.Value = companyName; cmd.Parameters.Add(pComp);
            conn.Open();
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
            string query = "SELECT CompanyID, CompanyName, Status, UserCount FROM dbo.Companies";
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " WHERE Status = @status";
                var pStat = cmd.CreateParameter(); pStat.ParameterName = "@status"; pStat.Value = status; cmd.Parameters.Add(pStat);
            }
            query += " ORDER BY CompanyName";
            cmd.CommandText = query;
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new Company
                {
                    CompanyID = rdr.GetInt32(0),
                    CompanyName = rdr.GetString(1),
                    Status = rdr.GetString(2),
                    UserCount = rdr.GetInt32(3)
                });
            }
            return list;
        }

        public void ArchiveCompany(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE dbo.Companies SET Status = 'Inactive' WHERE CompanyID = @id";
            var pId = cmd.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmd.Parameters.Add(pId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public SuperAdminDashboardViewModel GetSuperAdminDashboardStats()
        {
            var vm = new SuperAdminDashboardViewModel();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies";
            vm.TotalCompanies = (int)(cmd.ExecuteScalar() ?? 0);

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE Status = 'Active'";
            vm.ActiveCompanies = (int)(cmd.ExecuteScalar() ?? 0);

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Companies WHERE Status = 'Inactive'";
            vm.InactiveCompanies = (int)(cmd.ExecuteScalar() ?? 0);

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Users";
            vm.TotalUsers = (int)(cmd.ExecuteScalar() ?? 0);

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
    }
}
