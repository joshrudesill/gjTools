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
        List<CustomBlurb> queryCustomBlurbs(bool custom, string customCommand = "");  // Definitions in summary. Query all items, if true is passed and a string do custom query.
        List<JobSlot> queryJobSlots(bool custom = false, string customCommand = "");
        List<Location> queryLocations(bool custom = false, string customCommand = "");
        List<OEMColor> queryOEMColors(bool custom = false, string customCommand = "");
        List<VariableData> queryVariableData(bool custom = false, string customCommand = "");
        void executeCommand(string command); // Execute non query command on database.
    }

    // Custom structs for return types from database
    public struct CustomBlurb
    {
        public CustomBlurb(int id, string blurb)
        {
            _id = id;
            _blurb = blurb;
        }
        private readonly int _id;
        private readonly string _blurb;
        public string blurb
        {
            get
            {
                return _blurb;
            }
            
        }
        public int id
        {
            get
            {
                return _id;
            }
        }
    }
    public struct OEMColor
    {
        public OEMColor(string colorNum, string colorName, int id)
        {
            _colorNum = colorNum;
            _colorName = colorName;
            _id = id;
        }
        private readonly string _colorNum;
        private readonly string _colorName;
        private readonly int _id;

        public string colorName 
        {
            get
            {
                return _colorName;
            }
        }
        public string colorNum
        {
            get
            {
                return _colorNum;
            }
        }
        public int id
        {
            get
            {
                return _id;
            }
        }
    }
    public struct JobSlot
    {
        public JobSlot(int slot, string job, string due, string description, int quantity, string material)
        {
            _slot = slot;
            _job = job;
            _due = due;
            _description = description;
            _quantity = quantity;
            _material = material;
        }
        private readonly int _slot;
        private readonly string _job;
        private readonly string _due;
        private readonly string _description;
        private readonly int _quantity;
        private readonly string _material;

        public int slot
        {
            get
            {
                return _slot;
            }
        }
        public string job
        {
            get
            {
                return _job;
            }
        }
        public string due
        {
            get
            {
                return _due;
            }
        }
        public string description
        {
            get
            {
                return _description;
            }
        }
        public int quantity
        {
            get
            {
                return _quantity;
            }
        }
        public string material
        {
            get
            {
                return _material;
            }
        }

    }
    public struct Location
    {
        public Location(string locName, string path, int id)
        {
            _locName = locName;
            _path = path;
            _id = id;
        }
        private readonly string _locName;
        private readonly string _path;
        private readonly int _id;

        public string locName
        {
            get
            {
                return _locName;
            }
        } 
        public string path
        {
            get
            {
                return _path;
            }
        }
        public int id
        {
            get
            {
                return _id;
            }
        }
    }
    public struct VariableData
    {
        public VariableData(string userLastName, string userFirstName, string userInitials, int cutNumber)
        {
            _userFirstName = userFirstName;
            _userLastName = userLastName;
            _userInitials = userInitials;
            _cutNumber = cutNumber;
        }
        private readonly string _userLastName;
        private readonly string _userFirstName;
        private readonly string _userInitials;
        private readonly int _cutNumber;

        public string userLastName => _userLastName;
        public string userFirstName => _userFirstName;
        public string userInitials => _userInitials;
        public int cutNumber => _cutNumber;
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
        /// This function will return a CustomBlurb object. 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para>
        /// </summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<CustomBlurb> queryCustomBlurbs()
        {
            List<CustomBlurb> rList = new List<CustomBlurb>();
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
                var blrb = new CustomBlurb(r.GetInt32(0), r.GetString(1));
                rList.Add(blrb);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a JobSlot object. 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<JobSlot> queryJobSlots()
        {
            var rList = new List<JobSlot>();
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
                var blrb = new JobSlot(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetString(5));
                rList.Add(blrb);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a Location object. 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<Location> queryLocations()
        {
            var rList = new List<Location>();
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
                var blrb = new Location(r.GetString(0), r.GetString(1), r.GetInt32(2));
                rList.Add(blrb);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a OEMColor object. 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<OEMColor> queryOEMColors()
        {
            List<OEMColor> rList = new List<OEMColor>();
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
                var oemColor = new OEMColor(r.GetString(0), r.GetString(1), r.GetInt32(2));
                rList.Add(oemColor);
            }
            return rList;
        }
        /// <summary>
        /// This function will return a VariableData object. 
        /// <para>Use true and a custom string as parameters for custom queries.</para>
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <param name="custom"></param>
        /// <param name="customCommand"></param>
        /// <returns></returns>
        public List<VariableData> queryVariableData()
        {
            var rList = new List<VariableData>();
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
                var blrb = new VariableData(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3));
                rList.Add(blrb);
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
