$cloud = $false

if($cloud)
{
    $imageStoreConnectionString = "fabric:ImageStore"
}
else
{
    $imageStoreConnectionString = "file:C:\SfDevCluster\Data\ImageStoreShare"
}

$scriptPath = Get-Item(Convert-Path($MyInvocation.MyCommand.Path))
$scriptDirectory = Get-Item(Convert-Path($scriptPath.PSParentPath))

$upgradePackagePath = "$scriptDirectory\HealthMetricsV2ConfigOnlyPackage"

Test-ServiceFabricApplicationPackage -ApplicationPackagePath $upgradePackagePath -ImageStoreConnectionString $imageStoreConnectionString
Copy-ServiceFabricApplicationPackage -ApplicationPackagePath $upgradePackagePath -ImageStoreConnectionString $imageStoreConnectionString -ApplicationPackagePathInImageStore "HealthMetricsV2"
Register-ServiceFabricApplicationType -ApplicationPathInImageStore "HealthMetricsV2"

Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "2.0.0.0" -Monitored -FailureAction Rollback -Force 
