using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Mango.Services.Identity
{
    public static class SD
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";

        //private readonly string RedirectUrl = ConfigurationBinder.

        public static IConfiguration AppSetting { get; }
        static SD()
        {
            AppSetting = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public static IEnumerable<IdentityResource> IdentityResources => new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Email(),
            new IdentityResources.Profile(),
        };

        public static IEnumerable<ApiScope> ApiScopes => new List<ApiScope>
        {
            new ApiScope("Mango", "Mango Server"),
            new ApiScope("read", "Read your data."),
            new ApiScope("write", "Write your data."),
            new ApiScope("delete", "Delete your data."),
        };

        public static IEnumerable<Client> Clients => new List<Client>
        {
            new Client
            {
                ClientId = "Client",
                ClientSecrets = { new Secret(AppSetting["ClientSecret"].Sha256())},
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                AllowedScopes = {"read", "write", "profile" }
            },
            new Client
            {
                ClientId = "mango",
                ClientSecrets = { new Secret(AppSetting["ClientSecret"].Sha256())},
                AllowedGrantTypes = GrantTypes.Code,
                RedirectUris = { AppSetting["RedirectUrl"] },
                PostLogoutRedirectUris = { AppSetting["PostLogoutRedirectUrl"] },
                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "Mango"
                },
            },
        };
    }
}