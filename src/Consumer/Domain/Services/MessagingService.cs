using Consumer.Domain.Factories.Configurations;
using Consumidor.Infraestrutura.RabbitMQ;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer.Domain.Services
{
    public interface IMessagingService<T>
    {
        AsyncEventHandler<BasicDeliverEventArgs> Dequeue(Func<Exception, T, Task> callback);
        void Queue(string exchange, string routingKey, string message, Dictionary<string, object> headers = null);
    }

    public class MessagingService<T> : IMessagingService<T>
    {
        private readonly IMessagingFactory _messagingFactory;
        private readonly Messaging _messaging;

        public MessagingService(
            IMessagingFactory messagingFactory,
            IOptions<Messaging> messaging)
        {
            _messagingFactory = messagingFactory ?? throw new ArgumentNullException(nameof(messagingFactory));
            _messaging = messaging.Value ?? throw new ArgumentNullException(nameof(messaging));
        }

        public AsyncEventHandler<BasicDeliverEventArgs> Dequeue(Func<Exception, T, Task> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var channel = _messagingFactory.Configure();
            
            return async (model, ea) =>
            {
                Console.WriteLine("Foi");

                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                var retries = 0;

                try
                {
                    retries = Retries(ea);

                    var mensagem = JsonConvert.DeserializeObject<T>(message);

                    if (_messaging.Retries > retries)
                    {
                        await callback.Invoke(null, mensagem);
                    }
                    else
                    {
                        var headers = new Dictionary<string, object>
                        {
                            { "queue", _messaging.Consuming.Queue }
                        };

                        Queue(_messaging.Error.Exchange, _messaging.Error.Routingkey, message, headers);
                    }

                    channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    if (_messaging.Retries > retries)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        var headers = new Dictionary<string, object>
                        {
                            { "queue", _messaging.Consuming.Queue }
                        };

                        Queue(_messaging.Error.Exchange, _messaging.Error.Routingkey, message, headers);

                        channel.BasicAck(ea.DeliveryTag, false);
                    }

                    var obj = (T)Activator.CreateInstance(typeof(T));

                    await callback.Invoke(ex, obj);
                }
            };
        }

        public void Queue(string exchange, string routingKey, string message, Dictionary<string, object> headers = null)
        {
            var channel = _messagingFactory.Configure();

            var properties = channel.CreateBasicProperties();
            properties.Headers = headers;
            properties.Persistent = true;

            channel.BasicPublish(exchange, routingKey, false, properties, Encoding.ASCII.GetBytes(message));
        }

        private int Retries(BasicDeliverEventArgs ea)
        {
            int count = 0;

            if (ea.BasicProperties.Headers is Dictionary<string, object> dic && dic.ContainsKey("x-death"))
            {
                if (ea.BasicProperties.Headers["x-death"] is List<object> xdeath)
                {
                    if (xdeath.FirstOrDefault() is Dictionary<string, object> headers)
                    {
                        count = (int)headers["count"];
                    }
                }
            }

            return count++;
        }
    }
}
