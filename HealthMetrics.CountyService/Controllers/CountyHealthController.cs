// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    /// <summary>
    /// Default controller.
    /// </summary>
    public class CountyHealthController : ApiController
    {
        /// <summary>
        /// Reliable object state manager.
        /// </summary>
        private readonly IReliableStateManager stateManager;

        private readonly HealthIndexCalculator indexCalculator;

        /// <summary>
        /// Initializes a new instance of the DefaultController class.
        /// </summary>
        /// <param name="stateManager">Reliable object state manager.</param>
        public CountyHealthController(IReliableStateManager stateManager, HealthIndexCalculator indexCalculator)
        {
            this.stateManager = stateManager;
            this.indexCalculator = indexCalculator;
        }

        [HttpGet]
        [Route("county/health/{countyId}")]
        public async Task<IHttpActionResult> Get(int countyId)
        {
            IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(string.Format(Service.CountyHealthDictionaryName, countyId));

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                if (await countyHealth.GetCountAsync(tx) > 0)
                {
                    IList<KeyValuePair<Guid, CountyDoctorStats>> doctorStats = new List<KeyValuePair<Guid, CountyDoctorStats>>();

                    IAsyncEnumerator<KeyValuePair<Guid, CountyDoctorStats>> enumerator = (await countyHealth.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        doctorStats.Add(enumerator.Current);
                    }

                    return this.Ok(this.indexCalculator.ComputeAverageIndex(doctorStats.Select(x => x.Value.AverageHealthIndex)));
                }
                else
                {
                    return this.Ok(-1);
                }
            }
        }

        /// <summary>
        /// Saves health info for a county.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("county/health/{countyId}/{doctorId}")]
        public async Task<IHttpActionResult> Post([FromUri] int countyId, [FromUri] Guid doctorId, [FromBody] DoctorStatsViewModel stats)
        {
            try
            {
                IReliableDictionary<int, string> countyNameDictionary =
                    await this.stateManager.GetOrAddAsync<IReliableDictionary<int, string>>(Service.CountyNameDictionaryName);

                IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                    await
                        this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(
                            string.Format(Service.CountyHealthDictionaryName, countyId));

                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    await
                        countyHealth.SetAsync(
                            tx,
                            doctorId,
                            new CountyDoctorStats(stats.PatientCount, stats.HealthReportCount, stats.DoctorName, new HealthIndex(stats.AverageHealthIndex)));

                    // Add the county only if it does not already exist.
                    ConditionalValue<string> getResult = await countyNameDictionary.TryGetValueAsync(tx, countyId);

                    if (!getResult.HasValue)
                    {
                        await countyNameDictionary.AddAsync(tx, countyId, String.Empty);
                    }

                    // finally, commit the transaction and return a result
                    await tx.CommitAsync();
                }

                return this.Ok();
            }
            catch (Exception e)
            {
                return this.InternalServerError(e);
            }
        }
    }
}