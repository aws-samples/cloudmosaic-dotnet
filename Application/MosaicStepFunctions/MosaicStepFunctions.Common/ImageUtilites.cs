using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MosaicStepFunctions.Common
{
    public static class ImageUtilites
    {
        public static void ResizeImage(Image<Rgba32> image, int maxWidth, int maxHeight)
        {
            int width, height;

            if (image.Height < image.Width)
            {
                height = (int)((double)image.Height * (double)maxWidth / (double)image.Width);
                width = maxWidth;
            }
            else
            {
                height = maxHeight;
                width = (int)((double)image.Width * (double)maxHeight / (double)image.Height);
            }

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.Primitives.Size { Width = width, Height = height },
                Mode = ResizeMode.Stretch
            }));
        }
    }
}
