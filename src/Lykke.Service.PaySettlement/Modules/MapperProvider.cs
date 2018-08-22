using AutoMapper;
using AutoMapper.Configuration;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Core.Domain;

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
            mce.CreateMap<PaymentRequestDetailsMessage, PaymentRequest>(MemberList.Destination)
                .ForMember(d => d.PaymentRequestTimestamp, e => e.MapFrom(s => s.Timestamp))
                .ForMember(d => d.TransferToMarketTransactionId, e => e.Ignore())
                .ForMember(d => d.MarketAmount, e => e.Ignore())
                .ForMember(d => d.MarketPrice, e => e.Ignore())
                .ForMember(d => d.MarketOrderId, e => e.Ignore())
                .ForMember(d => d.MerchantClientId, e => e.Ignore())
                .ForMember(d => d.SettlementStatus, e => e.UseValue(SettlementStatus.None));
        }
    }
}
