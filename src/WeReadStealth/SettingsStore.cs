using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace WeReadStealth
{
    public sealed class SettingsStore
    {
        private readonly string _settingsPath;

        public SettingsStore(string settingsPath = null)
        {
            _settingsPath = settingsPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WeReadStealth",
                "settings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
            {
                return AppSettings.CreateDefault();
            }

            try
            {
                using (FileStream stream = File.OpenRead(_settingsPath))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    AppSettings settings = serializer.ReadObject(stream) as AppSettings ?? AppSettings.CreateDefault();
                    settings.Normalize();
                    return settings;
                }
            }
            catch (IOException)
            {
                return AppSettings.CreateDefault();
            }
            catch (SerializationException)
            {
                return AppSettings.CreateDefault();
            }
            catch (UnauthorizedAccessException)
            {
                return AppSettings.CreateDefault();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            AppSettings normalized = settings.Copy();
            normalized.Normalize();

            string directory = Path.GetDirectoryName(_settingsPath);
            if (string.IsNullOrEmpty(directory))
            {
                throw new InvalidOperationException("The settings path must include a directory.");
            }

            Directory.CreateDirectory(directory);
            string temporaryPath = _settingsPath + ".tmp";

            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(stream, normalized);
                    stream.Flush(true);
                }

                if (File.Exists(_settingsPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, _settingsPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(_settingsPath);
                        File.Move(temporaryPath, _settingsPath);
                    }
                    catch (IOException)
                    {
                        File.Delete(_settingsPath);
                        File.Move(temporaryPath, _settingsPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, _settingsPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
