# Generates a random 32-byte (AES-256) key and prints it as a C# byte-array
# initializer ready to paste into KeyMaterial.cs, plus base64 for reference.
$bytes = New-Object 'System.Byte[]' 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)

Write-Host "`nBase64 (reference):" -ForegroundColor Cyan
Write-Host ([Convert]::ToBase64String($bytes))

Write-Host "`nC# initializer for KeyMaterial.cs:" -ForegroundColor Cyan
$hex = ($bytes | ForEach-Object { '0x{0:X2}' -f $_ })
for ($i = 0; $i -lt 32; $i += 8) {
    Write-Host ("      " + (($hex[$i..($i+7)]) -join ', ') + ',')
}
