# ImageMagick UI

A portable **Windows WinForms** graphical interface for [ImageMagick](https://imagemagick.org), built with **.NET 4.8** — no Python, no extra dependencies, no installation required.

## Features

| Tab | Functions |
|---|---|
| 🔄 Transformations | Resize, Crop, Rotate, Flip/Flop, Transpose, Trim, Border |
| ✨ Visual Effects | Blur, Sharpen, Noise, Charcoal, Oil Paint, Sketch, Swirl, Wave, Vignette, Motion Blur, Pixelate, **Print/Scan Effect** |
| 🎨 Colors | Colorspace, Brightness/Contrast, Gamma, Levels, HSB Modulate, Sepia, Tint, Colorize, Threshold, Posterize, Dither, Channels |
| 📄 PDF | PDF→Images, Page extraction, Images→PDF, Compression, Montage |
| ✏️ Annotations | Text, Watermark, Rectangle, Circle |
| 📦 Batch & Format | Batch processing, Format conversion, Strip metadata |

## Requirements

- **Windows 10 / 11** (.NET 4.8 is pre-installed)
- [ImageMagick](https://imagemagick.org/script/download.php#windows) installed and accessible via `magick` in your PATH (or placed alongside the `.exe`)
- [Ghostscript](https://ghostscript.com/releases/gsdnld.html) (optional, for PDF operations)

## Download

Grab the latest `ImageMagickUI.exe` from the [Releases](../../releases) page — no installation needed.

## Build from source

```bash
# Requires .NET SDK
dotnet build ImageMagickUI/ImageMagickUI.csproj -c Release
```

The executable will be at `ImageMagickUI/bin/Release/net48/ImageMagickUI.exe`.

## CI/CD

Every push to `main` triggers a GitHub Actions build on `windows-latest`.
Tagging a commit as `v1.0.0` automatically creates a GitHub Release with the `.exe` attached.

```bash
git tag v1.0.0
git push origin v1.0.0
```

## License

MIT
