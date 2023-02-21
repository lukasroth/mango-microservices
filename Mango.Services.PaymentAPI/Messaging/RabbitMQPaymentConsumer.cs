using Mango.MessageBus;
using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private readonly IRabbitMQPaymentMessageSender _rabbitMQPaymentMessageSender;
        private readonly IProcessPayment _processPayment;
        private IConnection _connection;
        private IModel _channel;
        private IConfiguration _configuration;
        private readonly string orderPaymentProcessQueueName;
        private readonly string connectionHostname;
        private readonly string connectionUsername;
        private readonly string connectionPassword;

        public RabbitMQPaymentConsumer(IRabbitMQPaymentMessageSender rabbitMQPaymentMessageSender, IProcessPayment processPayment, IConfiguration configuration)
        {
            _rabbitMQPaymentMessageSender = rabbitMQPaymentMessageSender;
            _processPayment = processPayment;
            _configuration = configuration;

            orderPaymentProcessQueueName = _configuration.GetValue<string>("OrderPaymentProcessTopic");
            connectionHostname = _configuration.GetValue<string>("ConnectionHostname");
            connectionUsername = _configuration.GetValue<string>("ConnectionUsername");
            connectionPassword = _configuration.GetValue<string>("ConnectionPassword");

            var factory = new ConnectionFactory
            {
                HostName = connectionHostname,
                UserName = connectionUsername,
                Password = connectionPassword
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: orderPaymentProcessQueueName, false, false, false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);
                HandleMessage(paymentRequestMessage).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);
            };
            _channel.BasicConsume(orderPaymentProcessQueueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
        {
            var result = _processPayment.PaymentProcessor();

            var updatePaymentResultMessage = new UpdatePaymentResultMessage
            {
                Status = result,
                OrderId = paymentRequestMessage.OrderId,
                Email = paymentRequestMessage.Email,
            };

            try
            {
                _rabbitMQPaymentMessageSender.SendMessage(updatePaymentResultMessage);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
