---
services: service-fabric
platforms: dotnet
author: masnider
---

# Service Fabric Data Aggregation Sample (aka HealthMetrics)
Learn about large-scale data aggregation and management of large volumes of end-user devices with Service Fabric.

## Running this sample
Open the solution in Visual Studio, right click on the application project "HealthMetrics" and select the "Package" command.
Ensure that your local cluster is deployed and running in a 5 node configuration via Service Fabric Local Cluster Manager.
Inside the Visual Studio Solution Explorer, within the Application project, expand the scripts folder. Then right click on "HealthMetricsDeployment.ps1" and select "Execute as Script".
Once the script completes, the application should be deployed and visible in Service Fabric Explorer. By default the application is hosted in your local cluster at localhost:8080

## Deploy this sample to Azure
Open the HealthMetricsDeployment script and change the following variables defined at the top of the script:

```
$cloud = $false - change this to $true
$cloudAddress = "" - change this to the address of your cluster
```

## About the code
There are several services included in the sample. They are:

- BandCreation Service: this creates the individual band actors that then generate the actual health data
- BandActor Service: this service is the host for the band actors. Each BandActor represents an individual Microsoft Band or other device, which periodically generates health data then and sends it on to the Band's designated DoctorActor
- DoctorActor Service: Each band has an associated Doctor, hosted as Actors inside the DoctorActor Service. Each DoctorActor aggregates all of the data it receives from each BandActor and generates some statistics as well as an overall view for that Doctor of their Patients/Customers (pointing back to the Bands). The DoctorActors then push their data into the CountyService
- CountyService: The county service aggregates the information provided by the DoctorActors further and also pushes the data into the NationalService
- NationalService: This maintains the total aggregated data for the entire country and is used to serve data requested by WebService
- WebService: This Service just hosts a simple web UI, web API interfaces, and serves information obtained via the NationalService so that it can be rendered.

## Upgrade Patterns
This application supports multiple versions via the configuration parameter "Score Calcualtion Mode". This config is defined at the application level but is consumed by serveral of the underlying services to define their behavior.
This application supports a couple different upgrade mechanisms:

### Application Pakage Upgrade
- Differential Package Only Upgrade - You can perform an application config upgrade by deploying an updated application package defining new config settings and a new application version.

In this case, since the change is at the application parameters level, only the application package and the application manifest are present. This script grabs the updated application package containing the new application manifest with the updated application parameter values and starts an upgrade. This package is pre-built for you (HealthMetricsV2ConfigOnlyPackage) and is located alongside the scripts in the scripts folder.

To do this, inside the Scripts folder, right click on ApplicationPackageUpgrade.ps1 and select Execute as Script. Note that the new application package contains only the packages that changed (which in this case is none since no change to the service code is necessary and no specific changes are made within the service's code, configuration, or data packages). Since the configuration change we want to deploy is handled direcly via the application paramteres, no additional changes to the actual service code or configuration is necessary. The upgrade will take the updated configuration changes (in this case a change to the "ScoreCalculationMode") and roll it out to the necessary services upgrade domain by upgrade domain. These services recieve a notification that their configuration has changed and apply the change without having to restart.

When this happens you should see the UI of the application change to display the new scoring mode. In this case you will see a change from a three color scale (red, gree, black), to a 100 point/color scale.

To upgrade back to the original version of the application, run the following command (make sure you're connected to the cluster first - you can just start a new PS window and type Connect-ServiceFabricCluster if you're on localhost)

```
Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "1.0.0.0" -Monitored -FailureAction Rollback -Force
```

At this point, since the new version of the application package is already deployed and registered, you can roll forward again with the following command:

```
Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "2.0.0.0" -Monitored -FailureAction Rollback -Force
```

### Application Parameter Upgrade
Note: before trying this the first time, you want to make sure that the application is in the V1.0.0.0 configuration via the commands above. If you've just deployed the application the first time and haven't run ApplicationPackageUpgrade.ps1 yet, you can just start from here. Otherwise run the upgrade commands above to make sure the application is already in the v1.0.0.0 configuration.

- Versionless Update - The Application's parameters can be upgraded and rolled out without changing the version of the application type itself. No new application package or a version change is required, but the change will still roll out to the services upgrade domain by upgrade domain.

To do this, right click on the HealthMetricsConfigParamUpgrade.ps1 script within the script folder and select "Execute as Script". I fyou want to roll back to the previous mode of score calculation, just edit the script to change the value of the ScoreCalculationMode parameter to "Mode1" and run the whole script again.

### Testing Automated Rollback
- Integration with Health Monitoring - To test integration with Service Fabric's Health monitoring, run the following command while one of the upgrades is going on:

```
Send-ServiceFabricServiceHealthReport -HealthProperty SomeHealthReport -HealthState Error -ServiceName fabric:/HealthMetrics/HealthMetrics.NationalService -SourceId user -RemoveWhenExpired -TimeToLiveSec 120
```

This health report will set one of the services to an Error state, and this health error will be detected by Service Fabric. When it doesn't clear in time (by design) Service Fabric will automatically initiate a rollback to the prior deployed version. This works even for "versionless" application parameter only upgrades.

## More information
This repository contains a sample application that demostrates a scenario where thousands of devices submit information into a series of Service Fabric services which then aggregate and present that information. If you are looking for smaller samples as you are just getting started, please take a look at some of the [other Service Fabric samples][service-fabric-samples] available in the Azure samples gallery.

This sample is based on the original sample used at Service Fabric's [GA announcement at Build 2015][build-2015-video], and represents an improved version of that demonstration as well as the "health metrics" demonstration done by Scott Hanselman at [Connect 2015][connect-2015-video] when Service Fabric's public preview was announced.

<!-- Links -->

[service-fabric-samples]: http://aka.ms/servicefabricsamples
[build-2015-video]: https://channel9.msdn.com/Events/Build/2015/3-618
[connect-2015-video]: https://www.youtube.com/watch?v=PDCMmSVOhlY
