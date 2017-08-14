using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CryptoPriceWatcher {

    public class DBConnection {

        private String _connectionString;

        public DBConnection() {
            _connectionString = Properties.Settings.Default.DBConnectionString;
        }

        /// <summary>
        /// get coins to be tracked
        /// </summary>
        /// <returns>a list of CoinInfo objects for each coin</returns>
        public List<CoinInfo> GetCoins() {

            List<CoinInfo> coins = new List<CoinInfo>();

            System.Diagnostics.Debug.WriteLine("Connecting to db...");
            try {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand()) {

                    cmd.CommandText = "SELECT * FROM CoinTable";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Connection = connection;

                    connection.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        coins.Add(new CoinInfo(reader.GetString(1), reader.GetDouble(2), reader.GetDouble(3)));
                    }
                    reader.Close();
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error accessing database! {ex}");
            }
            return coins;
        }

        /// <summary>
        /// update the database with the given list of coins
        /// ***NOTE*** clears table completely and re-adds all coins
        /// </summary>
        /// <param name="coins"></param>
        public void UpdateCoins(List<CoinInfo> coins) {
            System.Diagnostics.Debug.WriteLine("Connecting to db...");
            try {
                using (SqlConnection connection = new SqlConnection(_connectionString)) {

                    //create clear command
                    SqlCommand clearCmd = new SqlCommand();
                    clearCmd.CommandText = "TRUNCATE TABLE CoinTable";
                    clearCmd.CommandType = System.Data.CommandType.Text;
                    clearCmd.Connection = connection;

                    connection.Open();

                    //first clear out table
                    clearCmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Table cleared! adding new coin info..");

                    //then insert new coin info into table
                    String insertCmdString = "INSERT INTO CoinTable (CoinName,Count,EntryPrice) VALUES (@val1, @val2, @val3)";
                    foreach (CoinInfo coin in coins) {
                        SqlCommand insertCmd = new SqlCommand();
                        insertCmd.CommandText = insertCmdString;
                        insertCmd.Parameters.AddWithValue("@val1", coin.Name);
                        insertCmd.Parameters.AddWithValue("@val2", coin.Count);
                        insertCmd.Parameters.AddWithValue("@val3", coin.EntryPrice);
                        insertCmd.Connection = connection;

                        insertCmd.ExecuteNonQuery();
                    }
                    System.Diagnostics.Debug.WriteLine("New coin info added!");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Error accessing database! {ex}");
            }

        }
    }
}
