$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Mwi.SwissBowling.AppHost'
$certificateDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'SwissBowling/certificates'
New-Item -ItemType Directory -Force -Path $certificateDirectory | Out-Null
$certificatePath = Join-Path $certificateDirectory 'data-protection.pfx'
if (Test-Path $certificatePath) { throw 'A development certificate already exists. Keep the existing user secrets; do not replace a key protecting persisted sessions.' }
function New-Secret {
	$bytes = New-Object byte[] 32
	$generator = [Security.Cryptography.RandomNumberGenerator]::Create()
	try { $generator.GetBytes($bytes); [Convert]::ToBase64String($bytes) } finally { $generator.Dispose() }
}
$secrets = @{
	'Parameters:postgres-password' = New-Secret
	'Parameters:identity-password' = New-Secret
	'Parameters:keycloak-password' = New-Secret
	'Parameters:development-password' = New-Secret
	'Parameters:web-client-secret' = New-Secret
	'Parameters:certificate-password' = New-Secret
	'Parameters:certificate-path' = $certificatePath
}
$rsa = [Security.Cryptography.RSACng]::new(3072)
try {
	$request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=SwissBowling Development Data Protection', $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
	$certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddYears(2))
	try { [IO.File]::WriteAllBytes($certificatePath, $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $secrets['Parameters:certificate-password'])) } finally { $certificate.Dispose() }
} finally { $rsa.Dispose() }
$secrets | ConvertTo-Json | dotnet user-secrets set --project $project
if ($LASTEXITCODE -ne 0) { throw 'Could not store development secrets.' }
Write-Host 'Development secrets initialized outside the repository. Use dotnet user-secrets list locally to retrieve the development login password; never publish that output.'
Write-Host 'Trust the local HTTPS certificate separately: dotnet dev-certs https --trust'
