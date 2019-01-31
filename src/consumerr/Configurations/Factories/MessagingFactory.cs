using Microsoft.Extensions.Options;
using Consumer.Domains.Models.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Consumer.Configurations.Factories
{
    public interface IMessagingFactory
    {
        IModel Configure();
        void Disconnect();
    }

    public class MessagingFactory : IMessagingFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly Messaging _messaging;
        private IModel _channel;
        private IConnection _connection;

        public MessagingFactory(IOptions<Messaging> messaging)
        {
            _messaging = messaging.Value ?? throw new ArgumentNullException(nameof(messaging));

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _messaging.Host,
                Port = _messaging.Port,
                UserName = _messaging.User,
                Password = _messaging.Password,
                VirtualHost = _messaging.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = 200
            };
        }

        public IModel Configure()
        {
            if (_channel != null)
            {
                return _channel;
            }   

            _connection = _connectionFactory.CreateConnection();

            _channel = _connection.CreateModel();

            // Creating of error queues, when the microservice can't process a message for an determined amount of tries, it goes to this queue
            _channel.ExchangeDeclare(_messaging.Error.Exchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(_messaging.Error.Queue, true, false, false, null);
            _channel.QueueBind(_messaging.Error.Queue, _messaging.Error.Exchange, _messaging.Error.Routingkey);

            // The deadletter of this microservices' queue, when it can't process a message, that message goes to deadletter, after an amount of seconds, that message comes back to the original queue to be reprocessed
            _channel.ExchangeDeclare(_messaging.Consuming.Deadletter.Exchange, ExchangeType.Direct, true);
            _channel.QueueDeclare(_messaging.Consuming.Deadletter.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Exchange },
                { "x-dead-letter-routing-key", _messaging.Consuming.Bindingkey },
                { "x-message-ttl", _messaging.TTL }
            });
            _channel.QueueBind(_messaging.Consuming.Deadletter.Queue, _messaging.Consuming.Deadletter.Exchange, _messaging.Consuming.Deadletter.Routingkey);

            // The queue this microservices will watch for new messages
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
            {
                _connection.Close();
            }
        }

        private void ConnectionReconnection()
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = Configure();
        }

        private void ChannelReconnection()
        {
            if (_connection.IsOpen)
            {
                _channel = Configure();
            }
            else
            {
                ConnectionReconnection();
            }
        }
    }
}
