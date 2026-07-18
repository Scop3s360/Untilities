Add-Type -AssemblyName System.IO.Compression.FileSystem
$docxPath = ".\James_Lock_Final_Infrastructure_CV.docx"
$extractPath = ".\temp_cv_extract"

# Expand the docx file (which is a ZIP archive)
Expand-Archive -Path $docxPath -DestinationPath $extractPath -Force

# Read the document.xml file
[xml]$doc = Get-Content "$extractPath\word\document.xml"

# Extract all text nodes
$text = $doc.SelectNodes("//w:t") | ForEach-Object { $_.InnerText }

# Join the text with spaces and output to a file
$text -join " " | Out-File ".\cv_text.txt"
Remove-Item -Recurse -Force $extractPath
Write-Host "Extracted text to cv_text.txt"
