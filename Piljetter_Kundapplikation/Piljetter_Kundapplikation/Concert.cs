using System;

namespace Piljetter_Kundapplikation
{
    public class Concert 
    { 
        public int Id { get; set; }
        public string ArtistName { get; set; }
        public string Venue { get; set; }
        public DateTime Date { get; set; }
        public int TicketPrice { get; set; }
        public string City { get; set; }
    }
}
