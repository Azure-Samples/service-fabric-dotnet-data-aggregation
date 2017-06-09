// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using HealthMetrics.Common;
    using HealthMetrics.NationalService.Models;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Votes controller.
    /// </summary>
    public class NationalHealthController : ApiController
    {
        private const string HealthStatusDictionary = "healthStatusDictionary";
        private readonly IReliableStateManager stateManager;
        private readonly ConcurrentBag<int> updatedCounties;

        public NationalHealthController(IReliableStateManager stateManager, ConcurrentBag<int> updatedCounties)
        {
            this.stateManager = stateManager;
            this.updatedCounties = updatedCounties;
        }

        /// <summary>
        /// HttpPost /votes/update/{county}
        /// </summary>
        /// <param name="countyId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("national/health/{countyId}")]
        public async Task<IHttpActionResult> Post([FromUri] int countyId, [FromBody] CountyStatsViewModel status)
        {
            IReliableDictionary<int, NationalCountyStats> dictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<int, NationalCountyStats>>(HealthStatusDictionary);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                await dictionary.SetAsync(
                    tx,
                    countyId,
                    new NationalCountyStats(
                        status.DoctorCount,
                        status.PatientCount,
                        status.HealthReportCount,
                        status.AverageHealthIndex));

                this.updatedCounties.Add(countyId);

                await tx.CommitAsync();
            }

            ServiceEventSource.Current.Message("National Service recieved and saved report {0}|{1}", countyId, status);
            return this.Ok();
        }

        /// <summary>
        /// GET /votes/counties
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("national/health")]
        public async Task<IHttpActionResult> Get()
        {
            IReliableDictionary<int, NationalCountyStats> dictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<int, NationalCountyStats>>(HealthStatusDictionary);

            List<CountyHealth> countyData = new List<CountyHealth>();


            IList<int> countiesToProcess = this.updatedCounties.ToArray();

            foreach (int countyId in countiesToProcess)
            {
                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    var result = await dictionary.TryGetValueAsync(tx, countyId);
                    if (result.HasValue)
                    {
                        countyData.Add(new CountyHealth() { Id = countyId, Health = result.Value.AverageHealthIndex });
                    }

                    await tx.CommitAsync();
                }

                int tmp = countyId;
                this.updatedCounties.TryTake(out tmp);
            }

            return this.Ok(countyData);
        }
    }
}
