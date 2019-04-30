using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Surescripts.WebUI.Services
{
    public interface IMQClient : IDisposable
    {
        void Send(object o);
    }

    public class MQClient : IMQClient
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConfiguration config;
        private readonly ILogger<MQClient> logger;

        IConnection _connection;
        bool _disposed;

        public MQClient(IConfiguration config, ILogger<MQClient> logger)
        {
            this.config = config;
            this.logger = logger;
            
            _connectionFactory = new ConnectionFactory()
            {
                HostName = getEnv("RABBIT_HOST", "localhost"),
                UserName = getEnv("RABBIT_USER", "rabbit"),
                Password = getEnv("RABBIT_PASS", "Cideloh7")            
            };
        }

        public void Send(object o)
        {
            var queue = getEnv("RABBIT_QUEUE", "calc") ;

            logger.LogInformation($"RabbitMQ sending message to {queue} queue...");
            
            if (!IsConnected)
            {
                logger.LogWarning($"Not connected - connecting to RabbitMQ...");
                TryConnect();
            }
            using (var channel = CreateModel())
            {
                channel.QueueDeclare(queue: queue,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                string message = JsonConvert.SerializeObject(o);
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "",
                                     routingKey: queue,
                                     basicProperties: null,
                                     body: body);
                logger.LogInformation($"RabbitMQ sent {message} to {queue}");
            }
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }
            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            try
            {
                logger.LogInformation("RabbitMQ Client is trying to connect...");
                _connection = _connectionFactory.CreateConnection();
            }
            catch (BrokerUnreachableException e)
            {
                Thread.Sleep(5000);
                logger.LogWarning($"RabbitMQ Client had error [{e.Message}] and is trying to reconnect...");
                _connection = _connectionFactory.CreateConnection();
            }

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                logger.LogInformation($"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");
                return true;
            }
            else
            {
                logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                return false;
            }
        }

        private string getEnv(string e, string d)
        {
            var v = config[e];
            return string.IsNullOrWhiteSpace(v) ? d : v;
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            logger.LogInformation("A RabbitMQ connection is shutdown. Trying to re-connect...");
            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            logger.LogError("A RabbitMQ connection throw exception. Trying to re-connect...");
            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            logger.LogInformation("A RabbitMQ connection is on shutdown. Trying to re-connect...");
            TryConnect();
        }
    }
}
