using System;

namespace Lykke.Service.PaySettlement.Models.Exceptions
{
    public class SettlementException: Exception
    {
        public SettlementException() : base()
        {
        }

        public SettlementException(string message) : base(message)
        {
        }

        public SettlementException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
