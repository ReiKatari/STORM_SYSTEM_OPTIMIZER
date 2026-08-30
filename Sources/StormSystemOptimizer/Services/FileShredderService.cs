using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace StormSystemOptimizer.Services
{
    public enum ShredAlgorithm
    {
        ZeroFill = 0,     // 1 pass of zeros
        RandomFill = 1,   // 1 pass of cryptographically random data
        DoD5220 = 2,      // 3 passes: Zeros, Ones, Random
        GutmannLite = 3   // 7 passes
    }

    public class FileShredderService
    {
        private static FileShredderService? _instance;
        public static FileShredderService Instance => _instance ??= new FileShredderService();

        public async Task<bool> ShredFileAsync(string filePath, ShredAlgorithm algorithm = ShredAlgorithm.DoD5220, IProgress<double>? progress = null)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(filePath)) return false;

                try
                {
                    // Remove read-only / hidden flags
                    File.SetAttributes(filePath, FileAttributes.Normal);

                    var fi = new FileInfo(filePath);
                    long length = fi.Length;
                    int passes = algorithm switch
                    {
                        ShredAlgorithm.ZeroFill => 1,
                        ShredAlgorithm.RandomFill => 1,
                        ShredAlgorithm.DoD5220 => 3,
                        ShredAlgorithm.GutmannLite => 7,
                        _ => 1
                    };

                    byte[] buffer = new byte[64 * 1024];

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        for (int currentPass = 1; currentPass <= passes; currentPass++)
                        {
                            fs.Position = 0;
                            long bytesWritten = 0;

                            while (bytesWritten < length)
                            {
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
                                        RandomNumberGenerator.Fill(buffer.AsSpan(0, toWrite));
                                        break;
                                }

                                fs.Write(buffer, 0, toWrite);
                                bytesWritten += toWrite;

                                double prog = ((currentPass - 1.0) / passes + ((double)bytesWritten / length) / passes) * 100.0;
                                progress?.Report(prog);
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
            });
        }
    }
}
