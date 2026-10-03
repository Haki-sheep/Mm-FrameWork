[CmdletBinding()]
param([string]$SkillRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$SkillRoot = [IO.Path]::GetFullPath($SkillRoot)
$ManifestPath = Join-Path $SkillRoot 'runtime/build-manifest.json'
$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if (-not $Manifest.payload -or -not $Manifest.runtimes) { throw 'This package has no complete payload manifest.' }
$Prefix = $SkillRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$Expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($Entry in $Manifest.payload) {
    $Target = [IO.Path]::GetFullPath((Join-Path $SkillRoot $Entry.path))
    if (-not $Target.StartsWith($Prefix, [StringComparison]::OrdinalIgnoreCase) -or -not $Expected.Add($Entry.path)) {
        throw "Unsafe or duplicate manifest path: $($Entry.path)"
    }
    if (-not (Test-Path -LiteralPath $Target -PathType Leaf) -or
        (Get-Item -LiteralPath $Target).Length -ne $Entry.size_bytes -or
        (Get-FileHash -LiteralPath $Target -Algorithm SHA256).Hash.ToLowerInvariant() -cne $Entry.sha256) {
        throw "Payload missing or changed: $($Entry.path)"
    }
}
foreach ($File in Get-ChildItem -LiteralPath $SkillRoot -File -Recurse) {
    $Relative = [IO.Path]::GetRelativePath($SkillRoot, $File.FullName).Replace([IO.Path]::DirectorySeparatorChar, '/')
    if ($Relative -ne 'runtime/build-manifest.json' -and -not $Expected.Contains($Relative)) {
        throw "Unmanifested payload file: $Relative"
    }
}
foreach ($Runtime in $Manifest.runtimes) {
    $Entry = @($Manifest.payload | Where-Object path -EQ ('runtime/' + $Runtime.path_from_runtime_root))
    if ($Entry.Count -ne 1 -or $Entry[0].sha256 -cne $Runtime.sha256) { throw "Runtime identity mismatch: $($Runtime.platform)" }
}
[pscustomobject]@{ verified = $true; release_channel = $Manifest.release_channel; source_commit = $Manifest.source_commit;
    platforms = @($Manifest.runtimes.platform); payload_files = $Expected.Count }
