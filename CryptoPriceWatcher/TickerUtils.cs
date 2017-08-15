using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoPriceWatcher {
    public static class TickerUtils {

        /// <summary>
        /// returns a queue of indices in random order
        /// </summary>
        /// <param name="tickerCount">the number of tickers</param>
        /// <returns></returns>
        public static Queue<int> GetRandomIndexQueue(int tickerCount) {
            int[] ordered = Enumerable.Range(0, tickerCount).ToArray<int>();
            Random rng = new Random();
            for (int i = ordered.Length-1; i > 0; i--) {
                int randomIndex = rng.Next(i);
                int temp = ordered[randomIndex];
                ordered[randomIndex] = ordered[i];
                ordered[i] = temp;
            }
            return new Queue<int>(ordered);
        }

        /// <summary>
        /// get readable string of queue for debugging
        /// </summary>
        /// <returns></returns>
        public static String StringifyQueue(Queue<int> queue) {
            String res = "[";
            foreach (int num in queue) {
                res += num + ",";
            }
            return res + "]";
        }

    }
}
