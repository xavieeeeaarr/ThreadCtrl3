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

        public List<User> GetAllUsers(string? status = null)
        {
            var list = new List<User>();
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            
            string query = @"SELECT u.UserID, u.RoleID, u.FullName, u.Email, u.Password, u.Status, r.RoleName
                             FROM dbo.Users u
                             LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID";
            
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " WHERE u.Status = @status";
                var pStat = cmd.CreateParameter();
                pStat.ParameterName = "@status";
                pStat.Value = status;
                cmd.Parameters.Add(pStat);
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
                    Email = rdr.GetString(3),
                    Password = rdr.GetString(4),
                    Status = rdr.GetString(5),
                    RoleName = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                });
            }
            return list;
        }

        public User? GetUserById(int id)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT UserID, RoleID, FullName, Email, Password, Status FROM dbo.Users WHERE UserID = @id";
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
                    Email = rdr.GetString(3),
                    Password = rdr.GetString(4),
                    Status = rdr.GetString(5)
                };
            }
            return null;
        }

        public int CreateUser(User user)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.Users (RoleID, FullName, Email, Password, Status)
                                VALUES (@RoleID, @FullName, @Email, @Password, @Status);
                                SELECT SCOPE_IDENTITY();";
            var pRole = cmd.CreateParameter(); pRole.ParameterName = "@RoleID"; pRole.Value = user.RoleID; cmd.Parameters.Add(pRole);
            var pFull = cmd.CreateParameter(); pFull.ParameterName = "@FullName"; pFull.Value = user.FullName; cmd.Parameters.Add(pFull);
            var pEmail = cmd.CreateParameter(); pEmail.ParameterName = "@Email"; pEmail.Value = user.Email; cmd.Parameters.Add(pEmail);
            var pPass = cmd.CreateParameter(); pPass.ParameterName = "@Password"; pPass.Value = user.Password; cmd.Parameters.Add(pPass);
            var pStat = cmd.CreateParameter(); pStat.ParameterName = "@Status"; pStat.Value = user.Status; cmd.Parameters.Add(pStat);
            conn.Open();
            var id = cmd.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        public void UpdateUser(User user)
        {
            using var conn = GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE dbo.Users SET RoleID = @RoleID, FullName = @FullName, Email = @Email, Password = @Password, Status = @Status
                                WHERE UserID = @UserID";
            var pRole = cmd.CreateParameter(); pRole.ParameterName = "@RoleID"; pRole.Value = user.RoleID; cmd.Parameters.Add(pRole);
            var pFull = cmd.CreateParameter(); pFull.ParameterName = "@FullName"; pFull.Value = user.FullName; cmd.Parameters.Add(pFull);
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
            cmd.CommandText = @"SELECT u.UserID, u.RoleID, u.FullName, u.Email, u.Password, u.Status, r.RoleName
                                FROM dbo.Users u
                                LEFT JOIN dbo.Roles r ON u.RoleID = r.RoleID
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
                    Email = rdr.GetString(3),
                    Password = rdr.GetString(4),
                    Status = rdr.GetString(5),
                    RoleName = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                };
            }
            return null;
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
