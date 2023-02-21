using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly IConfiguration _configuration;
        private readonly string ConnectionHostname;
        private readonly string ConnectionUsername;
        private readonly string ConnectionPassword;
        private readonly string ExchangeName;
        private readonly string PaymentOrderUpdateQueueName;
        private readonly string PaymentOrderRoute;

        private readonly OrderRepository _orderRepository;

        public RabbitMQPaymentConsumer(OrderRepository orderRepository, IConfiguration configuration)
        {
            _orderRepository = orderRepository;
            _configuration = configuration;
            ConnectionHostname = _configuration.GetValue<string>("ConnectionHostname");
            ConnectionUsername = _configuration.GetValue<string>("ConnectionUsername");
            ConnectionPassword = _configuration.GetValue<string>("ConnectionPassword");
            ExchangeName = _configuration.GetValue<string>("DirectPaymentUpdateExchangeName");
            PaymentOrderUpdateQueueName = _configuration.GetValue<string>("PaymentOrderUpdateQueueName");
            PaymentOrderRoute = _configuration.GetValue<string>("PaymentOrderRoute");

            var factory = new ConnectionFactory
            {
                HostName = ConnectionHostname,
                UserName = ConnectionUsername,
                Password = ConnectionPassword,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel(); 

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(PaymentOrderUpdateQueueName, false, false, false, null);
            _channel.QueueBind(PaymentOrderUpdateQueueName, ExchangeName, PaymentOrderRoute);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var paymentRequestMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(content);
                HandleMessage(paymentRequestMessage).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);
            };
            _channel.BasicConsume(PaymentOrderUpdateQueueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentRequestMessage)
        {
            try
            {
                await _orderRepository.UpdateOrderPaymentStatus(updatePaymentRequestMessage.OrderId, updatePaymentRequestMessage.Status);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
