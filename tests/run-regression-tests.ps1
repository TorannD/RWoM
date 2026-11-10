[CmdletBinding()]
param(
    [string]$GameDir = 'C:/Program Files (x86)/Steam/steamapps/common/RimWorld',
    [string]$JecsDir = 'C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/3312959701/1.7/Assemblies',
    [string]$HarmonyDir = 'C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/2009463077/1.7/Assemblies',
    [string]$ModAssembly,
    [string]$CoreAssembly,
    [string]$StagedRelease,
    [switch]$WithoutCore,
    [string]$VgeDir = 'C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/3609835606/1.7/Assemblies',
    [string]$AbilityAIDir,
    [string]$CEDir = 'C:/Program Files (x86)/Steam/steamapps/workshop/content/294100/1874644848/v1.7/Assemblies',
    [string]$ArtifactsDir = (Join-Path ([IO.Path]::GetTempPath()) ('rwom-regression-' + [Guid]::NewGuid().ToString('N'))),
    [string]$Filter = '',
    [switch]$Reverse
)

$ErrorActionPreference = 'Stop'
if ($StagedRelease) {
    if ($ModAssembly -or $CoreAssembly -or $WithoutCore) { throw 'StagedRelease selects its own Content and Core binaries.' }
    $StagedRelease = (Resolve-Path -LiteralPath $StagedRelease).Path
    $ModAssembly = Join-Path $StagedRelease 'Content/v1.7/Assemblies/TorannMagic.dll'
    $CoreAssembly = Join-Path $StagedRelease 'Core/Assemblies/TorannMagic.Core.dll'
}
if ($CoreAssembly -and $WithoutCore) { throw "Select a Core assembly or -WithoutCore, not both." }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$defaultCore = Join-Path $repo 'TorannMagic.Core/Assemblies/TorannMagic.Core.dll'
# SDK cases and consumers must use the same selection. A null value lets
# consumers discover repository Core but silently omits its public API cases.
$coreForTests = if ($CoreAssembly) { (Resolve-Path -LiteralPath $CoreAssembly).Path } elseif ($WithoutCore) { "disabled" } elseif (Test-Path -LiteralPath $defaultCore -PathType Leaf) { $defaultCore } else { "disabled" }
$refs = [IO.Path]::GetFullPath((Join-Path $repo '../refs/out'))
$ArtifactsDir = [IO.Path]::GetFullPath($ArtifactsDir)
if ($ArtifactsDir.StartsWith($repo + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'ArtifactsDir must be outside the repository so builds cannot change tracked assemblies or source inputs.'
}
foreach ($path in @((Join-Path $GameDir 'Assembly-CSharp.dll'), (Join-Path $GameDir 'UnityStubs.dll'), (Join-Path $JecsDir 'AbilityUser.dll'))) {
    if (!(Test-Path $path -PathType Leaf)) { throw "Required runtime dependency missing: $path" }
}
New-Item -ItemType Directory -Force $ArtifactsDir | Out-Null

# Compile matching Core sources externally; never update the repository DLL during a test.
$coreForBuild = if ($CoreAssembly) { $coreForTests } else { $null }
if (!$coreForBuild) {
    $coreProject = Join-Path $repo 'TorannMagic.Core/Source/TorannMagic.Core.csproj'
    & dotnet build $coreProject -c Release "-p:OutputPath=$ArtifactsDir/core/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/core/obj/" '-p:NuGetAudit=false' 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'core-build.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $coreForBuild = Join-Path $ArtifactsDir 'core/out/TorannMagic.Core.dll'
}
if (!$WithoutCore) { $coreForTests = [IO.Path]::GetFullPath($coreForBuild) }

# No downloads: production restore reads only packages already in the local NuGet cache.
# Both output and intermediate directories are outside the source tree; never build net48.
if (!$ModAssembly) {
    $project = Join-Path $repo 'RimWorldOfMagic/RimWorldOfMagic/RimWorldOfMagic.csproj'
    $productionArgs = @("-p:OutputPath=$ArtifactsDir/production/out/", "-p:BaseIntermediateOutputPath=$ArtifactsDir/production/obj/", '-p:NuGetAudit=false')
    $packageCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $HOME '.nuget/packages' }
    & dotnet restore $project --source $packageCache @productionArgs 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'production-restore.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & dotnet build $project --no-restore -c Release @productionArgs 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'production-build.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $ModAssembly = Join-Path $ArtifactsDir 'production/out/TorannMagic.dll'
}
$ModAssembly = [IO.Path]::GetFullPath($ModAssembly)
if (!(Test-Path $ModAssembly -PathType Leaf)) { throw "Production DLL missing: $ModAssembly" }

