// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using HealthMetrics.Common;
    using HealthMetrics.NationalService.Models;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;

    /// <summary>
    /// Votes controller.
    /// </summary>
    public class NationalStatsController : ApiController
    {
        private const string HealthStatusDictionary = "healthStatusDictionary";
        private const string TimeStatsDictionary = "TimeTracker";
        private readonly ConcurrentDictionary<string, long> statsDictionary;

        private readonly IReliableStateManager stateManager;

        public NationalStatsController(IReliableStateManager stateManager, ConcurrentDictionary<string, long> statsDictionary)
        {
            this.stateManager = stateManager;
            this.statsDictionary = statsDictionary;
        }

        /// <summary>
        /// GET /votes/counties
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("national/stats")]
        public async Task<NationalStatsViewModel> Get()
        {
            try
            {
                var timeDictionary = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, DateTimeOffset>>(TimeStatsDictionary);
               
                DateTimeOffset offset = DateTimeOffset.MinValue;
                IList<KeyValuePair<int, NationalCountyStats>> items = new List<KeyValuePair<int, NationalCountyStats>>();

                using (ITransaction tx = this.stateManager.CreateTransaction())
                {
                    var creationTimeResult = await timeDictionary.TryGetValueAsync(tx, "StartTime");

                    if (creationTimeResult.HasValue)
                    {
                        offset = creationTimeResult.Value;
                    }

                    //IAsyncEnumerator<KeyValuePair<int, NationalCountyStats>> enumerator = (await dictionary.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                    //while (await enumerator.MoveNextAsync(CancellationToken.None))
                    //{                    
                    //    items.Add(enumerator.Current);
                    //}

                    //foreach (KeyValuePair<int, NationalCountyStats> item in items)
                    //{
                    //    totalDoctorCount += item.Value.DoctorCount;
                    //    totalPatientCount += item.Value.PatientCount;
                    //    totalHealthReportCount += item.Value.HealthReportCount;
                    //}

                    return new NationalStatsViewModel(statsDictionary["totalDoctors"], statsDictionary["totalPatientCount"], statsDictionary["totalHealthReportCount"], 0, offset);
                }
            }
            catch (Exception e)
            {
                var ex = e;
                throw e;
            }
        }
    }
}