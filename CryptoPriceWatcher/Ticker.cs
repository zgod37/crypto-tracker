using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Windows;
using System.Windows.Media;

namespace CryptoPriceWatcher {
    public class Ticker : DependencyObject {

        #region Private Properties

        /// <summary>
        /// the base url to the main price api
        /// </summary>
        private String _baseApiUrl = "https://min-api.cryptocompare.com/data/price?fsym=";

        /// <summary>
        /// the base url to the price api (alternate)
        /// </summary>
        private String _baseAlternateApiUrl = "https://api.cryptonator.com/api/ticker/";

        /// <summary>
        /// the previous price for the coin
        /// </summary>
        private double _lastPrice = 0;

        /// <summary>
        /// the new price for the coin
        /// </summary>
        private double _newPrice = 0;

        /// <summary>
        /// the amount/direction of the price change for animating the UI
        /// </summary>
        private double _priceIncrementValue = 0.01;

        private byte _borderBackgroundColorA = 0;
        private byte _borderBackgroundColorR = 0;
        private byte _borderBackgroundColorG = 0;
        private byte _borderBackgroundColorB = 0;


        #endregion

        #region Public Properties

        /// <summary>
        /// the three letter name of the coin
        /// </summary>
        public String CoinName {
            get {
                return (String)GetValue(CoinNameDP);
            }
            set {
                SetValue(CoinNameDP, value);
            }
        }

        /// <summary>
        /// the currently shown price of the coin
        /// </summary>
        public String CurrentPriceText {
            get {
                return (String)GetValue(CurrentPriceDP);
            }
            set {
                SetValue(CurrentPriceDP, value);
            }
        }

        /// <summary>
        /// the text color of current price; green/red for increasing/decreasing
        /// </summary>
        public String CurrentPriceTextColor {
            get {
                return (String)GetValue(CurrentPriceColorDP);
            }
            set {
                SetValue(CurrentPriceColorDP, value);
            }
        }

        /// <summary>
        /// color of the border brush
        /// </summary>
        public String BorderBrushColor {
            get {
                return (String)GetValue(BorderBrushColorDP);
            }
            set {
                SetValue(BorderBrushColorDP, value);
            }
        }

        /// <summary>
        /// color of the border background
        /// </summary>
        public Color BorderBackgroundColor {
            get {
                return (Color)GetValue(BorderBackgroundColorDP);
            }
            set {
                SetValue(BorderBackgroundColorDP, value.ToString());
            }
        }
        
        /// <summary>
        /// the amount of coins that are being held
        /// </summary>
        public String CoinCount {
            get {
                return (String)GetValue(CoinCountDP);
            }
            set {
                SetValue(CoinCountDP, value);
            }
        }

        /// <summary>
        /// the amount of coins that are being held
        /// </summary>
        public String EntryPrice {
            get {
                return (String)GetValue(EntryPriceDP);
            }
            set {
                SetValue(EntryPriceDP, value);
            }
        }

        /// <summary>
        /// the current USD value of held coins
        /// </summary>
        public String USDHoldings {
            get {
                return (String)GetValue(USDHoldingsDP);
            }
            set {
                SetValue(USDHoldingsDP, value);
            }
        }

        /// <summary>
        /// the current on-paper profit of the coin
        /// </summary>
        public String Profit {
            get {
                return (String)GetValue(ProfitDP);
            }
            set {
                SetValue(ProfitDP, value);
            }
        }

        /// <summary>
        /// this coin's percentage of the total holdings
        /// </summary>
        public String Portion {
            get {
                return (String)GetValue(PortionDP);
            }
            set {
                SetValue(PortionDP, value);
            }
        }

