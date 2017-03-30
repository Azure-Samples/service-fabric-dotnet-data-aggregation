// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Web.Service;

    public class Service : StatefulService
    {
        internal const string ServiceTypeName = "HealthMetrics.CountyServiceType";

        internal const string CountyNameDictionaryName = "CountyNames";

        internal const string CountyHealthDictionaryName = "{0}-Health";

        private readonly TimeSpan interval = TimeSpan.FromSeconds(5);

        private readonly HttpCommunicationClientFactory clientFactory = new HttpCommunicationClientFactory(
            ServicePartitionResolver.GetDefault(),
            "EndpointName",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(3));

        private Uri nationalServiceInstanceUri;

        private HealthIndexCalculator indexCalculator;

        public Service(StatefulServiceContext serviceContext) : base(serviceContext)
        {
        }

        public Service(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica)
            : base(serviceContext, reliableStateManagerReplica)
        {
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ConfigurationPackage configPackage = this.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            this.UpdateConfigSettings(configPackage.Settings);

            this.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent
                += this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            this.indexCalculator = new HealthIndexCalculator(this.Context);

            ServicePrimer primer = new ServicePrimer();
            await primer.WaitForStatefulService(this.nationalServiceInstanceUri, cancellationToken);


            try
            {

                IReliableDictionary<int, string> countyNamesDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<int, string>>(CountyNameDictionaryName);

                ServiceEventSource.Current.ServiceMessage(this, "CountyService starting data processing.");
                while (!cancellationToken.IsCancellationRequested)
                {
                    //every ten seconds, grab the counties and send them to national
                    await Task.Delay(this.interval, cancellationToken);

                    ServicePartitionClient<HttpCommunicationClient> servicePartitionClient =
                        new ServicePartitionClient<HttpCommunicationClient>(
                            this.clientFactory,
                            this.nationalServiceInstanceUri);

                    IList<KeyValuePair<int, string>> countyNames = new List<KeyValuePair<int, string>>();

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        IAsyncEnumerator<KeyValuePair<int, string>> enumerator = (await countyNamesDictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                        while (await enumerator.MoveNextAsync(cancellationToken))
                        {
                            countyNames.Add(enumerator.Current);
                        }
                    }

                    foreach (KeyValuePair<int, string> county in countyNames)
                    {
                        IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                            await
                                this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                                    string.Format(CountyHealthDictionaryName, county.Key));

                        int totalDoctorCount = 0;
                        int totalPatientCount = 0;
                        long totalHealthReportCount = 0;
                        HealthIndex avgHealth;

                        using (ITransaction tx = this.StateManager.CreateTransaction())
                        {
                            IAsyncEnumerable<KeyValuePair<Guid, CountyDoctorStats>> healthRecords = await countyHealth.CreateEnumerableAsync(tx);

                            IAsyncEnumerator<KeyValuePair<Guid, CountyDoctorStats>> enumerator = healthRecords.GetAsyncEnumerator();

                            IList<KeyValuePair<Guid, CountyDoctorStats>> records = new List<KeyValuePair<Guid, CountyDoctorStats>>();

                            while (await enumerator.MoveNextAsync(cancellationToken))
                            {
                                records.Add(enumerator.Current);
                            }

                            avgHealth = this.indexCalculator.ComputeAverageIndex(records.Select(x => x.Value.AverageHealthIndex));

                            foreach (KeyValuePair<Guid, CountyDoctorStats> item in records)
                            {
                                totalDoctorCount++;
                                totalPatientCount += item.Value.PatientCount;
                                totalHealthReportCount += item.Value.HealthReportCount;
                            }
                        }

                        CountyStatsViewModel payload = new CountyStatsViewModel(totalDoctorCount, totalPatientCount, totalHealthReportCount, avgHealth);

                        await servicePartitionClient.InvokeWithRetryAsync(
                            client =>
                            {
                                Uri serviceAddress = new Uri(client.BaseAddress, string.Format("national/health/{0}", county.Key));

                                HttpWebRequest request = WebRequest.CreateHttp(serviceAddress);
                                request.Method = "POST";
                                request.ContentType = "application/json";
                                request.Timeout = (int)client.OperationTimeout.TotalMilliseconds;
                                request.ReadWriteTimeout = (int)client.ReadWriteTimeout.TotalMilliseconds;

                                using (Stream requestStream = request.GetRequestStream())
                                {
                                    using (BufferedStream buffer = new BufferedStream(requestStream))
                                    {
                                        using (StreamWriter writer = new StreamWriter(buffer))
                                        {
                                            JsonSerializer serializer = new JsonSerializer();
                                            serializer.Serialize(writer, payload);
                                            buffer.Flush();
                                        }

                                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                        {
                                            ServiceEventSource.Current.ServiceMessage(this, "County Data Sent {0}", serviceAddress);
                                            return Task.FromResult(true);
                                        }
                                    }
                                }
                            },
                            cancellationToken);
                    }
                }
            }
            catch (TimeoutException te)
            {
                // transient error. Retry.
                ServiceEventSource.Current.ServiceMessage(this, "CountyService encountered an exception trying to send data to National Service: TimeoutException in RunAsync: {0}", te.ToString());
            }
            catch (FabricTransientException fte)
            {
                // transient error. Retry.
                ServiceEventSource.Current.ServiceMessage(this, "CountyService encountered an exception trying to send data to National Service: FabricTransientException in RunAsync: {0}", fte.ToString());
            }
            catch (FabricNotPrimaryException)
            {
                // not primary any more, time to quit.
                return;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this, ex.ToString());
                throw;
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    initParams =>
                        new HttpCommunicationListener(
                            "healthcounty",
                            new Startup(
                                this.StateManager,
                                new HealthIndexCalculator(this.Context)),
                            this.Context))
            };
        }


        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            KeyedCollection<string, ConfigurationProperty> parameters = configSettings.Sections["HealthMetrics.CountyService.Settings"].Parameters;

            this.nationalServiceInstanceUri = new ServiceUriBuilder(parameters["NationalServiceName"].Value).ToUri();
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }
    }
}