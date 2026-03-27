dotnet build
if ($LASTEXITCODE -ne 0)
{
	return
}

$CsProjTemplate = '
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>__FRAMEWORK__</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>14.0</LangVersion>
  </PropertyGroup>

  <PropertyGroup>
    <NoWarn>3010,0618,CA1416</NoWarn>
  </PropertyGroup>
</Project>
'


$folders = dir "WrapCli\bin\Debug"
$originalDir = $pwd
foreach ($folder in $folders)
{
	$framework = $folder.Name
	$targetDir = "$PSScriptRoot\obj\$framework"
	$genDir = "$targetDir\gen"
	Write-Host "Generating $framework into $genDir"
	. "$($folder.FullName)\WrapCli.exe" $genDir

	$csProj = $CsProjTemplate.Replace("__FRAMEWORK__", $framework)
	$csProjFilename = "$targetDir\SystemWrapper2.csproj"
	$csProj | Out-File -FilePath $csProjFilename
	Copy-Item -LiteralPath "$PSScriptRoot\SystemWrapper2\WrapHelpers.cs" -Destination $targetDir
	Copy-Item -LiteralPath "$PSScriptRoot\SystemWrapper2\Assembly.cs" -Destination $targetDir

	cd $targetDir
	dotnet build

}

cd $originalDir