namespace Consumer.Domain.Factories.Configurations
{
    public class Messaging
    {
        public string Host { get; set; }
        public string VirtualHost { get; set; }
        public short Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool Durable { get; set; }
        public short TTL { get; set; }
        public short Retries { get; set; }
    }

    public class Consume
    {

    }

    public class Publish
    {

    }


}
