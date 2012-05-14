using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.Odbc;

namespace accounts
{
    //////////////////////////////////////////////////////////////////////////////////
    //     user class - contains methods + procedures to interact with user accounts
    //     calls database methods
    //////////////////////////////////////////////////////////////////////////////////
    class user
    {

        string username;
        string passwd;
        string surname;
        string name;
        string email;
        string tel;
        string exists;
        //////////////////////////////////////////////////////////////////////////////////
        // procedure creates user after checking if user does already exist.
        // procedure uses odbc database method to determine if username exists
        // check takes place after username + password are put in
        //////////////////////////////////////////////////////////////////////////////////
        public void createUser(string username, string passwd)
        {
            this.username = username;
            this.passwd = passwd;
            string query = "select * from userdb.csv where username='" + this.username + "';";
            exists = database.sqlExecute(query, "check");
            if (exists != "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!! WARNING user exists !!!!");
                Console.WriteLine("please choose another username");
                Console.ResetColor();
            }
            else
            {
                string[] data = { this.username + "," + this.passwd + ",,,," };
                database.writeData(data);
                Console.WriteLine("user " + this.username + " created");
            }
        }
        //////////////////////////////////////////////////////////////////////////////////
        // login procedure sets login state in main program
        // aborts after 3 bad tries
        //////////////////////////////////////////////////////////////////////////////////
        public void loginUser()
        {
            bool authenticated = false;
            int count = 1;
            while (authenticated == false && count < 4)
            {
                Console.WriteLine("please input Username");
                this.username = Console.ReadLine();
                Console.WriteLine("please input password");
                this.passwd = Console.ReadLine();
                Console.WriteLine("checking credentials");
                string query = "select passwd from userdb.csv where username='" + this.username + "';";
                string pwd = database.sqlExecute(query, "check");
                if (this.passwd == pwd)
                {
                    authenticated = true;
                    accountAdministration.loggedin = true;
                    accountAdministration.user = username;
                    accountAdministration.pwd = pwd;
                    Console.Clear();
                    Console.WriteLine("Hello "+username+" You are now logged in");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("wrong username or password try again");
                    Console.ResetColor();
                    count++;
                }
            }
        }
        //////////////////////////////////////////////////////////////////////////////////
        // logout procedure sets login state in main program
        // resets credentials in main program
        //////////////////////////////////////////////////////////////////////////////////
        public void logoutUser() {

            accountAdministration.loggedin = false;
            accountAdministration.user = "";
            accountAdministration.pwd = "";
            Console.WriteLine("logged out");
        }
        //////////////////////////////////////////////////////////////////////////////////
        // user editing procedure expects data from main program
        // calls array to file writing procedure
        //////////////////////////////////////////////////////////////////////////////////
        public void editUser(string username, string passwd, string surname, string name, string email, string tel)
        {
            this.username = username;
            this.passwd = passwd;
            this.surname = surname;
            this.name = name;
            this.email = email;
            this.tel = tel;
            var tmp=database.readFiletoArray();
            int index = 0;
            string [] tmpuser;
            int length= database.countLinesInFile();
            tmpuser = new string [length];
            foreach (string u in tmp) {
                if (u.StartsWith(username)){
                    tmpuser[index] = this.username + "," + this.passwd + "," + this.surname + "," + this.name + "," + this.email + "," + this.tel;                
                } 
                else {
                    tmpuser [index] = u;                    
                }
                index++;
            }
            database.writeArraytoFile(tmpuser);
            Console.Clear();
            Console.WriteLine("user edited");
        }
        //////////////////////////////////////////////////////////////////////////////////
        // user deleting procedure expects username from main program
        // calls array to file writing procedure
        //////////////////////////////////////////////////////////////////////////////////
        public void deleteUser(string username)
        {
            var tmp=database.readFiletoArray();
            int index = 0;
            string [] tmpuser;
            int length= database.countLinesInFile() -1;
            tmpuser = new string [length];
            foreach (string u in tmp) {
                if (u.StartsWith(username)) continue;
                else {
                    tmpuser [index] = u;
                    index++;
                    }
                
            }
            Console.WriteLine("if you are sure to delete account press (y)");
            Console.WriteLine("press any other key to abort");
            string state;
            state = Console.ReadLine();
            if (state == "y")
            {
                database.writeArraytoFile(tmpuser);
                Console.Clear();
                Console.WriteLine("user deleted");
                accountAdministration.loggedin = false;
            }
            else {
                Console.WriteLine("account deletion aborted");
                Console.WriteLine("You can edit Your account now if You want");
            }            
        }
    }
    //////////////////////////////////////////////////////////////////////////////////
    // database class
    // 
    //////////////////////////////////////////////////////////////////////////////////
    class database
    {
        static string result;
        static string dbName = "userdb.csv";
        static string basepath = Directory.GetCurrentDirectory();
        public static string dbFile = System.IO.Path.Combine(basepath, dbName);
        /////////////////////////////////////////////////////////////////////////
        /// procedure checks for database file + creates it if it does not exist
        /////////////////////////////////////////////////////////////////////////
        public static void checkDB()
        {
            if (!File.Exists(dbFile))
            {
                Console.WriteLine("database file " + dbFile + " does not exist, creating it");

                //create file + close it to avoid file still open exception
                System.IO.FileStream afile = System.IO.File.Create(dbFile);
                afile.Close();
                afile = null;

                //define header of csv
                string[] header = { "username,passwd,surname,name,email,telephone" };
                //database.writeArraytoFile(header);
                // write header to file
                writeData(header);

            }
            else
            {
                Console.WriteLine("database file " + dbFile + " exists. using it as database");
            }
        }
        ////////////////////////////////////////////////////////////////
        /// method connects to database executes query and returns result
        ////////////////////////////////////////////////////////////////
        public static string sqlExecute(string query, string action)
        {
            OdbcConnection conn = new OdbcConnection();
            conn.ConnectionString = @"Driver={Microsoft Text Driver (*.txt; *.csv)};" + "Dbq=" + basepath + ";" + "Extensions=asc,csv,tab,txt;";
            conn.Open();
            try
            {
                OdbcCommand oCmd = new OdbcCommand(query, conn);
                if (action == "read")
                {
                    OdbcDataReader oDR = oCmd.ExecuteReader();
                    object[] meta = new object[10];
                    bool read;
                    if (oDR.Read() == true)
                    {
                        do
                        {
                            int NumberOfColums = oDR.GetValues(meta);

                            for (int i = 0; i < NumberOfColums; i++)
                                result += "{0} " + meta[i].ToString();

                            Console.WriteLine();
                            read = oDR.Read();
                        } while (read == true);
                    }
                    oDR.Close();
                    oDR.Dispose();
                }
                if (action == "check")
                {
                    OdbcDataReader oDR = oCmd.ExecuteReader();
                    if (oDR.Read() == true)
                    {
                        result = oDR.GetString(0);
                    }
                    else
                    {
                        result = "";
                    }
                    oDR.Close();
                    oDR.Dispose();
                }

                if (action == "insert") {
                    oCmd.ExecuteNonQuery();                
                }
                //close reader and object;
                oCmd.Dispose();

            }
            catch (OdbcException e)
            {

                string errorMessages = "";

                for (int i = 0; i < e.Errors.Count; i++)
                {
                    errorMessages += "Index #" + i + "\n" +
                                     "Message: " + e.Errors[i].Message + "\n" +
                                     "NativeError: " + e.Errors[i].NativeError.ToString() + "\n" +
                                     "Source: " + e.Errors[i].Source + "\n" +
                                     "SQL: " + e.Errors[i].SQLState + "\n";
                }

                Console.WriteLine("command: " + query + "  failed - " + errorMessages);
            }
            finally
            {
                conn.Close();
                conn.Close();
                conn.Dispose();
            }
            return result;
        }
        public static int countLinesInFile()
        {
            int count = 0;
            using (StreamReader sr = new StreamReader(dbFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    count++;
                }
            }
            return count;
        }
        //////////////////////////////////////////////////////////////////////////////////
        // reading file to array method
        // returns array containing current database
        //////////////////////////////////////////////////////////////////////////////////
        public static string[] readFiletoArray()
        {

            String[] FileContent = File.ReadAllLines(dbFile);


            //debug
            /*int LineNo = 0;
            foreach (string Line in FileContent)
            {
                LineNo += 1;
                //debug
                Console.WriteLine(Line);
            }*/
            return FileContent;
        }
        //////////////////////////////////////////////////////////////////////////////////
        // writing edited array to file procedure
        // expects array
        //////////////////////////////////////////////////////////////////////////////////
        public static void writeArraytoFile(string[] FileContent)         
        {
            System.IO.File.WriteAllLines(dbFile, FileContent);
        }
        public static void writeData(string[] data)
        {

            // write data to file
            try
            {
                using (StreamWriter sw = File.AppendText(dbFile))
                {
                    foreach (string row in data)
                    {
                        sw.WriteLine(row);
                    }
                    sw.Close();
                }
            }
            catch (IOException e)
            {
                string errorMessages = "writing file failed ";
                errorMessages += e.GetType().Name + " - " + e.Message;
                Console.WriteLine(errorMessages);
            }
        }
    }
    //////////////////////////////////////////////////////////////////////////////////
    // class to process data strings
    // calls value checking method
    // expects kind of value to be processed from main program
    //////////////////////////////////////////////////////////////////////////////////
    class processData {

        public static string getValue(string valueKind)
        {
            bool valid = false;
            string value="";
            int count = 0;
            while (valid == false && count < 4)
            {
                Console.WriteLine("tries: " + count);
                Console.WriteLine("please input " + valueKind + " :");
                value = Console.ReadLine();
                valid = checkValue(valueKind, value);
                if (valid == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(valueKind + " not valid pse try again");
                    Console.ResetColor();
                    count++;
                }
            }
            if (count == 4)
            {
                Console.WriteLine("too many wrong tries - please start again");
                value = "";
            }
            return value;
        }
        //////////////////////////////////////////////////////////////////////////////////
        // data checking method expects value + regular expression to check against
        // called by valuecheck method
        // returns boolean if expression matches
        //////////////////////////////////////////////////////////////////////////////////
        public static bool checkString(string value, string strRegex)
        {
            return Regex.IsMatch(value, strRegex);
        }
        //////////////////////////////////////////////////////////////////////////////////
        // value checking method
        // calls check string method
        //////////////////////////////////////////////////////////////////////////////////
        public static bool checkValue(string valueKind, string value)
        {
            bool valid = false;
            Console.ForegroundColor = ConsoleColor.Blue;
            switch (valueKind)
            {
                case "username":
                    Console.WriteLine("checking username");
                    valid = checkString(value, "([a-zA-Z0-9]{4,10}$)");
                    break;
                case "passwd":
                    Console.WriteLine("checking passwd");
                    valid = checkString(value, "(^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])(?!.*s).*$)");
                    break;
                case "surname":
                    Console.WriteLine("checking surname");
                    valid = checkString(value, "(^[A-Za-z]*$)");
                    break;
                case "name":
                    Console.WriteLine("checking name");
                    valid = checkString(value, "(^[A-Za-z,.-]*$)");
                    break;
                case "email":
                    Console.WriteLine("checking email");
                    valid = checkString(value, @"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*"
                    + "@"
                    + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
                    break;
                default:
                    Console.WriteLine("checking Telephone");
                    valid = checkString(value, "(^[0-9]{6,15}$)");
                    break;
            }
            Console.ResetColor();
            return valid;
        }   
    }
    //////////////////////////////////////////////////////////////////////////////////////
    /// main class contains menu 
    //////////////////////////////////////////////////////////////////////////////////////
    class accountAdministration
    {
        #region initalize

        public bool inrange = false;
        static public bool valid = false;
        static public bool loggedin = false;
        static public string user;
        static public string pwd;
        static public string state;

        #endregion

        static void Main(string[] args)
        {
            //initialize user object to be able to call methods neccessary to process requests
            user newUser = new user();
            //set window size
            Console.SetWindowSize(140,40);
            bool run = true;
            database.checkDB();
            //debug
            //database.sqlExecute("select username from userdb.csv", "read");
            string task;

            #region main menu

            while (run == true)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine("¦   account administration  ¦");
                Console.WriteLine("-----------------------------");
                Console.WriteLine("please choose task");
                // menu part of program
                // depending on status of user different menus are shown
                if (loggedin == false)
                {
                    Console.WriteLine("(c) create account");
                    Console.WriteLine("(l) login");
                    Console.WriteLine("(d) delete account");
                }
                if (loggedin == true)
                {
                    Console.WriteLine("(a) administer account");
                    Console.WriteLine("(l) logout");
                }
                Console.WriteLine("(x) exit");
                task = Console.ReadLine();
                switch (task)
                {
                    #region create user
                    case "c":
                        Console.WriteLine("----------------------");
                        Console.WriteLine("¦   create account   ¦");
                        Console.WriteLine("----------------------");
                        Console.WriteLine("press (y) to create a new account");
                        Console.WriteLine("press any other key to abort");
                        state = Console.ReadLine();
                        if (state == "y")
                        {
                            Console.WriteLine("creating your account");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Attention: Your username MUST be 4 to 10 letters long");
                            Console.WriteLine("it can contain letters + numbers");
                            Console.ResetColor();
                            string username = processData.getValue("username");
                            if (username == "")
                            {
                                break;
                            }
                            valid = false;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Your password MUST be at least 8 characters long");
                            Console.WriteLine("Your password MUST contain a least one number one uppercase and one lower case letter");
                            Console.ResetColor();
                            string passwd = processData.getValue("passwd");
                            if (passwd == "")
                            {
                                break;
                            }
                            newUser.createUser(username, passwd);
                            valid = false;
                            break;
                        }else {break; }
                    #endregion

                    #region login
                    case "l":
                        if (loggedin == false)
                        {
                            Console.WriteLine("logging in");
                            newUser.loginUser();
                        }
                        else
                        {
                            Console.WriteLine("logging out");
                            newUser.logoutUser();
                        }
                        break;
                    #endregion

                    #region edit account
                    case "a":
                        Console.WriteLine("press (y) to edit account");
                        Console.WriteLine("press any other key to abort");
                        state = Console.ReadLine();
                        if (state == "y")
                        {                            
                            if (loggedin == true)
                            {
                                Console.WriteLine("----------------------");
                                Console.WriteLine("¦ administer account ¦");
                                Console.WriteLine("----------------------");
                                Console.WriteLine("if You dont want to fill in a field You can leave it empty");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("please enter surname");
                                Console.WriteLine("it should only contain letters");
                                Console.ResetColor();
                                string surname = processData.getValue("surname");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("please enter name");
                                Console.WriteLine("it should only contain letters");
                                Console.ResetColor();
                                string name = processData.getValue("name");                                
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("please enter email");
                                Console.WriteLine("Must be a valid email address");
                                Console.ResetColor();
                                string email = processData.getValue("email");
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("please enter Phone");
                                Console.WriteLine("it should only contain digits");
                                Console.WriteLine("it MUST be at least 6 digits long");
                                Console.ResetColor();
                                string tel = processData.getValue("tel");
                                newUser.editUser(user,pwd,surname,name,email,tel);                                
                            }
                        }
                        else {
                            break;
                        }
                        break;
                    #endregion

                    #region delete account
                    case "d":
                        Console.WriteLine("----------------------");
                        Console.WriteLine("¦   delete account   ¦");
                        Console.WriteLine("----------------------");
                        Console.WriteLine("press (y) to delete account");
                        Console.WriteLine("press any other key to abort");
                        state = Console.ReadLine();
                        if (state == "y")
                        {
                            newUser.loginUser();
                            if (loggedin==true)
                            newUser.deleteUser(user);
                        }
                        break;
                    #endregion

                    #region exit
                    case "x":
                        run = false;
                        break;
                    #endregion

                    #region wrong input
                    default:
                        Console.WriteLine("please choose proper action or exit with x");
                        break;
                    #endregion
                }
            }
            #endregion
        }       
    }
}
