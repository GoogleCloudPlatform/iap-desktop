$Releases = Invoke-RestMethod `
    -Uri "https://api.github.com/repos/GoogleCloudPlatform/iap-desktop/releases?per_page=30"

Write-Output "# What's new"

$Releases | % { 
    $PublishedAt = ([datetime]$_.published_at).ToLongDateString()
    Write-Output "## Release $($_.tag_name)"
    Write-Output ""
    Write-Output "$($_.body)"
    Write-Output ""
    Write-Output "_Published on $($PublishedAt)._ "
    Write-Output "_[More details :octicons-link-external-16:](https://github.com/GoogleCloudPlatform/iap-desktop/releases/tag/$($_.tag_name))._"
    Write-Output ""
}

Write-Output "[See all releases](https://github.com/GoogleCloudPlatform/iap-desktop/releases)"