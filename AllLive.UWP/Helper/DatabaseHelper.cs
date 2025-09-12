using AllLive.UWP.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace AllLive.UWP.Helper
{

    public static class DatabaseHelper
    {
        static SqliteConnection db;
        public async static Task InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("alllive.db", CreationCollisionOption.OpenIfExists);
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "alllive.db");
            db = new SqliteConnection($"Filename={dbPath}");
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
            if (CheckFavorite(item.RoomID, item.SiteName) != null) { return; }
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "INSERT INTO Favorite VALUES (NULL,@user_name,@site_name, @photo, @room_id);";
            command.Parameters.AddWithValue("@user_name", item.UserName);
            command.Parameters.AddWithValue("@site_name", item.SiteName);
            command.Parameters.AddWithValue("@photo", item.Photo);
            command.Parameters.AddWithValue("@room_id", item.RoomID);
            command.ExecuteReader();
        }
        public static void UpdateFavorite(FavoriteItem item)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "UPDATE Favorite SET user_name = @user_name, photo = @photo WHERE id = @id";
            command.Parameters.AddWithValue("@user_name", item.UserName);
            command.Parameters.AddWithValue("@photo", item.Photo);
            command.Parameters.AddWithValue("@id", item.ID);
            command.ExecuteReader();
        }
        public static void UpdateHistory(HistoryItem item)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "UPDATE History SET user_name = @user_name, photo = @photo WHERE id = @id";
            command.Parameters.AddWithValue("@user_name", item.UserName);
            command.Parameters.AddWithValue("@photo", item.Photo);
            command.Parameters.AddWithValue("@id", item.ID);
            command.ExecuteReader();
        }
        public static long? CheckFavorite(string roomId, string siteName)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "SELECT * FROM Favorite WHERE room_id=@room_id and site_name=@site_name";
            command.Parameters.AddWithValue("@site_name", siteName);
            command.Parameters.AddWithValue("@room_id", roomId);
            var result = command.ExecuteScalar();
            if (result == null)
            {
                return null;
            }
            return (long)result;
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

        public async static IAsyncEnumerable<FavoriteItem> GetFavorites()
        {
            string query = "SELECT * FROM Favorite";
            using (SqliteCommand command = new SqliteCommand(query, db))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) // 异步读取每一行
                {
                    yield return new FavoriteItem()
                    {
                        ID = reader.GetInt32(0),
                        UserName = reader.GetString(1),
                        SiteName = reader.GetString(2),
                        Photo = reader.GetString(3),
                        RoomID = reader.GetString(4)
                    };
                }
            }
        }

        public static void AddHistory(HistoryItem item)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            var hisId = CheckHistory(item.RoomID, item.SiteName);
            if (hisId != null)
            {
                //更新时间
                command.CommandText = "UPDATE History SET watch_time=@time WHERE room_id=@room_id and site_name=@site_name";
                command.Parameters.AddWithValue("@site_name", item.SiteName);
                command.Parameters.AddWithValue("@room_id", item.RoomID);
                command.Parameters.AddWithValue("@time", DateTime.Now);
                command.ExecuteReader();

                return;
            }

            command.CommandText = "INSERT INTO History VALUES (NULL,@user_name,@site_name, @photo, @room_id,@time);";
            command.Parameters.AddWithValue("@user_name", item.UserName);
            command.Parameters.AddWithValue("@site_name", item.SiteName);
            command.Parameters.AddWithValue("@photo", item.Photo);
            command.Parameters.AddWithValue("@room_id", item.RoomID);
            command.Parameters.AddWithValue("@time", DateTime.Now);
            command.ExecuteReader();
        }
        public static long? CheckHistory(string roomId, string siteName)
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = db;
            command.CommandText = "SELECT * FROM History WHERE room_id=@room_id and site_name=@site_name";
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
        public async static IAsyncEnumerable<HistoryItem> GetHistory()
        {
            string query = "SELECT * FROM History ORDER BY watch_time DESC";
            using (SqliteCommand command = new SqliteCommand(query, db))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) // 异步读取每一行
                {
                    yield return new HistoryItem()
                    {
                        ID = reader.GetInt32(0),
                        RoomID = reader.GetString(4),
                        Photo = reader.GetString(3),
                        SiteName = reader.GetString(2),
                        UserName = reader.GetString(1),
                        WatchTime = reader.GetDateTime(5)
                    };
                }
            }
        }

    }


}
