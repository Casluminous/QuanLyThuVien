param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$extensions = @('.cs', '.csproj', '.sln', '.slnx', '.config', '.sql', '.md', '.json', '.xml', '.resx', '.props', '.targets', '.editorconfig', '.gitattributes', '.gitignore', '.ps1')
$excludedDirectories = @('.git', '.vs', 'bin', 'obj', 'backup-before-button2', 'backup-topnav-20260714')

$utf8Strict = [System.Text.UTF8Encoding]::new($true, $true)
$errors = [System.Collections.Generic.List[string]]::new()

$files = Get-ChildItem -LiteralPath $Root -Recurse -File | Where-Object {
    $extension = [System.IO.Path]::GetExtension($_.Name).ToLowerInvariant()
    $isSpecialFile = $_.Name -in @('.editorconfig', '.gitattributes', '.gitignore')
    $isSupported = $isSpecialFile -or $extensions -contains $extension
    $relativePath = $_.FullName.Substring($Root.Length).TrimStart('\', '/')
    $parts = $relativePath -split '[\\/]'
    $isExcluded = $parts | Where-Object { $excludedDirectories -contains $_ }
    $isSupported -and -not $isExcluded
}

# mojibake pattern using \uXXXX escapes understood by .NET regex (not PS5)
$mojibakePattern = [regex]('\uFFFD|\u00C3.|\u00C2.|\u00C6.|\u00C4.|\u00E1\u00BB|\u00E1\u00BA|\u00F0\u0178')

foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)

    $relativePath = $file.FullName.Substring($Root.Length).TrimStart('\', '/')

    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        $errors.Add("Missing UTF-8 BOM: $relativePath")
        continue
    }

    try {
        $text = $utf8Strict.GetString($bytes, 3, $bytes.Length - 3)
    }
    catch {
        $errors.Add("Invalid UTF-8: $relativePath")
        continue
    }

    $match = $mojibakePattern.Match($text)
    if ($match.Success) {
        $line = ($text.Substring(0, $match.Index) -split "`n").Count
        $errors.Add("Possible mojibake: ${relativePath}:${line} [$($match.Value)]")
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Output "Encoding check passed for $($files.Count) files."
