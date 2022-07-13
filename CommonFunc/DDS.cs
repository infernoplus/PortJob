using DirectXTexNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TeximpNet.DDS;

namespace CommonFunc {
    public class DDS {
        public static int GetTpfFormatFromDdsBytes(byte[] ddsBytes) {
            using (MemoryStream ddsStream = new(ddsBytes)) {
                DXGIFormat format = DDSFile.Read(ddsStream).Format;

                switch (format) {
                    //DSR:
                    case DXGIFormat.BC1_UNorm:
                    case DXGIFormat.BC1_UNorm_SRGB:
                        return 0;
                    case DXGIFormat.BC2_UNorm:
                    case DXGIFormat.BC2_UNorm_SRGB:
                        return 3;
                    case DXGIFormat.BC3_UNorm:
                    case DXGIFormat.BC3_UNorm_SRGB:
                        return 5;
                    case DXGIFormat.R16G16_Float:
                        return 35;
                    case DXGIFormat.BC5_UNorm:
                        return 36;
                    case DXGIFormat.BC6H_UF16:
                        return 37;
                    case DXGIFormat.BC7_UNorm:
                    case DXGIFormat.BC7_UNorm_SRGB:
                        return 38;
                    //DS3:
                    case DXGIFormat.B5G5R5A1_UNorm:
                        return 6;
                    case DXGIFormat.B8G8R8A8_UNorm:
                    case DXGIFormat.B8G8R8A8_UNorm_SRGB:
                        return 9;
                    case DXGIFormat.B8G8R8X8_UNorm:
                    case DXGIFormat.B8G8R8X8_UNorm_SRGB:
                        return 10;
                    case DXGIFormat.R16G16B16A16_Float:
                        return 22;
                    default:
                        return 0;
                }
            }

        }

        /// <summary>
        /// Takes in a Byte4 width and height and returns an BC2_UNORM_SRGB DDS file. There are optional parameters,
        /// including scale (of which you have to provide both x and y for it to scale). Most of the Texture format and
        /// flags are also optionally available. Defaults: format:BC2_UNORM_SRGB texCompFlag:DEFAULT ddsFlags: FORCE_DX10_EXT
        /// filterFlags: LINEAR
        /// </summary>
        /// <returns>DDS texture as bytes.</returns>
        public static byte[] MakeTextureFromPixelData(Byte4[] pixels, int width, int height, int? scaleX = null, int? scaleY = null,
            DXGI_FORMAT format = DXGI_FORMAT.BC2_UNORM_SRGB, TEX_COMPRESS_FLAGS texCompFlag = TEX_COMPRESS_FLAGS.DEFAULT, DDS_FLAGS ddsFlags = DDS_FLAGS.FORCE_DX10_EXT,
            TEX_FILTER_FLAGS filterFlags = TEX_FILTER_FLAGS.LINEAR) {
            /* For some damn reason the System.Drawing.Common is a NuGet dll. Something something windows only something */
            Bitmap img = new(width, height);
            for (int x = 0; x < img.Width; x++) {
                for (int y = 0; y < img.Height; y++) {
                    Byte4 color = pixels[(y * img.Width) + x];
                    Color pixelColor = Color.FromArgb(color.w, color.x, color.y, color.z);
                    img.SetPixel(x, y, pixelColor);
                }
            }
            /* Bitmap only supports saving to a file or a stream. Let's just save to a stream and get the stream as and array */
            byte[] pngBytes;
            using (MemoryStream stream = new()) {
                img.Save(stream, ImageFormat.Png);
                pngBytes = stream.ToArray();
            }

            /* pin the array to memory so the garbage collector can't mess with it, */
            GCHandle pinnedArray = GCHandle.Alloc(pngBytes, GCHandleType.Pinned);
            ScratchImage sImage = TexHelper.Instance.LoadFromWICMemory(pinnedArray.AddrOfPinnedObject(), pngBytes.Length, WIC_FLAGS.DEFAULT_SRGB);

            if (scaleX != null && scaleY != null)
                sImage = sImage.Resize(0, scaleX.Value, scaleY.Value, filterFlags);

            sImage = sImage.Compress(format, texCompFlag, 0.5f);
            sImage.OverrideFormat(format);

            /* Save the DDS to memory stream and then read the stream into a byte array. */
            byte[] bytes;
            using (UnmanagedMemoryStream uStream = sImage.SaveToDDSMemory(ddsFlags)) {
                bytes = new byte[uStream.Length];
                uStream.Read(bytes);
            }

            pinnedArray.Free(); //We have to manually free pinned stuff, or it will never be collected.
            return bytes;

        }

    }
}
