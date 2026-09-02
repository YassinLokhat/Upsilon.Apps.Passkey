#Requires -Version 5.1
<#
.SYNOPSIS
  Sync package versions from versions.json and/or publish all shippable packages locally.

.DESCRIPTION
  versions.json is the single source of truth for component versions and dependency
  ranges. This script:

  - Writes Version / AssemblyVersion / FileVersion into each .csproj
  - Refreshes version tables / overview snippets in docs (marked regions)
  - Packs libraries and publishes the WPF client into ./_artifacts (replaces publish.bat)
  - Can run in -Check mode for CI (fail if csproj/docs drift from the JSON)

.PARAMETER Check
  Verify csproj + docs match versions.json and that dependency ranges hold. Exit 1 on drift.

.PARAMETER SyncOnly
  Apply versions.json to csproj + docs, then exit (no publish).

.PARAMETER PublishOnly
  Publish without syncing (assumes csproj already match the JSON).

.PARAMETER Package
  Optional package key (interfaces, utils, core, wpf). When set, only that package is published.

.PARAMETER ArtifactsDir
  Output directory for nupkg / zip / checksums / notes (default: _artifacts).

.EXAMPLE
  .\scripts\Sync-Versions.ps1
  Sync versions.json into the tree, then publish every shippable package locally.

.EXAMPLE
  .\scripts\Sync-Versions.ps1 -Check
  CI gate: fail if the tree drifted from versions.json.

.EXAMPLE
  .\scripts\Sync-Versions.ps1 -SyncOnly
  After editing versions.json, update csproj and docs only.
