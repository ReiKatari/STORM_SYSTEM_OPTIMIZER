using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class DefragService
    {
        private static DefragService? _instance;
        public static DefragService Instance => _instance ??= new DefragService();

        private DefragService() { }

        public async Task<string> AnalyzeVolumeAsync(string driveLetter)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string cleanLetter = driveLetter.TrimEnd('\\', ':');
                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = $"{cleanLetter}: /A /V",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(15000);
                        if (!string.IsNullOrWhiteSpace(output)) return output;
                    }
                }
                catch (Exception ex)
                {
                    return $"Ошибка анализа тома: {ex.Message}";
                }

                return "Анализ тома успешно завершен.";
            });
        }

        public async Task<bool> OptimizeVolumeAsync(string driveLetter, bool isSsd, Action<string>? outputCallback = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string cleanLetter = driveLetter.TrimEnd('\\', ':');
                    // For SSD: /O = perform appropriate optimization (TRIM/slab consolidation), /U = print progress
                    // For HDD: /D = defragment, /U = print progress, /V = verbose
                    string args = isSsd ? $"{cleanLetter}: /O /U /V" : $"{cleanLetter}: /D /U /V";

                    outputCallback?.Invoke($"Запуск оптимизации диска {cleanLetter}: ({(isSsd ? "Команда TRIM и оптимизация ячеек SSD" : "Глубокая дефрагментация секторов HDD")})...\n");

                    var psi = new ProcessStartInfo
                    {
                        FileName = "defrag.exe",
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        while (!p.StandardOutput.EndOfStream)
                        {
                            string? line = p.StandardOutput.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                outputCallback?.Invoke(line);
                            }
                        }
                        p.WaitForExit();
                        outputCallback?.Invoke("\n=== Оптимизация диска успешно завершена! ===");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    outputCallback?.Invoke($"\nОшибка выполнения: {ex.Message}");
                    return false;
                }

                return false;
            });
        }
    }
}
