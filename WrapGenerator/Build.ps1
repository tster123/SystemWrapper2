[CmdletBinding()]
param (
	[Parameter()]
	[switch]
	$PackageNuget
)

$originalDir = $pwd
cd $PSScriptRoot
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
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <PropertyGroup>
    <PackageId>SystemWrapper2</PackageId>
    <Version>1.0.0</Version>
    <Authors>Tyler Boone</Authors>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="WrapHelpers.cs" />
    <Compile Include="Assembly.cs" />
  </ItemGroup>

  <ItemGroup>
__COMPILE_FILES__
  </ItemGroup>

  <PropertyGroup>
    <NoWarn>3010,0618,CA1416</NoWarn>
  </PropertyGroup>
</Project>
'


$folders = dir "WrapCli\bin\Debug"
$frameworkString = ""
$compileItems = ""
foreach ($folder in $folders)
{
	$framework = $folder.Name

	if ($frameworkString -ne "") { $frameworkString += ";" }
	$frameworkString += $framework

	$genFrameworkDir = "$PSScriptRoot\gen\$framework"
	Write-Host "Generating $framework into $genFrameworkDir"
	. "$($folder.FullName)\WrapCli.exe" $genFrameworkDir

	#$compileItems += "  <ItemGroup>"
    $compileItems += "    <Compile Include=`"$genFrameworkDir\**\*.cs`" Condition=`"'`$(TargetFramework)' == '$framework'`"/>`n"
    #$compileItems += "  </ItemGroup>"

	

}

$genDir = "$PSScriptRoot\gen"
$source = "$PSScriptRoot\SystemWrapper2"
if (-not (test-path $genDir)) { mkdir $genDir }
$csProj = $CsProjTemplate
$csProj = $csProj.Replace("__FRAMEWORK__", $frameworkString)
$csProj = $csProj.Replace("__COMPILE_FILES__", $compileItems)
$csProj | Out-File -FilePath "$genDir\SystemWrapper2.csproj"
Copy-Item -LiteralPath "$source\WrapHelpers.cs" -Destination $genDir -Force
Copy-Item -LiteralPath "$source\Assembly.cs" -Destination $genDir -Force

cd $genDir

if ($PackageNuget)
{
	dotnet pack --version 1.0.1
}
else 
{
	dotnet build
}

cd $originalDir