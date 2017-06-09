$cloud = $false
$secure = $false

if($cloud)
{
    $cloudAddress = ""
    $clusterAddress = $cloudAddress+":19000"
}
else
{
    $clusterAddress = "127.0.0.1:19000"
}

if($secure)
{
    $thumbprint = ""
    $commonName = ""
}

if($secure)
{
    Connect-ServiceFabricCluster $clusterAddress -FindType FindByThumbprint -FindValue $thumbprint -X509Credential -ServerCertThumbprint $thumbprint -ServerCommonName $commonName -StoreLocation CurrentUser -StoreName My -Verbose
}
else
{
    Connect-ServiceFabricCluster -ConnectionEndpoint $clusterAddress
}

$parameters = @{}
$parameters.Add("ScoreCalculationMode","Mode2")
$parameters.Add("GenerateKnownPeople","false")
Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "1.0.0.0" -Monitored -FailureAction Rollback -ApplicationParameter $parameters -Force -UpgradeReplicaSetCheckTimeoutSec 60 -HealthCheckRetryTimeoutSec 1 -HealthCheckWaitDurationSec 1 -HealthCheckStableDurationSec 1 -UpgradeDomainTimeoutSec 180 -UpgradeTimeoutSec 900
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