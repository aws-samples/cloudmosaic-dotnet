using Amazon.AspNetCore.Identity.Cognito;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudMosaic.Frontend
{
    public static class ServiceCollectionExtensions
    {

        private const string ConfigurationClientIdKey = "UserPoolClientId";
        private const string ConfigurationClientSecretKey = "UserPoolClientSecret";
        private const string ConfigurationUserPoolIdKey = "UserPoolId";

        public static IServiceCollection AddCognitoIdentityProvider(this IServiceCollection services, IConfiguration configuration)
        {
            services.InjectCognitoUser<CognitoUser>();

            services.ConfigureCognitoIdentityProviderClient(configuration);

            return services;
        }

        public static IServiceCollection InjectCognitoUser<TUser>(this IServiceCollection services) where TUser : CognitoUser
        {
            services.AddIdentity<CognitoUser, CognitoRole>().AddDefaultTokenProviders();

            services.AddIdentityCore<TUser>()
                .AddDefaultTokenProviders()
                .AddPasswordValidator<CognitoPasswordValidator>();

            // Updates the manager to use custom stores
            services.AddTransient<IUserStore<TUser>, CognitoUserStore<TUser>>();

            // Following only needed if we want to inject custom managers
            services.AddTransient<UserManager<TUser>, CognitoUserManager<TUser>>();
            services.AddTransient<SignInManager<TUser>, CognitoSignInManager<TUser>>();
            services.AddTransient<IUserClaimStore<TUser>, CognitoUserStore<TUser>>();
            services.AddTransient<IUserClaimsPrincipalFactory<TUser>, CognitoUserClaimsPrincipalFactory<TUser>>();

            services.AddTransient<CognitoKeyNormalizer, CognitoKeyNormalizer>();

            services.AddHttpContextAccessor();
            return services;
        }

        public static void ConfigureCognitoIdentityProviderClient(this IServiceCollection services, IConfiguration configuration)
        {
            var configurationSection = configuration.GetSection("Authentication:Cognito");

            var poolclient = new UserPoolClientType
            {
                ClientId = configurationSection.GetValue<string>(ConfigurationClientIdKey),
                ClientSecret = configurationSection.GetValue<string>(ConfigurationClientSecretKey)
            };

            var awsOptions = configuration.GetAWSOptions();
            var provider = awsOptions.CreateServiceClient<IAmazonCognitoIdentityProvider>() as AmazonCognitoIdentityProviderClient;

            var pool = new CognitoUserPool(configurationSection.GetValue<string>(ConfigurationUserPoolIdKey), poolclient.ClientId, provider, poolclient.ClientSecret);

            services.AddSingleton(typeof(CognitoUserPool), pool);
            services.AddSingleton(typeof(AmazonCognitoIdentityProviderClient), provider);
        }
    }

    public class CognitoRole
    {
    }
}
