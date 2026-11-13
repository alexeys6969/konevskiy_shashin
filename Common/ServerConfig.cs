namespace Common
{
    public class ServerConfig
    {
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public int MaxClients { get; set; } = 5;
        public int SessionTimeoutSeconds { get; set; } = 60;
        public int RenewalIntervalSeconds { get; set; } = 15;
    }
}
