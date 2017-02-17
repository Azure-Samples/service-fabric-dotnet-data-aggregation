$parameters = @{}
$parameters.Add("ScoreCalculationMode","Mode2")
Start-ServiceFabricApplicationUpgrade -ApplicationName "fabric:/HealthMetrics" -ApplicationTypeVersion "1.0.0.0" -Monitored -FailureAction Rollback -UpgradeDomainTimeoutSec 120 -HealthCheckRetryTimeoutSec 10 -ApplicationParameter $parameters -Force 