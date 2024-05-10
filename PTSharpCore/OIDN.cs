using System;
using System.Runtime.InteropServices;


namespace PTSharpCore
{
    public class OIDN
    {
        public enum OIDNDeviceType
        {
            OIDN_DEVICE_TYPE_DEFAULT = 0,
            OIDN_DEVICE_TYPE_CPU = 1,
            ODIN_DEVICE_TYPE_SYCL = 2,
            OIDN_DEVICE_TYPE_CUDA = 3,
            OIDN_DEVICE_TYPE_HIP = 4,
            ODIN_DEVICE_TYPE_METAL = 5
        }
           
        public enum OIDNImageFormat
        {
            OIDN_FORMAT_UNDEFINED = 0,
            OIDN_FORMAT_FLOAT = 1,
            OIDN_FORMAT_FLOAT2 = 2,
            OIDN_FORMAT_FLOAT3 = 3,
            OIDN_FORMAT_FLOAT4 = 4,
            OIDN_FORMAT_HALF = 5,
            OIDN_FORMAT_HALF2 = 6,
            OIDN_FORMAT_HALF3 = 7,
            OIDN_FORMAT_HALF4 = 8
        }

        public enum OIDNError
        {
            OIDN_ERROR_NONE = 0,
            OIDN_ERROR_UNKNOWN = 1,
            OIDN_ERROR_INVALID_ARGUMENT = 2,
            OIDN_ERROR_INVALID_OPERATION = 3,
            OIDN_ERROR_OUT_OF_MEMORY = 4,
            OIDN_ERROR_UNSUPPORTED_HARDWARE = 5,
            OIDN_ERROR_CANCELLED = 6
        }

        const string oidn = "OpenImageDenoise.dll";

        [DllImport(oidn)]
        public static extern IntPtr oidnNewDevice(OIDNDeviceType type);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnSetDeviceInt(IntPtr device, string name, int value);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int oidnGetDeviceInt(IntPtr device, string name);

        [DllImport(oidn)]
        public static extern void oidnCommitDevice(IntPtr device);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr oidnNewFilter(IntPtr device, string type);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr oidnNewBuffer(IntPtr device, int size);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilterBool(IntPtr filter, string name, bool value);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilterFloat(IntPtr filter, string name, float value);

        [DllImport(oidn)]
        public static extern void oidnCommitFilter(IntPtr filter);

        [DllImport(oidn)]
        public static extern void oidnExecuteFilter(IntPtr filter);

        [DllImport(oidn)]
        public static extern OIDNError oidnGetDeviceError(IntPtr device, out string outMessage);

        [DllImport(oidn)]
        public static extern void oidnReleaseFilter(IntPtr filter);

        [DllImport(oidn)]
        public static extern void oidnReleaseDevice(IntPtr device);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnReleaseBuffer(IntPtr buffer);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilterImage(IntPtr filter, string oidnSetFilterImage);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void oidnSetFilterImage(IntPtr filter, string name, IntPtr ptr, OIDNImageFormat format,
            int width, int height, int byteOffset, int bytePixelStride, int byteRowStride);

        [DllImport(oidn, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr oidnGetBufferData(IntPtr buffer);
    }
}
