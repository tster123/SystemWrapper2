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
    <Compile Include=`"WrapHelpers.cs`" />
	<Compile Include=`"Assembly.cs`" />
  <ItemGroup>

  <ItemGroup>
__COMPILE_FILES__
  </ItemGroup>

  <PropertyGroup>
    <NoWarn>3010,0618,CA1416</NoWarn>
  </PropertyGroup>
</Project>
'


$folders = dir "WrapCli\bin\Debug"
$originalDir = $pwd
$frameworkString = ""
$compileItems = ""
foreach ($folder in $folders)
{
	$framework = $folder.Name

	if ($frameworkString != "") { $frameworkString += ";" }
	$frameworkString += $framework

	$genFrameworkDir = "$PSScriptRoot\gen\$framework"
	Write-Host "Generating $framework into $genFrameworkDir"
	. "$($folder.FullName)\WrapCli.exe" $genFrameworkDir

	#$compileItems += "  <ItemGroup>"
    $compileItems += "    <Compile Include=`"$genFrameworkDir/*.cs`" Condition=`"'`$(TargetFramework)' == '$framework'`"/>"
    #$compileItems += "  </ItemGroup>"

	

}

$genDir = "$PSScriptRoot\gen"

$csProj = $CsProjTemplate.Replace("__FRAMEWORK__", $frameworkString)
$csProj = $CsProjTemplate.Replace("__COMPILE_FILES__", $compileItems)
$csProjFilename = "$genDir\SystemWrapper2.csproj"
$csProj | Out-File -FilePath $genDir
Copy-Item -LiteralPath "$genDir\WrapHelpers.cs" -Destination $targetDir -Force
Copy-Item -LiteralPath "$genDir\Assembly.cs" -Destination $targetDir -Force

cd $targetDir
dotnet build

cd $originalDir