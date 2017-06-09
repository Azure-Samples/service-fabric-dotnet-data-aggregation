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

Get-ServiceFabricApplication -ApplicationName "fabric:/HealthMetrics" | Remove-ServiceFabricApplication -Force -ForceRemove
Get-ServiceFabricApplicationType -ApplicationTypeName "HealthMetrics" | Unregister-ServiceFabricApplicationType -Force

$folders = Get-ServiceFabricImageStoreContent -ImageStoreConnectionString $imageStoreConnectionString 

if($folders -ne "Invalid location or unable to retrieve image store content")
{
    foreach($folder in $folders)
    {
        if(($folder.StoreRelativePath -ne "Store") -and ($folder.StoreRelativePath -ne "WindowsFabricStore"))
        {
                Remove-ServiceFabricApplicationPackage -ApplicationPackagePathInImageStore $folder.StoreRelativePath -ImageStoreConnectionString $imageStoreConnectionString   
        }
    }
}
