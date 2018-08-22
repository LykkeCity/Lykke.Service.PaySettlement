namespace Lykke.Service.PaySettlement.Settings
{
    public class MatchingEngineSettings
    {
        public IpEndpointSettings IpEndpoint { get; set; }
    }

    public class IpEndpointSettings
    {
        public string Host { get; set; }

        public int Port { get; set; }
    }
}
