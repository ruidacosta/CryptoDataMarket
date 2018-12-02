using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CryptoCompareAPI.RateLimiter;
using log4net;
using Newtonsoft.Json;

namespace CryptoCompareAPI
{
    public class CoinAPIResponse
    {
        public string Response { get; set; }
        public string Message { get; set; }
        public Dictionary<string, CoinAPI> Data { get; set; }
        public string BaseImageUrl { get; set; }
        public string BaseLinkUrl { get; set; }
    }

    public class CoinAPI
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string CoinName { get; set; }
        public string FullName { get; set; }
        public string TotalCoinSupply { get; set; }
        public bool IsTrading { get; set; }
    }

    public class MarketDataAPIResponse
    {
        public string Response { get; set; }
        public int Type { get; set; }
        public bool Aggregated { get; set; }
        public List<OHLCVAPI> Data { get; set; }
        public long TimeTo { get; set; }
        public long TimeFrom { get; set; }
        public bool FirstValueInArray { get; set; }
        public ConvertionType ConversionType { get; set; }
    }

    class MarketDataAPIError
    {
        public string Response { get; set; }
        public string Message { get; set; }
        public string ParamWithError { get; set; }
    }

    public class ConvertionType
    {
        public string type { get; set; }
        public string convertionSymbol { get; set; }
    }

    public class OHLCVAPI
    {
        public double time { get; set; }
        public decimal close { get; set; }
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal open { get; set; }
        public decimal volumefrom { get; set; }
        public decimal volumeto { get; set; }
    }

    public class ParamErrorException : Exception
    {
        public string ParamWithError { get; set; }
        public ParamErrorException(string message, string paramWithError) : base(message)
        {
            ParamWithError = paramWithError;
        }
    }

    public class APIClient
    {
        readonly HttpClient client;
        readonly object lockObject = new object();
        readonly TimeLimiter timeConstraint;
        readonly static int RECONNECTION_NUMBER = 3;

        static readonly ILog logger = log4net.LogManager.GetLogger(typeof(APIClient));

        public APIClient()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri("https://min-api.cryptocompare.com/data/")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var constraint1 = new CountByIntervalAwaitableConstraint(15, TimeSpan.FromSeconds(1));
            var constraint2 = new CountByIntervalAwaitableConstraint(300, TimeSpan.FromMinutes(1));
            var constraint3 = new CountByIntervalAwaitableConstraint(8000, TimeSpan.FromHours(1));

            timeConstraint = TimeLimiter.Compose(constraint3, constraint2, constraint1);
        }

        public CoinAPIResponse GetAllCoins()
        {
            var apiCall = GetAPICoins();
            apiCall.Wait();
            return apiCall.Result;
        }

        async Task<CoinAPIResponse> GetAPICoins()
        {
            logger.Debug("Get API Coins");
            CoinAPIResponse respMessage = null;
            HttpResponseMessage response = await client.GetAsync("all/coinlist");
            if (response.IsSuccessStatusCode)
            {
                var respData = await response.Content.ReadAsStringAsync();

                respMessage = JsonConvert.DeserializeObject<CoinAPIResponse>(respData);
            }
            return respMessage;
        }

        public MarketDataAPIResponse GetOHLCVForInstrument(string coin, string referenceCoin, bool allData = false)
        {
            var apiCall = timeConstraint.Perform<MarketDataAPIResponse>(() => GetOHLCVAPIForInstrument(coin, referenceCoin, allData));
            apiCall.Wait();
            return apiCall.Result;
        }

        async Task<MarketDataAPIResponse> GetOHLCVAPIForInstrument(string coin, string referenceCoin, bool allData)
        {
            //logger.Debug("Getting OHLCV from API form ticker " + coin);

            MarketDataAPIResponse respMessage = null;

            string url = allData
                ? string.Format("histoday?fsym={0}&tsym={1}&allData=true&aggregate=3&e=CCCAGG&extraParams=CryptoMarketData", coin, referenceCoin)
                : string.Format("histoday?fsym={0}&tsym={1}&limit=1&aggregate=3&e=CCCAGG&extraParams=CryptoMarketData", coin, referenceCoin);

            var done = 0;
            HttpResponseMessage response = null;
            while (done < RECONNECTION_NUMBER)
            {
                try
                {
                    response = await client.GetAsync(url);
                    done = 4;
                }
                catch (HttpRequestException ex)
                {
                    logger.Error("Cannot connect with remote server",ex);
                    logger.InfoFormat("Trying reconnection {0} time", ++done);
                }
                await Task.Delay(50);
            }

            if (response.IsSuccessStatusCode)
            {
                var respData = await response.Content.ReadAsStringAsync();
                try
                {
                    respMessage = JsonConvert.DeserializeObject<MarketDataAPIResponse>(respData);
                    if (!allData)
                    {
                        if (respMessage.Data.Count > 0)
                            respMessage.Data.RemoveAt(respMessage.Data.Count - 1);
                    }
                }
                catch (Exception ex)
                {
                    MarketDataAPIError error = JsonConvert.DeserializeObject<MarketDataAPIError>(respData);
                    if (error.Response == "Error")
                    {
                        throw new ParamErrorException(error.Message, error.ParamWithError);
                    }
                    throw ex;
                }
            }

            return respMessage;
        }
    }
}
