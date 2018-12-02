using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using CryptoCompareAPI;
using CryptoMarketData.Configuration;
using CryptoMarketData.Database;
using log4net;
using Newtonsoft.Json;

namespace CryptoMarketData.DataClient
{
    public class MarketDataProcessor
    {
        readonly APIClient client;
        readonly BufferBlock<OHLCVInfo> dataBuffer;
        readonly DatabaseFactory databaseFactory;
        readonly Settings settings;
        bool consumerAlive;
        static readonly ILog logger = log4net.LogManager.GetLogger(typeof(MarketDataProcessor));

        public MarketDataProcessor(Settings _settings)
        {
            client = new APIClient();
            dataBuffer = new BufferBlock<OHLCVInfo>();
            databaseFactory = new DatabaseFactory(_settings.Database);
            settings = _settings;
        }

        public void Start()
        {
            logger.Info("Getting all coins");
            var coins = client.GetAllCoins();

            if (coins.Response == "Error")
            {
                logger.Error("Unable to get coins from API" + Environment.NewLine + JsonConvert.SerializeObject(coins));
                logger.Info("This issue will finish this application");
                return;
            }

            logger.InfoFormat("Trying to connect to database {0}...", databaseFactory.database.GetDatabasePath());
            if (!databaseFactory.DatabaseConnect())
            {
                logger.FatalFormat("Fatal Error: without open connection to database this program can't continue");
                Environment.Exit(1);
            }

            logger.Info("Starting consuming queue");
            var consumer = Consumer();
            logger.Info("Starting produce on queue");
            Producer(coins);
            consumer.Wait();

            logger.Info("Store new coins on database.");
            StoreCoins(coins.Data.Values);

            logger.InfoFormat("Trying to disconnect database");
            databaseFactory.DatabaseDisconnect();
        }

        void Producer(CoinAPIResponse coins)
        {
            try
            {
                int counter = 0;
                var traddingCoins = coins.Data.Where(coin => coin.Value.IsTrading).ToList();
                foreach(var coin in traddingCoins)
                {
                    logger.InfoFormat("Coin {0} of {1}",++counter,traddingCoins.Count);
                    MarketDataAPIResponse coinDataApi = null;
                    try
                    {
                        try
                        {
                            coinDataApi = client.GetOHLCVForInstrument(
                                    coin.Value.Symbol, settings.General.UserCoin, settings.General.FullData);
                        }
                        catch (AggregateException ae)
                        {
                            foreach (Exception ex in ae.InnerExceptions)
                            {
                                if (ex is ParamErrorException)
                                    logger.InfoFormat("{0} ParameterWithErrot: {1} Symbol: {2}", ((ParamErrorException)ex).Message, 
                                                      ((ParamErrorException)ex).ParamWithError, coin.Value.Symbol);
                                else
                                    logger.Error(string.Format("Error getting data for ticker {0}", coin.Value.Symbol), ex);
                            }
                            continue;
                        }

                        if (coinDataApi.Response == "Error")
                        {
                            if ((coinDataApi.TimeTo == 0) && (coinDataApi.TimeFrom == 0))
                                logger.InfoFormat("CryptoCoin {0} without trading information", coin.Value.Symbol);
                            else
                                logger.Debug(JsonConvert.SerializeObject(coinDataApi));
                        }

                        foreach (var coinData in coinDataApi.Data)
                        {
                            OHLCVInfo coinDataOHLCV = new OHLCVInfo
                            {
                                ticker = coin.Value.Symbol,
                                time = coinData.time,
                                open = coinData.open,
                                high = coinData.high,
                                low = coinData.low,
                                close = coinData.close,
                                volumefrom = coinData.volumefrom,
                                volumeto = coinData.volumeto
                            };

                            dataBuffer.Post(coinDataOHLCV);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(string.Format("Error on producer getting data for ticker {0}",coin.Value.Symbol), e);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("GetMarketData: ", e);
            }
            finally
            {
                dataBuffer.Complete();
            }
        }

        async Task Consumer()
        {
            consumerAlive = true;
            Thread bufferChecker = new Thread(new ThreadStart(Checker));
            bufferChecker.Start();
            while (await dataBuffer.OutputAvailableAsync())
            {
                while (dataBuffer.TryReceive(out OHLCVInfo data))
                {
                    // Save to database
                    try
                    {
                        //logger.InfoFormat("On Queue to process: {0}", dataBuffer.Count);
                        databaseFactory.InsertOHLCV(data);
                    }
                    catch(Exception e)
                    {
                        logger.Error(string.Format("Error insert OHLCV data for ticker {0}: " +
                                                   "INSERT INTO CryptoMarketDate VALUES " +
                                                   "({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})"
                                                   , data.ticker, DatabaseFactory.UnixTimestap2DateTime(data.time), data.close, data.high, data.low
                                                   , data.open, data.volumefrom, data.volumeto), e);
                    }
                }
            }
            consumerAlive = false;
            bufferChecker.Join();
        }

        void Checker()
        {
            while(consumerAlive)
            {
                logger.InfoFormat("Queue size: {0}", dataBuffer.Count);
                Thread.Sleep(1000);
            }
        }

        void StoreCoins(ICollection<CoinAPI> coins)
        {
            try
            {
                databaseFactory.InsertBulkCoins(coins);
            }
            catch (Exception e)
            {
                logger.Error("Error insert coins on database", e);
            }
        }
    }
}