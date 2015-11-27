$file= "AssemblyInfo.cs"
$content = Get-Content $file
$pattern = 'AssemblyVersion\("(.*?)"\)'
$match = [System.Text.RegularExpressions.Regex]::Match($content, $pattern)
Write-Host "AssemblyInfo.cs Version is: " $match.Groups[1]

$vsix = "MonoDebugger.VS2013\VisualStudio\source.extension.vsixmanifest"
$content = Get-Content $vsix
$content = [System.Text.RegularExpressions.Regex]::Replace($content, '" Version="(.*?)"', '" Version="'+$match.Groups[1]+'"')
Write-Host $content

Set-Content -Value $content -Path $vsix -encoding UTF8