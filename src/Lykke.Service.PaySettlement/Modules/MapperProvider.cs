using AutoMapper;
using AutoMapper.Configuration;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using Lykke.Service.PaySettlement.Cqrs;

namespace Lykke.Service.PaySettlement.Modules
{
    public class MapperProvider
    {
        public IMapper GetMapper()
        {
            var mce = new MapperConfigurationExpression();

            CreateRabbitMaps(mce);

            var mc = new MapperConfiguration(mce);
            mc.AssertConfigurationIsValid();

            return new Mapper(mc);
        }

        private void CreateRabbitMaps(MapperConfigurationExpression mce)
        {
            mce.CreateMap<PaymentRequestDetailsMessage, PaymentRequestDetailsEvent>(MemberList.Destination)
                .ForMember(d => d.PaymentRequestId, e => e.MapFrom(s => s.Id))
                .ForMember(d => d.PaymentRequestStatus, e => e.MapFrom(s => s.Status))
                .ForMember(d => d.PaymentRequestTimestamp, e => e.MapFrom(s => s.Timestamp));

            mce.CreateMap<PaymentRequestDetailsEvent, CreateSettlementCommand>();

            mce.CreateMap<CreateSettlementCommand, PaymentRequest>(MemberList.Destination)
                .ForMember(d => d.Status, e => e.MapFrom(s=>s.PaymentRequestStatus))
                .ForMember(d => d.TransferToMarketTransactionHash, e => e.Ignore())
                .ForMember(d => d.TransferToMarketTransactionFee, e => e.Ignore())
                .ForMember(d => d.ExchangeAmount, e => e.Ignore())
                .ForMember(d => d.MarketPrice, e => e.Ignore())
                .ForMember(d => d.MarketOrderId, e => e.Ignore())
                .ForMember(d => d.TransferredAmount, e => e.Ignore())
                .ForMember(d => d.MerchantClientId, e => e.Ignore())
                .ForMember(d => d.SettlementStatus, e => e.UseValue(SettlementStatus.None))
                .ForMember(d => d.Error, e => e.Ignore())
                .ForMember(d => d.ErrorDescription, e => e.Ignore());
        }
    }
}
