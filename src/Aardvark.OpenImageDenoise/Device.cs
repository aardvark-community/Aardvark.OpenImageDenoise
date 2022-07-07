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

        public Version Version
        {
            get
            {
                // two decimal digits per component
                var version = OidnAPI.oidnGetDevice1i(m_device, "version");
                var major = version / 10000;
                var minor = (version / 100) - major * 100;
                var patch = version % 100;
                return new Version(major, minor, patch);
            }
        }

        /// <summary>
        /// Denoises the input image (required to have 3 or 4 channels, only RGB are considered).
        /// Result will always be 3-channel float image
        /// inputScale: a value of 1 should represent 100cd/m² -> inputScale can be used to setup this (NaN for auto calculation)
        /// </summary>
        public PixImage<float> Denoise(PixImage<float> img, float inputScale = float.NaN)
        {
            var output = new PixImage<float>(img.Size.X, img.Size.Y, 3);
            Denoise(img, null, null, output, inputScale);
            return output;
        }

        /// <summary>
        /// Denoises the input image to the output image buffer.
        /// Input and output are required to have equal size and 3 or 4 channels (process will only consider RGB). 
        /// inputScale: a value of 1 should represent 100cd/m² -> inputScale can be used to setup this (NaN for auto calculation)
        /// </summary>
        public void Denoise(PixImage<float> img, PixImage<float> outImage, float inputScale = float.NaN)
        {
            Denoise(img, null, null, outImage);
        }

        /// <summary>
        /// Denoises the input image to the output image buffer.
        /// Input and output are required to have equal size and 3 or 4 channels (process will only consider RGB). 
        /// Optinal albedo and normal images can be used, otherwise set them to null.
        /// inputScale: a value of 1 should represent 100cd/m² -> inputScale can be used to setup this (NaN for auto calculation)
        /// </summary>
        public void Denoise(PixImage<float> color, PixImage<float> albedo, PixImage<float> normal, PixImage<float> outImage, float inputScale = float.NaN)
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
            if (!inputScale.IsNaN()) OidnAPI.oidnSetFilter1f(filter, "inputScale", inputScale);
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

        /// <summary>
        /// Denoises the input image as lightmap to the output image buffer.
        /// Input and output are required to have equal size and 3 or 4 channels (process will only consider RGB). 
        /// inputScale: a value of 1 should a luminance(?) of 100cd/m² -> inputScale can be used to setup this (NaN for auto calculation)
        /// </summary>
        public void DenoiseLightmap(PixImage<float> color, PixImage<float> outImage, bool directional = false, float inputScale = float.NaN)
        {
            if (color.ChannelCount < 3 || color.ChannelCount > 4) throw new ArgumentException("Image must have 3 or 4 channels");
            if (outImage.ChannelCount < 3 || outImage.ChannelCount > 4) throw new ArgumentException("Output image must have 3 or 4 channels");

            var width = color.Size.X;
            var height = color.Size.Y;

            if (outImage.Size.X != width || outImage.Size.Y != height) throw new ArgumentException("Ouput image size does not match with input");

            // Oidn only supports 3 channel images -> use pixelStride 
            var pixelStride = color.ChannelCount * 4;
            var outPixelStride = outImage.ChannelCount * 4;

            var colorPtr = GCHandle.Alloc(color.Data, GCHandleType.Pinned);
            var outputPtr = GCHandle.Alloc(outImage.Data, GCHandleType.Pinned);

            // Create a denoising filter
            var filter = OidnAPI.oidnNewFilter(m_device, "RTLightmap"); // optimized filter for HDR lightmaps
            OidnAPI.oidnSetSharedFilterImage(filter, "color", colorPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, pixelStride, pixelStride * width);
            OidnAPI.oidnSetSharedFilterImage(filter, "output", outputPtr.AddrOfPinnedObject(), ImageFormat.Float3, width, height, 0, outPixelStride, outPixelStride * width);
            if (!inputScale.IsNaN()) OidnAPI.oidnSetFilter1f(filter, "inputScale", inputScale); // default=NaN
            if (directional) OidnAPI.oidnSetFilter1b(filter, "directional", true); // default=false
            OidnAPI.oidnCommitFilter(filter);

            // Filter the image
            OidnAPI.oidnExecuteFilter(filter);

            colorPtr.Free();
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
            m_device = IntPtr.Zero;
        }
    }
}
