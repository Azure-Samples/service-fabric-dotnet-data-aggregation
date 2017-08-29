$cloud = $true
$certSecure = $false
$AADSecure = $true

if($cloud)
{
    $cloudAddress = ""
    $clusterAddress = $cloudAddress+":19000"
}
else
{
    $clusterAddress = "127.0.0.1:19000"
}

if($certSecure)
{
    $thumbprint = ""
    $commonName = ""
}

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
