using System;

namespace Piljetter_Kundapplikation
{
    public class TransactionLog
    {
        public DateTime Moment { get; set; }
        public string ToCustomer { get; set; }
        public string FromCustomer { get; set; }
        public int Amount { get; set; }
    }
}