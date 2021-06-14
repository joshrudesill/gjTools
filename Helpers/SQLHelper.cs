using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Rhino;
using Rhino.Commands;
namespace gjTools
{
    interface ISQLHelper
    {
        void testConnection(); // Test connection. For debugging only.
        List<object> queryCustomBlurbs(bool custom, string customCommand = ""); 
    }

    
    public sealed class SQLHelper : ISQLHelper
    {
        SQLiteConnection con;
        string connectionString;

        
        public SQLHelper()
        {
            connectionString = "Data Source=gjTools.db";
            con = new SQLiteConnection(connectionString);
            con.Open();
        }
        ~SQLHelper()
        {
            con.Close();
        }
        public void testConnection()
        {
            string stm = "SELECT SQLITE_VERSION()";
            var cmd = new SQLiteCommand(stm, con);
            string version = cmd.ExecuteScalar().ToString();
            RhinoApp.WriteLine($"SQLite version: {version}");
        }
        
        public List<object> queryCustomBlurbs(bool custom = false, string customCommand = "")
        {
            string stm = "SELECT * FROM customBlurbs;";
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {

            }
            return new List<object>();
        }

        private SQLiteDataReader executeQuery(string command)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, con);
            SQLiteDataReader r = cmd.ExecuteReader();
            return r;
        }

    }
}
