// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Http
{
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;

    public static class HttpClientExtensions
    {
        private static FabricClient fabricClient = new FabricClient();

        private static HttpCommunicationClientFactory clientFactory = new HttpCommunicationClientFactory(
            ServicePartitionResolver.GetDefault(),
            "endpointName",
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(2));


        public static Task<HttpResponseMessage> SendToServiceAsync(
            this HttpClient instance, Uri serviceInstanceUri, long partitionKey, Func<HttpRequestMessage> createRequest,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ServicePartitionClient<HttpCommunicationClient> servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                clientFactory,
                serviceInstanceUri,
                new ServicePartitionKey(partitionKey));

            return MakeHttpRequest(instance, createRequest, cancellationToken, servicePartitionClient);
        }

        public static Task<HttpResponseMessage> SendToServiceAsync(
            this HttpClient instance, Uri serviceInstanceUri, Func<HttpRequestMessage> createRequest,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ServicePartitionClient<HttpCommunicationClient> servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                clientFactory,
                serviceInstanceUri);

            return MakeHttpRequest(instance, createRequest, cancellationToken, servicePartitionClient);
        }

        private static Task<HttpResponseMessage> MakeHttpRequest(
            HttpClient instance, Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken,
            ServicePartitionClient<HttpCommunicationClient> servicePartitionClient)
        {
            return servicePartitionClient.InvokeWithRetryAsync(
                async
                    client =>
                    {
                        HttpRequestMessage request = createRequest();

                        Uri newUri = new Uri(client.BaseAddress, request.RequestUri.OriginalString.TrimStart('/'));

                        request.RequestUri = newUri;

                        HttpResponseMessage response = await instance.SendAsync(request, cancellationToken);

                        response.EnsureSuccessStatusCode();

                        return response;
                    });
        }
    }
}