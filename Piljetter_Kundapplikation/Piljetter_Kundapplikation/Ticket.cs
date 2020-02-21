using System;

namespace Piljetter_Kundapplikation
{
    public class Ticket
    {
        public int Id { get; set; }
        public int ConcertId { get; set; }
        public string Customer { get; set; }
        public int Price { get; set; }
        public DateTime PurchaseTime { get; set; }
    }
}