# Keep the optional interface implementation outside the core mod assembly and production folder.
$integrationProject = Join-Path $repo 'TorannMagic.Integrations/Source/TorannMagic.Integrations/TorannMagic.Integrations.csproj'
& dotnet build $integrationProject -c Release "-p:OutputPath=$ArtifactsDir/integrations/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/integrations/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:CoreAssemblyPath=$coreForBuild" "-p:GameDir=$GameDir" "-p:HarmonyDir=$HarmonyDir" "-p:JecsDir=$JecsDir" 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'integrations-build.log')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:RWOM_INTEGRATIONS_ASSEMBLY = Join-Path $ArtifactsDir 'integrations/out/TorannMagic.Integrations.dll'
$mpProject = Join-Path $repo 'TorannMagic.Integrations/Source/TorannMagic.Multiplayer/TorannMagic.Multiplayer.csproj'
& dotnet build $mpProject -c Release "-p:OutputPath=$ArtifactsDir/multiplayer/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/multiplayer/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:GameDir=$GameDir" "-p:HarmonyDir=$HarmonyDir" "-p:JecsDir=$JecsDir" '-p:NuGetAudit=false' 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'multiplayer-build.log')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$env:RWOM_MULTIPLAYER_ADAPTER = Join-Path $ArtifactsDir 'multiplayer/out/TorannMagic.Multiplayer.dll'
# A missing VGE install remains a supported core-only test environment.
$env:RWOM_VGE_ADAPTER = ''
if (Test-Path (Join-Path $VgeDir 'VanillaGravshipExpanded.dll')) {
    $adapter = Join-Path $repo 'TorannMagic.Integrations/Source/TorannMagic.VGE/TorannMagic.VGE.csproj'
    & dotnet build $adapter -c Release "-p:OutputPath=$ArtifactsDir/vge/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/vge/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:GameDir=$GameDir" "-p:VgeDir=$VgeDir" 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'vge-build.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $env:RWOM_VGE_ADAPTER = Join-Path $ArtifactsDir 'vge/out/TorannMagic.VGE.dll'
}
# A missing Ability AI install remains a supported test environment: the compat
# assembly is optional and its absence is a tested state, not a failure.
$env:RWOM_ABILITYAI_ADAPTER = ''
if (!$AbilityAIDir) { $AbilityAIDir = Join-Path $repo '../ext/AbilityAI' }
if (Test-Path (Join-Path $AbilityAIDir 'AbilityAI.dll')) {
    $adapter = Join-Path $repo 'TorannMagic.Integrations/Source/TorannMagic.AbilityAI/TorannMagic.AbilityAI.csproj'
    & dotnet build $adapter -c Release "-p:OutputPath=$ArtifactsDir/abilityai/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/abilityai/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:CoreAssemblyPath=$coreForBuild" "-p:GameDir=$GameDir" "-p:HarmonyDir=$HarmonyDir" "-p:JecsDir=$JecsDir" "-p:AbilityAIDir=$AbilityAIDir" 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'abilityai-build.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $env:RWOM_ABILITYAI_ADAPTER = Join-Path $ArtifactsDir 'abilityai/out/TorannMagic.AbilityAI.dll'
}
# Character Editor adapter: built when the CE workshop assembly is present.
$env:RWOM_CECOMPAT_ADAPTER = ''
if (Test-Path (Join-Path $CEDir 'CharacterEditor.dll')) {
    $ceAdapter = Join-Path $repo 'TorannMagic.Integrations/Source/TorannMagic.CharacterEditor/TorannMagic.CharacterEditor.csproj'
    & dotnet build $ceAdapter -c Release "-p:OutputPath=$ArtifactsDir/cecompat/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/cecompat/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:GameDir=$GameDir" "-p:HarmonyDir=$HarmonyDir" "-p:JecsDir=$JecsDir" 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'cecompat-build.log')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $env:RWOM_CECOMPAT_ADAPTER = Join-Path $ArtifactsDir 'cecompat/out/TorannMagic.CharacterEditor.dll'
}

