using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Data.SQLite;

namespace CryptoMarketData.Database.Sqlite
{
    public class SqliteModel : IDatabaseModel
    {
        SQLiteConnection m_dbConnection;
        readonly string database;

        public SqliteModel(string databasePath)
        {
            database = databasePath;
            if (!File.Exists(databasePath))
            {
                SQLiteConnection.CreateFile(databasePath);
                CreateDatabaseStructure();
            }
        }

        public void Connect()
        {
            if (!IsAlive())
            {
                string connectionString = string.Format("Data Source={0};Version=3;", database);
                m_dbConnection = new SQLiteConnection(connectionString);
                m_dbConnection.Open();
            }
        }

        public void Disconnect()
        {
            if (IsAlive())
                m_dbConnection.Close();
        }

        public void InsertCoin(string symbol, string name, string coinName, string fullName, string totalCoinSupply)
        {
            if (!IsAlive())
                throw new Exception(string.Format("Connection to database {0} is not open.", database));

            SQLiteCommand cmd = new SQLiteCommand(SqliteSql.InsertCryptoCurrencies, m_dbConnection);
            cmd.Parameters.AddWithValue("$symbol", symbol);
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$coinName", coinName);
            cmd.Parameters.AddWithValue("$fullNmae", fullName);
            cmd.Parameters.AddWithValue("$totalCoinSupply", totalCoinSupply);
            cmd.ExecuteNonQuery();
        }

        public void InsertOHLCV(string ticker, DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volumeto, decimal volumefrom)
        {
            if (!IsAlive())
                throw new Exception(string.Format("Connection to database {0} is not open.", database));

            SQLiteCommand cmd = new SQLiteCommand(SqliteSql.InsertCryptoMarkerData, m_dbConnection);
            cmd.Parameters.AddWithValue("$ticker", ticker);
            cmd.Parameters.AddWithValue("$time", time.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$open", open);
            cmd.Parameters.AddWithValue("$high", high);
            cmd.Parameters.AddWithValue("$low", low);
            cmd.Parameters.AddWithValue("$close", close);
            cmd.Parameters.AddWithValue("$volumeto", volumeto);
            cmd.Parameters.AddWithValue("$volumefrom", volumefrom);
            cmd.ExecuteNonQuery();
        }

        public bool IsAlive()
        {
            return m_dbConnection != null && m_dbConnection.State != ConnectionState.Closed;
        }

        void CreateDatabaseStructure()
        {
            Connect();

            SQLiteCommand cmd = new SQLiteCommand(SqliteSql.CreateTableCryptoCurrencies, m_dbConnection);
            cmd.ExecuteNonQuery();

            cmd = new SQLiteCommand(SqliteSql.CreateTableCryptoMarketData, m_dbConnection);
            cmd.ExecuteNonQuery();
        }

        public void InsertBulkCoins(ICollection<(string symbol, string name, string coinName, string fullName, string totalCoinSupply)> coinsBulk)
        {
            if (!IsAlive())
                throw new Exception(string.Format("Connection to database {0} is not open.", database));
                
            using (var transaction = m_dbConnection.BeginTransaction())
            {
                foreach (var (symbol, name, coinName, fullName, totalCoinSupply) in coinsBulk)
                {
                    var cmd = new SQLiteCommand(SqliteSql.InsertCryptoCurrencies, m_dbConnection, transaction);
                    cmd.Parameters.AddWithValue("$Name", name);
                    cmd.Parameters.AddWithValue("$Symbol", symbol);
                    cmd.Parameters.AddWithValue("$CoinName", coinName);
                    cmd.Parameters.AddWithValue("$FullName", fullName);
                    cmd.Parameters.AddWithValue("$TotalCoinSupply", totalCoinSupply); 
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
        }

        public string GetDatabasePath()
        {
            return database;
        }
    }
}