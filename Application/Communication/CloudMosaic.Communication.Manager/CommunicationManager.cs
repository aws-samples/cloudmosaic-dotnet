using System;

using Amazon;
using Amazon.Runtime;

using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;
using System.IO;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace CloudMosaic.Communication.Manager
{
    public class CommunicationManager
    {
        const string ConnectionIdField = "connectionId";
        const string UsernameField = "username";
        const string EndpointField = "endpoint";
        const string LoginDateField = "logindate";


        string _ddbTableName;
        IAmazonDynamoDB _ddbClient;

        MemoryCache _connectionsCache = new MemoryCache(new MemoryCacheOptions());

        private CommunicationManager(AWSCredentials awsCredentials, RegionEndpoint region, string ddbTableName)
        {
            _ddbTableName = ddbTableName;
            _ddbClient = new AmazonDynamoDBClient(awsCredentials, region);
        }


        public static CommunicationManager CreateManager(AWSCredentials awsCredentials, RegionEndpoint region, string ddbTableName)
        {
            return new CommunicationManager(awsCredentials, region, ddbTableName);
        }

        public static CommunicationManager CreateManager(string ddbTableName)
        {
            return CreateManager(FallbackCredentialsFactory.GetCredentials(), FallbackRegionFactory.GetRegionEndpoint(), ddbTableName);
        }


        public async Task LoginAsync(string connectionId, string endpoint, string username)
        {
            if (string.IsNullOrEmpty(_ddbTableName))
                return;

            var putRequest = new PutItemRequest
            {
                TableName = _ddbTableName,
                Item = new Dictionary<string, AttributeValue>
                    {
                        {ConnectionIdField, new AttributeValue{ S = connectionId}},
                        {EndpointField, new AttributeValue{ S = endpoint}},
                        {UsernameField, new AttributeValue{ S = username}},
                        {LoginDateField, new AttributeValue{S = DateTime.UtcNow.ToString()}}
                    }
            };

            await _ddbClient.PutItemAsync(putRequest);
        }

        public async Task LogoffAsync(string connectionId)
        {
            if (string.IsNullOrEmpty(_ddbTableName))
                return;

            var deleteRequest = new DeleteItemRequest
            {
                TableName = _ddbTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    {ConnectionIdField, new AttributeValue{ S = connectionId}}
                }
            };

            await _ddbClient.DeleteItemAsync(deleteRequest);
        }

        public async Task SendMessage(MessageEvent evnt)
        {
            if (string.IsNullOrEmpty(_ddbTableName))
                return;

            var payload = JsonConvert.SerializeObject(evnt);
            var stream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(payload));


            QueryResponse queryResponse;
            ICacheEntry entry;
            if(!_connectionsCache.TryGetValue(evnt.TargetUser, out entry) || entry.AbsoluteExpiration < DateTime.UtcNow)
            {
                var queryRequest = new QueryRequest
                {
                    TableName = _ddbTableName,
                    IndexName = "username",
                    KeyConditionExpression = $"{UsernameField} = :u",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":u", new AttributeValue{S = evnt.TargetUser } }
                    }
                };
                queryResponse = await _ddbClient.QueryAsync(queryRequest);

                entry = _connectionsCache.CreateEntry(evnt.TargetUser);
                entry.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(10);
                entry.SetValue(queryResponse);
                _connectionsCache.Set(evnt.TargetUser, entry);
            }
            else
            {
                queryResponse = entry.Value as QueryResponse;
            }

            AmazonApiGatewayManagementApiClient apiClient = null;
            try
            {
                var goneConnections = new List<Dictionary<string, AttributeValue>>();
                foreach (var item in queryResponse.Items)
                {
                    var endpoint = item[EndpointField].S;

                    if (apiClient == null || !apiClient.Config.ServiceURL.Equals(endpoint, StringComparison.Ordinal))
                    {
                        if (apiClient != null)
                        {
                            apiClient.Dispose();
                            apiClient = null;
                        }

                        apiClient = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
                        {
                            ServiceURL = endpoint
                        });
                    }

                    var connectionId = item[ConnectionIdField].S;

                    stream.Position = 0;
                    var postConnectionRequest = new PostToConnectionRequest
                    {
                        ConnectionId = connectionId,
                        Data = stream
                    };

                    try
                    {
                        await apiClient.PostToConnectionAsync(postConnectionRequest);
                    }
                    catch(GoneException)
                    {
                        goneConnections.Add(item);
                    }
                }

                // Remove connections from the cache that have disconnected.
                foreach(var goneConnectionItem in goneConnections)
                {
                    queryResponse.Items.Remove(goneConnectionItem);
                }
            }
            catch
            {
                // Never stop rendering based on communication errors.
            }
            finally
            {
                if (apiClient != null)
                {
                    apiClient.Dispose();
                }
            }
        }
    }
}
