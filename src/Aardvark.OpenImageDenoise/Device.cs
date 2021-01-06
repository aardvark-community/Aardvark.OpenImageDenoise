using System;
using System.Runtime.InteropServices;
using Aardvark.Base;

namespace Aardvark.OpenImageDenoise
{
    public class Device : IDisposable
    {
        IntPtr m_device;
        int m_numThreads;

        public Device()
            : this (DeviceFlags.Default, -1)
        {
        }

        public Device(DeviceFlags flags, int numThreads)
        {
            var device = OidnAPI.oidnNewDevice(flags);

            if (numThreads > 0)
                OidnAPI.oidnSetDevice1i(device, "numThreads", numThreads);

            OidnAPI.oidnCommitDevice(device);

            m_numThreads = numThreads > 0 ? numThreads : OidnAPI.oidnGetDevice1i(device, "numThreads");

            m_device = device;
        }

        public int NumThreads
        {
            get { return m_numThreads; }
            set
            {
                OidnAPI.oidnSetDevice1i(m_device, "numThreads", value);

                // update settings ???
                OidnAPI.oidnCommitDevice(m_device);

                if (value > 0)
                    m_numThreads = value;
                else
                    m_numThreads = OidnAPI.oidnGetDevice1i(m_device, "numThreads");
            }
        }

        /// <summary>
        /// Result will always be 3-channel float image
        /// </summary>
        public PixImage<float> Denoise(PixImage<float> img)
        {
            var output = new PixImage<float>(img.Size.X, img.Size.Y, 3);
            Denoise(img, null, null, output);
            return output;
        }

        /// <summary>
        /// Denoises the input image to the output image buffer
        /// </summary>
        public void Denoise(PixImage<float> img, PixImage<float> outImage)
        {
            Denoise(img, null, null, outImage);
        }

        /// <summary>
        /// Denoises the input image with optinal albedo and normal images to the output image buffer
        /// hdrScale: a value of 1 should represent 100cd/m² -> hdrScale can be used to setup this (NaN for auto calculation)
        /// </summary>
        public void Denoise(PixImage<float> color, PixImage<float> albedo, PixImage<float> normal, PixImage<float> outImage, float hdrScale = float.NaN)
        {
            if (color.ChannelCount < 3 || color.ChannelCount > 4) throw new ArgumentException("Image must have 3 or 4 channels");
            if (albedo != null && (albedo.ChannelCount < 3 || albedo.ChannelCount > 4)) throw new ArgumentException("Image must have 3 or 4 channels");
            if (normal != null && (normal.ChannelCount < 3 || normal.ChannelCount > 4)) throw new ArgumentException("Image must have 3 or 4 channels");
            if (outImage.ChannelCount < 3 || outImage.ChannelCount > 4) throw new ArgumentException("Output image must have 3 or 4 channels");

            var width = color.Size.X;
            var height = color.Size.Y;

            if (albedo != null && (albedo.Size.X != width || albedo.Size.Y != height)) throw new ArgumentException("Albedo image size does not match with input");
            if (normal != null && (normal.Size.X != width || normal.Size.Y != height)) throw new ArgumentException("Normal image size does not match with input");
            if (outImage.Size.X != width || outImage.Size.Y != height) throw new ArgumentException("Ouput image size does not match with input");

            // Oidn only supports 3 channel images -> use pixelStride 
            var pixelStride = color.ChannelCount * 4;
            var outPixelStride = outImage.ChannelCount * 4;
            var albedoPixelStride = albedo?.ChannelCount * 4 ?? 0;
            var normalPixelStride = normal?.ChannelCount * 4 ?? 0;

            var colorPtr = GCHandle.Alloc(color.Data, GCHandleType.Pinned);
            var albedoPtr = albedo != null ? GCHandle.Alloc(albedo.Data, GCHandleType.Pinned) : default;
            var normalPtr = normal != null ? GCHandle.Alloc(normal.Data, GCHandleType.Pinned) : default;
            var outputPtr = GCHandle.Alloc(outImage.Data, GCHandleType.Pinned);

            // Create a denoising filter
            var filter = OidnAPI.oidnNewFilter(m_device, "RT"); // generic ray tracing filter
            OidnAPI.oidnSetSharedFilterImage(filter, "color", colorPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, pixelStride, pixelStride * width);
            if (albedo != null)  OidnAPI.oidnSetSharedFilterImage(filter, "albedo", albedoPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, albedoPixelStride, albedoPixelStride * width);
            if (normal != null) OidnAPI.oidnSetSharedFilterImage(filter, "normal", normalPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, normalPixelStride, normalPixelStride * width);
            OidnAPI.oidnSetSharedFilterImage(filter, "output", outputPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, outPixelStride, outPixelStride * width);
            OidnAPI.oidnSetFilter1b(filter, "hdr", true); // image is HDR
            if (!hdrScale.IsNaN()) OidnAPI.oidnSetFilter1f(filter, "hdrScale", hdrScale); // image is HDR
            OidnAPI.oidnCommitFilter(filter);

            // Filter the image
            OidnAPI.oidnExecuteFilter(filter);

            colorPtr.Free();
            if (albedo != null) albedoPtr.Free();
            if (normal != null) normalPtr.Free();
            outputPtr.Free();

            // Check for errors
            if (OidnAPI.oidnGetDeviceError(m_device, out var errorMessage) != Error.None)
                Report.Warn("Error: {0}", errorMessage);

            // Cleanup
            OidnAPI.oidnReleaseFilter(filter);
        }

        public void Dispose()
        {
            OidnAPI.oidnReleaseDevice(m_device);
        }
    }
}
