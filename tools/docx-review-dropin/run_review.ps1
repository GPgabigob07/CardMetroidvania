param(
  [Parameter(Mandatory=$true)]
  [string]$Docx,

  [string]$Out = "",
  [switch]$AuditOnly,
  [switch]$NoRender,
  [switch]$Png,
  [switch]$Zip
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Script = Join-Path $Root "scripts\review_docx.py"

$Python = Get-Command python -ErrorAction SilentlyContinue
if (-not $Python) {
  $Python = Get-Command py -ErrorAction SilentlyContinue
}
if (-not $Python) {
  throw "Python não encontrado. Instale Python 3.10+ ou coloque python/py no PATH."
}

$ArgsList = @($Script, $Docx)
if ($Out -ne "") {
  $ArgsList += @("--out", $Out)
}
if ($AuditOnly) {
  $ArgsList += "--audit-only"
}
if ($NoRender) {
  $ArgsList += "--no-render"
}
if ($Png) {
  $ArgsList += "--png"
}
if ($Zip) {
  $ArgsList += "--zip"
}

& $Python.Source @ArgsList
