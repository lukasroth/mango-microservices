using Mango.Services.Email.Messaging;

namespace Mango.Services.Email.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IAzureServiceBusConsumer ServiceBusConsumer { get; set; }
        public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
        {
            ServiceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();

            var hostApplicationLive = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            hostApplicationLive.ApplicationStarted.Register(OnStart);
            hostApplicationLive.ApplicationStopped.Register(OnStop);
            return app;
        }

        private static void OnStart()
        {
            ServiceBusConsumer.Start();

        }

        private static void OnStop()
        { 
            ServiceBusConsumer.Stop();
        }
    }
}
