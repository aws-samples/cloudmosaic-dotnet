using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;

using CloudMosaic.API.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CloudMosaic.BlazorFrontend
{

    public interface IServiceClientFactory
    {
        public Task<MosaicClient> CreateMosaicClient();

        public Task<GalleryClient> CreateGalleryClient();
    }

    public class ServiceClientFactory : IServiceClientFactory
    {
        AppOptions _appOptions;
        AuthenticationStateProvider _authenticationStateProvider;
        CognitoUserManager<CognitoUser> _cognitoUserManager;

        public ServiceClientFactory(IOptions<AppOptions> appOptions, AuthenticationStateProvider authenticationStateProvider, UserManager<CognitoUser> userManager)
        {
            this._appOptions = appOptions.Value;

            this._authenticationStateProvider = authenticationStateProvider;
            this._cognitoUserManager = userManager as CognitoUserManager<CognitoUser>;
        }

        public async Task<MosaicClient> CreateMosaicClient()
        {
            var httpClient = await ConstructHttpClient();
            var mosaicClient = new MosaicClient(httpClient)
            {
                BaseUrl = this._appOptions.CloudMosaicApiUrl
            };


            return mosaicClient;
        }


        public async Task<GalleryClient> CreateGalleryClient()
        {
            var httpClient = await ConstructHttpClient();
            var galleryClient = new GalleryClient(httpClient)
            {
                BaseUrl = this._appOptions.CloudMosaicApiUrl
            };

            return galleryClient;
        }


        private async Task<HttpClient> ConstructHttpClient()
        {
            var authState = await this._authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if(!user.Identity.IsAuthenticated)
                throw new NoIdTokenException();

            var userId = this._cognitoUserManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                throw new NoIdTokenException();

            var cognitoUser = await this._cognitoUserManager.FindByIdAsync(userId);
            if (string.IsNullOrEmpty(cognitoUser?.SessionTokens.IdToken))
                throw new NoIdTokenException();


            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"bearer {cognitoUser.SessionTokens.IdToken}");


            return httpClient;
        }       
    }
}
