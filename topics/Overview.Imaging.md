---
layout: default
title:  Imaging Overview
---

# Imaging Overview

This is a brief overview of imaging components and APIs available in Platform for Situated Intelligence.

## Basic Components

There follow is currently the components provided by \\psi for handling of images:
- `ImageDecoder` - Decodes a compressed image
- `ImageEncoder` - Compresses an image
- `TransformImageComponent` - Performs some transformation on the image

## Common Patterns of Usage

The following are some examples of how to use the Image components in \\psi.

### Encode an image into .png

The following example shows how to take an image and compress it into a .png

```csharp
using Microsoft.Psi.Imaging;

public EncodedImage EncodeImage(Image testImage)
{
    EncodedImage encImg = new EncodedImage();
    encImg.EncodeFrom(testImage, new PngBitmapEncoder());
    return encImg;
}
```

### Decode a .png into a <see cref="Microsoft.Psi.Imaging.Image">Microsoft.Psi.Imaging.Image</see>

The following example shows how to take an image and compress it into a .png

```csharp
using Microsoft.Psi.Imaging;

public Image DecodeImage(EncodedImage encodedImage)
{
    Image decImg = new Image(encodedImage.Width, encodedImage.Height, encodedImage.Width * 3,  PixelFormat.Format24bppRgb);
    encodedImage.DecodeTo(decImg);
    return decImg;
}
```

### Crop an stream of images

This final sample shows how to create a stream of images from a single image, and crop each image in the stream by a fixed set of coordinates. We then write each cropped image out to a file.

```csharp
public void CropImages(Image testImage)
{
    using (var pipeline = Pipeline.Create())
    {
        var sharedImage = Microsoft.Psi.Imaging.ImagePool.GetOrCreate(testImage.Width, testImage.Height, testImage.PixelFormat);
        testImage.CopyTo(sharedImage.Resource);
        var images = Generators.Sequence(pipeline, sharedImage, x => sharedImage, 100);
        var rects = Generators.Sequence<System.Drawing.Rectangle>(pipeline, new System.Drawing.Rectangle(), x => {
	        System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
	        rect.X = 0;
	        rect.Y = 0;
	        rect.Width = 100;
	        rect.Height = 100;
	        return rect;
        }, 100);
        var croppedImages = images.Pair(rects).Crop();
        int count = 0;
        images.Do((img, e) => {
            img.Resource.ToManagedImage().Save(@"c:\temp\image-" + count.ToString() + ".bmp");
            count++;
        });
        pipeline.Run();
    }
}
```
