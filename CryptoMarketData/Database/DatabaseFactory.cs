using System;
using System.Collections.Generic;
using CryptoCompareAPI;
using CryptoMarketData.Configuration;
using CryptoMarketData.Database.Sqlite;
using CryptoMarketData.DataClient;
using log4net;

namespace CryptoMarketData.Database
{
    public class DatabaseFactory
    {
        public IDatabaseModel database;

        static readonly ILog logger = log4net.LogManager.GetLogger(typeof(DatabaseFactory));

        public DatabaseFactory(DatabaseConf conf)
        {
            switch(conf.Type.ToUpper())
            {
                case "SQLITE3":
                    database = new SqliteModel(conf.Database);
                    break;
                default:
                    logger.Error("No database type configured");
                    break;
            }
        }

        public bool DatabaseConnect()
        {
            try
            {
                database.Connect();
                return true;
            }
            catch (Exception e)
            {
                logger.Error(string.Format("Can't connect to database {0}",database.GetDatabasePath()), e);
            }
            return false;
        }

        public void DatabaseDisconnect()
        {
            try
            {
                database.Disconnect();
            }
            catch (Exception e)
            {
                logger.Error(string.Format("Can't disconnect from database {0}", database.GetDatabasePath()), e);
            }
        }

        public void InsertOHLCV(OHLCVInfo data)
        {
            //logger.Info(string.Format("Insert OHLCV data for ticker {0}",data.ticker));
            database.InsertOHLCV(data.ticker, UnixTimestap2DateTime(data.time), data.open, data.high, data.low, data.close, data.volumeto, data.volumefrom);
        }

        public void InsertCoin(CoinAPI coin)
        {
            database.InsertCoin(coin.Symbol, coin.Name, coin.CoinName, coin.FullName, coin.TotalCoinSupply);
        }

        public void InsertBulkCoins(ICollection<CoinAPI> coins)
        {
            var list = new List<(string symbol, string name, string coinName, string fullName, string totalCoinSupply)>();

            foreach(var coin in coins)
            {
                var item = (coin.Symbol, coin.Name, coin.CoinName, coin.FullName, coin.TotalCoinSupply);
                list.Add(item);
            }

            database.InsertBulkCoins(list);
        }

        public static DateTime UnixTimestap2DateTime(double unixTimestap)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimestap);
        }
    }
}