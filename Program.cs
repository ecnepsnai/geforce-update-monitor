namespace GeforceUpdateMonitor
{
    using Microsoft.Toolkit.Uwp.Notifications;
    using System.Diagnostics;
    using System.Management;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class DriverDownloadInfo
    {
        [JsonInclude]
        [JsonPropertyName("Version")]
        public string Version { get; set; }

        [JsonInclude]
        [JsonPropertyName("DownloadURL")]
        public string DownloadURL { get; set; }

        public string VersionFileName { get
            {
                return "skip_" + Version.Replace(@"/", "").Replace(".", "");
            }
        }
    }

    public class DriverID
    {
        [JsonInclude]
        [JsonPropertyName("downloadInfo")]
        public DriverDownloadInfo DownloadInfo { get; set; }
    }

    public class DriverResponse
    {
        [JsonInclude]
        [JsonPropertyName("IDS")]
        public DriverID[] IDS { get; set; }
    }

    internal class Program
    {
        public static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GeforceUpdateMonitor");
        private static readonly Logger logger = new Logger(typeof(Program));
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        static void Main(string[] args)
        {
            if (!Directory.Exists(AppDataDir))
            {
                Directory.CreateDirectory(AppDataDir);
            }

            LogWriter.Open();
            Settings.Load();

            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                // Obtain the arguments from the notification
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                string action = args["action"];

                DriverDownloadInfo info = new DriverDownloadInfo
                {
                    DownloadURL = args["downloadURL"],
                    Version = args["driverVersion"]
                };

                if (action == "skip")
                {
                    File.Create(Path.Combine(AppDataDir, info.VersionFileName));
                    logger.Info($"User requested to skip version {info.Version}");
                    return;
                }

                if (action != "download")
                {
                    Environment.Exit(0);
                    return;
                }

                try
                {
                    DownloadAndExtract(info);
                }
                catch (Exception ex)
                {
                    logger.Error($"Error installing driver version {info.Version}: {ex.Message} {ex.StackTrace}");
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch
                    {
                        //
                    }
                    Environment.Exit(0);
                }
            };

            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                ProgressWindow progressWindow = new ProgressWindow();
                progressWindow.ShowDialog();
            }
            else
            {
                try
                {
                    CheckForUpdates();
                }
                catch (Exception ex)
                {
                    logger.Error($"Error checking for driver updates: {ex.Message} {ex.StackTrace}");
                }
            }
        }

        static void CheckForUpdates()
        {
            string currentDriverVersion;
            try
            {
                currentDriverVersion = GetCurrentDriverVersion();
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting current driver version: {ex.Message} {ex.StackTrace}");
                return;
            }
            if (currentDriverVersion == null)
            {
                logger.Error("No Nvidia device found");
                return;
            }

            DriverDownloadInfo latestDriver;
            try
            {
                latestDriver = GetLatestDriverVersion(Settings.SeriesID, Settings.FamilyID, Settings.OSID, Settings.LanguageCode);
            }
            catch (Exception ex)
            {
                logger.Error($"Error getting latest driver version: {ex.Message} {ex.StackTrace}");
                return;
            }
            if (currentDriverVersion == null)
            {
                logger.Error("No driver found on GeForce website");
                return;
            }

            logger.Info($"InstalledVersion='{currentDriverVersion}' LatestVersion='{latestDriver.Version}'");

            if (File.Exists(Path.Combine(AppDataDir, latestDriver.VersionFileName)))
            {
                logger.Info("Skipping version");
                return;
            }

            if (currentDriverVersion != latestDriver.Version)
            {
                new ToastContentBuilder()
                    .AddArgument("action", "download")
                    .AddArgument("driverVersion", latestDriver.Version)
                    .AddArgument("downloadURL", latestDriver.DownloadURL)
                    .AddText("GeForce Driver Update Available")
                    .AddText("Click to download & install")
                    .AddVisualChild(new AdaptiveGroup()
                    {
                        Children =
                        {
                                new AdaptiveSubgroup()
                                {
                                    Children =
                                    {
                                        new AdaptiveText()
                                        {
                                            Text = "Current version",
                                            HintStyle = AdaptiveTextStyle.Base
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = currentDriverVersion,
                                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                                        }
                                    }
                                },
                                new AdaptiveSubgroup()
                                {
                                    Children =
                                    {
                                        new AdaptiveText()
                                        {
                                            Text = "Latest version",
                                            HintStyle = AdaptiveTextStyle.Base
                                        },
                                        new AdaptiveText()
                                        {
                                            Text = latestDriver.Version,
                                            HintStyle = AdaptiveTextStyle.CaptionSubtle
                                        }
                                    }
                                }
                        }
                    })
                    .AddButton(new ToastButton().SetContent("Install").AddArgument("action", "download").SetBackgroundActivation())
                    .AddButton(new ToastButton().SetContent("Skip").AddArgument("action", "skip").SetBackgroundActivation())
                    .Show();
            }
        }
        static string GetCurrentDriverVersion()
        {
            ManagementObjectSearcher mgmtObjSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            foreach (ManagementObject videoController in mgmtObjSearcher.Get())
            {
                string name = (string)videoController["Name"];
                string version = (string)videoController["DriverVersion"];

                if (name.StartsWith("NVIDIA"))
                {
                    return version.Replace(".", "").Substring(4).Insert(3, ".");
                }
            }

            return null;
        }

        static DriverDownloadInfo GetLatestDriverVersion(string seriesID, string familyID, string osID, string languageCode)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&psid={seriesID}&pfid={familyID}&osID={osID}&languageCode={languageCode}&beta=null&isWHQL=0&dltype=-1&dch=1&upCRD=null&qnf=0&sort1=0&numberOfResults=10");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/json"));
            var getTask = httpClient.SendAsync(request);
            getTask.Wait();
            var result = getTask.Result;
            var bodyTask = result.Content.ReadAsByteArrayAsync();
            bodyTask.Wait();

            var utf8Reader = new Utf8JsonReader(bodyTask.Result);
            DriverResponse response = JsonSerializer.Deserialize<DriverResponse>(ref utf8Reader);

            int largestVersion = 0;
            int versionIndex = -1;
            for (int i = 0; i < response.IDS.Count(); i++)
            {
                DriverID driver = response.IDS[i];
                try
                {
                    int version = int.Parse(driver.DownloadInfo.Version.Replace(".", ""));
                    if (version > largestVersion)
                    {
                        largestVersion = version;
                        versionIndex = i;
                    }
                }
                catch
                {
                    continue;
                }
            }
            if (versionIndex == -1)
            {
                throw new Exception("No suitable driver versions found");
            }

            return response.IDS[versionIndex].DownloadInfo;
        }

        static void DownloadAndExtract(DriverDownloadInfo driverInfo)
        {
            Directory.CreateDirectory(tempDirectory);
            logger.Info($"Request to download and install {driverInfo.Version}");

            var task = httpClient.GetStreamAsync(driverInfo.DownloadURL);
            task.Wait();

            logger.Info($"Downloading {driverInfo.DownloadURL} to {Path.Combine(tempDirectory, "setup.exe")}");
            using (var f = File.Create(Path.Combine(tempDirectory, "setup.exe")))
            {
                task.Result.CopyTo(f);
            }

            Process extract = new Process();
            extract.StartInfo.FileName = @"C:\Program Files\7-Zip\7z.exe";
            extract.StartInfo.ArgumentList.Add("x");
            extract.StartInfo.ArgumentList.Add($"-o{Path.Combine(tempDirectory, "install")}");
            extract.StartInfo.ArgumentList.Add(Path.Combine(tempDirectory, "setup.exe"));
            extract.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            extract.StartInfo.CreateNoWindow = true;
            logger.Info($"Extracting {Path.Combine(tempDirectory, "setup.exe")} to {Path.Combine(tempDirectory, "install")}");
            extract.Start();
            extract.WaitForExit();

            Process setup = new Process();
            setup.StartInfo.FileName = Path.Combine(tempDirectory, "install", "setup.exe");
            setup.StartInfo.Verb = "runas";
            setup.StartInfo.UseShellExecute = true;
            logger.Info($"Executing {Path.Combine(tempDirectory, "install", "setup.exe")} as administrator");
            setup.Start();
            setup.WaitForExit();
            logger.Info($"Setup exited with code {setup.ExitCode}");
        }
    }
}