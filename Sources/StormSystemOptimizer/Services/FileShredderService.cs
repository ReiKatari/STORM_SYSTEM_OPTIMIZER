using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public enum ShredAlgorithm
    {
        ZeroFill = 0,     // 1 pass of zeros
        RandomFill = 1,   // 1 pass of cryptographically random data
        DoD5220 = 2,      // 3 passes: Zeros, Ones, Random
        GutmannLite = 3,  // 7 passes
        Gutmann35 = 4     // 35 passes: Full Peter Gutmann Algorithm
    }

    public class FileShredderService
    {
        private static FileShredderService? _instance;
        public static FileShredderService Instance => _instance ??= new FileShredderService();

        public async Task<bool> ShredFileAsync(string filePath, ShredAlgorithm algorithm = ShredAlgorithm.DoD5220, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(filePath)) return false;

                try
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);

                    var fi = new FileInfo(filePath);
                    long length = fi.Length;
                    int passes = algorithm switch
                    {
                        ShredAlgorithm.ZeroFill => 1,
                        ShredAlgorithm.RandomFill => 1,
                        ShredAlgorithm.DoD5220 => 3,
                        ShredAlgorithm.GutmannLite => 7,
                        ShredAlgorithm.Gutmann35 => 35,
                        _ => 1
                    };

                    byte[] buffer = new byte[64 * 1024];

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        for (int currentPass = 1; currentPass <= passes; currentPass++)
                        {
                            if (ct.IsCancellationRequested) return false;

                            fs.Position = 0;
                            long bytesWritten = 0;

                            while (bytesWritten < length)
                            {
                                if (ct.IsCancellationRequested) return false;

                                int toWrite = (int)Math.Min(buffer.Length, length - bytesWritten);

                                switch (algorithm)
                                {
                                    case ShredAlgorithm.ZeroFill:
                                        Array.Clear(buffer, 0, toWrite);
                                        break;

                                    case ShredAlgorithm.RandomFill:
                                        RandomNumberGenerator.Fill(buffer.AsSpan(0, toWrite));
                                        break;

                                    case ShredAlgorithm.DoD5220:
                                        if (currentPass == 1) Array.Clear(buffer, 0, toWrite);
                                        else if (currentPass == 2) { for (int i = 0; i < toWrite; i++) buffer[i] = 0xFF; }
                                        else RandomNumberGenerator.Fill(buffer.AsSpan(0, toWrite));
                                        break;

                                    case ShredAlgorithm.GutmannLite:
                                    case ShredAlgorithm.Gutmann35:
                                        if (currentPass <= 4 || currentPass >= passes - 3)
                                            RandomNumberGenerator.Fill(buffer.AsSpan(0, toWrite));
                                        else
                                        {
                                            byte pattern = (byte)((currentPass * 37) % 256);
                                            for (int i = 0; i < toWrite; i++) buffer[i] = pattern;
                                        }
                                        break;
                                }

                                fs.Write(buffer, 0, toWrite);
                                bytesWritten += toWrite;

                                double prog = ((currentPass - 1.0) / passes + ((double)bytesWritten / length) / passes) * 100.0;
                                progress?.Report(Math.Min(99.9, prog));
                            }
                            fs.Flush();
                        }
                    }

                    // Obfuscate file name to avoid MFT recovery before deletion
                    string parentDir = Path.GetDirectoryName(filePath) ?? "";
                    string dummyPath = Path.Combine(parentDir, Guid.NewGuid().ToString("N") + ".tmp");
                    try
                    {
                        File.Move(filePath, dummyPath);
                        File.Delete(dummyPath);
                    }
                    catch
                    {
                        File.Delete(filePath);
                    }

                    progress?.Report(100.0);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileShredderService] Error: {ex.Message}");
                    return false;
                }
            }, ct);
        }

        public async Task<bool> ShredDirectoryAsync(string dirPath, ShredAlgorithm algorithm = ShredAlgorithm.DoD5220, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            return await Task.Run(async () =>
            {
                if (!Directory.Exists(dirPath)) return false;

                try
                {
                    var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                    int totalFiles = files.Length;
                    int processed = 0;

                    foreach (var file in files)
                    {
                        if (ct.IsCancellationRequested) return false;

                        var fileProgress = new Progress<double>(p =>
                        {
                            double overall = ((double)processed / Math.Max(1, totalFiles)) * 100.0 + (p / Math.Max(1, totalFiles));
                            progress?.Report(Math.Min(99.9, overall));
                        });

                        await ShredFileAsync(file, algorithm, fileProgress, ct);
                        processed++;
                    }

                    var dirs = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories);
                    Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));

                    foreach (var d in dirs)
                    {
                        try { Directory.Delete(d, false); } catch { }
                    }

                    try { Directory.Delete(dirPath, true); } catch { }

                    progress?.Report(100.0);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileShredderService] Directory shred error: {ex.Message}");
                    return false;
                }
            }, ct);
        }

        public async Task<bool> WipeFreeSpaceAsync(string driveLetter, IProgress<double>? progress = null, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                string cleanLetter = driveLetter.Trim().TrimEnd('\\', ':').ToUpperInvariant() + @":\";
                if (!Directory.Exists(cleanLetter)) return false;

                string tempWipeFolder = Path.Combine(cleanLetter, "$STORM_WIPE_TMP_" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tempWipeFolder);
                    var driveInfo = new DriveInfo(cleanLetter);

                    long freeSpace = driveInfo.AvailableFreeSpace;
                    long targetWipe = Math.Max(0, freeSpace - (256L * 1024 * 1024));

                    if (targetWipe <= 0)
                    {
                        progress?.Report(100.0);
                        return true;
                    }

                    long bytesWrittenTotal = 0;
                    byte[] zeroChunk = new byte[4 * 1024 * 1024];
                    Array.Clear(zeroChunk, 0, zeroChunk.Length);

                    int fileIndex = 0;
                    while (bytesWrittenTotal < targetWipe)
                    {
                        if (ct.IsCancellationRequested) break;

                        string tempFilePath = Path.Combine(tempWipeFolder, $"wipe_{fileIndex++}.dat");
                        long fileLimit = Math.Min(targetWipe - bytesWrittenTotal, 1024L * 1024 * 1024);
                        long fileWritten = 0;

                        try
                        {
                            using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                while (fileWritten < fileLimit && bytesWrittenTotal < targetWipe)
                                {
                                    if (ct.IsCancellationRequested) break;

                                    int toWrite = (int)Math.Min(zeroChunk.Length, targetWipe - bytesWrittenTotal);
                                    toWrite = (int)Math.Min(toWrite, fileLimit - fileWritten);

                                    fs.Write(zeroChunk, 0, toWrite);
                                    fileWritten += toWrite;
                                    bytesWrittenTotal += toWrite;

                                    double prog = ((double)bytesWrittenTotal / targetWipe) * 100.0;
                                    progress?.Report(Math.Min(99.0, prog));
                                }
                                fs.Flush();
                            }
                        }
                        catch (IOException)
                        {
                            break;
                        }
                    }

                    try
                    {
                        Directory.Delete(tempWipeFolder, true);
                    }
                    catch { }

                    progress?.Report(100.0);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileShredderService] WipeFreeSpace error: {ex.Message}");
                    try { if (Directory.Exists(tempWipeFolder)) Directory.Delete(tempWipeFolder, true); } catch { }
                    return false;
                }
            }, ct);
        }
    }
}
