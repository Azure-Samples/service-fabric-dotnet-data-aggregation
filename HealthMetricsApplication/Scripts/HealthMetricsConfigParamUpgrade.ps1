$cloud = $false
$certSecure = $false
$AADSecure = $false

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

$parameters = @{}
$parameters.Add("ScoreCalculationMode","Mode2")
Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "1.0.0.0" -Monitored -FailureAction Rollback -ApplicationParameter $parameters -Force -UpgradeReplicaSetCheckTimeoutSec 60 -HealthCheckRetryTimeoutSec 1 -HealthCheckWaitDurationSec 1 -HealthCheckStableDurationSec 1 -UpgradeDomainTimeoutSec 300 -UpgradeTimeoutSec 18000
#Send-ServiceFabricServiceHealthReport -HealthProperty SomeHealthReport -HealthState Error -ServiceName fabric:/HealthMetrics/HealthMetrics.NationalService -SourceId user -RemoveWhenExpired -TimeToLiveSec 240

while($true)
{
    $upgradeResults = Get-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics"
    $upgradeResults
    sleep 5

    if(($upgradeResults.UpgradeState -eq "RollingForwardCompleted") -or ($upgradeResults.UpgradeState -eq "RollingBackCompleted"))
    {
        break
    }
}