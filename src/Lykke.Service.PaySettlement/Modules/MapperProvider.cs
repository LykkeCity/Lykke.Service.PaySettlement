using AutoMapper;
using AutoMapper.Configuration;
using Lykke.Job.BlockchainCashinDetector.Contract.Events;
using Lykke.Service.PayInternal.Contract.PaymentRequest;
using Lykke.Service.PaySettlement.Contracts.Commands;
using Lykke.Service.PaySettlement.Contracts.Events;
using Lykke.Service.PaySettlement.Core.Domain;
using PaymentRequest = Lykke.Service.PaySettlement.Core.Domain.PaymentRequest;

namespace Lykke.Service.PaySettlement.Modules
{
    public class MapperProvider
    {
        public IMapper GetMapper()
        {
            var mce = new MapperConfigurationExpression();

            CreateCqrsMaps(mce);
            CreatePaymentRequestControllerMaps(mce);

            var mc = new MapperConfiguration(mce);
            mc.AssertConfigurationIsValid();

            return new Mapper(mc);
        }

        private void CreateCqrsMaps(MapperConfigurationExpression mce)
        {
            mce.CreateMap<PaymentRequestDetailsMessage, PaymentRequestConfirmedEvent>(MemberList.Destination)
                .ForMember(d => d.PaymentRequestId, e => e.MapFrom(s => s.Id))
                .ForMember(d => d.PaymentRequestTimestamp, e => e.MapFrom(s => s.Timestamp));

            mce.CreateMap<PaymentRequestConfirmedEvent, CreateSettlementCommand>();

            mce.CreateMap<CreateSettlementCommand, PaymentRequest>(MemberList.Destination)
                .ForMember(d => d.TransferToMarketTransactionHash, e => e.Ignore())
                .ForMember(d => d.TransferToMarketTransactionFee, e => e.Ignore())
                .ForMember(d => d.SettlementCreatedUtc, e => e.Ignore())
                .ForMember(d => d.TransferedToMarketUtc, e => e.Ignore())
                .ForMember(d => d.ExchangedUtc, e => e.Ignore())
                .ForMember(d => d.TransferedToMerchantUtc, e => e.Ignore())
                .ForMember(d => d.ExchangeAmount, e => e.Ignore())
                .ForMember(d => d.MarketPrice, e => e.Ignore())
                .ForMember(d => d.MarketOrderId, e => e.Ignore())
                .ForMember(d => d.TransferredAmount, e => e.Ignore())
                .ForMember(d => d.MerchantClientId, e => e.Ignore())
                .ForMember(d => d.SettlementStatus, e => e.UseValue(SettlementStatus.None))
                .ForMember(d => d.Error, e => e.Ignore())
                .ForMember(d => d.ErrorDescription, e => e.Ignore());

            mce.CreateMap<CashinCompletedEvent, ExchangeCommand>(MemberList.Destination);

            mce.CreateMap<SettlementProcessingError, Contracts.SettlementProcessingError>();
        }

        private void CreatePaymentRequestControllerMaps(MapperConfigurationExpression mce)
        {
            mce.CreateMap<IPaymentRequest, Models.PaymentRequestModel>();
        }
    }
}
