Add-Type -AssemblyName System.Drawing

$Root = Resolve-Path (Join-Path $PSScriptRoot '..')
$Output = Join-Path $Root 'assets\deskverse.ico'
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null

$Sizes = @(256, 64, 48, 32, 16)
$Pngs = [System.Collections.Generic.List[byte[]]]::new()

foreach ($Size in $Sizes) {
    $Bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $Graphics = [System.Drawing.Graphics]::FromImage($Bitmap)
    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $Graphics.Clear([System.Drawing.Color]::Transparent)
    $Scale = $Size / 256.0

    function Convert-Size([float]$Value) {
        return [int][Math]::Round($Value * $Scale)
    }

    function New-RoundedRectanglePath([System.Drawing.Rectangle]$Rectangle, [int]$Radius) {
        $Path = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $Path.AddArc($Rectangle.X, $Rectangle.Y, $Radius, $Radius, 180, 90)
        $Path.AddArc($Rectangle.Right - $Radius, $Rectangle.Y, $Radius, $Radius, 270, 90)
        $Path.AddArc($Rectangle.Right - $Radius, $Rectangle.Bottom - $Radius, $Radius, $Radius, 0, 90)
        $Path.AddArc($Rectangle.X, $Rectangle.Bottom - $Radius, $Radius, $Radius, 90, 90)
        $Path.CloseFigure()
        return $Path
    }

    $Background = [System.Drawing.Rectangle]::new(0, 0, $Size, $Size)
    $BackgroundPath = New-RoundedRectanglePath $Background (Convert-Size 56)
    $Graphics.FillPath([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(32, 37, 47)), $BackgroundPath)

    $Paper = [System.Drawing.Rectangle]::new((Convert-Size 31), (Convert-Size 29), (Convert-Size 194), (Convert-Size 198))
    $PaperPath = New-RoundedRectanglePath $Paper (Convert-Size 42)
    $PaperBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $Paper,
        [System.Drawing.Color]::FromArgb(243, 239, 227),
        [System.Drawing.Color]::FromArgb(159, 180, 188),
        45)
    $Graphics.FillPath($PaperBrush, $PaperPath)

    $Ink = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(44, 55, 64))
    $Graphics.FillPolygon($Ink, [System.Drawing.Point[]]@(
        [System.Drawing.Point]::new((Convert-Size 64), (Convert-Size 83)),
        [System.Drawing.Point]::new((Convert-Size 98), (Convert-Size 62)),
        [System.Drawing.Point]::new((Convert-Size 146), (Convert-Size 57)),
        [System.Drawing.Point]::new((Convert-Size 183), (Convert-Size 72)),
        [System.Drawing.Point]::new((Convert-Size 134), (Convert-Size 90)),
        [System.Drawing.Point]::new((Convert-Size 96), (Convert-Size 134)),
        [System.Drawing.Point]::new((Convert-Size 78), (Convert-Size 145))
    ))
    $Graphics.FillPolygon($Ink, [System.Drawing.Point[]]@(
        [System.Drawing.Point]::new((Convert-Size 86), (Convert-Size 167)),
        [System.Drawing.Point]::new((Convert-Size 128), (Convert-Size 149)),
        [System.Drawing.Point]::new((Convert-Size 206), (Convert-Size 154)),
        [System.Drawing.Point]::new((Convert-Size 178), (Convert-Size 184)),
        [System.Drawing.Point]::new((Convert-Size 122), (Convert-Size 194)),
        [System.Drawing.Point]::new((Convert-Size 86), (Convert-Size 180))
    ))

    $LinePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(248, 246, 239), [Math]::Max(1, (Convert-Size 11)))
    $LinePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $LinePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $Graphics.DrawLine($LinePen, (Convert-Size 71), (Convert-Size 116), (Convert-Size 176), (Convert-Size 116))
    $Graphics.DrawLine($LinePen, (Convert-Size 84), (Convert-Size 142), (Convert-Size 156), (Convert-Size 142))
    $Graphics.DrawLine($LinePen, (Convert-Size 102), (Convert-Size 168), (Convert-Size 141), (Convert-Size 168))

    $Stream = [System.IO.MemoryStream]::new()
    $Bitmap.Save($Stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $Pngs.Add($Stream.ToArray())

    $Stream.Dispose()
    $Graphics.Dispose()
    $Bitmap.Dispose()
}

$File = [System.IO.File]::Open($Output, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$Writer = [System.IO.BinaryWriter]::new($File)
$Writer.Write([UInt16]0)
$Writer.Write([UInt16]1)
$Writer.Write([UInt16]$Pngs.Count)

$Offset = 6 + 16 * $Pngs.Count
for ($Index = 0; $Index -lt $Pngs.Count; $Index++) {
    $Size = $Sizes[$Index]
    $Writer.Write([byte]$(if ($Size -eq 256) { 0 } else { $Size }))
    $Writer.Write([byte]$(if ($Size -eq 256) { 0 } else { $Size }))
    $Writer.Write([byte]0)
    $Writer.Write([byte]0)
    $Writer.Write([UInt16]1)
    $Writer.Write([UInt16]32)
    $Writer.Write([UInt32]$Pngs[$Index].Length)
    $Writer.Write([UInt32]$Offset)
    $Offset += $Pngs[$Index].Length
}

foreach ($Png in $Pngs) {
    $Writer.Write($Png)
}

$Writer.Dispose()
$File.Dispose()
Write-Host "Wrote $Output"
