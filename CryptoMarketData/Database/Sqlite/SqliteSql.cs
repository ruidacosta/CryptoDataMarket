using System;
namespace CryptoMarketData.Database.Sqlite
{
    public static class SqliteSql
    {
        /**************************** 
         * Table CryptoCurrencies   *
         ****************************/
        public static readonly string CreateTableCryptoCurrencies =
            @"CREATE TABLE IF NOT EXISTS CryptoCurrencies 
            (
                Name            TEXT,
                Symbol          TEXT    PRIMARY KEY,
                CoinName        TEXT,
                FullName        TEXT,
                TotalCoinSupply TEXT
            ) WITHOUT ROWID";

        public static readonly string InsertCryptoCurrencies =
            @"INSERT OR IGNORE INTO CryptoCurrencies VALUES 
            ($Name, $Symbol, $CoinName, $FullName, $TotalCoinSupply)"; 

        /**************************** 
         * Table CryptoMarketData   *
         ****************************/
        public static readonly string CreateTableCryptoMarketData =
            @"CREATE TABLE IF NOT EXISTS CryptoMarketData
            (
                ticker      TEXT,
                time        TEXT,
                close       NUMERIC,
                high        NUMERIC,
                low         NUMERIC,
                open        NUMERIC,
                volumefrom  NUMERIC,
                volumeto    NUMERIC,
                PRIMARY KEY (ticker,time),
                FOREIGN KEY (ticker) REFERENCES CryptoCurrencies(Symbol)
                ON DELETE CASCADE ON UPDATE NO ACTION
            ) WITHOUT ROWID";

        public static readonly string InsertCryptoMarkerData =
            @"INSERT INTO CryptoMarketData VALUES 
            ($ticker, $time, $close, $high, $low, $open, $volumefrom, $volumeto)";
    }
}
