﻿using Microsoft.Data.Sqlite;
using Models;

namespace db;

public class UsersCRUD
{
    private static readonly string connectionString = "Data Source=database.db";
    public List<User> GetAll()
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM Users";

            List<User> users = new List<User>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new User
                        (
                            reader["GovernmentID"].ToString()!,
                            reader["Password"].ToString()!,
                            reader["Role"].ToString()!
                        ));
                }
            }
            connection.Close();
            return users;
        }
    }
    public List<UserResponse> GetAllByRole(string role)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM Users WHERE Role = @role";
            cmd.Parameters.AddWithValue("@role", role);

            List<UserResponse> users = new List<UserResponse>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new UserResponse
                        (
                            reader["GovernmentID"].ToString()!,
                            reader["Password"].ToString()!
                        ));
                }
            }
            connection.Close();
            return users;
        }
    }
    public bool IsValid(string governmentID, string password)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT Password FROM Users WHERE GovernmentID = @governmentID";
            command.Parameters.AddWithValue("@governmentID", governmentID);
            string passwordFromDb = String.Empty;
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    passwordFromDb = reader["Password"].ToString()!;
                }
            }
            connection.Close();
            return password == passwordFromDb;
        }
    }
    public bool IsValidAdmin(string governmentID, string password)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT Password FROM Users WHERE Role = 'Admin' AND GovernmentID = @governmentID";
            command.Parameters.AddWithValue("@governmentID", governmentID);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    string? passwordFromDb = reader["Password"].ToString();
                    return password == passwordFromDb;
                }
            }
        }
        return false;
    }
    public User? GetByGovernmentID(string governmentID)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            User? user = null;
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM Users WHERE GovernmentID = @governmentID";
            command.Parameters.AddWithValue("@governmentID", governmentID);

            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                user = new User(
                    reader["GovernmentID"].ToString()!
                    ,reader["Password"].ToString()!
                    ,reader["Role"].ToString()!
                    );
            }
            connection.Close();
            return user;
        }
    }
    public User? GetByGovernmentIDWithRole(string governmentID, string role)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            User? user = null;
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM Users WHERE Role = @role AND GovernmentID = @governmentID";
            command.Parameters.AddWithValue("@governmentID", governmentID);
            command.Parameters.AddWithValue("@role", role);

            var reader = command.ExecuteReader();

            if (reader.Read())
            {
                user = new User(
                    reader["GovernmentID"].ToString()!
                    , reader["Password"].ToString()!
                    , reader["Role"].ToString()!
                    );
            }
            connection.Close();
            return user;
        }
    }
    public User? Add(User user)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO Users (GovernmentID, Password, Role)
            VALUES (@GovernmentID, @Password, @role)
            ";
            cmd.Parameters.AddWithValue("@GovernmentID", user.GovernmentID);
            cmd.Parameters.AddWithValue("@Password", user.Password);
            cmd.Parameters.AddWithValue("@role", user.Role);

            cmd.ExecuteNonQuery();

            connection.Close();
        }
        return GetByGovernmentID(user.GovernmentID);
    }
    public User? ChangePassword(User user)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @$"
            UPDATE Users
            SET Password = @password
            WHERE GovernmentID = @governmentID
            ";
            command.Parameters.AddWithValue("@governmentID", user.GovernmentID);
            command.Parameters.AddWithValue("@password", user.Password);

            command.ExecuteNonQuery();

            connection.Close();
        }
        return GetByGovernmentID(user.GovernmentID);
    }
    public void Delete(string governmentID)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @$"
            DELETE FROM Users WHERE GovernmentID = @governmentID
            ";
            command.Parameters.AddWithValue("@governmentID", governmentID);
            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}
