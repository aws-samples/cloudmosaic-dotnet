using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Amazon.AspNetCore.Identity.Cognito;
using System.Net.WebSockets;
using CloudMosaic.Communication.Manager;
using System.Text;

using Newtonsoft.Json;

namespace CloudMosaic.BlazorFrontend
{

    public interface ICommunicationClientFactory
    {
        Task<ICommunicationClient> CreateCommunicationClient(CancellationToken token);
    }

    public class CommunicationClientFactory : ICommunicationClientFactory
    {
        AppOptions _appOptions;
        AuthenticationStateProvider _authenticationStateProvider;
        CognitoUserManager<CognitoUser> _cognitoUserManager;

        public CommunicationClientFactory(IOptions<AppOptions> appOptions, AuthenticationStateProvider authenticationStateProvider, UserManager<CognitoUser> userManager)
        {
            this._appOptions = appOptions.Value;

            this._authenticationStateProvider = authenticationStateProvider;
            this._cognitoUserManager = userManager as CognitoUserManager<CognitoUser>;
        }

        public async Task<ICommunicationClient> CreateCommunicationClient(CancellationToken token)
        {
            var authState = await this._authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (!user.Identity.IsAuthenticated)
                throw new NoIdTokenException();

            var userId = this._cognitoUserManager.GetUserId(user);
            if (string.IsNullOrEmpty(userId))
                throw new NoIdTokenException();

            var cognitoUser = await this._cognitoUserManager.FindByIdAsync(userId);
            if (string.IsNullOrEmpty(cognitoUser?.SessionTokens.IdToken))
                throw new NoIdTokenException();


            var cws = new ClientWebSocket();
            cws.Options.SetRequestHeader("Authorization", cognitoUser.SessionTokens.IdToken);
            await cws.ConnectAsync(new Uri(_appOptions.CloudMosaicWebSocketAPI), token);

            return new CommunicationClient(cws);
        }

    }


    public interface ICommunicationClient : IDisposable
    {
        Task<MessageEvent> ReadEventAsync(CancellationToken token);
    }

    public class CommunicationClient : ICommunicationClient
    {
        ClientWebSocket _cws;
        byte[] _buffer;
        Memory<byte> _memoryBlock;


        public CommunicationClient(ClientWebSocket cws)
        {
            _cws = cws;

            _buffer = ArrayPool<byte>.Shared.Rent(65536);
            _memoryBlock = new Memory<byte>(_buffer);
        }

        public async Task<MessageEvent> ReadEventAsync(CancellationToken token)
        {
            try
            {
                var recvResult = await this._cws.ReceiveAsync(this._memoryBlock, token);

                if (WebSocketMessageType.Text != recvResult.MessageType)
                {
                    return null;
                }

                var content = UTF8Encoding.UTF8.GetString(this._buffer, 0, recvResult.Count);
                var evnt = JsonConvert.DeserializeObject<MessageEvent>(content);
                return evnt;
            }
            catch(TaskCanceledException)
            {
                return null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _cws.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