        /// <summary>
        /// whether or not the ticker is moving towards its new price
        /// </summary>
        public bool IsAnimating { get; set; } = false;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty CoinNameDP = DependencyProperty.Register("CoinName", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty CurrentPriceDP = DependencyProperty.Register("CurrentPriceText", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty CurrentPriceColorDP = DependencyProperty.Register("CurrentPriceTextColor", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty PriceChangeDP = DependencyProperty.Register("PriceChangeText", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty PriceChangeColorDP = DependencyProperty.Register("PriceChangeTextColor", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty CoinCountDP = DependencyProperty.Register("CoinCount", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty EntryPriceDP = DependencyProperty.Register("EntryPrice", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty USDHoldingsDP = DependencyProperty.Register("USDHoldings", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty ProfitDP = DependencyProperty.Register("Profit", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty PortionDP = DependencyProperty.Register("Portion", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty BorderBrushColorDP = DependencyProperty.Register("BorderBrushColor", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        public static readonly DependencyProperty BorderBackgroundColorDP = DependencyProperty.Register("BorderBackgroundColor", typeof(String), typeof(Ticker), new UIPropertyMetadata(""));

        //public static readonly DependencyProperty BorderBrushColorOpacityDP = DependencyProperty.Register("BorderBrushColorOpacity", typeof(double), typeof(Ticker), new UIPropertyMetadata(""));

        #endregion

        #region Constructor

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="coinName">the coin tracked in this ticker</param>
        public Ticker(String coinName, double amount, double averageBuyPrice) {

            CoinName = coinName;
            CoinCount = $"{amount}";
            EntryPrice = $"${averageBuyPrice:###0.00}";

            CurrentPriceTextColor = "Green";
            BorderBrushColor = "DarkGreen";
            BorderBackgroundColor = Colors.White;

            //initialize price values
            SetPriceFromApi();
            _lastPrice = _newPrice;
            Update();

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// check the API to see if the price has changed
        /// </summary>
        public void CheckForNewPrice() {
            System.Diagnostics.Debug.WriteLine($"Checking price for {CoinName}");

            SetPriceFromApi();
            
            if (_newPrice != _lastPrice) {
                UpdateDisplay();
            }
        }

        /// <summary>
        /// test function to manually increase/decrease price
        /// </summary>
        /// <param name="increment">the amount to move the price +/-</param>
        public void TestIncreaseNewPrice(double increment) {
            _newPrice += increment;
            UpdateDisplay();
        }


        /// <summary>
        /// updates the UI with new values without animating
        /// </summary>
        public void Update() {
            CurrentPriceText = $"${_newPrice:###0.00}";

            double coins = Double.Parse(CoinCount);
            double usd = _newPrice * coins;
            double profit = usd - (Double.Parse(EntryPrice.Substring(1)) * coins);
            System.Diagnostics.Debug.WriteLine($"Profit = {profit}");
            USDHoldings = $"${usd:###0.00}";
            Profit = $"{profit:+$###0.00;-$###0.00}";
        }

        /// <summary>
        /// update the USD total
        /// </summary>
        public void UpdateTotals() {
            USDHoldings = $"${_newPrice * Double.Parse(CoinCount):###0.00}";
            UpdateProfit();
        }

        /// <summary>
        /// increments the shown price value to create scrolling animation on UI
        /// </summary>
        public void AnimateMoveTowardNewPrice() {

            //move last price toward new price incrementally
            //until last price is within 3 cents of the new price
            if (_lastPrice < _newPrice - 0.03 || _lastPrice > _newPrice + 0.03) {
                _lastPrice += _priceIncrementValue;
            } else {
                _lastPrice = _newPrice;
            }

            //fade out background while animating new price
            if (_borderBackgroundColorA > 0) {
                _borderBackgroundColorA -= 1;
                BorderBackgroundColor = Color.FromArgb(_borderBackgroundColorA, _borderBackgroundColorR, _borderBackgroundColorG, _borderBackgroundColorB);
            } else {
                _lastPrice = _newPrice;
                IsAnimating = false;
            }

            UpdateCurrentText();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// update the info displayed on the UI
        /// </summary>
        private void UpdateDisplay() {
            System.Diagnostics.Debug.WriteLine($"New price found for {CoinName} = {_newPrice}");
            IsAnimating = true;

            //set UI elements based on whether price is increasing or decreasing
            if (_newPrice > _lastPrice == true) {
                CurrentPriceTextColor = "Green";
                BorderBrushColor = "DarkGreen";
                _borderBackgroundColorA = 50;
                _borderBackgroundColorR = 15;
                _borderBackgroundColorG = 242;
                _borderBackgroundColorB = 50;
                BorderBackgroundColor = Color.FromArgb(_borderBackgroundColorA, _borderBackgroundColorR, _borderBackgroundColorG, _borderBackgroundColorB);
                _priceIncrementValue = 0.01;
            } else {
                CurrentPriceTextColor = "Red";
                BorderBrushColor = "DarkRed";
                _borderBackgroundColorA = 50;
                _borderBackgroundColorR = 252;
                _borderBackgroundColorG = 52;
                _borderBackgroundColorB = 17;
                BorderBackgroundColor = Color.FromArgb(_borderBackgroundColorA, _borderBackgroundColorR, _borderBackgroundColorG, _borderBackgroundColorB);
                _priceIncrementValue = -0.01;
            }
        }

        /// <summary>
        /// update the info shown on screen
        /// </summary>
        /// <param name="price"></param>
        private void UpdateCurrentText() {
            CurrentPriceText = $"${_lastPrice:###0.00}";
            USDHoldings = $"${_newPrice*Double.Parse(CoinCount):###0.00}";
        }

        /// <summary>
        /// update profit based on current values
        /// </summary>
        private void UpdateProfit() {
            double coins = Double.Parse(CoinCount);
            double usd = _newPrice * coins;
            double profit = usd - (Double.Parse(EntryPrice.Substring(1)) * coins);
            Profit = $"{profit:+$###0.00;-$###0.00}";
        }


        /// <summary>
        /// set the price from the main
        /// ***NOTE*** doing this the ugly way for now, need to come back and change later
        /// </summary>
        private void SetPriceFromApi() {

            //get json string from the url
            String jsonString = null;
            try {
                using (WebClient wc = new WebClient()) {
                    jsonString = wc.DownloadString($"{_baseApiUrl}{CoinName}&tsyms=USD");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Exception thrown for {CoinName} - {ex.Message}");
                jsonString = null;
            }


            if (jsonString != null) {
                JObject json = JObject.Parse(jsonString);
                if (json["USD"] != null) {
                    _newPrice = (double)json["USD"];
                } else {
                    System.Diagnostics.Debug.WriteLine($"API ERROR for {CoinName} - {json["error"]}");
                }
            }
        }

        /// <summary>
        /// set price from the api
        /// </summary>
        private void SetPriceFromAlternateApi() {
            //get the ticker from the api
            var ticker = GetTickerFromAlternateApi();
            if (ticker == null) {
                System.Diagnostics.Debug.WriteLine("api get failed, price not updated");
                CurrentPriceTextColor = "DarkOrange";
                return;
            }

            //update the data from the json
            _newPrice = (double)ticker["price"];
        }

        /// <summary>
        /// get the current ticker from the api url
        /// </summary>
        /// <returns>the ticker as a Newtonsoft.Json JObject</returns>
        private JObject GetTickerFromAlternateApi() {

            //get json string from the url
            String jsonString = null;
            try {
                using (WebClient wc = new WebClient()) {
                    jsonString = wc.DownloadString($"{_baseAlternateApiUrl}{CoinName}-usd");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Exception received for {CoinName} - {ex.Message}");
                jsonString = null;
            }


            if (jsonString != null) {
                try {
                    JObject json = JObject.Parse(jsonString);
                    if ((bool)json["success"] == true) {
                        return (JObject)json["ticker"];
                    } else {
                        System.Diagnostics.Debug.WriteLine($"API ERROR for {CoinName} - {json["error"]}");
                    }
                } catch (JsonReaderException ex) {
                    System.Diagnostics.Debug.WriteLine($"Exception received for {CoinName} = {ex.Message}");
                }
                
            }

            return null;
        }

        #endregion

    }
}