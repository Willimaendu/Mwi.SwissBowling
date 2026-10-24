# Entfernt Dateien, die durch den einheitlichen Importlauf obsolet wurden. Einmalig ausführen: .\cleanup-obsolete.ps1
$root = $PSScriptRoot
$obsolete = @(
  'src\SwissBowling.Client\Features\Import\Admin\ImportButton.razor',
  'src\SwissBowling.Client\Features\Import\Admin\ImportButton.razor.cs'
)

foreach ($item in $obsolete) {
  $path = Join-Path $root $item
  if (Test-Path $path) {
    Remove-Item $path -Force
    Write-Host "Entfernt: $item"
  }
}

Remove-Item $PSCommandPath -Force
Write-Host 'Fertig.'
