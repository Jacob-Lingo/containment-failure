# Downloads the CC0 fantasy asset packs into Assets/Art/Fantasy/.
#
# Everything here is Kenney (kenney.nl), Creative Commons CC0 — public domain,
# no attribution required, commercial use fine. That matters for a graded
# submission: no licence file to ship, no credit to forget.
#
# Run from the repo root:   .\Tools\fetch-fantasy-assets.ps1
# Then let Unity reimport. Nothing is overwritten — packs already present are
# skipped, so re-running is safe.

$ErrorActionPreference = 'Stop'

$packs = @(
    @{ Name = 'TinyDungeon';        Url = 'https://kenney.nl/media/pages/assets/tiny-dungeon/f8422efb44-1674742415/kenney_tiny-dungeon.zip' }
    @{ Name = 'RoguelikeCharacters'; Url = 'https://kenney.nl/media/pages/assets/roguelike-characters/53ffff4133-1729196490/kenney_roguelike-characters.zip' }
    @{ Name = 'CavesDungeons';      Url = 'https://kenney.nl/media/pages/assets/roguelike-caves-dungeons/5195ceb8ca-1677694831/kenney_roguelike-caves-dungeons.zip' }
    @{ Name = 'UiRpg';              Url = 'https://kenney.nl/media/pages/assets/ui-pack-rpg-expansion/7ec4a46657-1677661824/kenney_ui-pack-rpg-expansion.zip' }
    # Expansion to Tiny Dungeon, same 16x16 style: 180 sprites, 100+ monsters.
    # This is what makes a deep evolution tree possible — TinyDungeon only has
    # about 15 creature tiles, which is why the tree read as thin.
    @{ Name = 'TinyCreatures';      Url = 'https://opengameart.org/sites/default/files/tiny-creatures.zip' }
    # Animated pixel spell/impact FX — the gap the procedural effects can't fill
    # (real flame, lightning, poison clouds). Both CC0, from OpenGameArt.
    @{ Name = 'FxPixelEffects';     Url = 'https://opengameart.org/sites/default/files/Free%20Pixel%20Effects%20Pack.zip' }
    @{ Name = 'FxPixelDesigner';    Url = 'https://opengameart.org/sites/default/files/pixel_effects.zip' }
)

$destRoot = Join-Path $PSScriptRoot '..\Assets\Art\Fantasy'
$destRoot = [System.IO.Path]::GetFullPath($destRoot)
New-Item -ItemType Directory -Force -Path $destRoot | Out-Null

$staging = Join-Path $env:TEMP 'containment-fantasy-assets'
New-Item -ItemType Directory -Force -Path $staging | Out-Null

foreach ($pack in $packs) {
    $target = Join-Path $destRoot $pack.Name

    if (Test-Path $target) {
        Write-Host "skip   $($pack.Name) (already present)"
        continue
    }

    $zip = Join-Path $staging "$($pack.Name).zip"
    Write-Host "fetch  $($pack.Name) ..."

    # Kenney's CDN rejects the default PowerShell agent on some links.
    Invoke-WebRequest -Uri $pack.Url -OutFile $zip -UserAgent 'Mozilla/5.0'

    Write-Host "unzip  $($pack.Name) -> Assets/Art/Fantasy/$($pack.Name)"
    Expand-Archive -Path $zip -DestinationPath $target -Force
    Remove-Item $zip -Force
}

Write-Host ''
Write-Host "Done. Assets are in $destRoot"
Write-Host 'Switch to Unity and let it reimport, then set the new textures to:'
Write-Host '  Texture Type   = Sprite (2D and UI)'
Write-Host '  Filter Mode    = Point (no filter)     <- required, or pixel art blurs'
Write-Host '  Compression    = None'
Write-Host 'For the tilesheets also set Sprite Mode = Multiple and use the Sprite Editor grid slicer (16x16).'
