using System;
using System.Collections.Generic;
using System.IO;
using KleiLib;
using SquishNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TEXCreator.Avalonia.Services
{
    public class TexConverter
    {
        public struct Mipmap
        {
            public ushort Width;
            public ushort Height;
            public ushort Pitch;
            public byte[] ARGBData;

            public Mipmap(ushort w, ushort h, ushort p, byte[] d)
            {
                Width = w;
                Height = h;
                Pitch = p;
                ARGBData = d;
            }
        }

        public class ConversionOptions
        {
            public TEXFile.PixelFormat PixelFormat { get; set; } = TEXFile.PixelFormat.DXT5;
            public TEXFile.TextureType TextureType { get; set; } = TEXFile.TextureType.TwoD;
            public bool GenerateMipmaps { get; set; } = false;
            public bool PreMultiplyAlpha { get; set; } = false;
        }

        public void ConvertPngToTex(string inputFile, string outputFile, ConversionOptions options)
        {
            using var outputStream = new FileStream(outputFile, FileMode.Create);
            ConvertPngToTex(inputFile, outputStream, options);
        }

        public void ConvertPngToTex(string inputFile, Stream outputStream, ConversionOptions options)
        {
            using var inputImage = Image.Load<Rgba32>(inputFile);

            // Flip image vertically (Y-axis)
            inputImage.Mutate(x => x.Flip(FlipMode.Vertical));

            var mipmaps = new List<Mipmap>();

            // Generate main mipmap
            mipmaps.Add(GenerateMipmap(inputImage, options.PixelFormat, options.PreMultiplyAlpha));

            // Generate mipmaps if requested
            if (options.GenerateMipmaps)
            {
                var width = inputImage.Width;
                var height = inputImage.Height;

                while (Math.Max(width, height) > 1)
                {
                    width = Math.Max(1, width >> 1);
                    height = Math.Max(1, height >> 1);

                    mipmaps.Add(GenerateMipmap(inputImage, options.PixelFormat, width, height, options.PreMultiplyAlpha));
                }
            }

            // Create TEX file
            var outputTEXFile = new TEXFile();
            outputTEXFile.File.Header.Platform = 0;
            outputTEXFile.File.Header.PixelFormat = (uint)options.PixelFormat;
            outputTEXFile.File.Header.TextureType = (uint)options.TextureType;
            outputTEXFile.File.Header.NumMips = (uint)mipmaps.Count;
            outputTEXFile.File.Header.Flags = 0;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Write mipmap headers
            foreach (var mip in mipmaps)
            {
                writer.Write(mip.Width);
                writer.Write(mip.Height);
                writer.Write(mip.Pitch);
                writer.Write((uint)mip.ARGBData.Length);
            }

            // Write mipmap data
            foreach (var mip in mipmaps)
            {
                writer.Write(mip.ARGBData);
            }

            outputTEXFile.File.Raw = ms.ToArray();
            outputTEXFile.SaveFile(outputStream);
        }

        private Mipmap GenerateMipmap(Image<Rgba32> inputImage, TEXFile.PixelFormat pixelFormat, bool preMultiplyAlpha)
        {
            return GenerateMipmap(inputImage, pixelFormat, inputImage.Width, inputImage.Height, preMultiplyAlpha);
        }

        private Mipmap GenerateMipmap(Image<Rgba32> sourceImage, TEXFile.PixelFormat pixelFormat, int width, int height, bool preMultiplyAlpha)
        {
            // Resize if needed
            Image<Rgba32> workingImage = sourceImage;
            bool needsDispose = false;

            if (width != sourceImage.Width || height != sourceImage.Height)
            {
                workingImage = sourceImage.Clone(ctx => ctx.Resize(width, height));
                needsDispose = true;
            }

            try
            {
                // Extract RGBA data
                byte[] rgba = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = workingImage[x, y];

                        if (preMultiplyAlpha)
                        {
                            float alphamod = pixel.A / 255.0f;
                            pixel.R = (byte)(pixel.R * alphamod);
                            pixel.G = (byte)(pixel.G * alphamod);
                            pixel.B = (byte)(pixel.B * alphamod);
                        }

                        int offset = y * width * 4 + x * 4;
                        rgba[offset + 0] = pixel.R;
                        rgba[offset + 1] = pixel.G;
                        rgba[offset + 2] = pixel.B;
                        rgba[offset + 3] = pixel.A;
                    }
                }

                byte[] finalImageData;
                int pitch;

                switch (pixelFormat)
                {
                    case TEXFile.PixelFormat.DXT1:
                        finalImageData = Squish.CompressImage(rgba, width, height, SquishFlags.Dxt1);
                        pitch = Squish.GetStorageRequirements(width, 1, SquishFlags.Dxt1);
                        break;
                    case TEXFile.PixelFormat.DXT3:
                        finalImageData = Squish.CompressImage(rgba, width, height, SquishFlags.Dxt3);
                        pitch = Squish.GetStorageRequirements(width, 1, SquishFlags.Dxt3);
                        break;
                    case TEXFile.PixelFormat.DXT5:
                        finalImageData = Squish.CompressImage(rgba, width, height, SquishFlags.Dxt5);
                        pitch = Squish.GetStorageRequirements(width, 1, SquishFlags.Dxt5);
                        break;
                    case TEXFile.PixelFormat.ARGB:
                        finalImageData = rgba;
                        pitch = width * 4;
                        break;
                    default:
                        throw new NotSupportedException($"Pixel format {pixelFormat} is not supported");
                }

                return new Mipmap((ushort)width, (ushort)height, (ushort)pitch, finalImageData);
            }
            finally
            {
                if (needsDispose)
                {
                    workingImage.Dispose();
                }
            }
        }
    }
}
