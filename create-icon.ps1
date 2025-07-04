# PowerShell script to create an app icon
Write-Host "Creating YT-DLP Wrapper Icon..." -ForegroundColor Cyan

# Check if System.Drawing is available
try {
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
    
    # Create a 256x256 bitmap
    $bitmap = New-Object System.Drawing.Bitmap(256, 256)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Set high quality rendering
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias
    
    # Create gradient background (dark theme)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point(256, 256)),
        [System.Drawing.Color]::FromArgb(10, 10, 10),     # #0A0A0A
        [System.Drawing.Color]::FromArgb(37, 37, 37)      # #252525
    )
    
    $graphics.FillRectangle($brush, 0, 0, 256, 256)
    
    # Create orange circle (SwarmAI style)
    $orangeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 107, 53)) # #FF6B35
    $graphics.FillEllipse($orangeBrush, 64, 64, 128, 128)
    
    # Add white text "YT"
    $font = New-Object System.Drawing.Font("Segoe UI", 48, [System.Drawing.FontStyle]::Bold)
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $textSize = $graphics.MeasureString("YT", $font)
    $x = (256 - $textSize.Width) / 2
    $y = (256 - $textSize.Height) / 2
    
    $graphics.DrawString("YT", $font, $whiteBrush, $x, $y)
    
    # Save as PNG first
    $bitmap.Save("icon.png", [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "✅ Created icon.png" -ForegroundColor Green
    
    # Create ICO file (requires ImageMagick or we'll use PNG)
    if (Get-Command "magick" -ErrorAction SilentlyContinue) {
        & magick convert icon.png -define icon:auto-resize=256,128,64,48,32,16 icon.ico
        Write-Host "✅ Created icon.ico with multiple sizes" -ForegroundColor Green
    } else {
        # Copy PNG as ICO for now
        Copy-Item "icon.png" "icon.ico"
        Write-Host "⚠️ ImageMagick not found. Using PNG as ICO." -ForegroundColor Yellow
        Write-Host "For best results, install ImageMagick or use an online PNG to ICO converter." -ForegroundColor Yellow
    }
    
    # Cleanup
    $graphics.Dispose()
    $bitmap.Dispose()
    $brush.Dispose()
    $orangeBrush.Dispose()
    $whiteBrush.Dispose()
    $font.Dispose()
    
    Write-Host "🎉 Icon creation complete!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error creating icon: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Alternative: Use an online logo maker or icon generator" -ForegroundColor Yellow
    Write-Host "   - Visit favicon.io or canva.com to create a logo" -ForegroundColor Yellow
    Write-Host "   - Use orange (#FF6B35) and dark theme colors" -ForegroundColor Yellow
    Write-Host "   - Save as icon.ico in this folder" -ForegroundColor Yellow
} 