#region License
/*
SquishNET is licensed under the MIT license.
Copyright © 2013 Matt Stevens
Modified 2025 to use BCnEncoder.NET for cross-platform support

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System.Runtime.InteropServices;
using BCnEncoder.Encoder;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SquishNET
{
    /// <summary>
    /// Cross-platform DXT compression/decompression library using BCnEncoder.NET.
    /// </summary>
    public static class Squish
    {
        /// <summary>
        /// Returns the final size in bytes of DXT data compressed with the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="width">Source image width.</param>
        /// <param name="height">Source image height.</param>
        /// <param name="flags">Compression parameters.</param>
        /// <returns>Size in bytes of the DXT data.</returns>
        public static int GetStorageRequirements(int width, int height, SquishFlags flags)
        {
            // Calculate block size
            int blockSize = GetBlockSize(flags);

            // Round up to nearest multiple of 4
            int blocksWide = (width + 3) / 4;
            int blocksHigh = (height + 3) / 4;

            return blocksWide * blocksHigh * blockSize;
        }

        /// <summary>
        /// Compress a 4x4 pixel block using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Source RGBA block.</param>
        /// <param name="block">Output DXT compressed block.</param>
        /// <param name="flags">Compression flags.</param>
        public static void Compress(IntPtr rgba, IntPtr block, SquishFlags flags)
        {
            byte[] rgbaData = new byte[4 * 4 * 4]; // 4x4 pixels, 4 bytes per pixel
            Marshal.Copy(rgba, rgbaData, 0, rgbaData.Length);

            byte[] compressed = Compress(rgbaData, flags);
            Marshal.Copy(compressed, 0, block, compressed.Length);
        }

        /// <summary>
        /// Compress a 4x4 pixel block using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Source RGBA block.</param>
        /// <param name="flags">Compression flags.</param>
        /// <returns>Output DXT compressed block.</returns>
        public static byte[] Compress(byte[] rgba, SquishFlags flags)
        {
            return CompressImage(rgba, 4, 4, flags);
        }

        /// <summary>
        /// Compress a 4x4 pixel block using the parameters specified in <paramref name="flags"/>. The <paramref name="mask"/> parameter is a used as
        /// a bit mask to specifify what pixels are valid for compression, corresponding the lowest bit to the first pixel.
        /// </summary>
        /// <param name="rgba">Source RGBA block.</param>
        /// <param name="mask">Pixel bit mask.</param>
        /// <param name="block">Output DXT compressed block.</param>
        /// <param name="flags">Compression flags.</param>
        public static void CompressMasked(IntPtr rgba, int mask, IntPtr block, SquishFlags flags)
        {
            // Note: BCnEncoder doesn't support masking, so we'll just ignore the mask
            Compress(rgba, block, flags);
        }

        /// <summary>
        /// Compress a 4x4 pixel block using the parameters specified in <paramref name="flags"/>. The <paramref name="mask"/> parameter is a used as
        /// a bit mask to specifify what pixels are valid for compression, corresponding the lowest bit to the first pixel.
        /// </summary>
        /// <param name="rgba">Source RGBA block.</param>
        /// <param name="mask">Pixel bit mask.</param>
        /// <param name="flags">Compression flags.</param>
        /// <returns>Output DXT compressed block.</returns>
        public static byte[] CompressMasked(byte[] rgba, int mask, SquishFlags flags)
        {
            // Note: BCnEncoder doesn't support masking, so we'll just ignore the mask
            return Compress(rgba, flags);
        }

        /// <summary>
        /// Decompresses a 4x4 pixel block using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Output RGBA decompressed block.</param>
        /// <param name="block">Source DXT block.</param>
        /// <param name="flags">Decompression flags.</param>
        public static void Decompress(IntPtr rgba, IntPtr block, SquishFlags flags)
        {
            int blockSize = GetBlockSize(flags);
            byte[] blockData = new byte[blockSize];
            Marshal.Copy(block, blockData, 0, blockSize);

            byte[] decompressed = Decompress(blockData, flags);
            Marshal.Copy(decompressed, 0, rgba, decompressed.Length);
        }

        /// <summary>
        /// Decompresses a 4x4 pixel block using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="block">Source DXT block.</param>
        /// <param name="flags">Decompression flags.</param>
        /// <returns>Output RGBA decompressed block.</returns>
        public static byte[] Decompress(byte[] block, SquishFlags flags)
        {
            return DecompressImage(block, 4, 4, flags);
        }

        /// <summary>
        /// Compresses an image using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Source RGBA image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="blocks">Output DXT compressed image.</param>
        /// <param name="flags">Compression flags.</param>
        public static void CompressImage(IntPtr rgba, int width, int height, IntPtr blocks, SquishFlags flags)
        {
            int dataSize = width * height * 4;
            byte[] rgbaData = new byte[dataSize];
            Marshal.Copy(rgba, rgbaData, 0, dataSize);

            byte[] compressed = CompressImage(rgbaData, width, height, flags);
            Marshal.Copy(compressed, 0, blocks, compressed.Length);
        }

        /// <summary>
        /// Compresses an image using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Source RGBA image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="flags">Compression flags.</param>
        /// <returns>Output DXT compressed image.</returns>
        public static byte[] CompressImage(byte[] rgba, int width, int height, SquishFlags flags)
        {
            // Create an ImageSharp image from the RGBA data
            using var image = Image.LoadPixelData<Rgba32>(rgba, width, height);

            // Create encoder and configure
            var encoder = new BcEncoder();
            encoder.OutputOptions.Format = GetCompressionFormat(flags);
            encoder.OutputOptions.GenerateMipMaps = false;
            encoder.OutputOptions.Quality = CompressionQuality.Balanced;

            // Encode to raw bytes (returns byte[][] with mipmap levels, we only need [0])
            byte[][] encoded = encoder.EncodeToRawBytes(image);

            return encoded[0];
        }

        /// <summary>
        /// Decompresses an image using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="rgba">Output RGBA decompressed image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="blocks">Source DXT compressed image.</param>
        /// <param name="flags">Decompression flags.</param>
        public static void DecompressImage(IntPtr rgba, int width, int height, IntPtr blocks, SquishFlags flags)
        {
            int blockSize = GetStorageRequirements(width, height, flags);
            byte[] blockData = new byte[blockSize];
            Marshal.Copy(blocks, blockData, 0, blockSize);

            byte[] decompressed = DecompressImage(blockData, width, height, flags);
            Marshal.Copy(decompressed, 0, rgba, decompressed.Length);
        }

        /// <summary>
        /// Decompresses an image using the parameters specified in <paramref name="flags"/>.
        /// </summary>
        /// <param name="blocks">Source DXT compressed image.</param>
        /// <param name="width">Width of the image.</param>
        /// <param name="height">Height of the image.</param>
        /// <param name="flags">Decompression flags.</param>
        /// <returns>Output RGBA decompressed image.</returns>
        public static byte[] DecompressImage(byte[] blocks, int width, int height, SquishFlags flags)
        {
            // Decode using BCnDecoder
            var decoder = new BcDecoder();
            using var image = decoder.DecodeRawToImageRgba32(blocks, width, height, GetCompressionFormat(flags));

            // Extract RGBA data from image
            byte[] rgba = new byte[width * height * 4];

            // Copy pixel data
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image[x, y];
                    int offset = (y * width + x) * 4;
                    rgba[offset + 0] = pixel.R;
                    rgba[offset + 1] = pixel.G;
                    rgba[offset + 2] = pixel.B;
                    rgba[offset + 3] = pixel.A;
                }
            }

            return rgba;
        }

        #region Helper Methods

        private static CompressionFormat GetCompressionFormat(SquishFlags flags)
        {
            if ((flags & SquishFlags.Dxt1) != 0)
                return CompressionFormat.Bc1;
            if ((flags & SquishFlags.Dxt3) != 0)
                return CompressionFormat.Bc2;
            if ((flags & SquishFlags.Dxt5) != 0)
                return CompressionFormat.Bc3;

            return CompressionFormat.Bc3; // Default to BC3/DXT5
        }

        private static int GetBlockSize(SquishFlags flags)
        {
            if ((flags & SquishFlags.Dxt1) != 0)
                return 8;  // DXT1/BC1 uses 8 bytes per 4x4 block
            else
                return 16; // DXT3/DXT5 (BC2/BC3) use 16 bytes per 4x4 block
        }

        #endregion
    }
}
