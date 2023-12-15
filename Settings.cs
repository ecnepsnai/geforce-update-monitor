namespace GeforceUpdateMonitor
{
    using System;

    internal class Settings
    {
        private static readonly Logger logger = new Logger(typeof(Settings));
        private static readonly string ConfigFilePath = Path.Combine(Program.AppDataDir, "config.txt");

        public static string SeriesID = "107"; // RTX 20 series
        public static string FamilyID = "904"; // RTX 2080 Super
        public static string OSID = "135"; // Windows 11
        public static string LanguageCode = "1033"; // en-US
        public static string ZipPath = @"C:\Program Files\7-Zip\7z.exe";

        public static void Load()
        {
            if (!File.Exists(ConfigFilePath))
            {
                logger.Info("Init settings");
                File.CreateText(ConfigFilePath);
            }

            var lines = File.ReadAllLines(ConfigFilePath);
            foreach (var line in lines)
            {
                if (line[0] == '#')
                {
                    continue;
                }

                var parts = line.Split(" = ", 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                var key = parts[0];
                var value = parts[1];

                switch (key)
                {
                    case "series_id":
                        SeriesID = value;
                        break;
                    case "family_id":
                        FamilyID = value;
                        break;
                    case "os_id":
                        OSID = value;
                        break;
                    case "language_code":
                        LanguageCode = value;
                        break;
                    case "7zip_path":
                        ZipPath = value;
                        break;
                    default:
                        logger.Error($"Unknown settings key {key}");
                        break;
                }
            }
        }
    }
}
