using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Aardvark.OpenImageDenoise
{
    public enum DeviceFlags
    {
        Default = 0, //OIDN_DEVICE_TYPE_DEFAULT
        CPU = 1, //OIDN_DEVICE_TYPE_CPU,
    }

    public enum ImageFormat
    {
        Float = 1,
        Float2 = 2,
        Float3 = 3,
        Float4 = 4,
    }

    public enum Error
    {
        None = 0,
        Unknown = 1,
        InvalidArgument = 2,
        InvalidOperation = 3,
        OutOfMemory = 4,
        UnsupportedHardware = 5,
        Cancelled = 6,
    }

    public class OidnAPI
    {
        [DllImport("OpenImageDenoise.dll")]
        public static extern IntPtr oidnNewDevice(DeviceFlags flags);

        [DllImport("OpenImageDenoise.dll", CharSet=CharSet.Ansi)]
        public static extern void oidnSetDevice1i(IntPtr device, string name, int value);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern int oidnGetDevice1i(IntPtr device, string name);

        [DllImport("OpenImageDenoise.dll")]
        public static extern void oidnCommitDevice(IntPtr device);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr oidnNewFilter(IntPtr device, string type);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern void oidnSetSharedFilterImage(IntPtr filter, string name, IntPtr data, ImageFormat fmt, int width, int height, int byteOffset, int pixelStride, int rowStride);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilter1b(IntPtr filter, string name, bool value);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilter1f(IntPtr filter, string name, float value);

        [DllImport("OpenImageDenoise.dll", CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilter1i(IntPtr filter, string name, int value);

        [DllImport("OpenImageDenoise.dll")]
        public static extern void oidnCommitFilter(IntPtr filter);

        [DllImport("OpenImageDenoise.dll")]
        public static extern void oidnExecuteFilter(IntPtr filter);

        [DllImport("OpenImageDenoise.dll")]
        public static extern Error oidnGetDeviceError(IntPtr device, out string outMessage);
        
        [DllImport("OpenImageDenoise.dll")]
        public static extern void oidnReleaseFilter(IntPtr filter);

        [DllImport("OpenImageDenoise.dll")]
        public static extern void oidnReleaseDevice(IntPtr device);
        
    }
}
