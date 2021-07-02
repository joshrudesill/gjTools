using System.Collections.Generic;
using System.Data.SQLite;
using Rhino;

namespace gjTools
{
    interface ISQLHelper
    {
        void testConnection(); // Test connection. For debugging only.
        List<CustomBlurb> queryCustomBlurbs();  // Definitions in summaries. Query all items.
        List<JobSlot> queryJobSlots();
        List<Location> queryLocations();
        List<OEMColor> queryOEMColors();
        List<VariableData> queryVariableData();
        int executeCommand(string command); // Execute non query command on database.
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
        private string _userLastName;
        private string _userFirstName;
        private string _userInitials;
        private int _cutNumber;

        public string userLastName
        {
            get
            {
                return _userLastName;
            }
            set
            {
                _userLastName = value;
            }
        }
            
        public string userFirstName
        {
            get
            {
                return _userFirstName;
            }
            set
            {
                _userFirstName = value;
            }
        }
            
        public string userInitials
        {
            get
            {
                return _userInitials;
            }
            set
            {
                _userInitials = value;
            }
        }
        public int cutNumber
        {
            get
            {
                return _cutNumber;
            }
            set
            {
                _cutNumber = value;
            }
        }
    }

    public struct DataStore
    {
        private int _index;
        public string stringValue;
        public int intValue;
        public double doubleValue;
        public DataStore(int i, string sv, int iv, double dv)
        {
            _index = i;
            stringValue = sv;
            intValue = iv;
            doubleValue = dv;
        }
        public int DBindex
        {
            get
            {
                return _index;
            }
        }
    }

    public sealed class SQLTools : ISQLHelper
    {
        SQLiteConnection con;
        string connectionString;
        
        public SQLTools()
        {
            connectionString = "Data Source=gToolsDatabase.db";
            con = new SQLiteConnection(connectionString);
            con.Open();
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
        /// <para>---Warning: must return all columns in order!---</para>
        /// </summary>
        /// <returns></returns>
        public List<CustomBlurb> queryCustomBlurbs()
        {
            List<CustomBlurb> rList = new List<CustomBlurb>();
            string stm;
            stm = "SELECT * FROM customBlurbs;";

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
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <returns></returns>
        public List<JobSlot> queryJobSlots()
        {
            var rList = new List<JobSlot>();
            string stm;
            stm = "SELECT * FROM jobSlots";
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                var blrb = new JobSlot(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), (string)r.GetValue(5));
                rList.Add(blrb);
            }
            return rList;
        }


        /// <summary>
        /// This function will return a Location object. 
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <returns></returns>
        public List<Location> queryLocations()
        {
            var rList = new List<Location>();
            string stm = "SELECT * FROM locations;";
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
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <returns></returns>
        public List<OEMColor> queryOEMColors()
        {
            List<OEMColor> rList = new List<OEMColor>();
            string stm = "SELECT * FROM oemColors;";
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                var oemColor = new OEMColor(r.GetString(0), r.GetString(1), r.GetInt32(2));
                rList.Add(oemColor);
            }
            return rList;
        }
        public List<OEMColor> queryOEMColors(string search)
        {
            var res = new List<OEMColor>();
            string que = string.Format("SELECT * FROM oemColors WHERE colorNum LIKE \"%{0}%\"", search);
            var r = executeQuery(que);
            while (r.Read())
            {
                res.Add(new OEMColor((string)r.GetValue(0), (string)r.GetValue(1), r.GetInt32(2)));
            }
            return res;
        }


        /// <summary>
        /// This function will return a VariableData object. 
        /// <para>---Warning: must return all columns in order!---</para></summary>
        /// <returns></returns>
        public List<VariableData> queryVariableData()
        {
            var rList = new List<VariableData>();
            string stm  = "SELECT * FROM variableData;";
            SQLiteDataReader r = executeQuery(stm);
            while (r.Read())
            {
                var blrb = new VariableData(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3));
                rList.Add(blrb);
            }
            return rList;
        }


        /// <summary>
        /// Returns a list of DataStore objects based on input DB ids
        /// <para>1-11 PrototypeTools</para>
        /// </summary>
        /// <param name="indexList"></param>
        /// <returns></returns>
        public List<DataStore> queryDataStore(List<int> indexList)
        {
            if (indexList.Count == 0)
                return null;

            var ds = new List<DataStore>();
            string que = "SELECT * FROM dataStore WHERE id IN (" + indexList[0].ToString();
            if (indexList.Count > 1)
            {
                foreach (var i in indexList)
                {
                    que += ", " + i.ToString();
                }
            }
            que += ")";

            SQLiteDataReader r = executeQuery(que);
            while (r.Read())
            {
                ds.Add(new DataStore(r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetFloat(3)));
            }
            return ds;
        }


        /// <summary>
        /// updates the dataStore Values
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool updateDataStore(DataStore data)
        {
            string que = string.Format("UPDATE dataStore SET string = '{1}', int = {2}, float = {3} WHERE id = {0}", 
                data.DBindex, data.stringValue, data.intValue, data.doubleValue);
            if (executeCommand(que) == 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// insert row into dataStore
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool insertDataStore(DataStore data)
        {
            string que = string.Format("INSERT INTO dataStore (string, int, float) VALUES ('{0}', {1}, {2})",
                data.stringValue, data.intValue, data.doubleValue);
            if (executeCommand(que) == 0)
                return false;
            else
                return true;
        }


        /// <summary>
        /// Takes a custom blurb object and updates based on ID.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool updateCustomBlurb(CustomBlurb c)
        {
            string s = string.Format("UPDATE customBlurbs SET blurb = '{0}' WHERE id = '{1}';", c.blurb, c.id);
            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Takes a Job Slot object and updates based on slot.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool updateJobSlot(JobSlot c)
        {
            string s = string.Format(
                "UPDATE jobSlots SET slot = {0}, job = '{1}', due = '{2}', description = '{3}', qty = {4}, material = '{5}' WHERE slot = {0}", 
                c.slot, c.job, c.due, c.description, c.quantity, c.material
            );

            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Takes a Location object and updates based on id.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool updateLocation(Location c)
        {
            string s = string.Format("UPDATE locations SET locName = '{0}', path = '{1}, id = '{2}' WHERE id = '{3}';", c.locName, c.path, c.id, c.id);
            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Takes a OEMColor object and updates based on id.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool updateOemColor(OEMColor c)
        {
            string s = string.Format("UPDATE oemColors SET colorNum = '{0}', colorName = '{1}, id = '{2}' WHERE id = '{3}';", c.colorNum, c.colorName, c.id, c.id);
            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Inserts a custom oemcolor object into the database.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool insertOemColor(OEMColor c)
        {
            string s = string.Format("INSERT INTO oemColors (colorNum, colorName, id) VALUES ('{0}', '{1}', '{2}');", c.colorNum, c.colorName, c.id);
            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
        }

        public bool updateVariableData(VariableData c)
        {
            string s = string.Format("UPDATE variableData SET userLastName = '{0}', userFirstName = '{1}', userInitials = '{2}', cutNumber = '{3}';", c.userLastName, c.userFirstName, c.userInitials, c.cutNumber);
            int r = executeCommand(s);
            if (r == 0)
            {
                return false;
            }
            return true;
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
        public int executeCommand(string command)
        {
            var cmd = new SQLiteCommand(con);
            cmd.CommandText = command;
            return cmd.ExecuteNonQuery();
        }

    }
}
