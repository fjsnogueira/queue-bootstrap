using Consumer.Domain.Factories.Configurations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer.Domain.Services
{
    public class MessagingService<T>
    {
        public MessagingService(IOptions<Messaging> messaging)
        {

        }

        const short TENTATIVAS = 3;
        const string QUEUE_ERRO = "queue_erro";
        const string QUEUE_DEADLETTER_EXCHANTE = "deadletter_exchange";

        public AsyncEventingBasicConsumer ProcessarMensagensFilaAsync(Action listen, Func<Exception, T, Task> callback)
        {
            IConnectionFactory connectionFactory = new ConnectionFactory();
            var connection = connectionFactory.CreateConnection();
            IModel model = connection.CreateModel();
            model.QueueDeclare(QUEUE_ERRO, true, false, false, null);

            // create retry exchange && queue
            model.ExchangeDeclare(exchange: _configRabbitMQ.DeadLetter.Exchange, type: ExchangeType.Topic, durable: true);
            model.QueueDeclare(_configRabbitMQ.DeadLetter.Nome, true, false, false, new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", _configRabbitMQ.FilaConsumer.Exchange },
                    { "x-dead-letter-routing-key", _configRabbitMQ.FilaConsumer.RoutingKey },
                    { "x-message-ttl", _configRabbitMQ.DeadLetter.TempoMilesegundos }
                });
            model.QueueBind(_configRabbitMQ.DeadLetter.Nome, _configRabbitMQ.DeadLetter.Exchange, _configRabbitMQ.DeadLetter.RoutingKey);

            // create main exchange && queue
            model.ExchangeDeclare(exchange: _configRabbitMQ.FilaConsumer.Exchange, type: ExchangeType.Topic, durable: true);
            model.QueueDeclare(_configRabbitMQ.FilaConsumer.Nome, true, false, false, new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", _configRabbitMQ.DeadLetter.Exchange },
                    { "x-dead-letter-routing-key", _configRabbitMQ.DeadLetter.RoutingKey }
                });
            model.QueueBind(_configRabbitMQ.FilaConsumer.Nome, _configRabbitMQ.FilaConsumer.Exchange, _configRabbitMQ.FilaConsumer.RoutingKey);

            using (var channel = model)
            {
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += ProcessarMensagemRecebidaAsync(channel, callback);

                var consumerTag = channel.BasicConsume("FILA CONSUMO", false, consumer);

                listen();

                channel.BasicCancel(consumerTag);

                return consumer;
            }
        }

        private long CountXDeath(BasicDeliverEventArgs ea)
        {
            long count = 0;

            if (ea.BasicProperties.Headers is Dictionary<string, object> dic && dic.ContainsKey("x-death"))
            {
                if (ea.BasicProperties.Headers["x-death"] is List<object> xdeath)
                {
                    if (xdeath.FirstOrDefault() is Dictionary<string, object> headers)
                    {
                        count = (long)headers["count"];
                    }
                }
            }

            return count++;
        }

        private AsyncEventHandler<BasicDeliverEventArgs> ProcessarMensagemRecebidaAsync(IModel channel, Func<Exception, T, Task> callback)
        {
            return async (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                long tentativasUtilizadas = 0;

                try
                {
                    tentativasUtilizadas = CountXDeath(ea);

                    var mensagem = JsonConvert.DeserializeObject<T>(message);

                    if (TENTATIVAS > tentativasUtilizadas)
                    {
                        if (callback != null)
                        {
                            await callback.Invoke(null, mensagem);
                        }
                    }
                    else
                    {
                        _rabbitMqService.Push(_configRabbitMQ.DeadLetter.FilaErro.Nome, body, channel);

                        channel?.BasicAck(ea.DeliveryTag, false);
                    }

                    channel?.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    if (_configRabbitMQ.DeadLetter.Tentativas > tentativasUtilizadas)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        _rabbitMqService.Push(_configRabbitMQ.DeadLetter.FilaErro.Nome, body, channel);
                        channel?.BasicAck(ea.DeliveryTag, false);
                    }
                }
            };
        }

        private void ReenviarMensagemFila(string fila, byte[] mensagem, IModel channel, int tentativas = 3)
        {
            var propriedades = channel.CreateBasicProperties();
            propriedades.ContentType = "text/plain";
            propriedades.DeliveryMode = 2;
            propriedades.Headers = new Dictionary<string, object> { ["x-redeliver-limit"] = tentativas };

            channel.BasicPublish(string.Empty, fila, propriedades, mensagem);
        }
    }
}
