using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Consumer.Domain.Factories
{
    public class MessagingFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger _loggger;
        private IConnection _conn;
        private IModel _channel;
        private RabbitConnectorRequest _configRabbitMQ;

        public RabbitMQService(IConnectionFactory connectionFactory, ILogger<RabbitMQService> logger)
        {
            _connectionFactory = connectionFactory;
            _loggger = logger;
        }

        public IModel CriarModel(RabbitConnectorRequest configRabbitMQ)
        {
            _configRabbitMQ = configRabbitMQ;
            _conn = _connectionFactory.CreateConnection();
            _channel = _conn.CreateModel();

            CriarInfraRabbitMQ(_channel);

            _conn.ConnectionShutdown += (x, y) => ReconectarRabbit();
            _channel.ModelShutdown += (x, y) => ReconectarChannel();

            return _channel;
        }

        public long CountXDeath(BasicDeliverEventArgs ea)
        {
            long contador = 0;
            var dic = ea.BasicProperties.Headers as Dictionary<string, object>;
            if (dic != null && dic.ContainsKey("x-death"))
            {
                var xdeath = ea.BasicProperties.Headers["x-death"] as List<object>;
                if (xdeath != null)
                {
                    var last = xdeath.FirstOrDefault() as Dictionary<string, object>;
                    if (last != null)
                    {
                        contador = (long)last["count"];
                    }
                }
            }

            return contador++;
        }

        public void Push(string fila, byte[] mensagem, IModel model)
        {
            model.BasicPublish(string.Empty, fila, null, mensagem);
        }

        private void CriarInfraRabbitMQ(IModel channel)
        {
            if (!string.IsNullOrEmpty(_configRabbitMQ.FilaConsumer?.Nome) && !string.IsNullOrEmpty(_configRabbitMQ.FilaConsumer?.Exchange))
            {
                // create error queue
                channel.QueueDeclare(_configRabbitMQ.DeadLetter.FilaErro.Nome, true, false, false, null);

                // create retry exchange && queue
                channel.ExchangeDeclare(exchange: _configRabbitMQ.DeadLetter.Exchange, type: ExchangeType.Topic, durable: true);
                channel.QueueDeclare(_configRabbitMQ.DeadLetter.Nome, true, false, false, new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", _configRabbitMQ.FilaConsumer.Exchange },
                    { "x-dead-letter-routing-key", _configRabbitMQ.FilaConsumer.RoutingKey },
                    { "x-message-ttl", _configRabbitMQ.DeadLetter.TempoMilesegundos }
                });
                channel.QueueBind(_configRabbitMQ.DeadLetter.Nome, _configRabbitMQ.DeadLetter.Exchange, _configRabbitMQ.DeadLetter.RoutingKey);

                // create main exchange && queue
                channel.ExchangeDeclare(exchange: _configRabbitMQ.FilaConsumer.Exchange, type: ExchangeType.Topic, durable: true);
                channel.QueueDeclare(_configRabbitMQ.FilaConsumer.Nome, true, false, false, new Dictionary<string, object>()
                {
                    { "x-dead-letter-exchange", _configRabbitMQ.DeadLetter.Exchange },
                    { "x-dead-letter-routing-key", _configRabbitMQ.DeadLetter.RoutingKey }
                });
                channel.QueueBind(_configRabbitMQ.FilaConsumer.Nome, _configRabbitMQ.FilaConsumer.Exchange, _configRabbitMQ.FilaConsumer.RoutingKey);
            }
        }

        private void ReconectarRabbit()
        {
            _loggger.LogInformation("Tentando reconectar rabbitMQ (CHANNEL)");
            _conn = _connectionFactory.CreateConnection();
            _channel = _conn.CreateModel();
        }

        private void ReconectarChannel()
        {
            _loggger.LogInformation("Tentando reconectar rabbitMQ (CONEXAO)");
            if (_conn.IsOpen)
            {
                _channel = _conn.CreateModel();
            }
            else
            {
                _loggger.LogInformation("Conexao do rabbit foi fechada. Tentar reconexao.");
                ReconectarRabbit();
            }
        }
    }
}
