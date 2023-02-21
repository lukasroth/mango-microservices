using Mango.Services.OrderAPI.Repository;

namespace Mango.Services.OrderAPI.Messaging
{
    public interface IAzureServiceBusConsumer
    {
        Task Start();
        Task Stop();
    }
}
