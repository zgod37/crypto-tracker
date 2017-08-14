﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Input;
using System.Windows.Threading;

namespace CryptoPriceWatcher {
    public class TickerContainerViewModel : BaseViewModel {

        /// <summary>
        /// the collection of tickers shown in the UI (one for each coin)
        /// </summary>
        private ObservableCollection<Ticker> _tickers;

        /// <summary>
        /// the dispatcher to update the prices of the tickers
        /// </summary>
        private DispatcherTimer _updateTimer;

        /// <summary>
        /// the interval between each UI update to the price values
        /// </summary>
        private int _animateIntervalMillis = 30;

        /// <summary>
        /// the interval between checking the API for new prices
        /// </summary>
        private int _updateIntervalMillis = 8000;

        /// <summary>
        /// the current interval count used for the dispatcher timer
        /// </summary>
        private int _currentIntervalCount = 0;

        /// <summary>
        /// the interval at which the api is checked for new prices
        /// </summary>
        private int _updateInterval = 0;

        /// <summary>
        /// the total USD spent on coins
        /// </summary>
        private double _initialCost = 0;

        /// <summary>
        /// list of indices in random order for updating tickers
        /// </summary>
        private Queue<int> _randomizedIndexQueue;

        /// <summary>
        /// the number of tickers to be polled via the API at a time
        /// </summary>
        private int _tickerBatchCount = 5;

        /// <summary>
        /// the list of active tickers
        /// </summary>
        public ObservableCollection<Ticker> Tickers { get { return _tickers; } }

        /// <summary>
        /// the total USD amount of all held coins
        /// </summary>
        public String TotalUSD { get; set; }

        /// <summary>
        /// text color of TotalUSD, used to indicate whether or not the updateTimer is running
        /// </summary>
        public String TotalUSDTextColor { get; set; } = "Black";

        /// <summary>
        /// the total profit currently held on paper
        /// </summary>
        public String TotalProfit { get; set; }

        /// <summary>
        /// Coin abbrev of a new ticker to be added
        /// </summary>
        public String NewCoin { get; set; }

        /// <summary>
        /// Back color for new coin textbox
        /// </summary>
        public String NewCoinBackgroundColor { get; set; } = "#CCC";

        /// <summary>
        /// command to update the total
        /// </summary>
        public ICommand UpdateCommand { get; set; }

        /// <summary>
        /// command to add a new blank ticker
        /// </summary>
        public ICommand AddNewTickerCommand { get; set; }

        /// <summary>
        /// command to start/stop the update timer
        /// </summary>
        public ICommand ToggleUpdateTimerCommand { get; set; }

        /// <summary>
        /// command to set all coin counts to zero
        /// </summary>
        public ICommand ZeroOutAllCoinsCommand { get; set; }

        /// <summary>
        /// default constructor
        /// </summary>
        public TickerContainerViewModel() {

            System.Diagnostics.Debug.AutoFlush = true;

            _tickers = new ObservableCollection<Ticker>();

            //get coins from database
            DBConnection db = new DBConnection();
            List<CoinInfo> coins = db.GetCoins();

            //create ticker for each coin
            foreach (CoinInfo coin in coins) {
                System.Diagnostics.Debug.WriteLine($"Adding coin {coin.Name} with amount {coin.Count} at buy price {coin.EntryPrice}");
                _tickers.Add(new Ticker(coin.Name, coin.Count, coin.EntryPrice));
            }
            CreateRandomTickerUpdateOrder();

            UpdateCommand = new RelayCommand(UpdateCoinDB);
            AddNewTickerCommand = new RelayCommand(AddNewTicker);
            ToggleUpdateTimerCommand = new RelayCommand(ToggleUpdateTimer);
            ZeroOutAllCoinsCommand = new RelayCommand(ZeroOutAllCoins);

            //**NOTE** not calculating initial cost from buy-ins, but using initial USD spent on coinbase
            _initialCost = 6513.19;
            //CalculateInitialCost(amounts, buys);

            //set interval for update check and start dispatcher timer
            _updateInterval = _updateIntervalMillis / _animateIntervalMillis;
            System.Diagnostics.Debug.WriteLine($"Prices animated every {_animateIntervalMillis}ms");
            System.Diagnostics.Debug.WriteLine($"Prices checked every {_updateIntervalMillis}ms)");

            UpdateTotal();

            //TestTickerAnimation(0.75);
            StartPriceUpdatingDispatcherTimer();
        }

