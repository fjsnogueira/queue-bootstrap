using Consumer.Domain.Factories.Configurations;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Collections.Generic;

namespace Consumidor.Infraestrutura.RabbitMQ
{
    public interface IMessagingFactory
    {
        IModel Connect();
        void Disconnect();
    }

    public class MessagingFactory : IMessagingFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly Messaging _messaging;
        private IModel _channel;
        private IConnection _connection;

        public MessagingFactory(
            IConnectionFactory connectionFactory,
            IOptions<Messaging> messaging)
        {
            _connectionFactory = connectionFactory;
            _messaging = messaging.Value;
        }

        public IModel Connect()
        {
            if (_channel != null)
                return _channel;

            _connection = _connectionFactory.CreateConnection();

            _channel = _connection.CreateModel();

            // Errors queue
            _channel.QueueDeclare(_messaging.Error.Queue, true, false, false, null);

            // Deadletter exchange and queue
            _channel.ExchangeDeclare(_messaging.Consuming.Deadletter.Exchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(_messaging.Consuming.Deadletter.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Exchange },
                { "x-dead-letter-routing-key", _messaging.Consuming.Bindingkey },
                { "x-message-ttl", _messaging.TTL }
            });
            _channel.QueueBind(_messaging.Consuming.Deadletter.Queue, _messaging.Consuming.Deadletter.Exchange, _messaging.Consuming.Deadletter.Routingkey);

            // This service exchange and queue
            _channel.ExchangeDeclare(_messaging.Consuming.Exchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(_messaging.Consuming.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Deadletter.Exchange },
                { "x-dead-letter-routing-key", _messaging.Consuming.Deadletter.Routingkey }
            });
            _channel.QueueBind(_messaging.Consuming.Queue, _messaging.Consuming.Exchange, _messaging.Consuming.Bindingkey);
            
            _connection.ConnectionShutdown += (x, y) => ConnectionReconnection();
            _channel.ModelShutdown += (x, y) => ChannelReconnection();

            return _channel;
        }

        public void Disconnect()
        {
            if (_connection.IsOpen)
                _connection.Close();
        }

        private void ConnectionReconnection()
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = Connect();
        }

        private void ChannelReconnection()
        {
            if (_connection.IsOpen)
            {
                _channel = Connect();
            }
            else
            {
                ConnectionReconnection();
            }
        }
    }
}
