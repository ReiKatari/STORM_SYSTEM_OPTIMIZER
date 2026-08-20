using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public class BenchmarkResult
    {
        public string ComponentName { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public string ScoreText { get; set; } = string.Empty;
        public double NumericScore { get; set; }
        public string Rating { get; set; } = "Отлично"; // Отлично, Хорошо, Средне
        public string Details { get; set; } = string.Empty;
        public string StatusColor => Rating == "Отлично" ? "#10B981" : (Rating == "Хорошо" ? "#38BDF8" : "#F59E0B");
    }

    public class HardwareBenchmarkService
    {
        private static HardwareBenchmarkService? _instance;
        public static HardwareBenchmarkService Instance => _instance ??= new HardwareBenchmarkService();

        private CancellationTokenSource? _stressCts;

        public bool IsStressTestingRunning => _stressCts != null && !_stressCts.IsCancellationRequested;

        public void CancelStressTest()
        {
            _stressCts?.Cancel();
        }

        public async Task<BenchmarkResult> RunCpuBenchmarkAsync(IProgress<double>? progress = null)
        {
            return await Task.Run(() =>
            {
                int cores = Environment.ProcessorCount;
                long totalOps = 0;
                var sw = Stopwatch.StartNew();
                int durationMs = 3000;

                Parallel.For(0, cores, i =>
                {
                    long localOps = 0;
                    var localSw = Stopwatch.StartNew();
                    using var sha = SHA256.Create();
                    byte[] buffer = new byte[1024];
                    new Random(i).NextBytes(buffer);

                    while (localSw.ElapsedMilliseconds < durationMs)
                    {
                        buffer = sha.ComputeHash(buffer);
                        localOps++;
                    }

                    Interlocked.Add(ref totalOps, localOps);
                });

                sw.Stop();
                double opsPerSec = totalOps / (sw.ElapsedMilliseconds / 1000.0);
                double cpuScore = Math.Round(opsPerSec / 150.0);

                string rating = cpuScore > 8000 ? "Отлично" : (cpuScore > 4000 ? "Хорошо" : "Средне");

                return new BenchmarkResult
                {
                    ComponentName = "Центральный процессор (CPU)",
                    MetricName = "Производительность вычислений",
                    ScoreText = $"{cpuScore:N0} STORM Pts",
                    NumericScore = cpuScore,
                    Rating = rating,
                    Details = $"Задействовано ядер/потоков: {cores} • Операций криптографии/с: {opsPerSec:N0}"
                };
            });
        }

        public async Task<BenchmarkResult> RunRamBenchmarkAsync(IProgress<double>? progress = null)
        {
            return await Task.Run(() =>
            {
                int bufferSize = 64 * 1024 * 1024; // 64 MB chunk
                int iterations = 16; // 1 GB total processed
                byte[] src = new byte[bufferSize];
                byte[] dst = new byte[bufferSize];
                new Random().NextBytes(src);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    Buffer.BlockCopy(src, 0, dst, 0, bufferSize);
                    progress?.Report((i + 1) / (double)iterations * 100.0);
                }
                sw.Stop();

                double totalGb = (bufferSize * (double)iterations) / (1024.0 * 1024.0 * 1024.0);
                double speedGbps = totalGb / (sw.ElapsedMilliseconds / 1000.0);

                string rating = speedGbps > 15.0 ? "Отлично" : (speedGbps > 8.0 ? "Хорошо" : "Средне");

                return new BenchmarkResult
                {
                    ComponentName = "Оперативная память (RAM)",
                    MetricName = "Скорость копирования блоков",
                    ScoreText = $"{speedGbps:F2} ГБ/с",
                    NumericScore = speedGbps,
                    Rating = rating,
                    Details = $"Обработано 1024 МБ • Задержка доступа: {sw.ElapsedMilliseconds / (double)iterations:F2} мс"
                };
            });
        }

        public async Task<BenchmarkResult> RunDiskBenchmarkAsync(string driveLetter, IProgress<double>? progress = null)
        {
            return await Task.Run(() =>
            {
                string targetDir = Path.Combine(driveLetter.TrimEnd('\\') + "\\", "STORM_BENCHMARK_TMP");
                string testFile = Path.Combine(targetDir, "bench_data.dat");
                int totalMb = 120;
                byte[] chunk = new byte[4 * 1024 * 1024]; // 4 MB chunk
                new Random().NextBytes(chunk);
                int chunksCount = totalMb / 4;

                double writeSpeed = 0;
                double readSpeed = 0;

                try
                {
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    // Write test
                    var swWrite = Stopwatch.StartNew();
                    using (var fs = new FileStream(testFile, FileMode.Create, FileAccess.Write, FileShare.None, chunk.Length, FileOptions.WriteThrough))
                    {
                        for (int i = 0; i < chunksCount; i++)
                        {
                            fs.Write(chunk, 0, chunk.Length);
                            progress?.Report((i + 1) / (double)(chunksCount * 2) * 100.0);
                        }
                    }
                    swWrite.Stop();
                    writeSpeed = totalMb / (swWrite.ElapsedMilliseconds / 1000.0);

                    // Read test
                    var swRead = Stopwatch.StartNew();
                    using (var fs = new FileStream(testFile, FileMode.Open, FileAccess.Read, FileShare.None, chunk.Length, FileOptions.SequentialScan))
                    {
                        byte[] readBuf = new byte[chunk.Length];
                        for (int i = 0; i < chunksCount; i++)
                        {
                            fs.Read(readBuf, 0, readBuf.Length);
                            progress?.Report(50.0 + ((i + 1) / (double)(chunksCount * 2) * 50.0));
                        }
                    }
                    swRead.Stop();
                    readSpeed = totalMb / (swRead.ElapsedMilliseconds / 1000.0);
                }
                finally
                {
                    try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
                    try { if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true); } catch { }
                }

                double avgSpeed = (writeSpeed + readSpeed) / 2.0;
                string rating = avgSpeed > 800 ? "Отлично (NVMe PCIe 4.0)" : (avgSpeed > 350 ? "Хорошо (SATA SSD)" : "Средне (HDD)");

                return new BenchmarkResult
                {
                    ComponentName = $"Накопитель {driveLetter}",
                    MetricName = "Скорость последовательного ввода/вывода",
                    ScoreText = $"Чтение: {readSpeed:F0} МБ/с • Запись: {writeSpeed:F0} МБ/с",
                    NumericScore = avgSpeed,
                    Rating = rating,
                    Details = $"Общий тест 120 МБ • Средняя скорость: {avgSpeed:F0} МБ/с"
                };
            });
        }

        public async Task RunSafeStressTestAsync(int durationSeconds, Action<int, double, string> onProgress, Action<bool, string> onCompleted)
        {
            _stressCts = new CancellationTokenSource();
            var token = _stressCts.Token;

            await Task.Run(() =>
            {
                int cores = Environment.ProcessorCount;
                var sw = Stopwatch.StartNew();
                bool abortedSafety = false;
                string finishReason = "Стресс-тест успешно завершен. Система работает стабильно!";

                try
                {
                    Parallel.For(0, cores, new ParallelOptions { CancellationToken = token }, (i, loopState) =>
                    {
                        var localRnd = new Random(i);
                        byte[] data = new byte[2048];
                        localRnd.NextBytes(data);
                        using var sha = SHA256.Create();

                        while (sw.Elapsed.TotalSeconds < durationSeconds && !token.IsCancellationRequested)
                        {
                            // Compute intensive math & hash
                            data = sha.ComputeHash(data);

                            if (i == 0 && sw.ElapsedMilliseconds % 500 < 20)
                            {
                                int elapsedSec = (int)sw.Elapsed.TotalSeconds;
                                double currentTemp = HardwareTemperatureService.Instance.GetCpuTemperature();

                                onProgress(elapsedSec, currentTemp, $"Тестирование всех {cores} потоков... Прошло {elapsedSec}/{durationSeconds} с • Температура: {currentTemp:F0} °C");

                                // Safety thermal limiter (88 °C)
                                if (currentTemp >= 88.0)
                                {
                                    abortedSafety = true;
                                    finishReason = $"Стресс-тест автоматически остановлен для безопасности: температура CPU достигла {currentTemp:F0} °C!";
                                    loopState.Stop();
                                    break;
                                }
                            }
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    finishReason = "Стресс-тест остановлен пользователем.";
                }
                finally
                {
                    _stressCts = null;
                }

                onCompleted(!abortedSafety, finishReason);
            });
        }
    }
}
