﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;

namespace Veldrid.ImageSharp
{
    public class ImageSharpCubemapTexture
    {
        /// <summary>
        /// An array of images, each face of a cubemap.
        /// Access of CubemapTextures[2][3] means face 2 with mipmap level 3
        /// </summary>
        public Image<Rgba32>[][] CubemapTextures { get; }

        /// <summary>
        /// The width of a cubemap texture.
        /// </summary>
        public uint Width => (uint)CubemapTextures[0][0].Width;

        /// <summary>
        /// The height of a cubemap texture.
        /// </summary>
        public uint Height => (uint)CubemapTextures[0][0].Height;

        /// <summary>
        /// The pixel format cubemap textures.
        /// </summary>
        public PixelFormat Format => PixelFormat.R8_G8_B8_A8_UNorm;

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public uint MipLevels => (uint)CubemapTextures[0].Length;

        /// <summary>
        /// Provides standardized access to the cubemap texture array
        /// </summary>
        private const int PositiveXArrayLayer = 0;
        private const int NegativeXArrayLayer = 1;
        private const int PositiveYArrayLayer = 2;
        private const int NegativeYArrayLayer = 3;
        private const int PositiveZArrayLayer = 4;
        private const int NegativeZArrayLayer = 5;

        public ImageSharpCubemapTexture(
            string positiveXPath,
            string negativeXPath,
            string positiveYPath,
            string negativeYPath,
            string positiveZPath,
            string negativeZPath) : this(
                Image.Load<Rgba32>(positiveXPath),
                Image.Load<Rgba32>(negativeXPath),
                Image.Load<Rgba32>(positiveYPath),
                Image.Load<Rgba32>(negativeYPath),
                Image.Load<Rgba32>(positiveZPath),
                Image.Load<Rgba32>(negativeZPath),
                true)
        { }

        public ImageSharpCubemapTexture(
            string positiveXPath,
            string negativeXPath,
            string positiveYPath,
            string negativeYPath,
            string positiveZPath,
            string negativeZPath,
            bool mipmap) : this(
                Image.Load<Rgba32>(positiveXPath),
                Image.Load<Rgba32>(negativeXPath),
                Image.Load<Rgba32>(positiveYPath),
                Image.Load<Rgba32>(negativeYPath),
                Image.Load<Rgba32>(positiveZPath),
                Image.Load<Rgba32>(negativeZPath),
                mipmap)
        { }

        public ImageSharpCubemapTexture(
            Stream positiveXStream,
            Stream negativeXStream,
            Stream positiveYStream,
            Stream negativeYStream,
            Stream positiveZStream,
            Stream negativeZStream,
            bool mipmap) : this(
                Image.Load<Rgba32>(positiveXStream),
                Image.Load<Rgba32>(negativeXStream),
                Image.Load<Rgba32>(positiveYStream),
                Image.Load<Rgba32>(negativeYStream),
                Image.Load<Rgba32>(positiveZStream),
                Image.Load<Rgba32>(negativeZStream),
                mipmap)
        { }

        public ImageSharpCubemapTexture(
            Image<Rgba32> positiveX,
            Image<Rgba32> negativeX,
            Image<Rgba32> positiveY,
            Image<Rgba32> negativeY,
            Image<Rgba32> positiveZ,
            Image<Rgba32> negativeZ,
            bool mipmap = true)
        {
            CubemapTextures = new Image<Rgba32>[6][];
            if (mipmap)
            {
                CubemapTextures[0] = MipmapHelper.GenerateMipmaps(positiveX);
                CubemapTextures[1] = MipmapHelper.GenerateMipmaps(negativeX);
                CubemapTextures[2] = MipmapHelper.GenerateMipmaps(positiveY);
                CubemapTextures[3] = MipmapHelper.GenerateMipmaps(negativeY);
                CubemapTextures[4] = MipmapHelper.GenerateMipmaps(positiveZ);
                CubemapTextures[5] = MipmapHelper.GenerateMipmaps(negativeZ);
            }

            else
            {
                CubemapTextures[0] = new Image<Rgba32>[1] { positiveX };
                CubemapTextures[1] = new Image<Rgba32>[1] { negativeX };
                CubemapTextures[2] = new Image<Rgba32>[1] { positiveY };
                CubemapTextures[3] = new Image<Rgba32>[1] { negativeY };
                CubemapTextures[4] = new Image<Rgba32>[1] { positiveZ };
                CubemapTextures[5] = new Image<Rgba32>[1] { negativeZ };
            }
        }

        public ImageSharpCubemapTexture(
            Image<Rgba32>[] positiveX,
            Image<Rgba32>[] negativeX,
            Image<Rgba32>[] positiveY,
            Image<Rgba32>[] negativeY,
            Image<Rgba32>[] positiveZ,
            Image<Rgba32>[] negativeZ)
        {
            CubemapTextures = new Image<Rgba32>[6][];
            if (positiveX.Length == 0)
            {
                throw new ArgumentException("Texture should have at least one mip level.");
            }
            if (positiveX.Length != negativeX.Length ||
                positiveX.Length != positiveY.Length ||
                positiveX.Length != negativeY.Length ||
                positiveX.Length != positiveZ.Length ||
                positiveX.Length != negativeZ.Length)
            {
                throw new ArgumentException("Mip count doesn't match.");
            }
            CubemapTextures[0] = positiveX;
            CubemapTextures[1] = negativeX;
            CubemapTextures[2] = positiveY;
            CubemapTextures[3] = negativeY;
            CubemapTextures[4] = positiveZ;
            CubemapTextures[5] = negativeZ;
        }

        public unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture cubemapTexture = factory.CreateTexture(TextureDescription.Texture2D(
                Width,
                Height,
                MipLevels,
                1,
                Format,
                TextureUsage.Sampled | TextureUsage.Cubemap));

            for (int level = 0; level < MipLevels; level++)
            {
                if (!CubemapTextures[PositiveXArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsPosX))
                {
                    throw new VeldridException("Unable to get positive x pixelspan.");
                }
                if (!CubemapTextures[NegativeXArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsNegX))
                {
                    throw new VeldridException("Unable to get negatve x pixelspan.");
                }
                if (!CubemapTextures[PositiveYArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsPosY))
                {
                    throw new VeldridException("Unable to get positive y pixelspan.");
                }
                if (!CubemapTextures[NegativeYArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsNegY))
                {
                    throw new VeldridException("Unable to get negatve y pixelspan.");
                }
                if (!CubemapTextures[PositiveZArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsPosZ))
                {
                    throw new VeldridException("Unable to get positive z pixelspan.");
                }
                if (!CubemapTextures[NegativeZArrayLayer][level].DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixelsNegZ))
                {
                    throw new VeldridException("Unable to get negatve z pixelspan.");
                }
                fixed (Rgba32* positiveXPin = pixelsPosX.Span)
                fixed (Rgba32* negativeXPin = pixelsNegX.Span)
                fixed (Rgba32* positiveYPin = pixelsPosY.Span)
                fixed (Rgba32* negativeYPin = pixelsNegY.Span)
                fixed (Rgba32* positiveZPin = pixelsPosZ.Span)
                fixed (Rgba32* negativeZPin = pixelsNegZ.Span)
                {
                    Image<Rgba32> image = CubemapTextures[0][level];
                    uint width = (uint)image.Width;
                    uint height = (uint)image.Height;
                    uint faceSize = width * height * PixelSizeInBytes;
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveXPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PositiveXArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeXPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeXArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveYPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PositiveYArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeYPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeYArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveZPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PositiveZArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeZPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeZArrayLayer);
                }
            }
            return cubemapTexture;
        }
    }
}

