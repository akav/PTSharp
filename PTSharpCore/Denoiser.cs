using System;
using System.Runtime.InteropServices;
using static PTSharpCore.OIDN;

namespace PTSharpCore
{
    public class Denoiser : IDisposable
    {   
        public IntPtr oidnFilter;
        public IntPtr oidnDevice;
        public int cpuNum;

        public Denoiser()
        {
        }

        public Denoiser(OIDNDeviceType flags, int numThreads)
        {
            var device = OIDN.oidnNewDevice(flags);

            if (numThreads > 0)
                OIDN.oidnSetDeviceInt(device, "numThreads", numThreads);

            OIDN.oidnCommitDevice(device);

            cpuNum = numThreads > 0 ? numThreads : OIDN.oidnGetDeviceInt(device, "numThreads");
            oidnDevice = device;
        }

        public void Dispose()
        {
            oidnReleaseDevice(oidnDevice);
        }
    }
}
