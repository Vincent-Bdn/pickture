# Pickture

A lightweight desktop tool for quickly browsing, selecting, and applying visual effects to your photos. Pickture streamlines the photo curation workflow by combining image viewing, selection, and basic visual enhancements in one intuitive interface.

## How It Works

### Viewing Images
1. Open Pickture and select a folder containing images
2. Thumbnails appear in the left panel
3. Click any thumbnail or use arrow keys to view the full image
4. Images with stars are previously selected favorites

### Applying Effects
1. Click "**+ Add to selection**" on any image
2. A confirmation dialog opens showing:
   - The current image with all visual effects available
   - Rotation controls (90° buttons and fine-tune slider)
   - Effect buttons to toggle White Balance and Custom adjustments
   - A live histogram with clamp controls
3. Use the histogram controls or sliders to preview changes
4. Click **Confirm** to save the processed image

### Selection Storage
Selected images are automatically saved to a `selection/` subfolder within your image folder, preserving the original images while keeping your curated collection organized.

## File Organization

```
Your Photos/
├── photo1.jpg
├── photo2.jpg
├── photo3.jpg
└── selection/
    ├── photo1_ORIG.jpg          (if you saved without effects)
    ├── photo2_WBV.jpg           (with White Balance Value effect)
    └── photo3_CUSTOM.jpg        (with Custom histogram adjustments)
```

## System Requirements

- Windows 10/11
- .NET 9.0 runtime
- Supports common image formats: JPG, PNG, BMP, GIF, TIFF

## Getting Started

1. Launch Pickture
2. Select a folder containing your photos
3. Start browsing and selecting your best shots
4. Apply effects as needed and save your selections

Enjoy curating your photo collection!
