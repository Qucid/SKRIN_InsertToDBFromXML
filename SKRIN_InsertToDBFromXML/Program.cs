using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Data;
using System.Xml.XPath;
using System.Xml;
using Google.Protobuf.WellKnownTypes;
using Mysqlx.Crud;
using System;

class Program
{
    const byte MAX_ACTIONS = 4; // 1..9
    const string WRONG_INPUT = "Wrong input!";
    static string? GetConnectionStringByName(string name)
    {
        string? returnValue = null;
        ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[name];
        if (settings != null)
            returnValue = settings.ConnectionString;
        return returnValue;
    }
    static bool IsCorrectDigit(string str)
    {
        foreach (char c in str)
        {
            if (c < '1' || c > (""+ MAX_ACTIONS)[0])
                return false;
        }
        return true;
    }
    [Serializable]
    class CorruptedXMLException : Exception
    {
        public CorruptedXMLException()
        { }

        public CorruptedXMLException(string message)
            : base(message)
        { }

        public CorruptedXMLException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
    class Order
    {
        public Order() 
        {
            products = new List<Product>();
            user = new User();
        }

        public int idOrder { get; set; }
        public DateTime reg_date { get; set; }
        public Decimal? sum { get; set; }
        public List<Product> products { get; set; }
        public User user { get; set; }

