using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPriceWatcher {

    public class DBConnection {

        private String _connectionString;

        public DBConnection() {
            _connectionString = Properties.Settings.Default.DBConnectionString;
        }

        public List<CoinInfo> GetCoins() {

            List<CoinInfo> coins = new List<CoinInfo>();

            System.Diagnostics.Debug.WriteLine("Attempting to connect to db...");
            System.Diagnostics.Debug.WriteLine($"Connection string = {_connectionString}");
            try {
                using (SqlConnection connection = new SqlConnection(_connectionString)) {

                    SqlCommand cmd = new SqlCommand();

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

    }
}
