using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Dapper;

namespace Piljetter_Adminverktyg
{
    class Program
    {
        static string connStr = "Data Source = localhost\\SQLEXPRESS; Initial Catalog=Piljetter; Integrated Security=SSPI;";
        static void Main(string[] args)
        {
            Console.WriteLine("Piljetter.se administration application.\n" +
                "To register a coming concert, type 'register'.\n" +
                "To cancel a coming concert, type 'cancel'.\n" +
                "To get statistical reports, type 'reports'.");
            Console.Write(">: ");
            var command = Console.ReadLine().ToLower();
            if (command == "register")
            {
                Console.WriteLine("Enter a date for the coming concert in following format: dd-MM-yyyy hh:mm");
                Console.Write(">: ");
                var date = Console.ReadLine();
                var match = Regex.Match(date, @"(\d{2})-(\d{2})-(\d{4}) (\d{2}):(\d{2})");
                if (match.Success)
                {
                    var dateTime = new DateTime(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value), int.Parse(match.Groups[4].Value), int.Parse(match.Groups[5].Value), 00);
                    Console.WriteLine("Enter a venue for the coming concert. To do this you will need to enter a VenueID. To see a Venues ID enter 'see venueId'");
                    bool venueCheck = false;
                    while (!venueCheck)
                    {
                        var venueId = Console.ReadLine().ToLower();
                        if (venueId == "see venueid")
                        {
                            Console.WriteLine("Enter venue name.");
                            Console.Write(">: ");
                            var venue = Console.ReadLine();
                            if (CheckVenueExists(venue))
                            {
                                PrintVenueId(venue);
                            }
                            else
                            {
                                Console.WriteLine("There is no venue with that name!");
                            }
                        }
                        else
                        {
                            if (Int32.TryParse(venueId, out int venueIdChecked))
                            {
                                bool artistCheck = false;
                                while (!artistCheck)
                                {

                                    Console.WriteLine("Enter an artist");
                                    Console.Write(">: ");
                                    var artist = Console.ReadLine();
                                    if (CheckArtistExists(artist))
                                    {
                                        RegisterConcert(artist, venueIdChecked, dateTime);
                                        artistCheck = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("The entered artist does not exist!");
                                        artistCheck = true;
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("VenueID was entered incorrectly!");
                            }
                        }
                        venueCheck = true;
                    }
                }
                else
                {
                    Console.WriteLine("You have registered a date in an incorrect format. Try again.");
                }
            }
            else if (command == "cancel")
            {
                Console.WriteLine("Enter the ID of the concert you would like to cancel");
                Console.Write(">: ");
                if (Int32.TryParse(Console.ReadLine(), out int concertId))
                {
                    CancelConcert(concertId);
                    Console.WriteLine();
                    Console.WriteLine("Would you like to issue coupons to customers affected by the cancellation?");
                    var issueCoupons = Console.ReadLine().ToLower();
                    if (issueCoupons == "yes" || issueCoupons == "y")
                    {
                        IssueCoupons(concertId);
                    }
                    else
                    {
                        Console.WriteLine("No coupons were issued!");
                    }
                }
                else
                {
                    Console.WriteLine("No concert with suggested ID exists.");
                }
            }
            else if (command == "report")
            {
                Console.WriteLine("Choose what report you would like to get! \n" +
                    "To see profit of all concerts, type 'concert'. \n" +
                    "To see most profitable artist, type 'artist'. \n" +
                    "To see ticket statistics for a given period, type 'tickets'.\n");
                Console.WriteLine();
                Console.Write(">: ");
                var reportCommand = Console.ReadLine().ToLower();
                if (reportCommand == "concert" || reportCommand == "concerts")
                {
                    SeeConcertProfit();
                }
                else if (reportCommand == "artist" || reportCommand == "artists")
                {
                    Console.WriteLine("Enter the time period you would like to see in following format: \n" +
                        "'dd-MM-yyyy : dd-MM-yyyy'");
                    Console.WriteLine();
                    Console.Write(">: ");
                    var reportDate = Console.ReadLine();
                    var match = Regex.Match(reportDate, @"(\d{2})-(\d{2})-(\d{4})(\s):(\s)(\d{2})-(\d{2})-(\d{4})");
                    if (match.Success)
                    {
                        var startDate = new DateTime(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));
                        var endDate = new DateTime(int.Parse(match.Groups[8].Value), int.Parse(match.Groups[7].Value), int.Parse(match.Groups[6].Value));
                        SeeMostProfitableArtist(startDate, endDate);
                    }
                }
                else if (reportCommand == "tickets" || reportCommand == "ticket")
                {
                    Console.WriteLine("Enter the time period you would like to see in following format: \n" +
                        "'dd-MM-yyyy : dd-MM-yyyy'");
                    Console.WriteLine();
                    Console.Write(">: ");
                    var reportDate = Console.ReadLine();
                    var match = Regex.Match(reportDate, @"(\d{2})-(\d{2})-(\d{4})(\s):(\s)(\d{2})-(\d{2})-(\d{4})");
                    if (match.Success)
                    {
                        var startDate = new DateTime(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));
                        var endDate = new DateTime(int.Parse(match.Groups[8].Value), int.Parse(match.Groups[7].Value), int.Parse(match.Groups[6].Value));
                        SeeTicketsSold(startDate, endDate);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid command.");
                }
            }
        }
        static void IssueCoupons(int concertId)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction())
                    {
                        var sql = "INSERT INTO Coupon(CustomerId, ExpireDate) SELECT r.CustomerId, CAST(DATEADD(year, 1, GETDATE())AS DATE) FROM RevokedTickets r WHERE r.ConcertId = @concertId;" +
                            "DELETE FROM RevokedTickets WHERE ConcertId = @concertId;";
                        c.Execute(sql, new { @concertId = concertId }, transaction: t);
                    }
                }
            }

            catch (SqlException)
            {
                Console.WriteLine("Transaction failed. Could not issue any coupons.");
            }
        }
        static void SeeTicketsSold(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {
                        var sql = "SELECT COUNT(t.Id) AS TicketsSold, SUM(t.Price) AS TicketRevenue, SUM(t.Price)/COUNT(t.Id) AS AverageTicketPrice FROM Ticket t WHERE t.PurchaseTime BETWEEN @startDate AND @endDate;";
                        var tickets = c.Query<Ticket>(sql, new { @startDate = startDate, @endDate = endDate });
                        foreach (var ticket in tickets)
                        {
                            Console.WriteLine($"Tickets Sold: {ticket.TicketsSold},  Revenue: {ticket.TicketRevenue},  Average Ticket Price: {ticket.AverageTicketPrice}");
                        }
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");
                        t.Commit();
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Could not get report.");
            }
        }
        static void SeeMostProfitableArtist(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {

                        var sql = "SELECT c.ArtistName, SUM(t.Price) - c.Cost AS Profit FROM Concert c INNER JOIN Ticket t ON t.ConcertId = c.Id WHERE c.Date Between @startDate AND @endDate GROUP BY c.ArtistName, c.Cost ORDER BY Profit DESC";
                        var artists = c.Query<ArtistProfit>(sql, new { @startDate = startDate, @endDate = endDate });
                        foreach (var artist in artists)
                        {
                            Console.WriteLine($"Artist: {artist.ArtistName} Profit: {artist.Profit}");
                        }
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Could not get report.");
            }
        }
        static void SeeConcertProfit()
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {

                        // nedan hämtar informationen från en vy som skapats i förväg. Se sql-fil.
                        var sql = "Select * From[ConcertProfit] Order By Profit DESC;";
                        var concerts = c.Query<ConcertProfit>(sql, transaction: t);
                        foreach (var concert in concerts)
                        {
                            Console.WriteLine($"ID: {concert.Id}  Profit: {concert.Profit}");
                        }
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;");
                        t.Commit();
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Could not get report");
            }
        }
        static void CancelConcert(int concertId)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        var sql = @"INSERT INTO RevokedTickets SELECT * FROM Ticket t WHERE t.ConcertId = @concertId;
                            WITH cte AS (SELECT t.CustomerId, SUM(t.Price) as ToBeRefunded FROM Ticket t WHERE t.ConcertId = @concertId GROUP BY CustomerId) UPDATE c SET c.Balance = c.Balance + cte.ToBeRefunded FROM Customer c INNER JOIN cte ON cte.CustomerId = c.Id;
                            DELETE FROM Ticket WHERE ConcertId = @concertId;
                            UPDATE Concert SET IsCancelled = 1 WHERE Id = @concertId;";
                        c.Execute(sql, new { @concertId = concertId }, transaction: t);
                        c.Execute("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;");
                        t.Commit();
                        Console.WriteLine("Cancellation completed.");
                    }
                }
            }
            catch (SqlException)
            {
                Console.WriteLine("Cancellation failed.");
            }
        }
        //static int CalculateTicketPrice(string artistName, int venueId)
        //{

        //}
        static bool CheckIfArtistCanBeBooked(SqlConnection c, string artistName, DateTime date)
        {
            var sql = "SELECT c.ArtistName as Artist FROM Concert c WHERE c.Date = @date";
            var artistDates = c.Query<Concert>(sql, new { @date = date });
            foreach (var artist in artistDates)
            {
                if (artist.Artist == artistName)
                {
                    return false;
                }
            }
            return true;
        }
        static bool CheckIfVenueCanBeBooked(SqlConnection c, int venueId, DateTime date)
        {
            var sql = "SELECT c.VenueId as VenueId FROM Concert c WHERE c.Date = @date";
            var venueDates = c.Query<Concert>(sql, new { @date = date });
            foreach (var venue in venueDates)
            {
                if (venue.VenueId == venueId)
                {
                    return false;
                }
            }
            return true;
        }
        static bool CheckVenueExists(string venueName)
        {
            using (var c = new SqlConnection(connStr))
            {
                var sql = "SELECT v.Name as Name FROM Venue v WHERE v.Name = @venue";
                var venues = c.Query<Artist>(sql, new { @venue = venueName });
                foreach (var scene in venues)
                {
                    if (venueName != scene.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static bool CheckArtistExists(string artistName)
        {
            using (var c = new SqlConnection(connStr))
            {
                var sql = "SELECT a.Name AS Name FROM Artist a WHERE a.Name = @name";
                var artists = c.Query<Artist>(sql, new { @name = artistName });
                foreach (var artist in artists)
                {
                    if (artistName != artist.Name)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        static void PrintVenueId(string venueName)
        {
            using (var c = new SqlConnection(connStr))
            {
                var sql = "SELECT v.Id AS Id, v.Name AS Name, c.Name AS City FROM Venue v INNER JOIN City c ON c.Id = v.CityId WHERE v.Name = @venue";
                var venues = c.Query<Venue>(sql, new { @venue = venueName });
                foreach (var venue in venues)
                {
                    Console.WriteLine($"ID: {venue.Id}  Name: {venue.Name}  City: {venue.City}");
                }
            }
        }
        static void RegisterConcert(string artistName, int venueId, DateTime date)
        {
            try
            {
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var t = c.BeginTransaction())
                    {
                        if (CheckIfVenueCanBeBooked(c, venueId, date))
                        {
                            if (CheckIfArtistCanBeBooked(c, artistName, date))
                            {

                                var sql = "INSERT INTO Concert(ArtistName, VenueId, Date, Price, AvailableTickets, Cost, IsCancelled) VALUES(@artist, @venue, @date, (SELECT(v.Rating)*(a.Popularity)*3.14 FROM Venue v, Artist a WHERE v.Id = @venue AND a.Name = @artist), (SELECT v.Capacity FROM Venue v WHERE v.Id = @venue), (SELECT(v.Rating)*(a.Popularity)*(v.Capacity) FROM Venue v, Artist a WHERE v.Id = @venue AND a.Name = @artist), 0)";
                                c.Execute(sql, new { @artist = artistName, @venue = venueId, @date = date }, transaction: t);
                            }
                            else
                            {
                                Console.WriteLine("Artist unavailable at this date");
                                throw new Exception();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Venue unavailable at this date");
                            throw new Exception();
                        }
                        t.Commit();
                        Console.WriteLine("Concert has been registered!");
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to register concert.");
            }
        }
    }
}

