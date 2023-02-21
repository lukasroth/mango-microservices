﻿using Mango.MessageBus;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace Mango.Services.OrderAPI.RabbitMQSender
{
    public class RabbitMQOrderMessageSender : IRabbitMQOrderMessageSender
    {
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;
        private IConnection _connection;
        private readonly IConfiguration _configuration;

        public RabbitMQOrderMessageSender(IConfiguration configuration)
        {
            _configuration = configuration;
            _hostname = _configuration.GetValue<string>("ConnectionHostname");
            _password = _configuration.GetValue<string>("ConnectionUsername");
            _username = _configuration.GetValue<string>("ConnectionPassword");
        }

        public void SendMessage(BaseMessage message, string queueName)
        {
            if (ConnectionExists()) { 

                using var channel = _connection.CreateModel();
                channel.QueueDeclare(queue: queueName, false, false, false, arguments: null);
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
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