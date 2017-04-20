[CmdletBinding(DefaultParameterSetName='RegularBuild')]
param (
    [string]$GalleryBaseUrl,
    [ValidateSet("Production", "Staging", "", IgnoreCase=$true)]
    [string]$GallerySlot,
    [string]$GalleryCloudServiceName,
    [string]$SubscriptionId,
    [string]$ApplicationId,
    [string]$TenantId,
    [string]$AzureCertificateThumbprint
)

Function Get-BaseUrl
{
    param (
        [string]$ApplicationId,
        [string]$AzureCertificateThumbprint,
        [string]$SubscriptionId,
        [string]$TenantId,
        [string]$BaseUrl,
        [ValidateSet("Production", "Staging", "", IgnoreCase=$true)]
        [string]$Slot,
        [string]$CloudServiceName
    )

    if ($Slot -eq "Staging")
    {
        # Use Azure PowerShell cmdlets to find the URL of the staging slot.
        Try
        {
            Write-Host "Logging into Azure as service principal."

            $login = Add-AzureRmAccount `
                -ApplicationId $ApplicationId `
                -CertificateThumbprint $AzureCertificateThumbprint `
                -ServicePrincipal `
                -SubscriptionId $SubscriptionId `
                -TenantId $TenantId

            Write-Host "Finding cloud service resource '$CloudServiceName'."
            $resourceGroupName = (Find-AzureRmResource -ResourceNameEquals $CloudServiceName).ResourceGroupName

            Write-Host "Get cloud service resource details for slot '$Slot'."
            $slotResource = Get-AzureRmResource `
                -Id "/subscriptions/$SubscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.ClassicCompute/domainNames/$CloudServiceName/slots/$Slot"
            
            $BaseUrl = ($slotResource.Properties.uri).Replace("http", "https")
        }
        Catch [System.Exception]
        {
            Write-Host "Failed to retrieve URL for testing!"
            Write-Host $_.Exception.Message
            Exit 1
        }
    }

    return $BaseUrl
}

$GalleryBaseUrl = Get-BaseUrl `
    -ApplicationId $ApplicationId `
    -AzureCertificateThumbprint $AzureCertificateThumbprint `
    -SubscriptionId $SubscriptionId `
    -TenantId $TenantId `
    -BaseUrl $GalleryBaseUrl `
    -Slot $GallerySlot `
    -CloudServiceName $GalleryCloudServiceName

Write-Host "Using the following GalleryBaseUrl: $GalleryBaseUrl" 
$env:GalleryBaseUrl = $GalleryBaseUrl
Write-Host "##vso[task.setvariable variable=GalleryBaseUrl;]$GalleryBaseUrl"
