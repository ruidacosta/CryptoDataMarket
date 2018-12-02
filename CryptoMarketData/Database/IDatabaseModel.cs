using System;
using System.Collections.Generic;

namespace CryptoMarketData.Database
{
    public interface IDatabaseModel
    {
         string GetDatabasePath();
        void Connect();
        void Disconnect();
        bool IsAlive();
        void InsertOHLCV(string ticker, DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volumeto, decimal volumefrom);
        void InsertCoin(string symbol, string name, string coinName, string fullName, string totalCoinSupply);
        void InsertBulkCoins(ICollection<(string symbol, string name, string coinName, string fullName, string totalCoinSupply)> coinsBulk);
    }
}