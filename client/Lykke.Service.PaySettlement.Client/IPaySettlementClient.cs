using JetBrains.Annotations;

namespace Lykke.Service.PaySettlement.Client
{
    /// <summary>
    /// PaySettlement client interface.
    /// </summary>
    [PublicAPI]
    public interface IPaySettlementClient
    {
        /// <summary>Application Api interface</summary>
        IPaySettlementApi Api { get; }
    }
}
