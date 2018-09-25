using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.PaySettlement.Core.Domain
{
    public class TransactionInfo
    {
        public string Hash { get; set; }

        public decimal Amount { get; set; }

        public decimal Fee { get; set; }
    }
}
