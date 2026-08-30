using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class FirewallRuleItem
    {
        public string Name { get; set; } = string.Empty;
        public string ApplicationPath { get; set; } = string.Empty;
        public string Direction { get; set; } = "Входящее"; // In / Out
        public string Action { get; set; } = "Разрешить";   // Allow / Block
        public bool IsOrphaned { get; set; }
    }

    public class FirewallAuditService
    {
        private static FirewallAuditService? _instance;
        public static FirewallAuditService Instance => _instance ??= new FirewallAuditService();

        public async Task<List<FirewallRuleItem>> ScanFirewallRulesAsync(IProgress<string>? progress = null)
        {
            return await Task.Run(() =>
            {
                var rules = new List<FirewallRuleItem>();
                progress?.Report("Чтение правил Брандмауэра Windows...");

                try
                {
                    // Use netsh advfirewall or PowerShell Get-NetFirewallRule to extract active application rules
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"Get-NetFirewallRule -Enabled True | Where-Object { $_.Direction -eq 'Inbound' -or $_.Direction -eq 'Outbound' } | Select-Object -Property DisplayName, Direction, Action, @{Name='App';Expression={(Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $_).Program}} | ConvertTo-Json -Compress\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();

                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            // Parse JSON simple structure or fallback to lines
                            if (output.StartsWith("["))
                            {
                                var parsed = System.Text.Json.JsonDocument.Parse(output);
                                foreach (var el in parsed.RootElement.EnumerateArray())
                                {
                                    string name = el.TryGetProperty("DisplayName", out var pName) ? pName.GetString() ?? "" : "";
                                    string dir = el.TryGetProperty("Direction", out var pDir) ? pDir.GetString() ?? "" : "";
                                    string act = el.TryGetProperty("Action", out var pAct) ? pAct.GetString() ?? "" : "";
                                    string app = el.TryGetProperty("App", out var pApp) ? pApp.GetString() ?? "" : "";

                                    if (string.IsNullOrWhiteSpace(name)) continue;

                                    bool isOrphan = false;
                                    if (!string.IsNullOrWhiteSpace(app) && app != "Any")
                                    {
                                        string expanded = Environment.ExpandEnvironmentVariables(app);
                                        if (!File.Exists(expanded))
                                        {
                                            isOrphan = true;
                                        }
                                    }

                                    rules.Add(new FirewallRuleItem
                                    {
                                        Name = name,
                                        ApplicationPath = string.IsNullOrWhiteSpace(app) ? "Системный сервис" : app,
                                        Direction = dir.Contains("In") ? "Входящее" : "Исходящее",
                                        Action = act.Contains("Allow") ? "Разрешить" : "Заблокировать",
                                        IsOrphaned = isOrphan
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FirewallAuditService] Scan Error: {ex.Message}");
                }

                progress?.Report($"Найдено правил: {rules.Count}, сиротских записей: {rules.FindAll(r => r.IsOrphaned).Count}");
                return rules;
            });
        }

        public async Task<int> PurgeOrphanedRulesAsync()
        {
            return await Task.Run(() =>
            {
                int removedCount = 0;
                try
                {
                    string script = @"
$rules = Get-NetFirewallRule -Enabled True
$removed = 0
foreach ($r in $rules) {
    $app = (Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $r).Program
    if ($app -and $app -ne 'Any') {
        $exp = [System.Environment]::ExpandEnvironmentVariables($app)
        if (-not (Test-Path $exp)) {
            Remove-NetFirewallRule -Name $r.Name -ErrorAction SilentlyContinue
            $removed++
        }
    }
}
Write-Output $removed
";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string outStr = proc.StandardOutput.ReadToEnd().Trim();
                        proc.WaitForExit();
                        int.TryParse(outStr, out removedCount);
                    }
                }
                catch { }

                return removedCount;
            });
        }
    }
}
