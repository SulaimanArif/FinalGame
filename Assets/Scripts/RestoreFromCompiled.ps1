$compiledFile = "AllScriptsCompiled.md"
$content = Get-Content $compiledFile -Raw

# Pattern to match script sections: ### ScriptName.cs: followed by ```csharp ... ```
$pattern = '(?s)###\s+([^:]+\.cs):\s*```csharp\s*(.*?)```'

$matches = [regex]::Matches($content, $pattern)

Write-Host "Found $($matches.Count) scripts to restore"

foreach ($match in $matches) {
    $scriptName = $match.Groups[1].Value.Trim()
    $scriptContent = $match.Groups[2].Value.Trim()
    
    # Find the file path
    $scriptPath = Get-ChildItem -Recurse -Filter $scriptName | Select-Object -First 1
    
    if ($scriptPath) {
        Write-Host "Restoring: $($scriptPath.FullName)"
        # Write the content back to the file
        [System.IO.File]::WriteAllText($scriptPath.FullName, $scriptContent)
    } else {
        Write-Host "WARNING: Could not find file for $scriptName"
    }
}

Write-Host "Restoration complete!"
