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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Votes controller.
    /// </summary>
    public class NationalStatsController : ApiController
    {
        private const string HealthStatusDictionary = "healthStatusDictionary";
        private readonly IReliableStateManager stateManager;

        public NationalStatsController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        /// <summary>
        /// GET /votes/counties
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("national/stats")]
        public async Task<NationalStatsViewModel> Get()
        {
            IReliableDictionary<int, NationalCountyStats> dictionary =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<int, NationalCountyStats>>(HealthStatusDictionary);

            int totalDoctorCount = 0;
            int totalPatientCount = 0;
            int totalHealthReportCount = 0;

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                IAsyncEnumerator<KeyValuePair<int, NationalCountyStats>> enumerator = (await dictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    IList<KeyValuePair<int, NationalCountyStats>> items = new List<KeyValuePair<int, NationalCountyStats>>();
                    items.Add(enumerator.Current);

                    foreach (KeyValuePair<int, NationalCountyStats> item in items)
                    {
                        totalDoctorCount += item.Value.DoctorCount;
                        totalPatientCount += item.Value.PatientCount;
                        totalHealthReportCount += item.Value.HealthReportCount;
                    }
                }

                return new NationalStatsViewModel(totalDoctorCount, totalPatientCount, totalHealthReportCount, 0);
            }
        }
    }
}