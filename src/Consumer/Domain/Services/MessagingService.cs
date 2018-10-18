using Consumidor.Infraestrutura.RabbitMQ;
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
        private readonly IMessagingFactory _messagingFactory;

        public MessagingService(
            IMessagingFactory messagingFactory)
        {
            _messagingFactory = messagingFactory;
        }

        public AsyncEventingBasicConsumer Consume(Action listen, Func<Exception, T, Task> callback)
        {
            using (var channel = _messagingFactory.Connect())
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

                //try
                //{
                //    tentativasUtilizadas = CountXDeath(ea);

                //    var mensagem = JsonConvert.DeserializeObject<T>(message);

                //    if (TENTATIVAS > tentativasUtilizadas)
                //    {
                //        if (callback != null)
                //        {
                //            await callback.Invoke(null, mensagem);
                //        }
                //    }
                //    else
                //    {
                //        _rabbitMqService.Push(_configRabbitMQ.DeadLetter.FilaErro.Nome, body, channel);

                //        channel?.BasicAck(ea.DeliveryTag, false);
                //    }

                //    channel?.BasicAck(ea.DeliveryTag, false);
                //}
                //catch (Exception ex)
                //{
                //    if (_configRabbitMQ.DeadLetter.Tentativas > tentativasUtilizadas)
                //    {
                //        channel.BasicNack(ea.DeliveryTag, false, false);
                //    }
                //    else
                //    {
                //        _rabbitMqService.Push(_configRabbitMQ.DeadLetter.FilaErro.Nome, body, channel);
                //        channel?.BasicAck(ea.DeliveryTag, false);
                //    }
                //}
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
