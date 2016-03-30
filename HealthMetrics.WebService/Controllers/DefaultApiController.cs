// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService.Controllers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;

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
        /// Average patient health
        /// List of patient info {ID, name, average health, BP health, BG health}
        /// </summary>
        /// <param name="doctorId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("doctors/{doctorId}")]
        public async Task<IHttpActionResult> GetDoctor(Guid doctorId)
        {
            try
            {
                ActorId doctorActor = new ActorId(doctorId);
                ServiceUriBuilder serviceUri = new ServiceUriBuilder(this.GetSetting("DoctorActorServiceInstanceName"));
                IDoctorActor actor = ActorProxy.Create<IDoctorActor>(doctorActor, serviceUri.ToUri());

                return this.Ok(await actor.GetPatientsAsync());
            }
            catch (AggregateException ae)
            {
                return this.InternalServerError(ae.InnerException);
            }
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

        private string GetSetting(string key)
        {
            return this.configPackageSettings[key].Value;
        }
    }
}