$harness = Join-Path $PSScriptRoot 'RWoM.RegressionTests/RWoM.RegressionTests.csproj'
if ($StagedRelease) {
    # Build-time checks above still compile source, but runtime acceptance must use
    # exactly the files intended for installation, not those fresh adapter outputs.
    $env:RWOM_INTEGRATIONS_ASSEMBLY = Join-Path $StagedRelease 'Integrations/v1.7/Assemblies/TorannMagic.Integrations.dll'
    $env:RWOM_MULTIPLAYER_ADAPTER = Join-Path $StagedRelease 'Integrations/v1.7/MultiplayerCompat/Assemblies/TorannMagic.Multiplayer.dll'
    $env:RWOM_VGE_ADAPTER = Join-Path $StagedRelease 'Integrations/v1.7/VGECompat/Assemblies/TorannMagic.VGE.dll'
    # Optional adapter: older staged releases predate it and stay valid.
    $stagedAbilityAI = Join-Path $StagedRelease 'Integrations/v1.7/AbilityAICompat/Assemblies/TorannMagic.AbilityAI.dll'
    $env:RWOM_ABILITYAI_ADAPTER = if (Test-Path -LiteralPath $stagedAbilityAI) { $stagedAbilityAI } else { '' }
    $stagedCE = Join-Path $StagedRelease 'Integrations/v1.7/CECompat/Assemblies/TorannMagic.CharacterEditor.dll'
    $env:RWOM_CECOMPAT_ADAPTER = if (Test-Path -LiteralPath $stagedCE) { $stagedCE } else { '' }
    foreach ($assembly in @($env:RWOM_INTEGRATIONS_ASSEMBLY,$env:RWOM_MULTIPLAYER_ADAPTER,$env:RWOM_VGE_ADAPTER)) {
        if (!(Test-Path -LiteralPath $assembly)) { throw "Staged assembly missing: $assembly" }
    }
    $env:RWOM_STAGED_RELEASE = $StagedRelease
}
& dotnet build $harness -c Release "-p:OutputPath=$ArtifactsDir/harness/out/" "-p:BaseIntermediateOutputPath=$ArtifactsDir/harness/obj/" "-p:ModAssemblyPath=$ModAssembly" "-p:GameDir=$GameDir" "-p:JecsDir=$JecsDir" 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'harness-build.log')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testArgs = @('--repo', $repo, '--game', $GameDir, '--jecs', $JecsDir, '--mod', $ModAssembly, '--work', (Join-Path $ArtifactsDir 'cases'), '--refs', $refs, '--harmony', $HarmonyDir)
if ($Filter) { $testArgs += @('--filter', $Filter) }
if ($Reverse) { $testArgs += '--reverse' }
# Tests install and remove scoped Harmony patches after earlier cases have warmed
# native methods. Inlining can retain an unpatched copy in a caller and make the
# same case fail only late in a run. Keep interception stable in this child process;
# do not change the game's runtime, production build, or the caller's environment.
$previousNoInline = $env:COMPlus_JitNoInline
$previousTiering = $env:COMPlus_TieredCompilation
$previousCore = $env:RWOM_TEST_CORE_ASSEMBLY
try {
    $env:RWOM_TEST_CORE_ASSEMBLY = $coreForTests
    $env:COMPlus_JitNoInline = '1'
    # On .NET 10 a hot method can acquire a new native entry point while a
    # short-lived test patch is installed. Keep the harness on one code version.
    $env:COMPlus_TieredCompilation = '0'
    Write-Host 'Harness runtime: COMPlus_JitNoInline=1, COMPlus_TieredCompilation=0 (scoped Harmony interception)'
    & dotnet (Join-Path $ArtifactsDir 'harness/out/RWoM.RegressionTests.dll') @testArgs 2>&1 | Tee-Object -FilePath (Join-Path $ArtifactsDir 'test-results.log')
    $testExit = $LASTEXITCODE
    if ($StagedRelease -and $testExit -eq 0) {
        @{stage=$StagedRelease;manifestHash=(Get-FileHash -LiteralPath (Join-Path $StagedRelease 'manifest.json')).Hash;filter=$Filter;exitCode=$testExit;log=(Join-Path $ArtifactsDir 'test-results.log');utc=[DateTime]::UtcNow.ToString('o')} |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $ArtifactsDir 'staged-acceptance.json') -Encoding utf8
    }
}
finally {
    $env:COMPlus_JitNoInline = $previousNoInline
    $env:COMPlus_TieredCompilation = $previousTiering
    $env:RWOM_TEST_CORE_ASSEMBLY = $previousCore
}
Write-Host "Regression exit code: $testExit; logs and Scribe XML: $ArtifactsDir"
exit $testExit
