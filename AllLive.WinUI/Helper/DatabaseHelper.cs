using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AllLive.WinUI.Models;
using Microsoft.Data.Sqlite;

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

CREATE TABLE IF NOT EXISTS FavoriteGroup (
id INTEGER PRIMARY KEY AUTOINCREMENT,
name TEXT NOT NULL,
sort_order INTEGER NOT NULL DEFAULT 0);

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

            // 迁移：为已有的 Favorite 表追加 group_id 列（NULL 表示默认分组）
            bool hasGroupId = false;
            SqliteCommand checkTable = new SqliteCommand("PRAGMA table_info(Favorite)", db);
            var tableReader = checkTable.ExecuteReader();
            while (tableReader.Read())
            {
                if (string.Equals(tableReader.GetString(1), "group_id", StringComparison.OrdinalIgnoreCase))
                {
                    hasGroupId = true;
                    break;
                }
            }
            tableReader.Dispose();
            if (!hasGroupId)
            {
                SqliteCommand alterTable = new SqliteCommand("ALTER TABLE Favorite ADD COLUMN group_id INTEGER", db);
                alterTable.ExecuteNonQuery();
            }
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
            command.CommandText = "INSERT INTO Favorite (user_name, site_name, photo, room_id, group_id) VALUES (@user_name,@site_name, @photo, @room_id, @group_id);";
            command.Parameters.AddWithValue("@user_name", item.UserName ?? "");
            command.Parameters.AddWithValue("@site_name", item.SiteName);
            command.Parameters.AddWithValue("@photo", item.Photo ?? "");
            command.Parameters.AddWithValue("@room_id", item.RoomID);
            command.Parameters.AddWithValue("@group_id", (object)item.GroupID ?? DBNull.Value);
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
            Trace.WriteLine($"[DatabaseHelper.UpdateFavorite] 更新收藏: id={id}");
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
            SqliteCommand command = new SqliteCommand("SELECT id, user_name, site_name, photo, room_id, group_id FROM Favorite ORDER BY id", db);
            var reader =await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                favoriteItems.Add(new FavoriteItem()
                {
                    ID = reader.GetInt32(0),
                    RoomID = reader.GetString(4),
                    Photo = reader.GetString(3),
                    SiteName = reader.GetString(2),
                    UserName = reader.GetString(1),
                    GroupID = reader.IsDBNull(5) ? (long?)null : reader.GetInt64(5)
                });
            }
            return favoriteItems;
        }

        // ============ 关注分组 ============

        public static List<FavoriteGroupItem> GetFavoriteGroups()
        {
            List<FavoriteGroupItem> groups = new List<FavoriteGroupItem>();
            SqliteCommand command = new SqliteCommand("SELECT id, name, sort_order FROM FavoriteGroup ORDER BY sort_order, id", db);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                groups.Add(new FavoriteGroupItem()
                {
                    ID = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    SortOrder = reader.GetInt32(2)
                });
            }
            reader.Dispose();
            return groups;
        }

        public static long AddFavoriteGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }
            SqliteCommand command = new SqliteCommand("INSERT INTO FavoriteGroup (name, sort_order) VALUES (@name, (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM FavoriteGroup));", db);
            command.Parameters.AddWithValue("@name", name);
            command.ExecuteNonQuery();

            SqliteCommand getId = new SqliteCommand("SELECT last_insert_rowid();", db);
            return (long)getId.ExecuteScalar();
        }

        public static void RenameFavoriteGroup(long id, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            SqliteCommand command = new SqliteCommand("UPDATE FavoriteGroup SET name=@name WHERE id=@id;", db);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除分组，组内主播移回默认分组
        /// </summary>
        public static void DeleteFavoriteGroup(long id)
        {
            SqliteCommand command = new SqliteCommand("UPDATE Favorite SET group_id=NULL WHERE group_id=@id;", db);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM FavoriteGroup WHERE id=@id;";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 按传入顺序整体重写分组排序
        /// </summary>
        public static void UpdateFavoriteGroupOrder(List<FavoriteGroupItem> groups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                SqliteCommand command = new SqliteCommand("UPDATE FavoriteGroup SET sort_order=@order WHERE id=@id;", db);
                command.Parameters.AddWithValue("@order", i);
                command.Parameters.AddWithValue("@id", groups[i].ID);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 设置关注所属分组，groupId 为 NULL 时移回默认分组
        /// </summary>
        public static void UpdateFavoriteGroup(long favoriteId, long? groupId)
        {
            SqliteCommand command = new SqliteCommand("UPDATE Favorite SET group_id=@group_id WHERE id=@id;", db);
            command.Parameters.AddWithValue("@group_id", (object)groupId ?? DBNull.Value);
            command.Parameters.AddWithValue("@id", favoriteId);
            command.ExecuteNonQuery();
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
