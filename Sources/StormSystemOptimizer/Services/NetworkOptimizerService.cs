using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace StormSystemOptimizer.Services
{
    public class NetworkOptimizerService
    {
        private static NetworkOptimizerService? _instance;
        public static NetworkOptimizerService Instance => _instance ??= new NetworkOptimizerService();

        private NetworkOptimizerService() { }

        public bool FlushDnsCache()
        {
            try
            {
                return NativeMethods.DnsFlushResolverCache();
            }
            catch
            {
                // Fallback to ipconfig
                try
                {
                    var psi = new ProcessStartInfo("ipconfig.exe", "/flushdns")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(3000);
                    return true;
                }
                catch { return false; }
            }
        }

        public bool OptimizeTcpSettings()
        {
            try
            {
                RunNetshCommand("int tcp set global autotuninglevel=normal");
                RunNetshCommand("int tcp set global rss=enabled");
                RunNetshCommand("int tcp set global timestamps=disabled");
                RunNetshCommand("int tcp set supplemental template=custom congestionprovider=ctcp");

                // Network Throttling Index in Registry
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile");
                if (key != null)
                {
                    key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                }

                return true;
            }
            catch { return false; }
        }

        public async Task<long> MeasurePingAsync(string host = "1.1.1.1")
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch { }
            return -1;
        }

        private void RunNetshCommand(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh.exe", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(2000);
            }
            catch { }
        }
    }
}
