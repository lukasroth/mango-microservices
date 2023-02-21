using Mango.MessageBus;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQCheckoutConsumer : BackgroundService
    {
        private readonly IRabbitMQOrderMessageSender _rabbitMQOrderMessageSender;
        private readonly OrderRepository _orderRepository;
        private IConnection _connection;
        private IModel _channel;
        private readonly IConfiguration _configuration;
        private readonly string CheckoutQueueName;
        private readonly string OrderPaymentProcessQueueName;
        private readonly string ConnectionHostname;
        private readonly string ConnectionUsername;
        private readonly string ConnectionPassword;


        public RabbitMQCheckoutConsumer(IRabbitMQOrderMessageSender rabbitMQOrderMessageSender, OrderRepository orderRepository, IConfiguration configuration)
        {
            _rabbitMQOrderMessageSender = rabbitMQOrderMessageSender;
            _orderRepository = orderRepository;
            _configuration = configuration;

            CheckoutQueueName = _configuration.GetValue<string>("CheckoutMessageTopic");
            OrderPaymentProcessQueueName = _configuration.GetValue<string>("OrderPaymentProcessTopic");
            ConnectionHostname = _configuration.GetValue<string>("ConnectionHostname");
            ConnectionUsername = _configuration.GetValue<string>("ConnectionUsername");
            ConnectionPassword = _configuration.GetValue<string>("ConnectionPassword");

            var factory = new ConnectionFactory
            {
                HostName = ConnectionHostname,
                UserName = ConnectionUsername,
                Password = ConnectionPassword,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: CheckoutQueueName, false, false, false, arguments: null);
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                var checkOutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(content);
                HandleMessage(checkOutHeaderDto).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);
            };
            _channel.BasicConsume(CheckoutQueueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(CheckoutHeaderDto checkoutHeaderDto)
        {
            var orderHeader = new OrderHeader
            {
                UserId = checkoutHeaderDto.UserId,
                FirstName = checkoutHeaderDto.FirstName,
                Lastname = checkoutHeaderDto.Lastname,
                OrderDetails = new List<OrderDetails>(),
                CardNumber = checkoutHeaderDto.CardNumber,
                CouponCode = checkoutHeaderDto.CouponCode,
                CVV = checkoutHeaderDto.CVV,
                DiscountTotal = checkoutHeaderDto.DiscountTotal,
                Email = checkoutHeaderDto.Email,
                ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
                OrderTime = DateTime.Now,
                OrderTotal = checkoutHeaderDto.OrderTotal,
                PaymentStatus = false,
                Phone = checkoutHeaderDto.Phone,
                PickupDateTime = checkoutHeaderDto.PickupDateTime,
            };
            foreach (var detailList in checkoutHeaderDto.CartDetails)
            {
                var orderDetails = new OrderDetails
                {
                    ProductId = detailList.ProductId,
                    ProductName = detailList.Product.Name,
                    Price = detailList.Product.Price,
                    Count = detailList.Count,
                };
                orderHeader.CartTotalItems += detailList.Count;
                orderHeader.OrderDetails.Add(orderDetails);
            }

            await _orderRepository.AddOrder(orderHeader);

            var paymentRequestMessage = new PaymentRequestMessage
            {
                Name = orderHeader.FirstName + " " + orderHeader.Lastname,
                CardNumber = orderHeader.CardNumber,
                CVV = orderHeader.CVV,
                ExpiryMonthYear = orderHeader.ExpiryMonthYear,
                OrderId = orderHeader.OrderHeaderId,
                OrderTotal = orderHeader.OrderTotal,
                Email = orderHeader.Email,
            };

            try
            {

                //await _messageBus.PublishMessage(paymentRequestMessage, orderPaymentProcessTopic);
                //await args.CompleteMessageAsync(args.Message);
                _rabbitMQOrderMessageSender.SendMessage(paymentRequestMessage, OrderPaymentProcessQueueName);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
