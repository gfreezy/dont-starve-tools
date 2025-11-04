using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using KleiLib;
using SquishNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TEXTool.Avalonia.Models;

namespace TEXTool.Avalonia.Services
{
    public class TexLoadResult
    {
        public Image<Rgba32> Image { get; set; } = null!;
        public List<AtlasElement> AtlasElements { get; set; } = new();
        public string Platform { get; set; } = "";
        public string PixelFormat { get; set; } = "";
        public string TextureType { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int MipmapCount { get; set; }
    }

    public class TexLoader
    {
        public TexLoadResult LoadTexFile(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            return LoadTexFile(stream, filePath);
        }

        public TexLoadResult LoadTexFile(Stream stream, string filePath)
        {
            var texFile = new TEXFile(stream);
            var result = new TexLoadResult();

            // Get file info
            result.Platform = ((TEXFile.Platform)texFile.File.Header.Platform).ToString();
            result.PixelFormat = ((TEXFile.PixelFormat)texFile.File.Header.PixelFormat).ToString();
            result.TextureType = ((TEXFile.TextureType)texFile.File.Header.TextureType).ToString();
            result.MipmapCount = (int)texFile.File.Header.NumMips;

            // Get main mipmap
            var mipmap = texFile.GetMainMipmap();
            result.Width = mipmap.Width;
            result.Height = mipmap.Height;

            // Decompress image data
            byte[] rgbaData = DecompressTexture(mipmap, (TEXFile.PixelFormat)texFile.File.Header.PixelFormat);

            // Create image
            result.Image = Image.LoadPixelData<Rgba32>(rgbaData, mipmap.Width, mipmap.Height);

            // Flip vertically (TEX format is flipped)
            result.Image.Mutate(x => x.Flip(FlipMode.Vertical));

            // Try to load atlas data
            var atlasPath = Path.ChangeExtension(filePath, ".xml");
            if (File.Exists(atlasPath))
            {
                result.AtlasElements = LoadAtlasData(atlasPath, mipmap.Width, mipmap.Height);
            }

            return result;
        }

        private byte[] DecompressTexture(TEXFile.Mipmap mipmap, TEXFile.PixelFormat pixelFormat)
        {
            return pixelFormat switch
            {
                TEXFile.PixelFormat.DXT1 => Squish.DecompressImage(mipmap.Data, mipmap.Width, mipmap.Height, SquishFlags.Dxt1),
                TEXFile.PixelFormat.DXT3 => Squish.DecompressImage(mipmap.Data, mipmap.Width, mipmap.Height, SquishFlags.Dxt3),
                TEXFile.PixelFormat.DXT5 => Squish.DecompressImage(mipmap.Data, mipmap.Width, mipmap.Height, SquishFlags.Dxt5),
                TEXFile.PixelFormat.ARGB => mipmap.Data,
                _ => throw new NotSupportedException($"Pixel format {pixelFormat} is not supported")
            };
        }

        private List<AtlasElement> LoadAtlasData(string atlasPath, int width, int height)
        {
            var elements = new List<AtlasElement>();

            try
            {
                var xDoc = new XmlDocument();
                xDoc.Load(atlasPath);

                var elementsNode = xDoc.SelectSingleNode("Atlas/Elements");
                if (elementsNode == null) return elements;

                foreach (XmlNode child in elementsNode.ChildNodes)
                {
                    if (child.Attributes == null) continue;

                    var name = child.Attributes.GetNamedItem("name")?.Value ?? "Unknown";
                    var u1 = ParseDouble(child.Attributes.GetNamedItem("u1")?.Value);
                    var u2 = ParseDouble(child.Attributes.GetNamedItem("u2")?.Value);
                    var v1 = ParseDouble(child.Attributes.GetNamedItem("v1")?.Value);
                    var v2 = ParseDouble(child.Attributes.GetNamedItem("v2")?.Value);

                    // Invert Y-axis (game coordinates start from bottom-left)
                    v1 = 1.0 - v1;
                    v2 = 1.0 - v2;

                    // Convert normalized coordinates to pixels
                    const double margin = 0.5;
                    int x = (int)(u1 * width - margin);
                    int y = (int)(v2 * height - margin);
                    int w = (int)((u2 - u1) * width);
                    int h = (int)((v1 - v2) * height);

                    elements.Add(new AtlasElement(name, x, y, w, h));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading atlas data: {ex.Message}");
            }

            return elements;
        }

        private double ParseDouble(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0.0;
            return double.Parse(value, CultureInfo.InvariantCulture);
        }

        public void ExportImage(Image<Rgba32> image, string outputPath)
        {
            image.SaveAsPng(outputPath);
        }

        public void ExportAtlasElement(Image<Rgba32> sourceImage, AtlasElement element, string outputPath)
        {
            // Crop the element from source image
            using var croppedImage = sourceImage.Clone(ctx =>
                ctx.Crop(new Rectangle(element.X, element.Y, element.Width, element.Height)));

            croppedImage.SaveAsPng(outputPath);
        }
    }
}
