#Note that this file deplicates a lot of what is present in Deploy-ServiceFabricApplication (which is included
#in every Service Fabric project in VS by default). This script demonstrates dynamic service creation, rather
#than relying on default services as the Visual Studio tooling does. It has the advantage of being able
#to be used outside of the VS environment. That said, it does not naturally understand environment profiles
#or the application parameters mechanism provided via the VS tooling. It is meant to serve as an example
#of manual application and service creation and configuration. 

$cloud = $false
$singleNode = $true
$secure = $false
$constrainedNodeTypes = $false


if($cloud)
{
    $cloudAddress = ""
}

if($secure)
{
    $thumbprint = ""
    $commonName = ""
}

$scriptPath = Get-Item(Convert-Path($MyInvocation.MyCommand.Path))
$scriptDirectory = Get-Item(Convert-Path($scriptPath.PSParentPath))
$appDirectory = Get-Item(Convert-Path($scriptDirectory.PSParentPath))
$rootName = $appDirectory.FullName
$packagePath = "$rootName\pkg\Debug"

$lowkey = "-9223372036854775808"
$highkey = "9223372036854775807" 

$countyLowKey = 0
$countyHighKey = 57000

$appName = "fabric:/HealthMetrics"
$appType = "HealthMetrics"
$appInitialVersion = "1.0.0.0"

$frontendNodeType = "Front"
$backendNodeType = "Back"
$loadGenNodeType = "System"

if($cloud)
{
    $clusterAddress = $cloudAddress+":19000"
    $webServiceInstanceCount = -1
    $bandCreationInstanceCount = -1
    $countyServicePartitionCount = @{$true=1;$false=5}[$singleNode -eq $true]
    $bandActorServicePartitionCount = @{$true=1;$false=15}[$singleNode -eq $true]
    $doctorActorServicePartitionCount = @{$true=1;$false=15}[$singleNode -eq $true]
    $imageStoreConnectionString = "fabric:ImageStore"
    $bandsPerService = "5000"
}
else
{
    $clusterAddress = "127.0.0.1:19000"
    $webServiceInstanceCount = 1
    $bandCreationInstanceCount = 1
    $countyServicePartitionCount = @{$true=1;$false=2}[$singleNode -eq $true]  
    $bandActorServicePartitionCount = @{$true=1;$false=2}[$singleNode -eq $true]  
    $doctorActorServicePartitionCount = @{$true=1;$false=2}[$singleNode -eq $true]  
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
$nationalServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

$countyServiceType = "HealthMetrics.CountyServiceType"
$countyServiceName = "HealthMetrics.CountyService"
$countyServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]  

$bandCreationServiceType = "HealthMetrics.BandCreationServiceType"
$bandCreationServiceName = "HealthMetrics.BandCreationService"

$doctorActorServiceType = "DoctorActorServiceType"
$doctorActorServiceName = "DoctorActorService"
$doctorServiceReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]

$bandActorServiceType = "BandActorServiceType"
$bandActorServiceName= "BandActorService"
$bandActorReplicaCount = @{$true=1;$false=3}[$singleNode -eq $true]

$parameters = @{}
$parameters.Add("ScoreCalculationMode","Mode1")
$parameters.Add("GenerateKnownPeople","false")
$parameters.Add("MaxBandsToCreatePerServiceInstance", $bandsPerService)

Write-Host "Connecting to $clusterAddress"

if($secure)
{
    Connect-ServiceFabricCluster -ConnectionEndpoint $clusterAddress -FindType FindByThumbprint -FindValue $thumbprint -X509Credential -ServerCertThumbprint $thumbprint -ServerCommonName $commonName -StoreLocation CurrentUser -StoreName My -Verbose
}
else
{
    Connect-ServiceFabricCluster -ConnectionEndpoint $clusterAddress
}

Test-ServiceFabricApplicationPackage -ApplicationPackagePath $packagePath -ImageStoreConnectionString $imageStoreConnectionString
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $packagePath -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore "HealthMetricsV1" -CompressPackage -ShowProgress

Register-ServiceFabricApplicationType -ApplicationPathInImageStore "HealthMetricsV1" -Async

while($true)
{
    $app = Get-ServiceFabricApplicationType -ApplicationTypeName "HealthMetrics"
    if($app.Status -eq "Available")
    {
        break
    }
    else
    {
        sleep 2
    }
}

Remove-ServiceFabricApplicationPackage -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore "HealthMetricsV1"

Read-Host -Prompt "Continue?"

New-ServiceFabricApplication -ApplicationName $appName -ApplicationTypeName $appType -ApplicationTypeVersion $appInitialVersion -ApplicationParameter $parameters

#create web
New-ServiceFabricService -ServiceTypeName $webServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$webServiceName" -PartitionSchemeSingleton -InstanceCount $webServiceInstanceCount -PlacementConstraint $webServiceConstraint 

#create national
New-ServiceFabricService -ServiceTypeName $nationalServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$nationalServiceName" -PartitionSchemeSingleton -MinReplicaSetSize $nationalServiceReplicaCount -TargetReplicaSetSize $nationalServiceReplicaCount -PlacementConstraint $nationalServiceConstraint

#create county
New-ServiceFabricService -ServiceTypeName $countyServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$countyServiceName" -PartitionSchemeUniformInt64 -LowKey $countyLowKey -HighKey $countyHighKey -PartitionCount $countyServicePartitionCount -MinReplicaSetSize $countyServiceReplicaCount -TargetReplicaSetSize $countyServiceReplicaCount -PlacementConstraint $countyServiceConstraint

#create doctor
New-ServiceFabricService -ServiceTypeName $doctorActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$doctorActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $doctorActorServicePartitionCount -MinReplicaSetSize $doctorServiceReplicaCount -TargetReplicaSetSize $doctorServiceReplicaCount -PlacementConstraint $doctorServiceConstraint

#create band
New-ServiceFabricService -ServiceTypeName $bandActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$bandActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $bandActorServicePartitionCount -MinReplicaSetSize $bandActorReplicaCount -TargetReplicaSetSize $bandActorReplicaCount -PlacementConstraint $bandServiceConstraint

#create band creation
New-ServiceFabricService -ServiceTypeName $bandCreationServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$bandCreationServiceName" -PartitionSchemeSingleton -InstanceCount $bandCreationInstanceCount -PlacementConstraint $bandCreationServiceConstraint