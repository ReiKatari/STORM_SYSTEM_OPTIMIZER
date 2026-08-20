using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
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
                bool win32Ok = NativeMethods.DnsFlushResolverCache();
                if (win32Ok) return true;
            }
            catch { }

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

        public bool OptimizeTcpSettings()
        {
            try
            {
                // 1. Netsh TCP / IP Optimizations
                RunNetshCommand("int tcp set global autotuninglevel=normal");
                RunNetshCommand("int tcp set global rss=enabled");
                RunNetshCommand("int tcp set global rsc=enabled");
                RunNetshCommand("int tcp set global timestamps=disabled");
                RunNetshCommand("int tcp set global ecncapability=enabled");
                RunNetshCommand("int tcp set global nonsackrttresiliency=disabled");
                RunNetshCommand("int tcp set global initialRto=2000");
                RunNetshCommand("int ip set global taskoffload=enabled");
                RunNetshCommand("int tcp set supplemental template=custom congestionprovider=ctcp");

                // 2. Multimedia / Network Throttling in Registry
                using (var sysProfileKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (sysProfileKey != null)
                    {
                        sysProfileKey.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        sysProfileKey.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    }
                }

                // 3. QoS NonBestEffortLimit (0% Bandwidth reservation)
                using (var qosKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Psched"))
                {
                    if (qosKey != null)
                    {
                        qosKey.SetValue("NonBestEffortLimit", 0, RegistryValueKind.DWord);
                    }
                }

                // 4. DNS Cache Parameters
                using (var dnsKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters"))
                {
                    if (dnsKey != null)
                    {
                        dnsKey.SetValue("MaxCacheTtl", 86400, RegistryValueKind.DWord);
                        dnsKey.SetValue("MaxNegativeCacheTtl", 5, RegistryValueKind.DWord);
                        dnsKey.SetValue("NetFailureCacheTime", 0, RegistryValueKind.DWord);
                        dnsKey.SetValue("NegativeSOACacheTime", 0, RegistryValueKind.DWord);
                    }
                }

                // 5. Global TCPIP Tweaks
                using (var tcpKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"))
                {
                    if (tcpKey != null)
                    {
                        tcpKey.SetValue("DefaultTTL", 64, RegistryValueKind.DWord);
                        tcpKey.SetValue("EnableTCPA", 1, RegistryValueKind.DWord);
                        tcpKey.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                        tcpKey.SetValue("TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
                        tcpKey.SetValue("SynAttackProtect", 1, RegistryValueKind.DWord);
                    }
                }

                // 6. Network Interface Gaming Tweaks (TCPNoDelay, TcpAckFrequency)
                using (var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true))
                {
                    if (interfacesKey != null)
                    {
                        foreach (string subKeyName in interfacesKey.GetSubKeyNames())
                        {
                            using var adapterKey = interfacesKey.OpenSubKey(subKeyName, true);
                            if (adapterKey != null)
                            {
                                adapterKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                adapterKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                adapterKey.SetValue("TcpDelAckTicks", 0, RegistryValueKind.DWord);
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SetDnsServers(string primaryDns, string? secondaryDns = null)
        {
            try
            {
                // Set DNS for all active IPv4 adapters via netsh
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetAdapter | Where-Object {{ $_.Status -eq 'Up' }} | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses ('{primaryDns}'{(string.IsNullOrEmpty(secondaryDns) ? "" : $",'{secondaryDns}'")}) }}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);

                FlushDnsCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ResetDnsToDhcp()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetAdapter | Where-Object { $_.Status -eq 'Up' } | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses }\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(5000);

                FlushDnsCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> MeasurePingAsync(string host = "1.1.1.1")
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 1500);
                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch { }
            return -1;
        }

        public async Task<Dictionary<string, long>> BenchmarkDnsServersAsync()
        {
            var servers = new Dictionary<string, string>
            {
                { "Cloudflare (1.1.1.1)", "1.1.1.1" },
                { "Google (8.8.8.8)", "8.8.8.8" },
                { "Quad9 (9.9.9.9)", "9.9.9.9" },
                { "AdGuard (94.140.14.14)", "94.140.14.14" }
            };

            var results = new Dictionary<string, long>();
            foreach (var kv in servers)
            {
                long latency = await MeasurePingAsync(kv.Value);
                results[kv.Key] = latency;
            }
            return results;
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
