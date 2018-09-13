using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lykke.HttpClientGenerator.Infrastructure;
using Lykke.Service.PaySettlement.Client;
using Xunit;

namespace Lykke.Service.PaySettlement.Tests
{
    public class TestClient
    {
        //[Fact]
        //public async Task Test1()
        //{
        //    var clientBuilder = HttpClientGenerator.HttpClientGenerator.BuildForUrl("http://localhost:5000")
        //        .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper());

        //    clientBuilder = clientBuilder.WithoutRetries();

        //    var client = new PaySettlementClient(clientBuilder.Create());
        //    var result1 = await client.Api.GetPaymentRequestAsync("Begun1", "0f7cb15a-3e2c-4bb3-be6a-c2ce93baf676");
        //    var result2 = await client.Api.GetPaymentRequestsByMerchantAsync("Begun1", 10);
        //}
    }
}
