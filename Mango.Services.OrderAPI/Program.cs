using AutoMapper;
using Mango.MessageBus;
using Mango.Services.OrderAPI.DbContexts;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Messaging;
using Mango.Services.OrderAPI.RabbitMQSender;
using Mango.Services.OrderAPI.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Mango.Services.OrderAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options => {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
            builder.Services.AddSingleton(mapper);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            var optionBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

            builder.Services.AddHostedService<RabbitMQPaymentConsumer>();
            builder.Services.AddHostedService<RabbitMQCheckoutConsumer>();
            builder.Services.AddSingleton(new OrderRepository(optionBuilder.Options));
            builder.Services.AddSingleton<IAzureServiceBusConsumer, AzureServiceBusConsumer>();
            builder.Services.AddSingleton<IMessageBus>(new AzureServiceBusMessageBus(builder.Configuration.GetConnectionString("AzureConnection")));
            builder.Services.AddSingleton<IRabbitMQOrderMessageSender, RabbitMQOrderMessageSender>();
            builder.Services.AddControllers();
            
            builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = builder.Configuration.GetValue<string>("IdentityServerHost");
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", polizy =>
                {
                    polizy.RequireAuthenticatedUser();
                    polizy.RequireClaim("scope", "mango");
                });
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"Enter 'Bearer' [space] and your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In=ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseAzureServiceBusConsumer();

            app.Run();
        }
    }
}