. "$PSScriptRoot/ReleaseTools.ps1"
$checks = 0
foreach ($tag in @('v1.0.0','v1.0.1','v1.1.0','v65534.65534.65534')) {
    $result = Get-ReleaseVersion $tag
    if ($result.PackageVersion -ne ($tag.Substring(1) + '.0')) { throw "Version mapping failed: $tag" }
    $checks++
}
foreach ($tag in @('1.0.0','v0.1.0','v01.0.0','v1.01.0','v1.0.0.1','v1.0.0-beta','v1.0.0+build','v65535.0.0','v99999999999999.0.0','v1.0.0/anything')) {
    $rejected = $false
    try { $null = Get-ReleaseVersion $tag } catch { $rejected = $true }
    if (!$rejected) { throw "Invalid tag accepted: $tag" }
    $checks++
}
Write-Host "$checks release-version checks passed."

$root = Split-Path $PSScriptRoot -Parent
[xml]$manifest = [IO.File]::ReadAllText((Join-Path $root 'packaging/AppxManifest.xml'))
$namespaces = [Xml.XmlNamespaceManager]::new($manifest.NameTable)
$namespaces.AddNamespace('v','http://schemas.microsoft.com/appx/manifest/virtualization/windows10')
$namespaces.AddNamespace('u5','http://schemas.microsoft.com/appx/manifest/uap/windows10/5')
if ($manifest.Package.Identity.Name -cne 'DaniloStoletovic.AutoBright' -or
    $manifest.Package.Identity.Publisher -cne 'CN=65C1819A-0B07-4365-9B17-61242ED51E2F' -or
    $manifest.Package.Properties.PublisherDisplayName -cne 'Danilo Stoletovic') {
    throw 'Store manifest must match the supplied AutoBright Partner Center identity.'
}
if (!$manifest.SelectSingleNode('//u5:ExecutionAlias[@Alias="AutoBright.Store.exe"]',$namespaces) -or
    $manifest.OuterXml.Contains('unvirtualizedResources') -or $manifest.SelectSingleNode('//v:RegistryWriteVirtualization',$namespaces)) {
    throw 'Manifest must retain the scheduling alias without unvirtualizedResources or registry exclusions.'
}
Write-Host '2 Store template checks passed.'
$namespaces.AddNamespace('f','http://schemas.microsoft.com/appx/manifest/foundation/windows10')
$namespaces.AddNamespace('r','http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')
if (!$manifest.SelectSingleNode('//f:DeviceCapability[@Name="webcam"]',$namespaces) -or
    !$manifest.SelectSingleNode('//r:Capability[@Name="runFullTrust"]',$namespaces) -or
    $manifest.SelectSingleNode('//f:DeviceCapability[@Name="location" or @Name="microphone"]',$namespaces)) {
    throw 'Store manifest must declare webcam and runFullTrust without location or microphone.'
}
Write-Host 'Camera capability check passed.'
$rejected = $false
try {
    & "$PSScriptRoot/Build-Store.ps1" -Tag v1.0.0 -Executable 'missing.exe' `
        -IdentityName '@@IDENTITY_NAME@@' -Publisher '@@PUBLISHER@@' -PublisherDisplayName '@@PUBLISHER_DISPLAY_NAME@@'
} catch {
    if ($_.Exception.Message -notlike 'Store packaging requires real Identity Name*') { throw }
    $rejected = $true
}
if (!$rejected) { throw 'Store packaging must reject unresolved identity before producing output.' }
Write-Host 'Unresolved Store identity correctly rejected.'
