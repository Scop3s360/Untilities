Add-Type -AssemblyName System.IO.Compression.FileSystem
$docxPath = "d:\Projects\Utilities\Job_Hutn\James_Lock_Final_Infrastructure_CV.docx"
$extractPath = "d:\Projects\Utilities\Job_Hutn\temp_cv_extract"

if (Test-Path $extractPath) { Remove-Item -Recurse -Force $extractPath }
[System.IO.Compression.ZipFile]::ExtractToDirectory($docxPath, $extractPath)

$xmlContent = Get-Content -Path "$extractPath\word\document.xml" -Raw
$text = [regex]::Matches($xmlContent, '<w:t[^>]*>(.*?)</w:t>') | ForEach-Object { $_.Groups[1].Value }

$text -join " " | Out-File "d:\Projects\Utilities\Job_Hutn\cv_text.txt"
Remove-Item -Recurse -Force $extractPath
Write-Host "Extracted text to cv_text.txt"
