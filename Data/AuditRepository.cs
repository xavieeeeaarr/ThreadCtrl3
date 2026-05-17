using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ThrdCtrl2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ThrdCtrl2.Data
{
    public class AuditRepository
    {
        private readonly string _connectionString;

        public AuditRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public void Log(AuditLog log)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.AuditLogs (CompanyID, UserID, Action, Module, Details, IPAddress, Timestamp)
                                VALUES (@CompanyID, @UserID, @Action, @Module, @Details, @IPAddress, @Timestamp)";
            
            cmd.Parameters.AddWithValue("@CompanyID", (object?)log.CompanyID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserID", (object?)log.UserID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Action", (object?)log.Action ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Module", (object?)log.Module ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Details", (object?)log.Details ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IPAddress", (object?)log.IPAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp", log.Timestamp == default ? DateTime.Now : log.Timestamp);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<AuditLog> GetLogs(int companyId, int? userId = null, string? module = null, string? roleName = null, string? actionName = null, int limit = 100)
        {
            var list = new List<AuditLog>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string query = @"SELECT TOP (@Limit) a.LogID, a.CompanyID, a.UserID, u.FullName as UserFullName, r.RoleName,
                                    a.Action, a.Module, a.Details, a.IPAddress, a.Timestamp, c.CompanyName
                             FROM dbo.AuditLogs a
                             LEFT JOIN dbo.Users u ON a.UserID = u.UserID
                             LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                             LEFT JOIN dbo.Companies c ON a.CompanyID = c.CompanyID
                             WHERE a.CompanyID = @CompanyID";
            
            if (userId.HasValue)
            {
                query += " AND a.UserID = @UserID";
                cmd.Parameters.AddWithValue("@UserID", userId.Value);
            }
            if (!string.IsNullOrEmpty(module) && module != "All Modules")
            {
                query += " AND a.Module = @Module";
                cmd.Parameters.AddWithValue("@Module", module);
            }
            if (!string.IsNullOrEmpty(roleName) && roleName != "All Roles")
            {
                query += " AND r.RoleName = @RoleName";
                cmd.Parameters.AddWithValue("@RoleName", roleName);
            }
            if (!string.IsNullOrEmpty(actionName) && actionName != "All Actions")
            {
                query += " AND a.Action LIKE @ActionName";
                cmd.Parameters.AddWithValue("@ActionName", "%" + actionName + "%");
            }

            query += " ORDER BY a.Timestamp DESC";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("@CompanyID", companyId);
            cmd.Parameters.AddWithValue("@Limit", limit);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new AuditLog
                {
                    LogID = rdr.GetInt32(0),
                    CompanyID = rdr.IsDBNull(1) ? null : (int?)rdr.GetInt32(1),
                    UserID = rdr.IsDBNull(2) ? null : (int?)rdr.GetInt32(2),
                    UserFullName = rdr.IsDBNull(3) ? "System" : rdr.GetString(3),
                    RoleName = rdr.IsDBNull(4) ? "System" : rdr.GetString(4),
                    Action = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    Module = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                    Details = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                    IPAddress = rdr.IsDBNull(8) ? null : rdr.GetString(8),
                    Timestamp = rdr.GetDateTime(9),
                    CompanyName = rdr.IsDBNull(10) ? "N/A" : rdr.GetString(10)
                });
            }
            return list;
        }

        public List<AuditLog> GetAllSystemLogs(int? companyId = null, string? roleName = null, int limit = 1000)
        {
            var list = new List<AuditLog>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string query = @"SELECT TOP (@Limit) a.LogID, a.CompanyID, a.UserID, u.FullName as UserFullName, r.RoleName,
                                    a.Action, a.Module, a.Details, a.IPAddress, a.Timestamp, c.CompanyName
                             FROM dbo.AuditLogs a
                             LEFT JOIN dbo.Users u ON a.UserID = u.UserID
                             LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
                             LEFT JOIN dbo.Companies c ON a.CompanyID = c.CompanyID
                             WHERE 1=1";
            
            if (companyId.HasValue)
            {
                query += " AND a.CompanyID = @CompanyID";
                cmd.Parameters.AddWithValue("@CompanyID", companyId.Value);
            }
            if (!string.IsNullOrEmpty(roleName) && roleName != "All Roles")
            {
                query += " AND r.RoleName = @RoleName";
                cmd.Parameters.AddWithValue("@RoleName", roleName);
            }

            query += " ORDER BY a.Timestamp DESC";
            cmd.CommandText = query;
            cmd.Parameters.AddWithValue("@Limit", limit);

            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                list.Add(new AuditLog
                {
                    LogID = rdr.GetInt32(0),
                    CompanyID = rdr.IsDBNull(1) ? null : (int?)rdr.GetInt32(1),
                    UserID = rdr.IsDBNull(2) ? null : (int?)rdr.GetInt32(2),
                    UserFullName = rdr.IsDBNull(3) ? "System" : rdr.GetString(3),
                    RoleName = rdr.IsDBNull(4) ? "System" : rdr.GetString(4),
                    Action = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    Module = rdr.IsDBNull(6) ? null : rdr.GetString(6),
                    Details = rdr.IsDBNull(7) ? null : rdr.GetString(7),
                    IPAddress = rdr.IsDBNull(8) ? null : rdr.GetString(8),
                    Timestamp = rdr.GetDateTime(9),
                    CompanyName = rdr.IsDBNull(10) ? "N/A" : rdr.GetString(10)
                });
            }
            return list;
        }

        public SecurityStats GetSecurityStats(int companyId)
        {
            var stats = new SecurityStats();
            using var conn = GetConnection();
            conn.Open();

            // Failed logins last 24h
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.AuditLogs WHERE CompanyID = @cid AND Action = 'Failed Login' AND Timestamp >= DATEADD(day, -1, GETDATE())";
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.FailedLogins24h = (int)cmd.ExecuteScalar();
            }

            // Active sessions
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.UserSessions s JOIN dbo.Users u ON s.UserID = u.UserID WHERE u.CompanyID = @cid AND s.LogoutTime IS NULL";
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.ActiveSessions = (int)cmd.ExecuteScalar();
            }

            // Data modifications last 24h
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.AuditLogs WHERE CompanyID = @cid AND Module <> 'Auth' AND Timestamp >= DATEADD(day, -1, GETDATE())";
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.DataModifications24h = (int)cmd.ExecuteScalar();
            }

            // Security alerts (e.g. critical events)
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.AuditLogs WHERE CompanyID = @cid AND Action LIKE '%Critical%' AND Timestamp >= DATEADD(day, -7, GETDATE())";
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.SecurityAlerts = (int)cmd.ExecuteScalar();
            }

            return stats;
        }
    }

    public class SecurityStats
    {
        public int FailedLogins24h { get; set; }
        public int ActiveSessions { get; set; }
        public int DataModifications24h { get; set; }
        public int SecurityAlerts { get; set; }
    }
}
