using Dapper;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Piljetter_Kundapplikation
{
    class Program
    {
        static string connStr = "Data Source = localhost\\SQLEXPRESS; Initial Catalog=Piljetter; Integrated Security=SSPI;";

        static void Main(string[] args)
        {
            string initialCommand = null;
            string login = null;
            bool loggedIn = false;
            int customerId = 0;
            while (initialCommand != "login" && initialCommand != "register")
            {
                Console.WriteLine("Welcome to Piljetter.se! To order tickets please login to your account, using the command 'login'!\n If you do not yet have on account, please register using the command 'register'!");
                Console.Write(">: ");
                initialCommand = Console.ReadLine().ToLower();
                if (initialCommand != "login" && initialCommand != "register")
                {
                    Console.WriteLine("Your command is erroneous! Try again!");
                    Console.WriteLine();
                }
            }
            while (!loggedIn)
            {
                if (initialCommand == "register")
                {
                    //här måste första regex in för att kolla så att det är ett valid username?
                    Console.WriteLine("To register, first enter an email");
                    Console.Write(">: ");
                    login = Console.ReadLine();
                    if (!CheckIfAccountExists(login))
                    {
                        RegisterAccount(login);
                        customerId = GetCustomerIdFromEmail(login);
                        loggedIn = true;
                    }
                    else
                    {
                        Console.WriteLine("There is already a user with this email.");
                        login = null;
                    }
                }
                else if (initialCommand == "login")
                {
                    Console.WriteLine("To login, please type your email");
                    Console.Write(">: ");
                    login = Console.ReadLine();
                    if (!CheckIfAccountExists(login))
                    {
                        Console.WriteLine("Account does not exist!");
                        login = null;
                    }
                    else
                    {
                        customerId = GetCustomerIdFromEmail(login);
                        loggedIn = true;
                    }
                }
            }
            while (login != null && loggedIn)
            {
                Console.WriteLine($"Welcome to Piljetter.se!\n\nPlease feel free to explore the commands and features of our great new Console App!\n\n" +
                    $"To search for artists, concerts, venues etc. please type 'search' and you will be given further instructions.\n" +
                    $"To deposit Pesetas please type 'deposit', press Enter and type the amount you would like to deposit from your bank account.\n"
                    + $"To purchase tickets to any concert in our catalogue, please type 'purchase'. \n"
                    + $"To see your tickets and coupons type 'items'.\n"
                    + $"To see your pesetas, type 'balance'.\n"
                    + $"To logout from your account type 'logout'.\n");
                Console.WriteLine();
                Console.Write(">: ");
                var command = Console.ReadLine().ToLower();
                Console.WriteLine();
                if (command == "search")
                {
                    Console.WriteLine("Enter your search in the following format:\n"
                        + "'artist', 'venue', 'dd-MM-yyyy', 'city', 'country'\n"); // regex nedan funkar inte riktigt som det ska. just nu måste man söka på alla. detta är inte tanken.
                    Console.Write(">: ");
                    var input = Console.ReadLine();
                    var match = Regex.Match(input, @"(\w+),(\s)(\w+),(\s)(\d{2})-(\d{2})-(\d{4}),(\s)(\w+),(\s)(\w+)");

                    if (match.Success)
                    {
                        var dateTime = new DateTime(int.Parse(match.Groups[7].Value), int.Parse(match.Groups[6].Value), int.Parse(match.Groups[5].Value));
                        Search(match.Groups[1].Value, match.Groups[3].Value, dateTime, match.Groups[9].Value, match.Groups[11].Value);
                    }
                    else
                    {
                        Console.WriteLine("Your search was entered in an incorrect format!"); // om hela sökningen görs i fel format
                    }
                }
                else if (command == "deposit")
                {
                    Console.WriteLine("Enter the amount you would like to deposit!");
                    Console.Write(">: ");
                    if (Int32.TryParse(Console.ReadLine(), out int depositAmount))
                    {
                        Deposit(customerId, depositAmount);
                    }
                    else
                    {
                        Console.WriteLine("You have entered a format which can not be represented as a number!");
                    }
                }
                else if (command == "purchase")
                {
                    Console.WriteLine("If you would like to pay for your ticket with pesetas type 'cash'.\n"
                        + "If you have a coupon you would like to use type 'coupon'.");
                    var purchaseCommand = Console.ReadLine().ToLower();
                    if (purchaseCommand == "cash")
                    {
                        Console.WriteLine("Please enter the ID for the concert you would like to purchase a ticket for!");
                        if (Int32.TryParse(Console.ReadLine(), out int concertId))
                        {
                            PurchaseWithPesetas(customerId, concertId);
                        }
                        else
                        {
                            Console.WriteLine("You have not entered a valid concertID!");
                        }
                    }
                    else if (purchaseCommand == "coupon")
                    {
                        if (CheckIfCustomerHasValidCoupon(customerId))
                        {
                            Console.WriteLine("Please enter the ID for the concert you would like to purchase a ticket for!");
                            Console.Write(">: ");
                            if (Int32.TryParse(Console.ReadLine(), out int concertId))
                            {
                                PurchaseTicketWithCoupon(customerId, concertId);
                            }
                            else
                            {
                                Console.WriteLine("You have not entered a valid concertID!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You do not have a valid coupon!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Your input does not match any valid command!");
                    }
                }
                else if (command == "logout")
                {
                    login = null;
                    loggedIn = false;
                }
                else if (command == "items")
                {
                    SeeTickets(customerId);
                    SeeCoupons(customerId);
                }
                else if (command == "balance")
                {
                    using (var c = new SqlConnection(connStr))
                    {
                        var balance = CheckCustomerBalance(c, customerId);
                        Console.WriteLine(balance);
                    }
                }
                else
                {
                    Console.WriteLine("Your input does not match any valid command!");
                }
            }
            //}
        }
        static void SeeCoupons(int customerId)
        {
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    var sql = "SELECT c.* FROM Coupon c WHERE c.CustomerId = @customerId";
                    var coupons = c.Query<Coupon>(sql, new { @customerId = customerId });
                    Console.WriteLine();
                    foreach (var coupon in coupons)
                    {
                        Console.WriteLine($"CouponID: {coupon.Id}, Expires: {coupon.ExpireDate.Date}");
                    }
                }
            }
        }
        static void SeeTickets(int customerId)
        {
            using (var c = new SqlConnection(connStr))
            {
                c.Open();
                var sql = "SELECT t.* FROM Ticket t WHERE t.CustomerId = @customerId";
                var tickets = c.Query<Ticket>(sql, new { @customerId = customerId });
                Console.WriteLine("Tickets:");
                foreach (var ticket in tickets)
                {
                    Console.WriteLine($"TicketID: {ticket.Id}, ConcertID: {ticket.ConcertId}, Price: {ticket.Price}, Purchase Date: {ticket.PurchaseTime}");
                }
            }
        }
        static int GetCustomerIdFromEmail(string email)
        {
            int customerId = 0;
            using (var c = new SqlConnection(connStr))
            {
                c.Open();
                var sql = "SELECT c.Id FROM Customer c WHERE c.Email = @email";
                var customers = c.Query<Customer>(sql, new { @email = email });
                foreach (var customer in customers)
                {
                    customerId = customer.Id;
                }
            }
            return customerId;
        }
        static int GetOldestCoupon(SqlConnection c, int customerId)
        {
            int couponId = 0;
            var sql = "SELECT TOP 1 c.Id FROM Coupon c WHERE c.CustomerId = @customerId AND c.ExpireDate >= GETDATE() ORDER BY c.ExpireDate";
            var coupons = c.Query<Coupon>(sql, new { @customerId = customerId });
            foreach (var coupon in coupons)
            {
                couponId = coupon.Id;
            }

            return couponId;
        }
        static void PurchaseTicketWithCoupon(int customerId, int concertId)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        if (ValidateTicketDate(c, concertId))
                        {
                            var couponId = GetOldestCoupon(c, customerId);
                            var sql = @"INSERT INTO Ticket(ConcertId, Price, PurchaseTime, CustomerId) VALUES((SELECT c.Id FROM Concert c WHERE c.Id = @concert), (SELECT c.Price FROM Concert c WHERE c.Id = @concert), GETDATE(), @customerId);
                        UPDATE Concert SET AvailableTickets = AvailableTickets - 1 WHERE Concert.Id = @concert;
                        DELETE FROM Coupon WHERE CustomerId = @customerId AND Id = @couponId;";
                            c.Execute(sql, new { @customerId = customerId, @couponId = couponId, concert = concertId }, transaction: t);
                            c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");
                        }
                        t.Commit();
                        Console.WriteLine("Your ticket has been purchased!");
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Your purchase has failed!");
            }
        }
        static bool CheckIfCustomerHasValidCoupon(int customerId)
        {
            using (var c = new SqlConnection(connStr))
            {
                c.Open();
                var sql = "SELECT c.ExpireDate FROM Coupon c WHERE c.CustomerId = @customerId ";
                var coupons = c.Query<Coupon>(sql, new { @customerId = customerId });
                foreach (var coupon in coupons)
                {
                    var expireDate = coupon.ExpireDate;
                    if (expireDate >= DateTime.Today)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static bool CheckIfAccountExists(string email)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    var sql = "SELECT c.Email AS Email FROM Customer c";
                    var customers = c.Query<Customer>(sql);
                    foreach (var customer in customers)
                    {
                        if (email == customer.Email)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (SqlException)
            {
                return false;
            }
            return false;
        }
        static void RegisterAccount(string email)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction())
                    {
                        var sql = "INSERT INTO Customer(Email, Balance) VALUES(@email, 0)";
                        c.Execute(sql, new { @email = email }, transaction: t);
                        t.Commit();
                        Console.WriteLine($"Your account has been successfully registered! \n You are now logged in as {email}!");
                        Console.WriteLine();
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Something unexpected happened. Your account could not be registered.");
            }
        }
        static int CheckCustomerBalance(SqlConnection c, int customerId)
        {
            int balance = 0;
            var sql = "SELECT c.Balance FROM Customer c WHERE c.Id = @customerId";
            var customers = c.Query<Customer>(sql, new { @customerId = customerId });
            foreach (var customer in customers)
            {
                balance = customer.Balance;
            }
            return balance;
        }

        static int CheckTicketPrice(SqlConnection c, int concertId)
        {
            int price = 0;
            var sql = "SELECT c.Price FROM Concert c WHERE c.Id = @concert";
            var concerts = c.Query<Concert>(sql, new { @concert = concertId });
            foreach (var item in concerts)
            {
                price = item.TicketPrice;
            }
            return price;
        }

        static bool ValidateTicketDate(SqlConnection conn, int concertId)
        {
            DateTime validDate = DateTime.Today;
            var ticketDateSql = "SELECT c.Date FROM Concert c WHERE c.Id = @concert";
            var dates = conn.Query<Concert>(ticketDateSql, new { @concert = concertId });
            foreach (var validConcert in dates)
            {
                validDate = validConcert.Date;
            }
            if (validDate > DateTime.Now)
            {
                return true;
            }
            else return false;
        }
        static void PurchaseWithPesetas(int customerId, int concertId)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        if (CheckCustomerBalance(c, customerId) >= CheckTicketPrice(c, concertId))
                        {
                            if (ValidateTicketDate(c, concertId))
                            {
                                var sql = @"UPDATE Customer SET Balance = Balance - (SELECT Price FROM Concert c WHERE c.Id = @concert) WHERE Id = @customerId;
                             INSERT INTO Ticket(ConcertId, Price, PurchaseTime, CustomerId) VALUES((SELECT c.Id FROM Concert c WHERE c.Id = @concert), (SELECT c.Price FROM Concert c WHERE c.Id = @concert), GETDATE(), @customerId);
                            UPDATE Concert SET AvailableTickets = AvailableTickets - 1 WHERE Concert.Id = @concert;";
                                c.Execute(sql, new { @customerId = customerId, @concert = concertId }, transaction: t);
                            }
                            else
                            {
                                Console.WriteLine("The concert for which you were to purchase a ticket is due.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Insufficient funds. Please deposit more pesetas into your account!");
                        }
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");
                        t.Commit();
                        Console.WriteLine("Ticket has been purchased!");
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Purchase failed!");
            }

        }
        static void Deposit(int customerId, int amount)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var sql = "UPDATE Customer SET Balance = Balance + @amount WHERE Id = @customerId;";
                        c.Execute(sql, new { @amount = amount, @customerId = customerId }, transaction: t);
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;");
                        t.Commit();
                        Console.WriteLine("Deposit recieved!");
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Deposit failed!");
            }
        }
        //AND (c.Artist = @artist OR @artist IS NULL)
        // INSERT INTO Test
        // SELECT a+1, b+123 FROM Test
        static void Search(string artistName, string venue, DateTime date, string city, string country)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    var sql = "SELECT c.Id as Id, a.Name as ArtistName, v.Name as Venue, p.Name as City, c.Date as Date, c.Price as TicketPrice FROM Artist a INNER JOIN Concert c ON a.Name = c.ArtistName INNER JOIN Venue v ON v.Id = c.VenueId INNER JOIN City p ON p.Id = v.CityId";
                    int count = 0;
                    c.Open();
                    if (artistName != null && artistName != "")
                    {
                        count++;
                        sql += " WHERE a.Name = @artist";
                    }
                    if (venue != null && venue != "")
                    {
                        count++;
                        if (count > 1)
                        {
                            sql += " AND v.Name = @venue";
                        }
                        else
                        {
                            sql += " WHERE v.Name = @venue";
                        }
                    }
                    if (date != null)
                    {
                        count++;
                        if (count > 1)
                        {
                            sql += " AND c.Date = @date";
                        }
                        else
                        {
                            sql += " WHERE c.Date = @date";
                        }

                    }
                    if (city != null && city != "")
                    {
                        count++;
                        if (count > 1)
                        {
                            sql += " AND p.Name = @city";
                        }
                        else
                        {
                            sql += " WHERE p.Name = @city";
                        }
                    }
                    if (country != null && country != "")
                    {
                        count++;
                        if (count > 1)
                        {
                            sql += " AND p.Country = @country";
                        }
                        else
                        {
                            sql += " WHERE p.Country = @country";
                        }
                    }
                    if (count > 0)
                    {
                        sql += " AND c.Date >= GETDATE() AND c.IsCancelled = 0";
                    }
                    else
                    {
                        sql += " WHERE c.Date >= GETDATE() AND c.IsCancelled = 0";
                    }
                    using (var t = c.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {

                        var concerts = c.Query<Concert>(sql, new { @venue = venue, @artist = artistName, @city = city, @date = date, @country = country }, transaction: t);
                        foreach (var concert in concerts)
                        {
                            Console.WriteLine($"ConcertID: {concert.Id} Artist: {concert.ArtistName}, Venue: {concert.Venue}, City: {concert.City}, Date: {concert.Date}, Price: {concert.TicketPrice}");
                        }
                        Console.WriteLine();
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;");
                        t.Commit();
                    }
                    count = 0;
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Something went wrong");
            }
        }
    }
}
