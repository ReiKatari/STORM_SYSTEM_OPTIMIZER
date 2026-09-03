using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public enum TaskRiskLevel
    {
        Safe,
        Suspicious,
        Dangerous
    }

    public class ScheduledTaskItem
    {
        public string TaskName { get; set; } = string.Empty;
        public string TaskPath { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public string ActionCommand { get; set; } = string.Empty;
        public string State { get; set; } = "Включена";
        public bool IsEnabled { get; set; } = true;
        public bool IsWmi { get; set; } = false;
        public TaskRiskLevel RiskLevel { get; set; } = TaskRiskLevel.Safe;
        public string RiskReason { get; set; } = string.Empty;

        public string RiskBadge => RiskLevel switch
        {
            TaskRiskLevel.Dangerous => "Опасная",
            TaskRiskLevel.Suspicious => "Подозрительная",
            _ => "Штатная"
        };
        public string RiskBadgeColor => RiskLevel switch
        {
            TaskRiskLevel.Dangerous => "#EF4444",
            TaskRiskLevel.Suspicious => "#F59E0B",
            _ => "#10B981"
        };
        public string RiskBgColor => RiskLevel switch
        {
            TaskRiskLevel.Dangerous => "#26EF4444",
            TaskRiskLevel.Suspicious => "#26F59E0B",
            _ => "#1A10B981"
        };
        public string TypeBadge => IsWmi ? "WMI Триггер" : "Планировщик";
        public string TypeBadgeColor => IsWmi ? "#A855F7" : "#00D2FF";
        public string TypeBgColor => IsWmi ? "#26A855F7" : "#1A00D2FF";
    }

    public class TaskSchedulerAuditService
    {
        private static TaskSchedulerAuditService? _instance;
        public static TaskSchedulerAuditService Instance => _instance ??= new TaskSchedulerAuditService();

        public async Task<List<ScheduledTaskItem>> ScanAllTasksAndWmiAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<ScheduledTaskItem>();

                // 1. Scan Windows Task Scheduler
                ScanScheduledTasks(list);

                // 2. Scan WMI Persistence
                ScanWmiPersistence(list);

                return list.OrderByDescending(t => t.RiskLevel).ThenBy(t => t.TaskName).ToList();
            });
        }

        private void ScanScheduledTasks(List<ScheduledTaskItem> list)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/Query /FO CSV /V",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.GetEncoding(866) // OEM Russian CP
                };

                using var proc = Process.Start(psi);
                if (proc == null) return;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length <= 1) return;

                // First line is CSV header
                for (int i = 1; i < lines.Length; i++)
                {
                    var cols = ParseCsvLine(lines[i]);
                    if (cols.Count < 9) continue;

                    string taskPath = cols[0].Trim('"');
                    string taskName = Path.GetFileName(taskPath);
                    string state = cols.Count > 3 ? cols[3].Trim('"') : "Ready";
                    string author = cols.Count > 7 ? cols[7].Trim('"') : "";
                    string action = cols.Count > 8 ? cols[8].Trim('"') : "";

                    // Ignore standard safe Microsoft tasks
                    if (taskPath.StartsWith(@"\Microsoft\Windows\", StringComparison.OrdinalIgnoreCase) &&
                        !IsPathSuspicious(action))
                    {
                        continue;
                    }

                    bool isEnabled = !state.Equals("Disabled", StringComparison.OrdinalIgnoreCase) &&
                                     !state.Equals("Отключено", StringComparison.OrdinalIgnoreCase);

                    var risk = AnalyzeTaskRisk(taskPath, action, author, out string reason);

                    list.Add(new ScheduledTaskItem
                    {
                        TaskName = string.IsNullOrEmpty(taskName) ? taskPath : taskName,
                        TaskPath = taskPath,
                        Author = author,
                        Trigger = cols.Count > 5 ? cols[5].Trim('"') : "По расписанию",
                        ActionCommand = action,
                        State = isEnabled ? "Включена" : "Отключена",
                        IsEnabled = isEnabled,
                        IsWmi = false,
                        RiskLevel = risk,
                        RiskReason = reason
                    });
                }
            }
            catch { }
        }

        private void ScanWmiPersistence(List<ScheduledTaskItem> list)
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\subscription");
                scope.Connect();

                // 1. CommandLineEventConsumer
                using var searcherCmd = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM CommandLineEventConsumer"));
                foreach (ManagementObject obj in searcherCmd.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "WMI CommandLine Consumer";
                    string cmd = obj["CommandLineTemplate"]?.ToString() ?? "";

                    list.Add(new ScheduledTaskItem
                    {
                        TaskName = name,
                        TaskPath = $"WMI:CommandLine:{name}",
                        Author = "WMI Subscription",
                        Trigger = "WMI Event (Фильтр системных событий)",
                        ActionCommand = cmd,
                        State = "Активен",
                        IsEnabled = true,
                        IsWmi = true,
                        RiskLevel = TaskRiskLevel.Dangerous,
                        RiskReason = "Скрытая персистентность WMI. Часто используется майнерами и бэкдорами."
                    });
                }

                // 2. ActiveScriptEventConsumer
                using var searcherScript = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM ActiveScriptEventConsumer"));
                foreach (ManagementObject obj in searcherScript.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "WMI Script Consumer";
                    string scriptText = obj["ScriptText"]?.ToString() ?? "";

                    list.Add(new ScheduledTaskItem
                    {
                        TaskName = name,
                        TaskPath = $"WMI:ActiveScript:{name}",
                        Author = "WMI Subscription",
                        Trigger = "WMI Event (Скриптовый перехватчик)",
                        ActionCommand = scriptText.Length > 80 ? scriptText.Substring(0, 80) + "..." : scriptText,
                        State = "Активен",
                        IsEnabled = true,
                        IsWmi = true,
                        RiskLevel = TaskRiskLevel.Dangerous,
                        RiskReason = "Скрытое исполнение VBScript/JScript через WMI при системных событиях."
                    });
                }
            }
            catch { }
        }

        private TaskRiskLevel AnalyzeTaskRisk(string taskPath, string action, string author, out string reason)
        {
            reason = "Штатная задача установленного программного обеспечения.";

            string lowerAction = action.ToLowerInvariant();

            // Suspicious scripting / command interpreters
            if (lowerAction.Contains("powershell") && (lowerAction.Contains("-enc") || lowerAction.Contains("-w hidden") || lowerAction.Contains("downloadstring") || lowerAction.Contains("iex")))
            {
                reason = "Скрытое выполнение закодированного PowerShell скрипта!";
                return TaskRiskLevel.Dangerous;
            }

            if (lowerAction.Contains("mshta") || lowerAction.Contains("wscript") || lowerAction.Contains("cscript") || lowerAction.Contains("rundll32"))
            {
                reason = "Вызов системного интерпретатора скриптов без графического окна.";
                return TaskRiskLevel.Suspicious;
            }

            // Suspicious paths (Temp, AppData\Local\Temp, public)
            if (lowerAction.Contains(@"\appdata\local\temp") || lowerAction.Contains(@"\users\public\") || lowerAction.Contains(@"c:\windows\temp\"))
            {
                reason = "Исполняемый файл запускается из временного каталога (Temp/Public)!";
                return TaskRiskLevel.Dangerous;
            }

            if (taskPath.Contains("Miner", StringComparison.OrdinalIgnoreCase) ||
                taskPath.Contains("UpdateCheck", StringComparison.OrdinalIgnoreCase) && !taskPath.Contains("Google", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Нетипичное имя задачи автообновления.";
                return TaskRiskLevel.Suspicious;
            }

            return TaskRiskLevel.Safe;
        }

        private bool IsPathSuspicious(string action)
        {
            string lower = action.ToLowerInvariant();
            return lower.Contains("temp") || lower.Contains("powershell") || lower.Contains("mshta") || lower.Contains("wscript");
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var pattern = new Regex(@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
            var matches = pattern.Split(line);
            foreach (var m in matches)
            {
                result.Add(m.Trim());
            }
            return result;
        }

        public async Task<bool> ToggleTaskStateAsync(ScheduledTaskItem item)
        {
            return await Task.Run(() =>
            {
                if (item.IsWmi) return false; // WMI consumers cannot be just toggled, only deleted

                try
                {
                    string actionArg = item.IsEnabled ? "/Disable" : "/Enable";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Change /TN \"{item.TaskPath}\" {actionArg}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit();

                    if (p?.ExitCode == 0)
                    {
                        item.IsEnabled = !item.IsEnabled;
                        item.State = item.IsEnabled ? "Включена" : "Отключена";
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> DeleteTaskAsync(ScheduledTaskItem item)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (item.IsWmi)
                    {
                        // Delete WMI consumer and bindings
                        var scope = new ManagementScope(@"\\.\root\subscription");
                        scope.Connect();

                        string query = item.TaskPath.Contains("CommandLine")
                            ? $"SELECT * FROM CommandLineEventConsumer WHERE Name = '{item.TaskName}'"
                            : $"SELECT * FROM ActiveScriptEventConsumer WHERE Name = '{item.TaskName}'";

                        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            obj.Delete();
                        }
                        return true;
                    }
                    else
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = $"/Delete /TN \"{item.TaskPath}\" /F",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit();
                        return p?.ExitCode == 0;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
