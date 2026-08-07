$inputFile = "publicrel1.data.unityweb"
$splitSize = 20MB
$buffer = New-Object byte[] $splitSize
$fileStream = [System.IO.File]::OpenRead((Resolve-Path $inputFile).Path)
$partIndex = 1

try {
    while ($fileStream.Position -lt $fileStream.Length) {
        $bytesRead = $fileStream.Read($buffer, 0, $splitSize)
        # This line creates the 01, 02, 10 formatting
        $formattedIndex = "{0:D2}" -f $partIndex
        $outputFile = "$inputFile.part$formattedIndex"
        $outputStream = [System.IO.File]::Create((Join-Path (Get-Location) $outputFile))
        $outputStream.Write($buffer, 0, $bytesRead)
        $outputStream.Close()
        
        Write-Host "Created: $outputFile"
        $partIndex++
    }
}
finally {
    $fileStream.Close()
}