#Note that this file is duplicative of what is present in Deploy-ServiceFabricApplication which is included
#in every Service Fabric project in VS by default. This shows real dynamic service creation, rather
#than relying on default services as the visual studio tooling does. It has the advantage of being able
#to be used outside of the VS environment. That said, it does not naturally understand environment profiles
#or the application parameters mechanism provided via the VS tooling. It is meant to serve as an example
#of manual application and service creation and configuration. 

$cloud = $false
$cloudAddress = ""
$constrainedNodeTypes = $false

$scriptPath = Get-Item(Convert-Path($MyInvocation.MyCommand.Path))
$scriptDirectory = Get-Item(Convert-Path($scriptPath.PSParentPath))
$appDirectory = Get-Item(Convert-Path($scriptDirectory.PSParentPath))
$rootName = $appDirectory.FullName
$packagePath = "$rootName\pkg\Debug\"

$lowkey = "-9223372036854775808"
$highkey = "9223372036854775807" 

$countyLowKey = 0
$countyHighKey = 57000

$appName = "fabric:/HealthMetrics"
$appType = "HealthMetrics"
$appInitialVersion = "1.0.0.0"

$frontendNodeType = "FrontEndNodeType"
$backendNodeType = "BackendNodeType"
$loadGenNodeType = "LoadGenNodeType"

if($cloud)
{
    $clusterAddress = $cloudAddress+":19000"
    $webServiceInstanceCount = -1
    $bandCreationInstanceCount = -1
    $countyServicePartitionCount = 5
    $bandActorServicePartitionCount = 5
    $doctorActorServicePartitionCount = 5
    $imageStoreConnectionString = "fabric:ImageStore"
    $bandsPerService = "5000"
}
else
{
    $clusterAddress = "localhost:19000"
    $webServiceInstanceCount = 1
    $bandCreationInstanceCount = 1
    $countyServicePartitionCount = 2
    $bandActorServicePartitionCount = 2
    $doctorActorServicePartitionCount = 2
    $imageStoreConnectionString = "file:C:\SfDevCluster\Data\ImageStoreShare"
    $bandsPerService = "300"
}

if($constrainedNodeTypes)
{
    $webServiceConstraint = "NodeType == $frontendNodeType"
    $countyServiceConstraint = "NodeType == $backendNodeType"
    $nationalServiceConstraint = "NodeType == $backendNodeType"
    $bandServiceConstraint = "NodeType == $backendNodeType"
    $doctorServiceConstraint = "NodeType == $backendNodeType"   
    $bandCreationServiceConstraint = "NodeType == $loadGenNodeType"   
     
}
else
{
    $webServiceConstraint = ""
    $countyServiceConstraint = ""
    $nationalServiceConstraint = ""
    $bandServiceConstraint = ""
    $doctorServiceConstraint = ""
    $bandCreationServiceConstraint = ""   
}


$webServiceType = "HealthMetrics.WebServiceType"
$webServiceName = "HealthMetrics.WebService"

$nationalServiceType = "HealthMetrics.NationalServiceType"
$nationalServiceName = "HealthMetrics.NationalService"
$nationalServiceReplicaCount = 3

$countyServiceType = "HealthMetrics.CountyServiceType"
$countyServiceName = "HealthMetrics.CountyService"
$countyServiceReplicaCount = 3

$bandCreationServiceType = "HealthMetrics.BandCreationServiceType"
$bandCreationServiceName = "HealthMetrics.BandCreationService"

$doctorActorServiceType = "DoctorActorServiceType"
$doctorActorServiceName = "DoctorActorService"
$doctorServiceReplicaCount = 3

$bandActorServiceType = "BandActorServiceType"
$bandActorServiceName= "BandActorService"
$bandActorReplicaCount = 3

$parameters = @{}
$parameters.Add("ScoreCalculationMode","Mode1")
$parameters.Add("CreationServiceParallelThreads","4")
$parameters.Add("MaxBandsToCreatePerServiceInstance", $bandsPerService)
$parameters.Add("GenerateKnownPeople","false")

Write-Host "Connecting to $clusterAddress"

Connect-ServiceFabricCluster $clusterAddress

Test-ServiceFabricApplicationPackage -ApplicationPackagePath $packagePath -ImageStoreConnectionString $imageStoreConnectionString
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $packagePath -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore "HealthMetricsV1"

Register-ServiceFabricApplicationType -ApplicationPathInImageStore "HealthMetricsV1"

Remove-ServiceFabricApplicationPackage -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore "HealthMetricsV1"

New-ServiceFabricApplication -ApplicationName $appName -ApplicationTypeName $appType -ApplicationTypeVersion $appInitialVersion -ApplicationParameter $parameters

#create web
New-ServiceFabricService -ServiceTypeName $webServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$webServiceName" -PartitionSchemeSingleton -InstanceCount $webServiceInstanceCount -PlacementConstraint $webServiceConstraint

#create national
New-ServiceFabricService -ServiceTypeName $nationalServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$nationalServiceName" -PartitionSchemeSingleton -MinReplicaSetSize $nationalServiceReplicaCount -TargetReplicaSetSize $nationalServiceReplicaCount -PlacementConstraint $nationalServiceConstraint

#create county
New-ServiceFabricService -ServiceTypeName $countyServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$countyServiceName" -PartitionSchemeUniformInt64 -LowKey $countyLowKey -HighKey $countyHighKey -PartitionCount $countyServicePartitionCount -MinReplicaSetSize $countyServiceReplicaCount -TargetReplicaSetSize $countyServiceReplicaCount -PlacementConstraint $countyServiceConstraint

#create doctor
New-ServiceFabricService -ServiceTypeName $doctorActorServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$doctorActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $doctorActorServicePartitionCount -MinReplicaSetSize $doctorServiceReplicaCount -TargetReplicaSetSize $doctorServiceReplicaCount -PlacementConstraint $doctorServiceConstraint

#create band
New-ServiceFabricService -ServiceTypeName $bandActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$bandActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $bandActorServicePartitionCount -MinReplicaSetSize $bandActorReplicaCount -TargetReplicaSetSize $bandActorReplicaCount -PlacementConstraint $bandServiceConstraint

#create band creation
New-ServiceFabricService -ServiceTypeName $bandCreationServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$bandCreationServiceName" -PartitionSchemeSingleton -InstanceCount $bandCreationInstanceCount -PlacementConstraint $bandCreationServiceConstraint