        public void ThrowExceptionIfNull()
        {
            if (idOrder == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"no\". Probably corrupted XML");
            }
            else if (reg_date == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"reg_date\". Probably corrupted XML");
            }
            else if (sum == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"sum\". Probably corrupted XML");
            }
        }

    }
    class User
    {
        private string _fio;
        public string fio {
            get => _fio;
            set
            {
                _fio = value;
                fioParse = new FIOParse(value);
            }
        }
        public string email { get; set; }
        public FIOParse fioParse { get; set;}
        public class FIOParse
        {
            public string fio { get; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string middle_name { get; set; }
            public FIOParse(string fio)
            {
                this.fio = fio;
                string[] nameSplit = fio.Split(' '); // If name words > 3 ?
                last_name = nameSplit[0];
                first_name = nameSplit[1];
                middle_name = nameSplit[2];
            }
        }
    }
    class Product
    { 
        public Product() { } 
        public int? quantity { get; set; }
        public decimal? price { get; set; }
        public string? name { get; set; }
        public void ThrowExceptionIfNull()
        {
            if (quantity == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"quantity\". Probably corrupted XML");
            }
            else if (price == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"price\". Probably corrupted XML");
            }
            else if (name == null)
            {
                throw new CorruptedXMLException("Doesn't found node \"name\". Probably corrupted XML");
            }
        }
    }

    private static void XmlToDatabase(MySqlConnection mySqlConnection, string xmlFilename)
    {
        List<Order> orders = new List<Order>();
        using (XmlReader reader = XmlReader.Create(xmlFilename))
        {
            reader.MoveToContent();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "order")
                {
                    Order xmlOrder = new Order();
                    int idOrder = 0;
                    DateTime reg_date;
                    Decimal sum = 0;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "no")
                            xmlOrder.idOrder = reader.ReadElementContentAsInt();
                        if(reader.NodeType == XmlNodeType.Element && reader.Name == "reg_date")
                            xmlOrder.reg_date = DateTime.Parse(reader.ReadElementContentAsString().Replace('.','-'));
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "sum")
                            xmlOrder.sum = reader.ReadElementContentAsDecimal();
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                            break;
                    }
                    xmlOrder.ThrowExceptionIfNull();
                    do
                    {
                        if(reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                        {
                            Product product = new Product();
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "quantity")
                                    product.quantity = reader.ReadElementContentAsInt();
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                                    product.name = reader.ReadElementContentAsString();
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "price")
                                    product.price = reader.ReadElementContentAsDecimal();
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "product")
                                {
                                    xmlOrder.products.Add(product);
                                    product.ThrowExceptionIfNull();
                                }
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                                    product = new Product();
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
                                    break;
                            }
                            
                            
                            
                        }
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "user")
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "fio")
                                    xmlOrder.user.fio = reader.ReadElementContentAsString();
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "email")
                                    xmlOrder.user.email = reader.ReadElementContentAsString();
                                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "user")
                                    break;
                            }
                        }

                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "order")
                            break;
                    }
                    while (reader.Read());
                    orders.Add(xmlOrder);
                }
            }
        }

        foreach(var o in orders)
        {
            if (IsOrderExistById(o.idOrder, mySqlConnection))
            {
                Console.WriteLine("Order with idOrder=" + o.idOrder + " already exists! Skipped.");
                continue;
            }
            int idUser = FindIdByNameUser(o.user, mySqlConnection);
            if(idUser == -1)
            {
                InsertUserToDB(o.user, mySqlConnection);
                idUser = FindIdByNameUser(o.user, mySqlConnection);
            }
            String sql = "INSERT INTO orders(idOrder,reg_date, sum, idUser) VALUES(" + o.idOrder + ",\'" +
                o.reg_date.ToString("yyyy-MM-dd") + "\'," + o.sum.ToString().Replace(',','.') + "," + idUser + ");";
            Console.WriteLine(sql);
            MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
            cmd.ExecuteNonQuery();
            foreach (var p in o.products)
            {
                int idProduct = FindIdByNameProduct(p.name!, mySqlConnection);
                if(idProduct == -1)
                {
                    InsertProductToDB(p, mySqlConnection);
                    idProduct = FindIdByNameProduct(p.name!,mySqlConnection);
                }
                InsertCartToDB(p, o.idOrder, idProduct, mySqlConnection);
                // Reducing the quantity of products in the database
                sql = "UPDATE products SET quantity = quantity - " + p.quantity + " WHERE idProduct=" + idProduct + " AND quantity>="+p.quantity+";";
                cmd = new MySqlCommand(sql, mySqlConnection);
                cmd.ExecuteNonQuery();
            }
        }

    }
    /// <summary>
    /// Return -1 if doesn't exist
    /// </summary>
    /// <param name="productName"></param>
    /// <returns></returns>
    private static int FindIdByNameProduct(string productName, MySqlConnection mySqlConnection)
    {
        int idProduct = -1;
        string sql = "SELECT idProduct FROM products WHERE name=\'"+productName+"\';";
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            if(rdr.HasRows)
            {
                string readValue = rdr[0].ToString() ?? "";
                int.TryParse(readValue, out idProduct);
            }
            rdr.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
        return idProduct;
    }
    

    /// <summary>
    /// Return -1 if doesn't exist
    /// </summary>
    /// <param name="Fio"></param>
    /// <param name="mySqlConnection"></param>
    /// <returns></returns>
    private static int FindIdByNameUser(User user, MySqlConnection mySqlConnection)
    {
        int idUser = -1;
        string sql = "SELECT idUser FROM users WHERE last_name=\'"+ user.fioParse.last_name+ "\' AND first_name=\'"+ 
            user.fioParse.first_name+ "\' AND middle_name=\'"+user.fioParse.middle_name+"\';";
        Console.WriteLine(sql);
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            if(rdr.HasRows)
            {
                string readValue = rdr[0].ToString() ?? "";
                int.TryParse(readValue, out idUser);
            }
            rdr.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
        return idUser;
    }
    /// <summary>
    /// Return true if order with same id already exists
    /// </summary>
    /// <param name="order"></param>
    /// <param name="mySqlConnection"></param>
    /// <returns></returns>
    private static bool IsOrderExistById(int idOrder, MySqlConnection mySqlConnection)
    {
        bool isExist = false;
        string sql = "SELECT idOrder FROM orders WHERE idOrder=" + idOrder + ";";
        Console.WriteLine(sql);
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            MySqlDataReader rdr = cmd.ExecuteReader();
            rdr.Read();
            if (rdr.HasRows)
                isExist = true;
            rdr.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
        return isExist;
    }
    private static void InsertProductToDB(Product product, MySqlConnection mySqlConnection)
    {
        string sql = "INSERT INTO products (name, price, quantity) VALUES(\'"+product.name+"\',"+product.price.ToString().Replace(',','.')+","+product.quantity+");";
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            Console.WriteLine(sql);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
    }
    private static void InsertUserToDB(User user, MySqlConnection mySqlConnection)
    {

        string sql = "INSERT INTO users (username, first_name, last_name, middle_name, email) VALUES(\'" + RandomUsername() + "\',\'" +
            user.fioParse.first_name + "\',\'" + user.fioParse.last_name + "\',\'" + user.fioParse.middle_name + "\',\'" + user.email + "\');";
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            Console.WriteLine(sql);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
    }
    private static void InsertCartToDB(Product product, int idOrder, int idProduct, MySqlConnection mySqlConnection)
    {
        string sql = "INSERT INTO cart (idOrder, idProduct, price, quantity) VALUES(" + idOrder + "," +
            idProduct + "," + product.price.ToString().Replace(',','.') + "," + product.quantity + ");";
        MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
        try
        {
            Console.WriteLine(sql);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + $"{ex.Message}");
            Console.ReadKey();
        }
    }
    public static string RandomUsername()
    {
        int length = 10;
        Random random = new Random((int)(DateTime.Now.Ticks%int.MaxValue));
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Welcome to InsertToDBFromXMLApp.\nSelect an action:\n" +
                "[1] Insert new order(s) to database\n[2] Select current orders\n[3] Select current products\n[4] Exit\n");
            string actionString = Console.ReadLine() ?? "";
            if (IsCorrectDigit(actionString) && actionString.Length == 1)
            {
                byte parsedAction = 0;
                if(!byte.TryParse(actionString, out parsedAction))
                {
                    Console.WriteLine(WRONG_INPUT);
                    continue;
                }

                string? dbConnection = GetConnectionStringByName("DB");
                if (dbConnection == null)
                {
                    Console.WriteLine("Failed to get the connection string!\nType any button to close application.");
                    Console.ReadKey();
                    return; 
                }


                string sql = "";
                string filename = "";
                switch (parsedAction)
                {
                    case 1:
                        {

                            Console.Write("Type name of xml file: ");
                            filename = Console.ReadLine() ?? "";
                            Console.WriteLine();
                            if (!File.Exists(filename))
                            {
                                Console.WriteLine($"{filename} does not exist");
                                continue;
                            }


                            break;
                        }
                    case 2:
                        {
                            sql = "SELECT * FROM orders;";
                            break;
                        }
                    case 3:
                        {
                            sql = "SELECT * FROM products;";
                            break;
                        }
                    case 4:
                        {
                            return;
                        }
                }



                using (MySqlConnection mySqlConnection = new MySqlConnection(dbConnection))
                {
                    MySqlCommand cmd = new MySqlCommand(sql, mySqlConnection);
                    try
                    {
                        mySqlConnection.Open();
                        if (parsedAction == 1)
                        {
                            XmlToDatabase(mySqlConnection, filename);
                        }
                        else
                        {
                            MySqlDataReader rdr = cmd.ExecuteReader();
                            while (rdr.Read())
                            {
                                for (int i = 0; i < rdr.FieldCount; i++)
                                    Console.Write(rdr[i] + " -- ");
                                Console.WriteLine();
                            }
                            rdr.Close();
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error: "+$"{ex.Message}");
                        Console.ReadKey();
                        continue;
                    }
                }
                

            }
            else
            {
                Console.WriteLine(WRONG_INPUT);
                continue;
            }
            Console.WriteLine("Type any button to continue");
            Console.ReadKey();
        }
    }
}