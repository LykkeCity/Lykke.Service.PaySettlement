using Lykke.Service.PaySettlement.Core.Domain;
using System.Collections.Generic;

namespace Lykke.Service.PaySettlement.Services
{
    public class TransferToMarketMessageEqualityComparer : IEqualityComparer<TransferToMarketMessage>
    {
        public bool Equals(TransferToMarketMessage x, TransferToMarketMessage y)
        {
            return string.Equals(x.PaymentRequestWalletAddress, y.PaymentRequestWalletAddress);
        }

        public int GetHashCode(TransferToMarketMessage obj)
        {
            return obj.PaymentRequestWalletAddress.GetHashCode();
        }
    }
}
