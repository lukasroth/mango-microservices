using Mango.Services.Email.Messages;
using Mango.Services.Email.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.PaymentAPI.Messaging
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
        private readonly string PaymentEmailUpdateQueueName;
        private readonly string PaymentEmailRoutingKey;


        private readonly EmailRepository _emailRepository;
        public RabbitMQPaymentConsumer(EmailRepository emailRepository, IConfiguration configuration)
        {
            _emailRepository = emailRepository;
            _configuration = configuration;
            ConnectionHostname = _configuration.GetValue<string>("ConnectionHostname");
            ConnectionUsername = _configuration.GetValue<string>("ConnectionUsername");
            ConnectionPassword = _configuration.GetValue<string>("ConnectionPassword");
            ExchangeName = _configuration.GetValue<string>("DirectPaymentUpdateExchangeName");
            PaymentEmailUpdateQueueName = _configuration.GetValue<string>("PaymentEmailUpdateQueueName");
            PaymentEmailRoutingKey = _configuration.GetValue<string>("PaymentEmailRoutingKey");

            var factory = new ConnectionFactory
            {
                HostName = ConnectionHostname,
                UserName = ConnectionUsername,
                Password = ConnectionPassword,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct);
            _channel.QueueDeclare(PaymentEmailUpdateQueueName, false, false, false, null);
            _channel.QueueBind(PaymentEmailUpdateQueueName, ExchangeName, PaymentEmailRoutingKey);
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
            _channel.BasicConsume(PaymentEmailUpdateQueueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentRequestMessage)
        {
            try
            {
                await _emailRepository.SendAndLogEmail(updatePaymentRequestMessage);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
