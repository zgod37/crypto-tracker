using Newtonsoft.Json.Linq;
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
        private int _updateIntervalMillis = 30000;

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
        public ICommand UpdateTotalCommand { get; set; }

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

            foreach (CoinInfo coin in coins) {
                System.Diagnostics.Debug.WriteLine($"Adding coin {coin.Name} with amount {coin.Count} at buy price {coin.EntryPrice}");
                _tickers.Add(new Ticker(coin.Name, coin.Count, coin.EntryPrice));
            }

            UpdateTotalCommand = new RelayCommand(UpdateTotal);
            AddNewTickerCommand = new RelayCommand(AddNewTicker);
            ToggleUpdateTimerCommand = new RelayCommand(ToggleUpdateTimer);
            ZeroOutAllCoinsCommand = new RelayCommand(ZeroOutAllCoins);

            //**NOTE** not calculating initial cost from buy-ins, but using initial USD spent on coinbase
            _initialCost = 6513.19;
            //CalculateInitialCost(amounts, buys);

            //set interval for update check and start dispatcher timer
            _updateInterval = _updateIntervalMillis / _animateIntervalMillis;
            System.Diagnostics.Debug.WriteLine($"Prices animated every {_animateIntervalMillis}ms");
            System.Diagnostics.Debug.WriteLine($"Prices checked every {_updateIntervalMillis / 2}ms)");

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

                //split up ticker list into two halves
                if (_currentIntervalCount == _updateInterval / 2) {
                    _currentIntervalCount++;
                    for (int i = 0; i < Tickers.Count / 2; i++) {
                        Tickers[i].CheckForNewPrice();
                        Tickers[i].Portion = $"{(Convert.ToDouble(Tickers[i].USDHoldings.Substring(1)) / Convert.ToDouble(TotalUSD.Substring(1))):%##0}";
                    }
                    UpdateTotal();
                } 
                
                else if (_currentIntervalCount == _updateInterval) {
                    _currentIntervalCount = 0;
                    for (int i = Tickers.Count / 2; i < Tickers.Count; i++) {
                        Tickers[i].CheckForNewPrice();
                        Tickers[i].Portion = $"{(Convert.ToDouble(Tickers[i].USDHoldings.Substring(1)) / Convert.ToDouble(TotalUSD.Substring(1))):%##0}";
                    }
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
        /// start/stop the update timer
        /// </summary>
        private void ToggleUpdateTimer() {
            if (_updateTimer.IsEnabled == false) {
                TotalUSDTextColor = "Black";
                _updateTimer.IsEnabled = true;
            } else {
                TotalUSDTextColor = "Orange";
                _updateTimer.IsEnabled = false;
            }
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
        /// test animation of ticker with sentinel value
        /// </summary>
        /// <param name="value">test value to increase the tickers by</param>
        private void TestTickerAnimation(double value) {

            DispatcherTimer updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(_animateIntervalMillis);
            updateTimer.Tick += (sender, e) => {

                //larger interval - check the api for price updates
                if (_currentIntervalCount == _updateInterval) {
                    _currentIntervalCount = 0;
                    foreach (Ticker ticker in Tickers) {
                        ticker.TestIncreaseNewPrice(value);
                    }
                }

                //smaller invterval - otherwise check if the price needs to be animated
                else {
                    _currentIntervalCount++;
                    foreach (Ticker ticker in Tickers) {
                        if (ticker.IsAnimating) {
                            ticker.AnimateMoveTowardNewPrice();
                        }
                    }
                }
            };

            updateTimer.Start();

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