        /// <summary>
        /// dispatcher timer to update prices for each ticker, uses fast tick rate to animate price changes
        /// </summary>
        private void StartPriceUpdatingDispatcherTimer() {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(_animateIntervalMillis);
            _updateTimer.Tick += (sender, e) => {

                //larger interval - check API for new prices
                if (_currentIntervalCount == _updateInterval) {

                    _currentIntervalCount = 0;

                    //choose random batch of tickers to update
                    int[] tickerIndices = new int[_tickerBatchCount];
                    for (int i = 0; i < _tickerBatchCount; i++) {
                        if (_randomizedIndexQueue.Count == 0) {
                            CreateRandomTickerUpdateOrder();
                        }
                        tickerIndices[i] = _randomizedIndexQueue.Dequeue();
                    }
                    UpdateTickers(tickerIndices);
                    UpdateTotal();
                    if (NewCoinBackgroundColor != "#CCC") {
                        NewCoinBackgroundColor = "#CCC";
                    }
                }

                //smaller interval - otherwise check if the price needs to be animated
                else {
                    _currentIntervalCount++;
                    foreach (Ticker ticker in Tickers) {
                        if (ticker.IsAnimating) {
                            ticker.AnimateMoveTowardNewPrice();
                        }
                    }
                }
            };

            _updateTimer.IsEnabled = true;
        }

        /// <summary>
        /// create randomized order for tickers to be updated
        /// </summary>
        private void CreateRandomTickerUpdateOrder() {
            _randomizedIndexQueue = TickerUtils.GetRandomIndexQueue(Tickers.Count);
            System.Diagnostics.Debug.WriteLine($"Randomized order = {TickerUtils.StringifyQueue(_randomizedIndexQueue)}");
        }

