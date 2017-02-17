// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Votes controller.
    /// </summary>
    public class CountyDoctorsController : ApiController
    {
        private const string DoctorServiceName = "DoctorActorService";
        private readonly IReliableStateManager stateManager;
        private readonly HealthIndexCalculator indexCalculator;

        public CountyDoctorsController(IReliableStateManager stateManager, HealthIndexCalculator indexCalculator)
        {
            this.stateManager = stateManager;
            this.indexCalculator = indexCalculator;
        }

        /// <summary>
        /// Returns { DoctorId, DoctorName, HealthStatus }
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("county/doctors/{countyId}")]
        public async Task<IHttpActionResult> Get(int countyId)
        {
            IReliableDictionary<Guid, CountyDoctorStats> countyHealth =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<Guid, CountyDoctorStats>>(string.Format(Service.CountyHealthDictionaryName, countyId));

            IList<KeyValuePair<Guid, CountyDoctorStats>> doctors = new List<KeyValuePair<Guid, CountyDoctorStats>>();

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                IAsyncEnumerator<KeyValuePair<Guid, CountyDoctorStats>> enumerator = (await countyHealth.CreateEnumerableAsync(tx)).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    doctors.Add(enumerator.Current);
                }
            }

            var doctorInfo = doctors.Select(
                (x) =>
                {
                    return new
                    {
                        DoctorId = x.Key,
                        DoctorName = x.Value.DoctorName,
                        HealthStatus = this.indexCalculator.ComputeIndex(x.Value.AverageHealthIndex)
                    };
                }).OrderByDescending((x) => x.HealthStatus);

            return this.Ok(doctorInfo);
        }
    }
}