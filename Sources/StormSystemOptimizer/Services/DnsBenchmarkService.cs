using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class DnsServerItem
    {
        public string ProviderName { get; set; } = string.Empty;
        public string PrimaryDns { get; set; } = string.Empty;
        public string SecondaryDns { get; set; } = string.Empty;
        public string PingText { get; set; } = "— мс";
        public long PingMs { get; set; } = 999;
        public string Features { get; set; } = string.Empty;
        public string StatusColor => PingMs < 20 ? "#10B981" : (PingMs < 45 ? "#38BDF8" : "#F59E0B");
    }

    public class DnsBenchmarkService
    {
        private static DnsBenchmarkService? _instance;
        public static DnsBenchmarkService Instance => _instance ??= new DnsBenchmarkService();

        private DnsBenchmarkService() { }

        public List<DnsServerItem> GetDefaultDnsProviders()
        {
            return new List<DnsServerItem>
            {
                new DnsServerItem { ProviderName = "Cloudflare DNS (1.1.1.1)", PrimaryDns = "1.1.1.1", SecondaryDns = "1.0.0.1", Features = "Максимальная скорость • Приватность" },
                new DnsServerItem { ProviderName = "Google Public DNS (8.8.8.8)", PrimaryDns = "8.8.8.8", SecondaryDns = "8.8.4.4", Features = "Глобальная стабильность • Высокий аптайм" },
                new DnsServerItem { ProviderName = "Quad9 Secure DNS (9.9.9.9)", PrimaryDns = "9.9.9.9", SecondaryDns = "149.112.112.112", Features = "Встроенная защита от фишинга и малвари" },
                new DnsServerItem { ProviderName = "AdGuard DNS (Anti-Ad)", PrimaryDns = "94.140.14.14", SecondaryDns = "94.140.15.15", Features = "Блокировка баннеров, трекеров и рекламы" }
            };
        }

        public async Task<List<DnsServerItem>> BenchmarkAllDnsAsync()
        {
            var list = GetDefaultDnsProviders();

            await Task.Run(() =>
            {
                using var ping = new Ping();

                Parallel.ForEach(list, item =>
                {
                    try
                    {
                        var reply = ping.Send(item.PrimaryDns, 1200);
                        if (reply.Status == IPStatus.Success)
                        {
                            item.PingMs = reply.RoundtripTime;
                            item.PingText = $"{reply.RoundtripTime} мс";
                        }
                        else
                        {
                            item.PingMs = 999;
                            item.PingText = "Таймаут";
                        }
                    }
                    catch
                    {
                        item.PingMs = 999;
                        item.PingText = "Ошибка";
                    }
                });
            });

            list.Sort((a, b) => a.PingMs.CompareTo(b.PingMs));
            return list;
        }

        public async Task<bool> ApplyDnsToActiveAdapterAsync(string primaryDns, string secondaryDns)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string script = $@"
$adapter = Get-NetAdapter | Where-Object {{ $_.Status -eq 'Up' -and $_.InterfaceDescription -notlike '*Virtual*' -and $_.InterfaceDescription -notlike '*VPN*' }} | Select-Object -First 1
if ($adapter) {{
    Set-DnsClientServerAddress -InterfaceIndex $adapter.InterfaceIndex -ServerAddresses ('{primaryDns}','{secondaryDns}')
}}
";
                    var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(4000);
                    NativeMethods.DnsFlushResolverCache();
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