        /// <summary>
        /// polls the API to get new prices for the desired tickers
        /// </summary>
        /// <param name="tickerIndices">the indices of the tickers to be updated</param>
        private void UpdateTickers(int[] tickerIndices) {

            //build URL string for desired coins
            String fsyms = "";
            foreach (int i in tickerIndices) {
                fsyms += Tickers[i].CoinName + ",";
            }
            fsyms = fsyms.Substring(0, fsyms.Length - 1);
            System.Diagnostics.Debug.WriteLine($"Checking prices for = {fsyms}");
            String apiUrl = $"https://min-api.cryptocompare.com/data/pricemulti?fsyms={fsyms}&tsyms=USD";

            //get info from API
            String jsonString = null;
            try {
                using (WebClient wc = new WebClient()) {
                    jsonString = wc.DownloadString(apiUrl);
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error occurred polling API - {ex}");
                jsonString = null;
            }

            //parse json and set new prices
            if (jsonString != null) {
                try {
                    JObject json = JObject.Parse(jsonString);
                    foreach (int i in tickerIndices) {
                        JObject coinResult = (JObject)json[Tickers[i].CoinName];
                        Tickers[i].SetNewPrice((double)coinResult["USD"]);
                        Tickers[i].Portion = $"{(Double.Parse(Tickers[i].USDHoldings.Substring(1)) / Double.Parse(TotalUSD.Substring(1))):%##0}";
                    }
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Error parsing JSON = {ex}");
                }
            }

        }

        /// <summary>
        /// start/stop the update timer
        /// </summary>
        private void ToggleUpdateTimer() {
            if (_updateTimer.IsEnabled == false) {
                EnableUpdateTimer();
            } else {
                DisableUpdateTimer();
            }
        }

        /// <summary>
        /// enable the update timer
        /// </summary>
        private void EnableUpdateTimer() {
            TotalUSDTextColor = "Black";
            _updateTimer.IsEnabled = true;
        }


        /// <summary>
        /// disable the update timer
        /// </summary>
        private void DisableUpdateTimer() {
            TotalUSDTextColor = "Orange";
            _updateTimer.IsEnabled = false;
        }

        /// <summary>
        /// set all coin counts to zero
        /// </summary>
        private void ZeroOutAllCoins() {
            foreach (Ticker ticker in Tickers) {
                ticker.CoinCount = "0";
            }
        }

        /// <summary>
        /// update the total USD holdings
        /// </summary>
        private void UpdateTotal() {
            double total = 0;
            foreach (Ticker ticker in Tickers) {
                ticker.UpdateTotals();
                total += Double.Parse(ticker.USDHoldings.Substring(1));
            }
            TotalUSD = $"${total:###0.00}";
            TotalProfit = $"{total - _initialCost:+$###0.00;-$###0.00}";
        }

        /// <summary>
        /// update the coins and save the new coin info in the database
        /// ***NOTE*** function needs optimizing, currently stops dispatcher, should change to async task
        /// ***NOTE*** and also builds new CoinInfo list which is unnecessary
        /// ***NOTE***  b/c info is already stored Tickers ObservableCollection
        /// </summary>
        private void UpdateCoinDB() {

            //update the totals
            UpdateTotal();

            //stop update dispatcher temporarily while updating DB
            DisableUpdateTimer();

            //build list of new coininfo
            List<CoinInfo> newCoins = new List<CoinInfo>();
            foreach (Ticker ticker in Tickers) {
                newCoins.Add(new CoinInfo(ticker.CoinName, Double.Parse(ticker.CoinCount), Double.Parse(ticker.EntryPrice.Substring(1))));
            }

            //update DB
            DBConnection db = new DBConnection();
            db.UpdateCoins(newCoins);

            //restart update dispatcher
            EnableUpdateTimer();
        }

        /// <summary>
        /// calculate the initial cost spent on coins based on coin amounts and entry points
        /// </summary>
        /// <param name="amounts"></param>
        /// <param name="buys"></param>
        private void CalculateInitialCost(double[] amounts, double[] buys) {
            double total = 0;
            for (int i = 0; i < amounts.Length; i++) {
                total += (amounts[i] * buys[i]);
            }
            _initialCost = total;
            System.Diagnostics.Debug.WriteLine($"Initial cost = {_initialCost}");
        }

        /// <summary>
        /// add new ticker for an unadded coin
        /// </summary>
        private void AddNewTicker() {
            if (String.IsNullOrEmpty(NewCoin) || IsCoinValid(NewCoin) == false) {
                return;
            }
            Tickers.Add(new Ticker(NewCoin, 0, 0));
            NewCoin = "";
        }

        /// <summary>
        /// whether or not the coin is available on the currently used API
        /// </summary>
        /// <param name="coinName"></param>
        /// <returns></returns>
        private bool IsCoinValid(String coinName) {
            
            //get json string from the url
            String jsonString = null;
            try {
                using (WebClient wc = new WebClient()) {
                    jsonString = wc.DownloadString($"https://min-api.cryptocompare.com/data/price?fsym={coinName}&tsyms=USD");
                }
            } catch (Exception) {
                jsonString = null;
            }

            if (jsonString != null) {
                JObject json = JObject.Parse(jsonString);
                if (String.IsNullOrEmpty((String)json["Message"])) {
                    NewCoinBackgroundColor = "#CCC";
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"{coinName} not found!");
            NewCoinBackgroundColor = "LightPink";
            return false;
        }

    }
}
