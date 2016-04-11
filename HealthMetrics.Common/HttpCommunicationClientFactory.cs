// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// Factory that creates clients that know to communicate with the WordCount service.
    /// Contains a service partition resolver that resolves a partition key
    /// and sets BaseAddress to the address of the replica that should serve a request.
    /// </summary>
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>
    {
        private TimeSpan OperationTimeout;
        private TimeSpan ReadWriteTimeout;
        private string EndpointName;

        public HttpCommunicationClientFactory(ServicePartitionResolver resolver, string endpointName, TimeSpan operationTimeout, TimeSpan readWriteTimeout)
            : base(resolver, null, null)
        {
            this.OperationTimeout = operationTimeout;
            this.ReadWriteTimeout = readWriteTimeout;
            this.EndpointName = endpointName;
        }

        protected override void AbortClient(HttpCommunicationClient client)
        {
            return;
        }

        protected override Task<HttpCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            // Create a communication client. This doesn't establish a session with the server.
            HttpCommunicationClient client = new HttpCommunicationClient(new Uri(endpoint), this.EndpointName, this.OperationTimeout, this.ReadWriteTimeout);

            if (this.ValidateClient(endpoint, client))
            {
                return Task.FromResult<HttpCommunicationClient>(client);
            }
            else
            {
                throw new ArgumentException("Error creating HttpCommunicationClient, bad endpoint format");
            }
        }

        protected override bool ValidateClient(HttpCommunicationClient client)
        {
            return true;
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            if (string.IsNullOrEmpty(endpoint) || !endpoint.StartsWith("http"))
            {
                return false;
            }

            return true;
        }
    }
}