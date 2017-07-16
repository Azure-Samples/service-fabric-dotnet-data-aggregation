#Note that this file deplicates a lot of what is present in Deploy-ServiceFabricApplication (which is included
#in every Service Fabric project in VS by default). This script demonstrates dynamic service creation, rather
#than relying on default services as the Visual Studio tooling does. It has the advantage of being able
#to be used outside of the VS environment. That said, it does not naturally understand environment profiles
#or the application parameters mechanism provided via the VS tooling. It is meant to serve as an example
#of manual application and service creation and configuration. 

$cloud = $true
$singleNode = $false
$certSecure = $false
$AADSecure = $true
$constrainedNodeTypes = $false

if($cloud)
{
    $cloudAddress = "wincontainer005.southcentralus.cloudapp.azure.com"
}

if($certSecure)
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

if($cloud)
{
    $clusterAddress = $cloudAddress+":19000"
    $webServiceInstanceCount = -1
    $bandCreationInstanceCount = -1
    $bandsPerService = "3000"
    $countyServicePartitionCount = @{$true=1;$false=200}[$singleNode -eq $true]
    $bandActorServicePartitionCount = @{$true=1;$false=400}[$singleNode -eq $true]
    $doctorActorServicePartitionCount = @{$true=1;$false=400}[$singleNode -eq $true]
    $imageStoreConnectionString = "fabric:ImageStore"
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
#    $webServiceConstraint = "NodeType == $frontendNodeType"
#    $countyServiceConstraint = "NodeType == $backendNodeType"
#    $nationalServiceConstraint = "NodeType == $backendNodeType"
#    $bandServiceConstraint = "NodeType == $backendNodeType"
#    $doctorServiceConstraint = "NodeType == $backendNodeType"   
#    $bandCreationServiceConstraint = "NodeType == $loadGenNodeType"   


    $webServiceConstraint = "NodeType != sf"
    $countyServiceConstraint = "(NodeType == bg0) || (NodeType == bg1)"
    $nationalServiceConstraint = "(NodeType == bg0) || (NodeType == bg1)"
    $bandServiceConstraint = "(NodeType == bg2) || (NodeType == bg3) || (NodeType == bg4) || (NodeType == bg5)"
    $doctorServiceConstraint = "(NodeType == bg6) || (NodeType == bg7) || (NodeType == bg8) || (NodeType == bg9)"  
    $bandCreationServiceConstraint = "NodeType != sf"   
     
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
$parameters.Add("MaxBandsToCreatePerServiceInstance", $bandsPerService)

Write-Host "Connecting to $clusterAddress"

if($certSecure)
{
    Connect-ServiceFabricCluster -ConnectionEndpoint $clusterAddress -FindType FindByThumbprint -FindValue $thumbprint -X509Credential -ServerCertThumbprint $thumbprint -ServerCommonName $commonName -StoreLocation CurrentUser -StoreName My -Verbose
}
elseif($AADSecure) 
{
    Connect-ServiceFabricCluster -ConnectionEndpoint $clusterAddress -AzureActiveDirectory
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
New-ServiceFabricService -ServiceTypeName $webServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$webServiceName" -PartitionSchemeSingleton -InstanceCount $webServiceInstanceCount -PlacementConstraint $webServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create national
New-ServiceFabricService -ServiceTypeName $nationalServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$nationalServiceName" -PartitionSchemeSingleton -MinReplicaSetSize $nationalServiceReplicaCount -TargetReplicaSetSize $nationalServiceReplicaCount -PlacementConstraint $nationalServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create county
New-ServiceFabricService -ServiceTypeName $countyServiceType -Stateful -HasPersistedState -ApplicationName $appName -ServiceName "$appName/$countyServiceName" -PartitionSchemeUniformInt64 -LowKey $countyLowKey -HighKey $countyHighKey -PartitionCount $countyServicePartitionCount -MinReplicaSetSize $countyServiceReplicaCount -TargetReplicaSetSize $countyServiceReplicaCount -PlacementConstraint $countyServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create doctor
New-ServiceFabricService -ServiceTypeName $doctorActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$doctorActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $doctorActorServicePartitionCount -MinReplicaSetSize $doctorServiceReplicaCount -TargetReplicaSetSize $doctorServiceReplicaCount -PlacementConstraint $doctorServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create band
New-ServiceFabricService -ServiceTypeName $bandActorServiceType -Stateful -ApplicationName $appName -ServiceName "$appName/$bandActorServiceName" -PartitionSchemeUniformInt64 -LowKey $lowkey -HighKey $highkey -PartitionCount $bandActorServicePartitionCount -MinReplicaSetSize $bandActorReplicaCount -TargetReplicaSetSize $bandActorReplicaCount -PlacementConstraint $bandServiceConstraint -ServicePackageActivationMode ExclusiveProcess

#create band creation
New-ServiceFabricService -ServiceTypeName $bandCreationServiceType -Stateless -ApplicationName $appName -ServiceName "$appName/$bandCreationServiceName" -PartitionSchemeSingleton -InstanceCount $bandCreationInstanceCount -PlacementConstraint $bandCreationServiceConstraint -ServicePackageActivationMode ExclusiveProcess