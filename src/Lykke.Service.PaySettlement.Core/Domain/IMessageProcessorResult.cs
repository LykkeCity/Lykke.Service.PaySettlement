namespace Lykke.Service.PaySettlement.Core.Domain
{
    public interface IMessageProcessorResult
    {
        bool IsSuccess { get; }
    }
}
