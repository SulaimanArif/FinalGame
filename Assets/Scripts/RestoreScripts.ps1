$markdownFile = "AllScriptsCompiled.md"
$content = Get-Content $markdownFile -Raw

$scripts = @{}
$currentScript = $null
$currentCode = @()
$inCodeBlock = $false

$lines = $content -split "`r?`n"

for ($i = 0; $i -lt $lines.Length; $i++) {
    $line = $lines[$i]
    
    # Check for script header (e.g., # ScriptName.cs or ## ScriptName.cs)
    if ($line -match "^#+\s+(.+\.cs)$") {
        # Save previous script if exists
        if ($currentScript -and $currentCode.Count -gt 0) {
            $code = ($currentCode -join "`n").Trim()
            if ($code.Length -gt 0) {
                $scripts[$currentScript] = $code
            }
        }
        # Start new script
        $currentScript = $matches[1]
        $currentCode = @()
        $inCodeBlock = $false
    }
    # Check for code block start
    elseif ($line -match "^```csharp" -or ($line -match "^```" -and -not $inCodeBlock)) {
        $inCodeBlock = $true
    }
    # Check for code block end
    elseif ($line -match "^```" -and $inCodeBlock) {
        $inCodeBlock = $false
    }
    # Collect code lines
    elseif ($inCodeBlock -and $currentScript) {
        $currentCode += $line
    }
}

# Save last script
if ($currentScript -and $currentCode.Count -gt 0) {
    $code = ($currentCode -join "`n").Trim()
    if ($code.Length -gt 0) {
        $scripts[$currentScript] = $code
    }
}

Write-Host "Found $($scripts.Count) scripts to restore"

# Restore each script
foreach ($scriptName in $scripts.Keys) {
    $scriptPath = $null
    
    # Try to find the file in the directory structure
    $found = Get-ChildItem -Recurse -Filter $scriptName -File | Select-Object -First 1
    if ($found) {
        $scriptPath = $found.FullName
        Write-Host "Restoring: $scriptPath"
        $scripts[$scriptName] | Set-Content -Path $scriptPath -Encoding UTF8
    } else {
        Write-Host "WARNING: Could not find file for $scriptName"
    }
}

Write-Host "Restoration complete!"

