// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService.Controllers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Query;

    [RoutePrefix("api")]
    public class DefaultApiController : ApiController
    {
        private readonly KeyedCollection<string, ConfigurationProperty> configPackageSettings;

        public DefaultApiController(ConfigurationSettings configPackageSettings)
        {
            this.configPackageSettings = configPackageSettings.Sections["HealthMetrics.WebService.Settings"].Parameters;
        }

        [HttpGet]
        [Route("settings/{setting}")]
        public Task<string> GetSettingValue(string setting)
        {
            return Task.FromResult<string>(this.GetSetting(setting));
        }

        [HttpGet]
        [Route("national/health")]
        public Task<HttpResponseMessage> GetNationalHealth()
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("NationalServiceInstanceName"));
            HttpClient client = new HttpClient();

            return client.SendToServiceAsync(
                serviceUri.ToUri(),
                () => new HttpRequestMessage(HttpMethod.Get, "/national/health"));
        }

        [Route("national/stats")]
        public Task<HttpResponseMessage> GetNationalStats()
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("NationalServiceInstanceName"));
            HttpClient client = new HttpClient();

            return client.SendToServiceAsync(
                serviceUri.ToUri(),
                () => new HttpRequestMessage(HttpMethod.Get, "/national/stats"));
        }

        /// <summary>
        /// List of {doctor ID, average patient health}
        /// </summary>
        /// <param name="countyId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("county/{countyId}/doctors/")]
        public Task<HttpResponseMessage> GetDoctors(int countyId)
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("CountyServiceInstanceName"));
            HttpClient client = new HttpClient();

            return client.SendToServiceAsync(
                serviceUri.ToUri(),
                countyId,
                () => new HttpRequestMessage(HttpMethod.Get, "/county/doctors/" + countyId));
        }

        [HttpGet]
        [Route("county/{countyId}/health/")]
        public Task<HttpResponseMessage> GetCountyHealth(int countyId)
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("CountyServiceInstanceName"));
            HttpClient client = new HttpClient();

            return client.SendToServiceAsync(
                serviceUri.ToUri(),
                countyId,
                () => new HttpRequestMessage(HttpMethod.Get, "/county/health/" + countyId));
        }

        /// <summary>
        /// Doctor Id
        /// County Record
        ///     County Name
        ///     County Id
        ///     County Health
        /// Health Status
        /// Heart Rate[]
        /// </summary>
        /// <param name="bandId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("patients/{bandId}")]
        public async Task<IHttpActionResult> GetPatientData(Guid bandId)
        {
            try
            {
                ActorId bandActorId = new ActorId(bandId);
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("BandActorServiceInstanceName"));
                IBandActor actor = ActorProxy.Create<IBandActor>(bandActorId, serviceUri.ToUri());

                return this.Ok(await actor.GetBandDataAsync());
            }
            catch (AggregateException ae)
            {
                return this.InternalServerError(ae.InnerException);
            }
        }

        [HttpGet]
        [Route("settings/GetIds")]
        public async Task<string> GetPatientId()
        {
            if (bool.Parse(this.configPackageSettings["GenerateKnownPeople"].Value))
            {
                string patientId = this.configPackageSettings["KnownPatientId"].Value;
                string doctorId = this.configPackageSettings["KnownDoctorId"].Value;

                return string.Format("{0}|{1}", patientId, doctorId);
            }
            else
            {
                return await this.GetRandomIdsAsync();
            }
        }

        private string GetSetting(string key)
        {
            return this.configPackageSettings[key].Value;
        }

        private async Task<string> GetRandomIdsAsync()
        {
            ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("BandActorServiceInstanceName"));
            Uri fabricServiceName = serviceUri.ToUri();

            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            CancellationToken token = cts.Token;

            FabricClient fc = new FabricClient();
            ServicePartitionList partitions = await fc.QueryManager.GetPartitionListAsync(fabricServiceName);

            ActorId bandActorId = null;

            try
            {
                while (!token.IsCancellationRequested && bandActorId == null)
                {
                    foreach (Partition p in partitions)
                    {
                        long partitionKey = ((Int64RangePartitionInformation) p.PartitionInformation).LowKey;
                        token.ThrowIfCancellationRequested();
                        ContinuationToken queryContinuationToken = null;
                        IActorService proxy = ActorServiceProxy.Create(fabricServiceName, partitionKey);
                        PagedResult<ActorInformation> result = await proxy.GetActorsAsync(queryContinuationToken, token);
                        foreach (ActorInformation info in result.Items)
                        {
                            bandActorId = info.ActorId;
                            break;
                        }
                        //otherwise we will bounce around other partitions until we find an actor
                    }
                }

                IBandActor bandActor = ActorProxy.Create<IBandActor>(bandActorId, fabricServiceName);
                BandDataViewModel data = await bandActor.GetBandDataAsync();

                return string.Format("{0}|{1}", bandActorId, data.DoctorId);
            }
            catch
            {
                //no actors found within timeout
                throw;
            }
        }
    }
}