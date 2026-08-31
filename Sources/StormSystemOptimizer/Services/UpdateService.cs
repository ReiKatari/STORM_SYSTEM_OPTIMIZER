using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace StormSystemOptimizer.Services
{
    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public bool IsUpdateAvailable => HasUpdate;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleasePageUrl { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class UpdateService
    {
        private static UpdateService? _instance;
        public static UpdateService Instance => _instance ??= new UpdateService();

        public const string CurrentVersion = "2.0.2";
        private const string GitHubApiUrl = "https://api.github.com/repos/ReiKatari/STORM_SYSTEM_OPTIMIZER/releases/latest";
        private const string GitHubReleasesUrl = "https://github.com/ReiKatari/STORM_SYSTEM_OPTIMIZER/releases";

        private readonly HttpClient _httpClient;

        private UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "STORM-SYSTEM-OPTIMIZER-Updater");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            var result = new UpdateCheckResult
            {
                ReleasePageUrl = GitHubReleasesUrl
            };

            try
            {
                var response = await _httpClient.GetAsync(GitHubApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    result.StatusMessage = $"У вас установлена актуальная версия v{CurrentVersion}";
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
                string cleanTag = tagName.TrimStart('v', 'V');
                string releaseNotes = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
                string htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? GitHubReleasesUrl : GitHubReleasesUrl;

                result.LatestVersion = cleanTag;
                result.ReleaseNotes = releaseNotes;
                result.ReleasePageUrl = htmlUrl;

                // Check assets for installer
                if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsProp.EnumerateArray())
                    {
                        if (asset.TryGetProperty("browser_download_url", out var dlProp))
                        {
                            string dl = dlProp.GetString() ?? "";
                            if (dl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                result.DownloadUrl = dl;
                                break;
                            }
                        }
                    }
                }

                if (IsNewerVersion(cleanTag, CurrentVersion))
                {
                    result.HasUpdate = true;
                    result.StatusMessage = $"Доступна новая версия v{cleanTag}!";
                }
                else
                {
                    result.StatusMessage = $"У вас установлена последняя версия v{CurrentVersion}";
                }
            }
            catch (Exception ex)
            {
                result.StatusMessage = $"У вас актуальная версия v{CurrentVersion} ({ex.Message})";
            }

            return result;
        }

        public async Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, Action<int>? progressCallback = null)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "StormOptimizerUpdate");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                string installerFile = Path.Combine(tempDir, "StormSetup_Update.exe");

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(installerFile, FileMode.Create, FileAccess.Write, FileShare.None);

                    byte[] buffer = new byte[81920];
                    long totalRead = 0;
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (totalBytes > 0 && progressCallback != null)
                        {
                            int pct = (int)((totalRead / (double)totalBytes) * 100);
                            progressCallback(pct);
                        }
                    }
                }

                // Write updater script for clean restart
                string scriptPath = Path.Combine(tempDir, "run_update.cmd");
                string scriptContent = $@"@echo off
timeout /t 2 /nobreak >nul
start """" ""{installerFile}""
exit
";
                File.WriteAllText(scriptPath, scriptContent);

                // Run updater process and gracefully shutdown current app
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            if (string.IsNullOrEmpty(latest)) return false;
            try
            {
                var vLatest = new Version(NormalizeVersion(latest));
                var vCurrent = new Version(NormalizeVersion(current));
                return vLatest > vCurrent;
            }
            catch
            {
                return false;
            }
        }

        private string NormalizeVersion(string v)
        {
            var parts = v.Split('.');
            if (parts.Length == 1) return $"{parts[0]}.0.0";
            if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0";
            return v;
        }
    }
}
