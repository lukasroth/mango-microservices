using Mango.MessageBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.Services.PaymentAPI.RabbitMQSender
{
    public class RabbitMQPaymentMessageSender : IRabbitMQPaymentMessageSender
    {
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;
        private readonly string _exchangeName;
        private readonly string _paymentEmailUpdateQueueName;
        private readonly string _paymentOrderUpdateQueueName;
        private readonly string _paymentEmailRoute;
        private readonly string _paymentOrderRoute;
        private IConnection _connection;
        private readonly IConfiguration _configuration;
        

        public RabbitMQPaymentMessageSender(IConfiguration configuration)
        {
            _configuration = configuration;

            _hostname = _configuration.GetValue<string>("ConnectionHostname");
            _password = _configuration.GetValue<string>("ConnectionUsername");
            _username = _configuration.GetValue<string>("ConnectionPassword");
            _exchangeName = _configuration.GetValue<string>("ExchangeName");
            _paymentEmailUpdateQueueName = _configuration.GetValue<string>("PaymentEmailUpdateQueueName");
            _paymentOrderUpdateQueueName = _configuration.GetValue<string>("PaymentOrderUpdateQueueName");
            _paymentEmailRoute = _configuration.GetValue<string>("PaymentEmailRoute");
            _paymentOrderRoute = _configuration.GetValue<string>("PaymentOrderRoute");
        }

        public void SendMessage(BaseMessage message)
        {
            if (ConnectionExists()) { 

                using var channel = _connection.CreateModel();
                channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: false);
                channel.QueueDeclare(_paymentOrderUpdateQueueName, false, false, false, null);
                channel.QueueDeclare(_paymentEmailUpdateQueueName, false, false, false, null);

                channel.QueueBind(_paymentEmailUpdateQueueName, _exchangeName, _paymentEmailRoute);
                channel.QueueBind(_paymentOrderUpdateQueueName, _exchangeName, _paymentOrderRoute);

                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: _exchangeName, _paymentEmailRoute, basicProperties: null, body: body);
                channel.BasicPublish(exchange: _exchangeName, _paymentOrderRoute, basicProperties: null, body: body);
            }
        }

        private void CreateConnection()
        { 
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostname,
                    UserName = _username,
                    Password = _password,
                };
                _connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                //log exception
            }
        }

        private bool ConnectionExists()
        {
            if(_connection != null )
            {
                return true;
            }
            CreateConnection();
            return _connection != null;
        }
    }
}
