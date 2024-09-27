namespace SKRIN_InsertToDBFromXML.Console;
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
using System.Transactions;

public class Program
{
    public const byte MAX_ACTIONS = 4; // 1..9
    const string WRONG_INPUT = "Wrong input!";
    static string? GetConnectionStringByName(string name)
    {
        string? returnValue = null;
        ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[name];
        if (settings != null)
            returnValue = settings.ConnectionString;
        return returnValue;
    }
    public static bool IsCorrectDigit(string str)
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
        public Order(int idOrder, DateTime? reg_date, decimal? sum)
        {
            this.idOrder = idOrder;
            this.reg_date = reg_date;
            this.sum = sum;
            user = new User();
        }

        public int idOrder { get; set; }
        public DateTime? reg_date { get; set; }
        public Decimal? sum { get; set; }
        public User user { get; set; }

    }
    public class User
    {
        private string _fio;
        public string fio 
        {
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
        public Product(int idOrder, int quantity, decimal price, string name) 
        {
            this.idOrder = idOrder;
            this.quantity = quantity;
            this.price = price;
            this.name = name;
        } 
        public int quantity { get; set; }
        public decimal price { get; set; }
        public string name { get; set; }
        public int idOrder { get; set; }
    }

    class DB : IDisposable
    {
        string? connectionString;
        MySqlConnection connection;
        public DB() 
        {
            string? connectionString = GetConnectionStringByName("DB");
            if (connectionString == null)
                throw new ConfigurationErrorsException("Connection string is null!");
            connection = new MySqlConnection(connectionString);
        }

        public DB(string? connectionString, MySqlConnection connection)
        {
            if (connectionString == null)
                throw new ConfigurationErrorsException("Connection string is null!");
            this.connectionString = connectionString;
            this.connection = connection;
        }



        /// <summary>
        /// Return -1 if doesn't exist
        /// </summary>
        /// <param name="productName"></param>
        /// <returns></returns>
        public int FindIdByNameProduct(string productName)
        {
            int idProduct = -1;
            var cmd = new MySqlCommand("SELECT idProduct FROM products WHERE name=@productName;", connection);
            cmd.Parameters.AddWithValue("@productName", productName);
            try
            {
                MySqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                if (rdr.HasRows)
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
        public int FindIdByNameUser(User user)
        {
            int idUser = -1;
            var cmd = new MySqlCommand("SELECT idUser FROM users WHERE last_name=@last_name AND first_name=@first_name AND middle_name=@middle_name;", connection);
            cmd.Parameters.AddWithValue("@last_name", user.fioParse.last_name);
            cmd.Parameters.AddWithValue("@first_name", user.fioParse.first_name);
            cmd.Parameters.AddWithValue("@middle_name", user.fioParse.middle_name);
            try
            {
                MySqlDataReader rdr = cmd.ExecuteReader();
                rdr.Read();
                if (rdr.HasRows)
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
        public bool IsOrderExistById(int idOrder)
        {
            bool isExist = false;
            var cmd = new MySqlCommand("SELECT idOrder FROM orders WHERE idOrder=@idOrder;", connection);
            cmd.Parameters.AddWithValue("@idOrder", idOrder);
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
        private void InsertProductToDB(Product product)
        {
            var cmd = new MySqlCommand("INSERT INTO products (name, price, quantity) VALUES(@name,@price,@quantity);", connection);
            cmd.Parameters.AddWithValue("@name", product.name);
            cmd.Parameters.AddWithValue("@price", product.price);
            cmd.Parameters.AddWithValue("@quantity", product.quantity);
            try
            {
                Console.WriteLine("Adding [" + product.name+"]");
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + $"{ex.Message}");
                Console.ReadKey();
            }
        }
        public void InsertUserToDB(User user)
        {
            var cmd = new MySqlCommand("INSERT INTO users (username, first_name, last_name, middle_name, email) VALUES(@username, @first_name, @last_name," +
                "@middle_name,@email);", connection);
            string rdUsername = RandomUsername();
            cmd.Parameters.AddWithValue("@username", rdUsername);
            cmd.Parameters.AddWithValue("@first_name", user.fioParse.first_name);
            cmd.Parameters.AddWithValue("@last_name", user.fioParse.last_name);
            cmd.Parameters.AddWithValue("@middle_name", user.fioParse.middle_name);
            cmd.Parameters.AddWithValue("@email", user.email);
            
            Console.WriteLine("Adding ["+user.fio+"] with username = "+ rdUsername);
            cmd.ExecuteNonQuery();
        }
        private void InsertCartToDB(Product product, int idOrder, int idProduct)
        {
            var cmd = new MySqlCommand("INSERT INTO cart (idOrder, idProduct, price, quantity) VALUES(@idOrder, @idProduct, @price, @quantity);", connection);
            cmd.Parameters.AddWithValue("@idOrder", idOrder);
            cmd.Parameters.AddWithValue("@idProduct", idProduct);
            cmd.Parameters.AddWithValue("@price", product.price);
            cmd.Parameters.AddWithValue("@quantity", product.quantity);
            Console.WriteLine("Adding to cart [idOrder = "+idOrder+" ; idProduct = "+ idProduct+"]");
            cmd.ExecuteNonQuery();
        }

        private void InsertOrderToDB(Order order, int idUser)
        {
            var cmd = new MySqlCommand("INSERT INTO orders(idOrder, reg_date, sum, idUser) VALUES(@idOrder, @reg_date, @sum, @idUser);", connection);
            cmd.Parameters.AddWithValue("@idOrder", order.idOrder);
            cmd.Parameters.AddWithValue("@reg_date", order.reg_date);
            cmd.Parameters.AddWithValue("@sum", order.sum);
            cmd.Parameters.AddWithValue("@idUser", idUser);
            cmd.ExecuteNonQuery();
            Console.WriteLine("Adding order #" + order.idOrder);
        }

        private void UpdateOrder(Order order, int idUser)
        {
            var cmd = new MySqlCommand("UPDATE orders SET sum = @sum, reg_date = @reg_date, idUser = @idUser WHERE idOrder = @idOrder;", connection);
            cmd.Parameters.AddWithValue("@sum", order.sum);
            cmd.Parameters.AddWithValue("@reg_date", order.reg_date);
            cmd.Parameters.AddWithValue("@idUser", idUser);
            cmd.Parameters.AddWithValue("@idOrder", order.idOrder);
            Console.WriteLine("Updating order # " + order.idOrder);
            cmd.ExecuteNonQuery();
        }

        private void DecreaseQuantityByIdProduct(int idProduct, int quantity)
        {
            var cmd = new MySqlCommand("UPDATE products SET quantity = quantity - @quantity WHERE idProduct=@idProduct AND quantity >= @quantity;", connection);
            cmd.Parameters.AddWithValue("@quantity", quantity);
            cmd.Parameters.AddWithValue("@idProduct", idProduct);
            cmd.ExecuteNonQuery();
        }

        private void DeleteCartByIdOrder(int idOrder)
        {
            var cmdUpdate = new MySqlCommand("UPDATE products p INNER JOIN cart c ON " +
                "c.idProduct = p.idProduct SET p.quantity = p.quantity + c.quantity WHERE c.idOrder = @idOrder;", connection);
            cmdUpdate.Parameters.AddWithValue("@idOrder", idOrder);
            var cmd = new MySqlCommand("DELETE FROM cart WHERE idOrder = @idOrder;", connection);
            cmd.Parameters.AddWithValue("@idOrder", idOrder);
            cmdUpdate.ExecuteNonQuery();
            cmd.ExecuteNonQuery();            
        }

        public void AddOrUpdateOrders(Dictionary<Order, List<Product>> orders)
        {
            if(orders.Count == 0) { return; }

            using (var trans = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.Serializable }))
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();
                foreach (var o in orders)
                {
                    int idUser = FindIdByNameUser(o.Key.user);
                    if (idUser == -1)
                    {
                        InsertUserToDB(o.Key.user);
                        idUser = FindIdByNameUser(o.Key.user);
                    }
                    if (IsOrderExistById(o.Key.idOrder)) // Need update
                    {
                        DeleteCartByIdOrder(o.Key.idOrder);
                        UpdateOrder(o.Key, idUser);
                    }
                    else
                    {
                        InsertOrderToDB(o.Key, idUser);
                    }
                    foreach (var p in o.Value)
                    {
                        int idProduct = FindIdByNameProduct(p.name!);
                        if (idProduct == -1)
                        {
                            InsertProductToDB(p);
                            idProduct = FindIdByNameProduct(p.name!);
                        }
                        InsertCartToDB(p, o.Key.idOrder, idProduct);
                        DecreaseQuantityByIdProduct(idProduct, p.quantity);
                    }
                        
                }
                connection.Close();
                trans.Complete();
            }
        }
        public void Dispose()
        {
            connection.Dispose();
        }
    }

    class XMLParser
    {
        string filename;
        public XMLParser(string xmlFilename) 
        {
            filename = xmlFilename;
        }

        public Dictionary<Order, List<Product>> Parse()
        {
            Dictionary<Order, List<Product>> result = new Dictionary<Order, List<Product>>();
            using (XmlReader reader = XmlReader.Create(filename))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "order")
                    {
                        int idOrder = -1;
                        DateTime? reg_date = null;
                        Decimal? sum = 0;
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "no")
                                idOrder = reader.ReadElementContentAsInt();
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "reg_date")
                                reg_date = DateTime.Parse(reader.ReadElementContentAsString().Replace('.', '-'));
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "sum")
                                sum = reader.ReadElementContentAsDecimal();
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                                break;
                        }
                        if (idOrder == -1 || reg_date == null || sum == 0)
                            throw new CorruptedXMLException("Bad order!");
                        Order xmlOrder = new Order(idOrder, reg_date, sum);
                        result.Add(xmlOrder, new List<Product>());
                        do
                        {
                            if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                            {
                                int _quantity = 0;
                                string _name = "";
                                decimal _price = 0;


                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "quantity")
                                        _quantity = reader.ReadElementContentAsInt();
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "name")
                                        _name = reader.ReadElementContentAsString();
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "price")
                                        _price = reader.ReadElementContentAsDecimal();
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "product")
                                    {
                                        if (_quantity == 0 || _name.CompareTo("") == 0 || _price == 0)
                                            throw new CorruptedXMLException("Bad product!");
                                        result[xmlOrder].Add(new Product(xmlOrder.idOrder, _quantity, _price, _name));
                                    }
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "product")
                                    {
                                        _quantity = 0;
                                        _name = "";
                                        _price = 0;
                                    }
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
                    }
                }
            }
            return result;
            
        }
    }
    public const int lengthOfRandomUsername = 10;
    public static string RandomUsername()
    {
        Random random = new Random((int)(DateTime.Now.Ticks%int.MaxValue));
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, lengthOfRandomUsername)
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
                            XMLParser parser = new XMLParser(filename);
                            DB dB = new DB(dbConnection, mySqlConnection);
                            var orders = parser.Parse();
                            dB.AddOrUpdateOrders(orders);
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