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
        List<object> queryCustomBlurbs(bool custom, string customCommand = "");  // Definitions in summary. Query all items, if true is passed and a string do custom query.
        List<object> queryJobSlots(bool custom = false, string customCommand = "");
        List<object> queryLocations(bool custom = false, string customCommand = "");
        List<object> queryOEMColors(bool custom = false, string customCommand = "");
        List<object> queryVariableData(bool custom = false, string customCommand = "");
        void executeCommand(string command); // Execute non query command on database.
    }
    
    public sealed class SQLHelper : ISQLHelper
    {
        SQLiteConnection con;
        string connectionString;
        
        public SQLHelper()
        {
            connectionString = "Data Source=gToolsDatabase.db";
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

        /// <summary>
        /// This function will return a 2D list. Each child list within will contain values matching the style exactly of the table. (int, string). 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para>
        /// </summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<object> queryCustomBlurbs(bool custom = false, string customCommand = "")
        {
            List<object> rList = new List<object>();
            string stm;
            if (custom && customCommand != "")
            {
                stm = customCommand;
            } 
            else
            {
                stm = "SELECT * FROM customBlurbs;";
            }

            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                List<object> olist = new List<object> { r.GetInt32(0), r.GetString(1) };
                rList.Add(olist);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a 2D list. Each child list within will contain values matching the style exactly of the table. (int, string, string, string, int, string). 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<object> queryJobSlots(bool custom = false, string customCommand = "")
        {
            List<object> rList = new List<object>();
            string stm;
            if (custom && customCommand != "")
            {
                stm = customCommand;
            }
            else
            {
                stm = "SELECT * FROM jobSlots;";
            }
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                List<object> olist = new List<object> { r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetString(5) };
                rList.Add(olist);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a 2D list. Each child list within will contain values matching the style exactly of the table. (string, string, int). 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<object> queryLocations(bool custom = false, string customCommand = "")
        {
            List<object> rList = new List<object>();
            string stm;
            if (custom && customCommand != "")
            {
                stm = customCommand;
            }
            else
            {
                stm = "SELECT * FROM locations;";
            }
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                List<object> olist = new List<object> { r.GetString(0), r.GetString(1), r.GetInt32(2) };
                rList.Add(olist);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a 2D list. Each child list within will contain values matching the style exactly of the table. (string, string, int). 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<object> queryOEMColors(bool custom = false, string customCommand = "")
        {
            List<object> rList = new List<object>();
            string stm;
            if (custom && customCommand != "")
            {
                stm = customCommand;
            }
            else
            {
                stm = "SELECT * FROM oemColors;";
            }
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                List<object> olist = new List<object> { r.GetString(0), r.GetString(1), r.GetInt32(2) };
                rList.Add(olist);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a 2D list. Each child list within will contain values matching the style exactly of the table. (string, string, string, int). 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<object> queryVariableData(bool custom = false, string customCommand = "")
        {
            List<object> rList = new List<object>();
            string stm;
            if (custom && customCommand != "")
            {
                stm = customCommand;
            }
            else
            {
                stm = "SELECT * FROM variableData;";
            }
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                List<object> olist = new List<object> { r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3) };
                rList.Add(olist);
            }
            return rList;
        }

        private SQLiteDataReader executeQuery(string command) 
        {
            SQLiteCommand cmd = new SQLiteCommand(command, con);
            SQLiteDataReader r = cmd.ExecuteReader();
            return r;
        }
        /// <summary>
        /// Executes a non-query command on the database.
        /// </summary>
        /// <param name="command"></param>
        public void executeCommand(string command)
        {
            var cmd = new SQLiteCommand(con);
            cmd.CommandText = command;
            cmd.ExecuteNonQuery();
        }

    }
}
