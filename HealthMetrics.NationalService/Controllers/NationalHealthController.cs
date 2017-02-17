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

        public NationalHealthController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
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

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                IAsyncEnumerator<KeyValuePair<int, NationalCountyStats>> enumerator = (await dictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                IList<KeyValuePair<int, NationalCountyStats>> items = new List<KeyValuePair<int, NationalCountyStats>>();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    items.Add(enumerator.Current);
                }

                return this.Ok(items.Select(item => new CountyHealth() {CountyId = item.Key, Health = item.Value.AverageHealthIndex}).ToList());
            }
        }
    }
}