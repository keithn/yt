param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format X.Y.Z (e.g. 1.0.0)"
    exit 1
}

$tag = "v$Version"

Write-Host "Tagging $tag and pushing to trigger release build..."

git tag $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git push origin $tag
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Check GitHub Actions for build progress."