#>
[CmdletBinding()]
param(
   [switch]$Check,
   [switch]$SyncOnly,
   [switch]$PublishOnly,
   [string]$Package,
   [string]$ArtifactsDir = "_artifacts"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ManifestPath = Join-Path $RepoRoot "versions.json"

function Get-Manifest {
   if (-not (Test-Path -LiteralPath $ManifestPath)) {
      throw "versions.json not found at '$ManifestPath'."
   }

   return (Get-Content -LiteralPath $ManifestPath -Raw -Encoding utf8 | ConvertFrom-Json)
}

function Get-NumericVersion([string]$Version) {
   if ($Version -notmatch '^(\d+\.\d+\.\d+)') {
      throw "Cannot read a numeric X.Y.Z version from '$Version'."
   }

   return $Matches[1]
}

function Get-SupportLine([string]$Version) {
   $numeric = Get-NumericVersion $Version
   $parts = $numeric.Split('.')
   return "$($parts[0]).$($parts[1]).x"
}

function ConvertTo-VersionObject([string]$Version) {
   $numeric = Get-NumericVersion $Version
   $parts = $numeric.Split('.') | ForEach-Object { [int]$_ }
   return [pscustomobject]@{
      Major = $parts[0]
      Minor = $parts[1]
      Patch = $parts[2]
      Original = $Version
   }
}

function Test-VersionInRange([string]$Version, [string]$Range) {
   if ($Range -notmatch '^\[([^,\s]+)\s*,\s*([^\s\]\)]+)(\]|\))$') {
      throw "Unsupported version range '$Range'. Use NuGet syntax such as [1.0.0,2.0.0)."
   }

   $minText = $Matches[1]
   $maxText = $Matches[2]
   $maxInclusive = $Matches[3] -eq ']'

   $value = ConvertTo-VersionObject $Version
   $min = ConvertTo-VersionObject $minText
   $max = ConvertTo-VersionObject $maxText

   function Compare-VersionParts($Left, $Right) {
      if ($Left.Major -ne $Right.Major) { return $Left.Major - $Right.Major }
      if ($Left.Minor -ne $Right.Minor) { return $Left.Minor - $Right.Minor }
      return $Left.Patch - $Right.Patch
   }

   if ((Compare-VersionParts $value $min) -lt 0) {
      return $false
   }

   $vsMax = Compare-VersionParts $value $max
   if ($maxInclusive) {
      return $vsMax -le 0
   }

   return $vsMax -lt 0
}

function Format-RangeHuman([string]$Range) {
   if ($Range -notmatch '^\[([^,\s]+)\s*,\s*([^\s\]\)]+)(\]|\))$') {
      return $Range
   }

   $min = $Matches[1]
   $max = $Matches[2]
   $maxInclusive = $Matches[3] -eq ']'
   $upper = if ($maxInclusive) { "<= $max" } else { "< $max" }
   return ">= $min and $upper"
}

function Get-CsprojVersions([string]$ProjectPath) {
   [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw -Encoding utf8
   $pg = $xml.Project.PropertyGroup | Select-Object -First 1
   return [pscustomobject]@{
      Version = [string]$pg.Version
      AssemblyVersion = [string]$pg.AssemblyVersion
      FileVersion = [string]$pg.FileVersion
   }
}

function Set-CsprojVersions([string]$ProjectPath, [string]$Version) {
   $numeric = Get-NumericVersion $Version
   $content = Get-Content -LiteralPath $ProjectPath -Raw -Encoding utf8

   if ($content -notmatch '<Version>') {
      throw "No <Version> element in '$ProjectPath'."
   }

   $content = [regex]::Replace($content, '<Version>[^<]*</Version>', "<Version>$Version</Version>")
   $content = [regex]::Replace($content, '<AssemblyVersion>[^<]*</AssemblyVersion>', "<AssemblyVersion>$numeric</AssemblyVersion>")
   $content = [regex]::Replace($content, '<FileVersion>[^<]*</FileVersion>', "<FileVersion>$numeric</FileVersion>")
   Write-Utf8File -Path $ProjectPath -Content $content
}

function Update-MarkedRegion([string]$Path, [string]$Name, [string]$InnerContent) {
   if (-not (Test-Path -LiteralPath $Path)) {
      throw "Missing doc file '$Path'."
   }

   $text = Get-Content -LiteralPath $Path -Raw -Encoding utf8
   $begin = "<!-- BEGIN:$Name -->"
   $end = "<!-- END:$Name -->"
   if ($text -notlike "*$begin*") {
      throw "Marker '$begin' not found in '$Path'."
   }

   $pattern = [regex]::Escape($begin) + ".*?" + [regex]::Escape($end)
   if ($InnerContent -match "[\r\n]") {
      $replacement = $begin + "`n" + $InnerContent.TrimEnd() + "`n" + $end
   }
   else {
      $replacement = $begin + $InnerContent.Trim() + $end
   }
   $updated = [regex]::Replace($text, $pattern, { param($m) $replacement }, [System.Text.RegularExpressions.RegexOptions]::Singleline)
   Write-Utf8File -Path $Path -Content $updated
}

function Get-MarkedRegion([string]$Path, [string]$Name) {
   $text = Get-Content -LiteralPath $Path -Raw -Encoding utf8
   $begin = "<!-- BEGIN:$Name -->"
   $end = "<!-- END:$Name -->"
   $pattern = [regex]::Escape($begin) + "\r?\n?(.*?)\r?\n?" + [regex]::Escape($end)
   $match = [regex]::Match($text, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
   if (-not $match.Success) {
      throw "Marker '$begin' not found in '$Path'."
   }

   return $match.Groups[1].Value.TrimEnd()
}

function Get-PackageSortRank([string]$Key) {
   switch ($Key) {
      "wpf" { return 0 }
      "core" { return 1 }
      "utils" { return 2 }
      "interfaces" { return 3 }
      default { return 100 }
   }
}

function Get-ShippedPackages($Manifest) {
   return @($Manifest.packages.PSObject.Properties | ForEach-Object {
         $key = $_.Name
         $pkg = $_.Value
         if ($pkg.ship) {
            [pscustomobject]@{ Key = $key; Package = $pkg; Rank = (Get-PackageSortRank $key) }
         }
      } | Sort-Object Rank, Key)
}

function Normalize-Text([string]$Value) {
   return ($Value -replace "`r`n", "`n" -replace "`r", "`n").Trim()
}

function Write-Utf8File([string]$Path, [string]$Content) {
   $utf8 = New-Object System.Text.UTF8Encoding $false
   if (-not $Content.EndsWith("`n")) {
      $Content += "`n"
   }

   [System.IO.File]::WriteAllText($Path, $Content.Replace("`r`n", "`n").Replace("`n", "`r`n"), $utf8)
}

function Get-OverviewVersion($Manifest) {
   $key = [string]$Manifest.overviewPackage
   $pkg = $Manifest.packages.$key
   if ($null -eq $pkg) {
      throw "overviewPackage '$key' is not defined in versions.json."
   }

   return [string]$pkg.version
}

function Build-SecuritySummary($Manifest) {
   $shipped = Get-ShippedPackages $Manifest
   $groups = $shipped | Group-Object { Get-SupportLine $_.Package.version }
   $parts = foreach ($g in ($groups | Sort-Object Name)) {
      $ids = @($g.Group | Sort-Object Rank, Key | ForEach-Object { "``$($_.Package.id)``" })
      if ($ids.Count -eq 1) {
         "$($ids[0]) is on **$($g.Name)**"
      }
      else {
         $last = $ids[-1]
         $head = $ids[0..($ids.Count - 2)] -join ", "
         "$head and $last are on **$($g.Name)**"
      }
   }

   return "At the time of writing, $($parts -join '; '). Each assembly is versioned independently and may diverge."
}

function Build-SecurityTableMarkdown($Manifest, [string]$Style) {
   $shipped = Get-ShippedPackages $Manifest
   $lines = New-Object System.Collections.Generic.List[string]

   if ($Style -eq "security-md") {
      $lines.Add("| Component (assembly)                  | Supported version | Supported          |")
      $lines.Add("| ------------------------------------- | ----------------- | ------------------ |")
      foreach ($entry in $shipped) {
         $id = $entry.Package.id
         $label = if ($entry.Key -eq "wpf") { "``$id`` (app)" } else { "``$id``" }
         $line = Get-SupportLine $entry.Package.version
         $lines.Add("| $($label.PadRight(37)) | $($line.PadRight(17)) | :white_check_mark: |")
      }
   }
   else {
      $lines.Add("| Component (assembly) | Supported version | Supported |")
      $lines.Add("| -------------------- | ----------------- | --------- |")
      foreach ($entry in $shipped) {
         $lines.Add("| ``$($entry.Package.id)`` | $(Get-SupportLine $entry.Package.version) | Yes |")
      }
   }

   return ($lines -join "`n")
}

function Build-HomeVersionsBlurb($Manifest) {
   return "$(Build-SecuritySummary $Manifest) Always upgrade to the latest release of a component before reporting an issue."
}

function Sync-Documentation($Manifest) {
   $overview = Get-OverviewVersion $Manifest
   $summary = Build-SecuritySummary $Manifest

   Update-MarkedRegion (Join-Path $RepoRoot "README.md") "versions-overview" "**$overview**"
   Update-MarkedRegion (Join-Path $RepoRoot "SECURITY.md") "versions-summary" $summary
   Update-MarkedRegion (Join-Path $RepoRoot "SECURITY.md") "versions-supported-table" (Build-SecurityTableMarkdown $Manifest "security-md")
   Update-MarkedRegion (Join-Path $RepoRoot "Wiki/Home.md") "versions-summary" (Build-HomeVersionsBlurb $Manifest)
   Update-MarkedRegion (Join-Path $RepoRoot "Wiki/Security.md") "versions-summary" $summary
   Update-MarkedRegion (Join-Path $RepoRoot "Wiki/Security.md") "versions-supported-table" (Build-SecurityTableMarkdown $Manifest "wiki")
}

function Sync-Projects($Manifest) {
   foreach ($prop in $Manifest.packages.PSObject.Properties) {
      $pkg = $prop.Value
      $projectPath = Join-Path $RepoRoot $pkg.project
      if (-not (Test-Path -LiteralPath $projectPath)) {
         throw "Project '$($pkg.project)' for package '$($prop.Name)' was not found."
      }

      Set-CsprojVersions -ProjectPath $projectPath -Version ([string]$pkg.version)
      Write-Host "Synced $($prop.Name) -> $([string]$pkg.version) ($($pkg.project))"
   }

   Sync-Documentation $Manifest
   Write-Host "Synced documentation markers from versions.json."
}

function Test-DependencyGraph($Manifest) {
   $errors = New-Object System.Collections.Generic.List[string]

   foreach ($prop in $Manifest.packages.PSObject.Properties) {
      $pkg = $prop.Value
      if ($null -eq $pkg.dependencies) {
         continue
      }

      foreach ($dep in $pkg.dependencies.PSObject.Properties) {
         $depKey = $dep.Name
         $range = [string]$dep.Value
         $depPkg = $Manifest.packages.$depKey
         if ($null -eq $depPkg) {
            $errors.Add("Package '$($prop.Name)' depends on unknown key '$depKey'.")
            continue
         }

         $builtAgainst = [string]$depPkg.version
         if (-not (Test-VersionInRange -Version $builtAgainst -Range $range)) {
            $errors.Add("Package '$($prop.Name)' requires $depKey $range but versions.json has $depKey=$builtAgainst.")
         }
      }
   }

   return @($errors.ToArray())
}

function Test-TreeMatchesManifest($Manifest) {
   $errors = New-Object System.Collections.Generic.List[string]
   foreach ($e in (Test-DependencyGraph $Manifest)) {
      $errors.Add($e)
   }

   foreach ($prop in $Manifest.packages.PSObject.Properties) {
      $pkg = $prop.Value
      $projectPath = Join-Path $RepoRoot $pkg.project
      if (-not (Test-Path -LiteralPath $projectPath)) {
         $errors.Add("Missing project '$($pkg.project)' for '$($prop.Name)'.")
         continue
      }

      $current = Get-CsprojVersions $projectPath
      $expectedNumeric = Get-NumericVersion ([string]$pkg.version)
      if ($current.Version -ne [string]$pkg.version) {
         $errors.Add("$($pkg.project): Version is '$($current.Version)' but versions.json has '$([string]$pkg.version)'.")
      }
      if ($current.AssemblyVersion -ne $expectedNumeric) {
         $errors.Add("$($pkg.project): AssemblyVersion is '$($current.AssemblyVersion)' but expected '$expectedNumeric'.")
      }
      if ($current.FileVersion -ne $expectedNumeric) {
         $errors.Add("$($pkg.project): FileVersion is '$($current.FileVersion)' but expected '$expectedNumeric'.")
      }
   }

   $overview = Get-OverviewVersion $Manifest
   $expectedOverview = Normalize-Text "**$overview**"
   $actualOverview = Normalize-Text (Get-MarkedRegion (Join-Path $RepoRoot "README.md") "versions-overview")
   if ($actualOverview -ne $expectedOverview) {
      $errors.Add("README.md versions-overview is '$actualOverview' but expected '$expectedOverview'.")
   }

   $expectedSummary = Normalize-Text (Build-SecuritySummary $Manifest)
   foreach ($pair in @(
         @{ Path = "SECURITY.md"; Name = "versions-summary"; Expected = $expectedSummary },
         @{ Path = "Wiki/Security.md"; Name = "versions-summary"; Expected = $expectedSummary },
         @{ Path = "Wiki/Home.md"; Name = "versions-summary"; Expected = (Normalize-Text (Build-HomeVersionsBlurb $Manifest)) }
      )) {
      $actual = Normalize-Text (Get-MarkedRegion (Join-Path $RepoRoot $pair.Path) $pair.Name)
      $expected = Normalize-Text $pair.Expected
      if ($actual -ne $expected) {
         $errors.Add("$($pair.Path) $($pair.Name) is out of sync with versions.json. Run .\scripts\Sync-Versions.ps1 -SyncOnly.")
      }
   }

   $expectedSecurityTable = Normalize-Text (Build-SecurityTableMarkdown $Manifest "security-md")
   $actualSecurityTable = Normalize-Text (Get-MarkedRegion (Join-Path $RepoRoot "SECURITY.md") "versions-supported-table")
   if ($actualSecurityTable -ne $expectedSecurityTable) {
      $errors.Add("SECURITY.md versions-supported-table is out of sync with versions.json.")
   }

   $expectedWikiTable = Normalize-Text (Build-SecurityTableMarkdown $Manifest "wiki")
   $actualWikiTable = Normalize-Text (Get-MarkedRegion (Join-Path $RepoRoot "Wiki/Security.md") "versions-supported-table")
   if ($actualWikiTable -ne $expectedWikiTable) {
      $errors.Add("Wiki/Security.md versions-supported-table is out of sync with versions.json.")
   }

   return @($errors.ToArray())
}

function Resolve-AssetStem($Package, [string]$Version) {
   $stem = [string]$Package.assetStem
   if ([string]::IsNullOrWhiteSpace($stem)) {
      throw "Package '$($Package.id)' has kind '$($Package.kind)' but no assetStem."
   }

   return $stem.Replace("{version}", $Version)
}

function New-Sha256Sidecar([string]$FilePath) {
   $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $FilePath).Hash.ToLowerInvariant()
   $name = Split-Path -Leaf $FilePath
   $sidecar = "$FilePath.sha256"
   Set-Content -LiteralPath $sidecar -Value "$hash  $name" -Encoding ascii
   return $hash
}

function Build-ReleaseNotes($Manifest, [string]$PackageKey, [string]$Hash, [string]$AssetName) {
   $pkg = $Manifest.packages.$PackageKey
   $lines = New-Object System.Collections.Generic.List[string]
   $lines.Add("## $($pkg.id) $([string]$pkg.version)")
   $lines.Add("")

   if ($pkg.kind -eq "app") {
      $lines.Add("Self-contained Windows x64 client. The .NET 10 runtime is bundled; Windows 10 (1809 / build 18362) or later is required. No SDK install is needed.")
      $lines.Add("")
   }

   $lines.Add("### Dependencies")
   $depProps = @($pkg.dependencies.PSObject.Properties)
   if ($depProps.Count -eq 0) {
      $lines.Add("")
      $lines.Add("_None._")
   }
   else {
      $lines.Add("")
      $lines.Add("| Package | Required range | Built against |")
      $lines.Add("| ------- | -------------- | ------------- |")
      foreach ($dep in $depProps) {
         $depPkg = $Manifest.packages.($dep.Name)
         $lines.Add("| ``$($depPkg.id)`` | $(Format-RangeHuman ([string]$dep.Value)) | ``$([string]$depPkg.version)`` |")
      }
   }

   $lines.Add("")
   $lines.Add("### Assets")
   $lines.Add("")
   $lines.Add("- ``$AssetName``")
   if (-not [string]::IsNullOrWhiteSpace($Hash)) {
      $lines.Add("- SHA-256: ``$Hash``")
   }

   return ($lines -join "`n")
}

function Publish-LibraryPackage($Manifest, [string]$PackageKey, [string]$OutDir) {
   $pkg = $Manifest.packages.$PackageKey
   $projectPath = Join-Path $RepoRoot $pkg.project
   $version = [string]$pkg.version
   $numeric = Get-NumericVersion $version

   Write-Host "Packing $($pkg.id) $version ..."
   & dotnet pack $projectPath `
      --configuration Release `
      "-p:PackageVersion=$version" `
      "-p:Version=$version" `
      "-p:AssemblyVersion=$numeric" `
      "-p:FileVersion=$numeric" `
      -o $OutDir
   if ($LASTEXITCODE -ne 0) {
      throw "dotnet pack failed for $($pkg.id)."
   }

   $nupkg = Get-ChildItem -LiteralPath $OutDir -Filter "$($pkg.id).*.nupkg" |
      Where-Object { $_.Name -like "$($pkg.id).$version.nupkg" -or $_.Name -like "$($pkg.id).$version-*.nupkg" } |
      Select-Object -First 1
   if ($null -eq $nupkg) {
      $nupkg = Get-ChildItem -LiteralPath $OutDir -Filter "*.nupkg" |
         Where-Object { $_.Name.StartsWith("$($pkg.id).") } |
         Sort-Object LastWriteTime -Descending |
         Select-Object -First 1
   }
   if ($null -eq $nupkg) {
      throw "No nupkg produced for $($pkg.id)."
   }

   $hash = New-Sha256Sidecar $nupkg.FullName
   $notesPath = Join-Path $OutDir "$($pkg.id)-$version-release-notes.md"
   Write-Utf8File -Path $notesPath -Content (Build-ReleaseNotes $Manifest $PackageKey $hash $nupkg.Name)
   Write-Host "  -> $($nupkg.FullName)"
}

function Publish-WpfPackage($Manifest, [string]$PackageKey, [string]$OutDir) {
   $pkg = $Manifest.packages.$PackageKey
   $projectPath = Join-Path $RepoRoot $pkg.project
   $version = [string]$pkg.version
   $numeric = Get-NumericVersion $version

   Write-Host "Publishing $($pkg.id) $version ..."
   & dotnet publish $projectPath `
      --configuration Release `
      "-p:PublishProfile=FolderProfile" `
      "-p:Version=$version" `
      "-p:AssemblyVersion=$numeric" `
      "-p:FileVersion=$numeric" `
      "-p:InformationalVersion=$version"
   if ($LASTEXITCODE -ne 0) {
      throw "dotnet publish failed for $($pkg.id)."
   }

   $publishDir = Join-Path $RepoRoot ([string]$pkg.publishDir)
   if (-not (Test-Path -LiteralPath $publishDir)) {
      throw "Publish output was not found at '$publishDir'."
   }

   Remove-Item -Path (Join-Path $publishDir "raw") -Recurse -Force -ErrorAction SilentlyContinue
   Get-ChildItem -Path $publishDir -Filter *.pdb -File -ErrorAction SilentlyContinue | Remove-Item -Force

   $exe = Get-ChildItem -Path $publishDir -Filter *.exe -File | Select-Object -First 1
   if ($null -eq $exe) {
      throw "No executable was produced in '$publishDir'."
   }

   $stem = Resolve-AssetStem $pkg $version
   $zipName = "$stem.zip"
   $zipPath = Join-Path $OutDir $zipName
   if (Test-Path -LiteralPath $zipPath) {
      Remove-Item -LiteralPath $zipPath -Force
   }

   $staging = Join-Path ([System.IO.Path]::GetTempPath()) ("passkey-publish-" + [guid]::NewGuid().ToString("n"))
   New-Item -ItemType Directory -Path $staging | Out-Null
   try {
      Copy-Item -Path (Join-Path $publishDir "*") -Destination $staging -Recurse
      Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -CompressionLevel Optimal
   }
   finally {
      Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
   }

   $hash = New-Sha256Sidecar $zipPath
   $notesPath = Join-Path $OutDir "$($pkg.id)-$version-release-notes.md"
   Write-Utf8File -Path $notesPath -Content (Build-ReleaseNotes $Manifest $PackageKey $hash $zipName)
   Write-Host "  -> $zipPath"
}

function Publish-Packages($Manifest, [string]$OutDir, [string]$OnlyKey) {
   New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

   $targets = Get-ShippedPackages $Manifest
   if (-not [string]::IsNullOrWhiteSpace($OnlyKey)) {
      $targets = @($targets | Where-Object { $_.Key -eq $OnlyKey })
      if ($targets.Count -eq 0) {
         throw "Package '$OnlyKey' is not a shippable entry in versions.json."
      }
   }

   foreach ($entry in $targets) {
      switch ([string]$entry.Package.kind) {
         "library" { Publish-LibraryPackage $Manifest $entry.Key $OutDir }
         "app" { Publish-WpfPackage $Manifest $entry.Key $OutDir }
         default { throw "Unknown kind '$($entry.Package.kind)' for '$($entry.Key)'." }
      }
   }
}

# --- entry point ---

if (($Check.IsPresent -and ($SyncOnly.IsPresent -or $PublishOnly.IsPresent)) -or
   ($SyncOnly.IsPresent -and $PublishOnly.IsPresent)) {
   throw "Use only one of -Check, -SyncOnly, or -PublishOnly (or none for sync+publish)."
}

Push-Location $RepoRoot
try {
   $manifest = Get-Manifest

   if ($Check) {
      $errors = @(Test-TreeMatchesManifest $manifest)
      if ($errors.Count -gt 0) {
         Write-Host "versions.json check FAILED:" -ForegroundColor Red
         foreach ($e in $errors) {
            Write-Host " - $e" -ForegroundColor Red
         }
         exit 1
      }

      Write-Host "versions.json check passed."
      exit 0
   }

   if (-not $PublishOnly) {
      Sync-Projects $manifest
   }
   else {
      $errors = @(Test-TreeMatchesManifest $manifest)
      if ($errors.Count -gt 0) {
         Write-Host "Refusing -PublishOnly while the tree is out of sync:" -ForegroundColor Red
         foreach ($e in $errors) {
            Write-Host " - $e" -ForegroundColor Red
         }
         exit 1
      }
   }

   if (-not $SyncOnly) {
      $outDir = if ([System.IO.Path]::IsPathRooted($ArtifactsDir)) {
         $ArtifactsDir
      }
      else {
         Join-Path $RepoRoot $ArtifactsDir
      }

      Publish-Packages -Manifest $manifest -OutDir $outDir -OnlyKey $Package
      Write-Host "Publish complete. Artifacts: $outDir"
   }
}
finally {
   Pop-Location
}
