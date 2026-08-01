using Windows.ApplicationModel;
using Microsoft.UI;
using AllLive.Core.Helper;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Windows.Storage;
using System.IO;
using AllLive.WinUI.Models;

namespace AllLive.WinUI.Helper
{

    public static class DatabaseHelper
    {
        static SqliteConnection db;
        public async static Task InitializeDatabase()
        {
            string folderPath = Utils.GetLocalFolderPath();
            Directory.CreateDirectory(folderPath);
            string dbPath = Path.Combine(folderPath, "alllive.db");
            // 添加 UTF-8 编码支持
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
            db = new SqliteConnection(connectionString);
            db.Open();

            string tableCommand = @"CREATE TABLE IF NOT EXISTS Favorite (
id INTEGER PRIMARY KEY AUTOINCREMENT,
user_name TEXT,
site_name TEXT,
photo TEXT,
room_id TEXT);

CREATE TABLE IF NOT EXISTS History (
id INTEGER PRIMARY KEY AUTOINCREMENT,
user_name TEXT,
site_name TEXT,
photo TEXT,
room_id TEXT,
watch_time DATETIME);
";
            SqliteCommand createTable = new SqliteCommand(tableCommand, db);
            createTable.ExecuteReader();
        }

        public static void AddFavorite(FavoriteItem item)
        {
            // 空值检查
            if (string.IsNullOrEmpty(item.RoomID) || string.IsNullOrEmpty(item.SiteName))
            {
                return;
            }

            if (CheckFavorite(item.RoomID, item.SiteName)!=null) { return; }
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "INSERT INTO Favorite VALUES (NULL,@user_name,@site_name, @photo, @room_id);";
            command.Parameters.AddWithValue("@user_name", item.UserName ?? "");
            command.Parameters.AddWithValue("@site_name", item.SiteName);
            command.Parameters.AddWithValue("@photo", item.Photo ?? "");
            command.Parameters.AddWithValue("@room_id", item.RoomID);
            command.ExecuteNonQuery();
        }
        public static long? CheckFavorite(string roomId, string siteName)
        {
            // 空值检查
            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(siteName))
            {
                return null;
            }

            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "SELECT id FROM Favorite WHERE room_id=@room_id and site_name=@site_name";
            command.Parameters.AddWithValue("@site_name", siteName);
            command.Parameters.AddWithValue("@room_id", roomId);
            var result = command.ExecuteScalar();
            if (result==null)
            {
                return null;
            }
            return (long)result;
        }

        public static void UpdateFavorite(long id, string userName, string photo)
        {
            System.Diagnostics.Trace.WriteLine($"[DatabaseHelper.UpdateFavorite] 更新收藏: id={id}");
            using (var command = new SqliteCommand())
            {
                command.Connection = db;
                command.CommandText = "UPDATE Favorite SET user_name=@user_name, photo=@photo WHERE id=@id";
                command.Parameters.AddWithValue("@user_name", userName ?? "");
                command.Parameters.AddWithValue("@photo", photo ?? "");
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }

        public static void DeleteFavorite(long id)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "DELETE FROM Favorite WHERE id=@id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();

        }

        public static void DeleteFavorite()
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "DELETE FROM Favorite";
            command.ExecuteNonQuery();

        }

        public async static Task<List<FavoriteItem>> GetFavorites()
        {
            List<FavoriteItem> favoriteItems = new List<FavoriteItem>();
            SqliteCommand command = new SqliteCommand("SELECT * FROM Favorite", db);
            var reader =await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                favoriteItems.Add(new FavoriteItem()
                {
                    ID= reader.GetInt32(0),
                    RoomID = reader.GetString(4),
                    Photo = reader.GetString(3),
                    SiteName = reader.GetString(2),
                    UserName = reader.GetString(1)
                });
            }
            return favoriteItems;
        }


        public static void AddHistory(HistoryItem item)
        {
            // 空值检查，防止 SQLite 参数绑定失败
            if (string.IsNullOrEmpty(item.RoomID) || string.IsNullOrEmpty(item.SiteName))
            {
                return;
            }

            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            var hisId = CheckHistory(item.RoomID, item.SiteName);
            if (hisId != null)
            {
                //更新时间和用户信息
                command.CommandText = "UPDATE History SET watch_time=@time, user_name=@user_name, photo=@photo WHERE room_id=@room_id and site_name=@site_name";
                command.Parameters.AddWithValue("@site_name", item.SiteName);
                command.Parameters.AddWithValue("@room_id", item.RoomID);
                command.Parameters.AddWithValue("@time", DateTime.Now);
                command.Parameters.AddWithValue("@user_name", item.UserName ?? "");
                command.Parameters.AddWithValue("@photo", item.Photo ?? "");
                command.ExecuteNonQuery();

                return;
            }

            command.CommandText = "INSERT INTO History VALUES (NULL,@user_name,@site_name, @photo, @room_id,@time);";
            command.Parameters.AddWithValue("@user_name", item.UserName ?? "");
            command.Parameters.AddWithValue("@site_name", item.SiteName);
            command.Parameters.AddWithValue("@photo", item.Photo ?? "");
            command.Parameters.AddWithValue("@room_id", item.RoomID);
            command.Parameters.AddWithValue("@time", DateTime.Now);
            command.ExecuteNonQuery();
        }
        public static long? CheckHistory(string roomId, string siteName)
        {
            // 空值检查
            if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(siteName))
            {
                return null;
            }

            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "SELECT id FROM History WHERE room_id=@room_id and site_name=@site_name";
            command.Parameters.AddWithValue("@site_name", siteName);
            command.Parameters.AddWithValue("@room_id", roomId);
            var result = command.ExecuteScalar();
            if (result == null)
            {
                return null;
            }
            return (long)result;
        }
        public static void DeleteHistory(long id)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "DELETE FROM History WHERE id=@id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();

        }
        public static void DeleteHistory()
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "DELETE FROM History";
            command.ExecuteNonQuery();

        }
        public async static Task<List<HistoryItem>> GetHistory()
        {
            List<HistoryItem> favoriteItems = new List<HistoryItem>();
            SqliteCommand command = new SqliteCommand("SELECT * FROM History ORDER BY watch_time DESC", db);
            var reader =await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                favoriteItems.Add(new HistoryItem()
                {
                    ID= reader.GetInt32(0),
                    RoomID = reader.GetString(4),
                    Photo = reader.GetString(3),
                    SiteName = reader.GetString(2),
                    UserName = reader.GetString(1),
                    WatchTime= reader.GetDateTime(5)
                });
            }
            return favoriteItems;
        }

    }


}
