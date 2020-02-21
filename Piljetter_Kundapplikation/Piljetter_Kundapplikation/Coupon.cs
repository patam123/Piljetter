using System;

namespace Piljetter_Kundapplikation
{
    public class Coupon
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime ExpireDate { get; set; }
    }
}
