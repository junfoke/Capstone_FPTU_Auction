using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace BE_AuctionOT_Cronjob.RabbitMQ.BidQueue.Publishers
{
    public class BidPublisher : IBidPublisher
    {
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private const int RetryDelay = 5000;
        private const int MaxRetries = 5;

        public BidPublisher()
        {
            _factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                Port = 5672,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(30)
            };

            ConnectToRabbitMQ();
        }

        private readonly object _lock = new object();
        private void ConnectToRabbitMQ()
        {
            if (_connection != null && _connection.IsOpen)
            {
                Console.WriteLine("Connection to RabbitMQ already established.");
                return;
            }

            int retryCount = 0;

            lock (_lock)
            {
                while (retryCount < MaxRetries)
                {
                    try
                    {
                        if (_connection == null || !_connection.IsOpen)
                        {
                            _connection = _factory.CreateConnection();
                            Console.WriteLine("Connected to RabbitMQ successfully.");
                        }
                        return; // Exit if the connection is successful
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        retryCount++;
                        Console.WriteLine($"Connection failed: {ex.Message}. Retrying ({retryCount}/{MaxRetries})...");
                        Thread.Sleep(RetryDelay);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error: {ex.Message}");
                        throw; // Rethrow if it's an unknown exception
                    }
                }
            }

            Console.WriteLine("Failed to connect to RabbitMQ after multiple attempts.");
            throw new Exception("Could not establish a connection to RabbitMQ.");
        }


        public void CreateANewQueue(string queueName)
        {
            try
            {
                using (var channel = _connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating queue: {ex.Message}");
            }
        }

        public void PublishBid(Bid bid)
        {
            using (var channel = _connection.CreateModel())
            {
                string queueName = $"bidsQueue_{bid.AuctionId}";

                // Declare the queue for the specific auction
                channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var message = JsonSerializer.Serialize(bid);
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
            }
        }
    }

    public class Bid
    {
        public int AuctionId { get; set; }
        public int UserId { get; set; }
        public decimal BidAmount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
