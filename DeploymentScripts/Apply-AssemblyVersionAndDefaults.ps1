param (
    [Parameter(Mandatory = $true)] [string] $buildNumber,
    [Parameter(Mandatory = $true)] [string] $solutionDirectory
)

Write-Host "Setting assembly versions and defaults..."
Write-Host "    BuildNumber: $buildNumber"
Write-Host "    SolutionDir: $solutionDirectory"

$majorMinorVersion = "1.0"

$buildNumberRegex = "(.+)_20([0-9]{3,5}).([0-9]{1,2})"
$validBuildNumber = $buildNumber -match $buildNumberRegex

if ($validBuildNumber -eq $false) {
    Write-Error "Build number passed in must be in the following format: (BuildDefinitionName)_(yyyyDDD).(rev)"
    return
}

$buildNumberSplit = $buildNumber.Split('_')
$buildRevisionNumber = $buildNumberSplit[1] -replace ".DRAFT", ""
$versionToApply = "$majorMinorVersion.$buildRevisionNumber"

$assemblyValues = @{
    "Company"         = "UK Hydrographic Office";
    "Copyright"       = "Crown Copyright © UK Hydrographic Office 2019";
    "Description"     = "UKHO.ConfigurableStub";
    "Product"         = "UKHO.ConfigurableStub";
    "AssemblyVersion" = $versionToApply;
    "FileVersion"     = $versionToApply;
    "Version"         = $versionToApply;
}

Write-Host "    Set version to $versionToApply"

function UpdateOrAddAttributes([xml]$xmlContent, $assemblyKey, $newValue) {
    $propertyGroupNode = $xmlContent.Project.PropertyGroup.$assemblyKey

    if ($null -ne $propertyGroupNode ) {
        Write-Host "Assembly key $assemblyKey has been located in source file. Updating..."
        $xmlContent.Project.PropertyGroup.$assemblyKey = $newValue
    }

    Write-Host "Assembly key $assemblyKey could not be located in source file. Appending..."

    $newChild = $xmlContent.CreateElement($assemblyKey)
    $newChild.InnerText = $newValue
    $xmlContent.Project.PropertyGroup.AppendChild($newChild)

    return $propertyGroupNode
}

(Get-ChildItem -Path $solutionDirectory -File -Filter "*.csproj" -Recurse) | ForEach-Object
{
    $file = $_

    Write-Host "Updating assembly file at path: $file"
    [xml]$xmlContent = (Get-Content $file.FullName)

    $assemblyValues.Keys | ForEach-Object {
        $key = $_
        
        UpdateOrAddAttributes $xmlContent $key $assemblyValues[$key]
    }

    $xmlContent.Save($file.FullName)
}