#$imageStoreConnectionString = "file:C:\SfDevCluster\Data\ImageStoreShare"
$imageStoreConnectionString = "fabric:ImageStore"
Connect-ServiceFabricCluster ""

Get-ServiceFabricApplication -ApplicationName "fabric:/HealthMetrics" | Remove-ServiceFabricApplication -Force -ForceRemove
Get-ServiceFabricApplicationType -ApplicationTypeName "HealthMetrics" | Unregister-ServiceFabricApplicationType -Force

$folders = Get-ServiceFabricImageStoreContent -Path -ImageStoreConnectionString $imageStoreConnectionString -RemoteRelativePath "\"

if($folders -ne "Invalid location or unable to retrieve image store content")
{
    foreach($folder in $folders)
    {
        Remove-ServiceFabricApplicationPackage -ApplicationPackagePathInImageStore $folder.StoreRelativePath -ImageStoreConnectionString $imageStoreConnectionString   
    }
}