---
services: service-fabric
platforms: dotnet
author: masnider
---

# Service Fabric Data Aggregation Sample (aka HealthMetrics)
Learn about large-scale data aggregation and management of large volumes of end-user devices with Service Fabric.
## Running this sample
Just open the solution in Visual Studio and hit F5, or right click on the Service Fabric application project and Publish the application to the cluster of your choice.

## Deploy this sample to Azure
Right click on the Application project (HealthMetrics) and select Publish. Then select the azure cluster of your choice (or create a new profile)

## About the code
There are several services included in the sample. They are:

- BandCreation Service: this creates the individual band actors that then generate the actual health data
- BandActor Service: this service is the host for the band actors. Each BandActor represents an individual Microsoft Band or other device, which periodically generates health data then and sends it on to the Band's designated DoctorActor
- DoctorActor Service: Each band has an associated Doctor, hosted as Actors inside the DoctorActor Service. Each DoctorActor aggregates all of the data it receives from each BandActor and generates some statistics as well as an overall view for that Doctor of their Patients/Customers (pointing back to the Bands). The DoctorActors then push their data into the CountyService
- CountyService: The county service aggregates the information provided by the DoctorActors further and also pushes the data into the NationalService
- NationalService: This maintains the total aggregated data for the entire country and is used to serve data requested by WebService
- WebService: This Service just hosts a simple web UI, web API interfaces, and serves information obtained via the NationalService so that it can be rendered.

## More information
This repository contains a sample application that demostrates a scenario where thousands of devices submit information into a series of Service Fabric services which then aggregate and present that information. If you are looking for smaller samples as you are just getting started, please take a look at some of the [other Service Fabric samples][service-fabric-samples] available in the Azure samples gallery.

This sample is based on the original sample used at Service Fabric's [GA announcement at Build 2015][build-2015-video], and represents an improved version of that demonstration as well as the "health metrics" demonstration done by Scott Hanselman at [Connect 2015][connect-2015-video] when Service Fabric's public preview was announced.

<!-- Links -->

[service-fabric-samples]: http://aka.ms/servicefabricsamples
[build-2015-video]: https://channel9.msdn.com/Events/Build/2015/3-618
[connect-2015-video]: https://www.youtube.com/watch?v=PDCMmSVOhlY
