namespace CryptoPriceWatcher {

    /// <summary>
    /// the basic information for a cryptocurrency
    /// </summary>
    public class CoinInfo {

        /// <summary>
        /// the name of the coin
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the amount of coin held
        /// </summary>
        public double Count { get; set; }

        /// <summary>
        /// the average buy price for the coin
        /// </summary>
        public double EntryPrice { get; set; }

        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="count"></param>
        /// <param name="entryPrice"></param>
        public CoinInfo(string name, double count, double entryPrice) {

            this.Name = name;
            this.Count = count;
            this.EntryPrice = entryPrice;
        }

    }
}
