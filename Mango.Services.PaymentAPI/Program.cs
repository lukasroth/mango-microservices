using Mango.MessageBus;
using Mango.Services.PaymentAPI.Extensions;
using Mango.Services.PaymentAPI.Messaging;
using Mango.Services.PaymentAPI.RabbitMQSender;
using PaymentProcessor;

namespace Mango.Services.PaymentAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSingleton<IProcessPayment, ProcessPayment>();
            builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
            builder.Services.AddSingleton<IMessageBus>(new AzureServiceBusMessageBus(builder.Configuration.GetConnectionString("AzureConnection")));
            builder.Services.AddSingleton<IRabbitMQPaymentMessageSender, RabbitMQPaymentMessageSender>();

            builder.Services.AddHostedService<RabbitMQPaymentConsumer>();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.UseAzureServiceBusConsumer();
            app.Run();
        }
    }
}