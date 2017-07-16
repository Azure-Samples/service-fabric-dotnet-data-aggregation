// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandCreationService
{
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class Service : StatelessService
    {
        // This is the name of the ServiceType that is registered with FabricRuntime. 
        // This name must match the name defined in the ServiceManifest. If you change
        // this name, please change the name of the ServiceType in the ServiceManifest.
        public const string ServiceTypeName = "HealthMetrics.BandCreationServiceType";

        private static FabricClient fabricClient = new FabricClient();
        private Uri ActorServiceUri;
        private Uri DoctorServiceUri;
        private int NumberOfCreationThreads;
        private int MaxBandsToCreatePerServiceInstance;
        private bool GenerateKnownPeople;

        private ConcurrentDictionary<int, ServicePartitionClient<HttpCommunicationClient>> communicationClientDictionary =
            new ConcurrentDictionary<int, ServicePartitionClient<HttpCommunicationClient>>();

        public Service(StatelessServiceContext serviceContext) : base(serviceContext)
        {
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            ConfigurationSettings configSettings = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config").Settings;
            KeyedCollection<string, ConfigurationProperty> serviceParameters = configSettings.Sections["HealthMetrics.BandCreationService.Settings"].Parameters;

            this.NumberOfCreationThreads = int.Parse(serviceParameters["NumberOfCreationThreads"].Value);
            this.MaxBandsToCreatePerServiceInstance = int.Parse(serviceParameters["MaxBandsToCreatePerServiceInstance"].Value);
            this.ActorServiceUri = new ServiceUriBuilder(serviceParameters["BandActorServiceName"].Value).ToUri();
            this.DoctorServiceUri = new ServiceUriBuilder(serviceParameters["DoctorActorServiceName"].Value).ToUri();
            this.GenerateKnownPeople = bool.Parse(serviceParameters["GenerateKnownPeople"].Value);

            string dataPath = FabricRuntime.GetActivationContext().GetDataPackageObject("Data").Path;
            BandActorGenerator bag = new BandActorGenerator(configSettings, dataPath);

            bag.Prepare();

            ServicePrimer primer = new ServicePrimer();
            await primer.WaitForStatefulService(this.ActorServiceUri, cancellationToken);

            List<Task> tasks = new List<Task>();

            if (this.GenerateKnownPeople)
            {
                tasks.Add(Task.Run(() => this.CreateKnownActors(bag, configSettings, cancellationToken)));
            }

            for (int i = 0; i < this.NumberOfCreationThreads; i++)
            {
                tasks.Add(Task.Run(() => this.CreateBandActorTask(bag, cancellationToken), cancellationToken));
            }

            ServiceEventSource.Current.ServiceMessage(this, "Band Creation has begun.");
            await Task.WhenAll(tasks);
            ServiceEventSource.Current.ServiceMessage(this, "Band Creation has completed.");
        }

        private async Task CreateBandActorTask(BandActorGenerator bag, CancellationToken cancellationToken)
        {
            CryptoRandom random = new CryptoRandom();

            while (!cancellationToken.IsCancellationRequested && this.MaxBandsToCreatePerServiceInstance > 0)
            {
                bool created = false;
                while (!created && !cancellationToken.IsCancellationRequested)
                {
                    ActorId bandActorId;
                    ActorId doctorActorId;
                    int randomCountyId = -1;
                    string doctorName = null;

                    randomCountyId = random.Next(0, bag.doctorsPerCounty.Keys.Count);
                    doctorName = bag.GetRandomName(random);

                    CountyRecord randomCountyRecord = bag.doctorsPerCounty.Keys.ElementAt(randomCountyId);
                    BandInfo bandActorInfo = bag.GetRandomHealthStatus(randomCountyRecord, random);

                    try
                    {
                        bandActorId = new ActorId(Guid.NewGuid());
                        doctorActorId = new ActorId(bandActorInfo.DoctorId);

                        IDoctorActor docActor = ActorProxy.Create<IDoctorActor>(doctorActorId, this.DoctorServiceUri);
                        await docActor.NewAsync(doctorName, randomCountyRecord);

                        IBandActor bandActor = ActorProxy.Create<IBandActor>(bandActorId, this.ActorServiceUri);
                        await bandActor.NewAsync(bandActorInfo);

                        ServiceEventSource.Current.Message("Actor created {0} verifying...", bandActorId);

                        await VerifyActors(new HealthIndexCalculator(this.Context), bandActorId, doctorName, randomCountyRecord, bandActorInfo, docActor, bandActor, cancellationToken);
                    }

                    catch (Exception e)
                    {
                        ServiceEventSource.Current.ServiceMessage(this, "Failed to iniitalize band or doctor. {0}", e.ToString());
                    }

                    created = true;
                }

                this.MaxBandsToCreatePerServiceInstance--;

                ServiceEventSource.Current.ServiceMessage(this, "Created Actors, {0} remaining", this.MaxBandsToCreatePerServiceInstance);

                await Task.Delay(100, cancellationToken);
            }
        }

        private async Task CreateKnownActors(BandActorGenerator bag, ConfigurationSettings settings, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(this, "Creating known actors");

            CryptoRandom random = new CryptoRandom();
            FabricClient fc = new FabricClient();
            HealthIndexCalculator hic = new HealthIndexCalculator(this.Context);

            KeyedCollection<string, ConfigurationProperty> serviceParameters = settings.Sections["HealthMetrics.BandCreationService.Settings"].Parameters;

            while (!cancellationToken.IsCancellationRequested)
            {
                ActorId bandActorId;
                ActorId doctorActorId;
                int randomCountyId = -1;
                string doctorName = null;

                randomCountyId = int.Parse(serviceParameters["KnownCountyIdIndex"].Value);
                //(2968 is King, WA) || (2231 is Multnomah, OR) || (1870 is St. Lawrence, NY)
                doctorName = serviceParameters["KnownDoctorName"].Value;

                CountyRecord randomCountyRecord = bag.doctorsPerCounty.Keys.ElementAt(randomCountyId);
                BandInfo bandActorInfo = bag.GetRandomHealthStatus(randomCountyRecord, random);

                try
                {
                    bandActorInfo.PersonName = serviceParameters["KnownPatientName"].Value;
                    bandActorId = new ActorId(new Guid(serviceParameters["KnownPatientId"].Value));

                    bandActorInfo.DoctorId = new Guid(serviceParameters["KnownDoctorId"].Value);
                    doctorActorId = new ActorId(bandActorInfo.DoctorId);

                    bag.doctorsPerCounty[bag.doctorsPerCounty.Keys.ElementAt(randomCountyId)].Add(bandActorInfo.DoctorId);

                    IDoctorActor docActor = ActorProxy.Create<IDoctorActor>(doctorActorId, this.DoctorServiceUri);
                    await docActor.NewAsync(doctorName, randomCountyRecord);

                    IBandActor bandActor = ActorProxy.Create<IBandActor>(bandActorId, this.ActorServiceUri);
                    await bandActor.NewAsync(bandActorInfo);

                    ServiceEventSource.Current.ServiceMessage(this, "Known actors news sent, verifying");

                    await VerifyActors(hic, bandActorId, doctorName, randomCountyRecord, bandActorInfo, docActor, bandActor, cancellationToken);

                    break;

                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.ServiceMessage(this, "Exception when creating actor {0}", e.ToString());
                }
            }
        }

        private static async Task VerifyActors(
            HealthIndexCalculator hic, ActorId bandActorId, string doctorName, CountyRecord randomCountyRecord, BandInfo bandActorInfo, IDoctorActor docActor,
            IBandActor bandActor, CancellationToken ct)
        {

            ServiceEventSource.Current.Message("Verifying Actor {0}", bandActorId);

            bool bandVerified = false;
            bool doctorVerified = false;
            int bandErrorCount = 0;
            int doctorErrorCount = 0;

            while (!ct.IsCancellationRequested && !bandVerified && !doctorVerified)
            {
                await Task.Delay(100, ct);

                if (!bandVerified)
                {
                    try
                    {
                        BandDataViewModel view = await bandActor.GetBandDataAsync();

                        if (view.PersonName == bandActorInfo.PersonName)
                        {
                            if (view.CountyInfo == bandActorInfo.CountyInfo)
                            {
                                if (view.DoctorId == bandActorInfo.DoctorId)
                                {
                                    if (view.PersonId == bandActorId.GetGuidId())
                                    {
                                        if (view.HealthIndexValue == bandActorInfo.HealthIndex)
                                        {
                                            bandVerified = true;
                                            ServiceEventSource.Current.Message("Band actor verified.");
                                        }
                                        else
                                        {
                                            await bandActor.NewAsync(bandActorInfo);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        bandErrorCount++;
                        ServiceEventSource.Current.Message("Exception Count {0} verifying band actor, retrying. {1}", bandErrorCount, e);
                    }
                }
                else
                {
                    ServiceEventSource.Current.Message("band already verified, skipping");
                }


                if (!doctorVerified)
                {
                    try
                    {
                        Tuple<CountyRecord, string> info = await docActor.GetInfoAndNameAsync();
                        if (info.Item2 == String.Format("Dr. {0}", doctorName)
                            && info.Item1 == randomCountyRecord)
                        {
                            doctorVerified = true;
                            ServiceEventSource.Current.Message("Doctor actor verified.");
                        }
                        else
                        {
                            await docActor.NewAsync(doctorName, randomCountyRecord);
                        }
                    }
                    catch (Exception e)
                    {
                        doctorErrorCount++;
                        ServiceEventSource.Current.Message("Exception Count {0} verifying doctor actor, retrying. {1}", doctorErrorCount, e);
                    }
                }
                else
                {
                    ServiceEventSource.Current.Message("doctor already verified, skipping");
                }
            }
        }
    }
}
