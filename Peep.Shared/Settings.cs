#nullable enable
using System;
using System.Configuration;

namespace Peep.Shared;

public enum ChosenCharacter
{
    Ventress,
    KawKaw,
}

public class Settings
{
    public ChosenCharacter ChosenCharacter
    {
        get =>
            Enum.TryParse(ReadSetting(nameof(ChosenCharacter)), out ChosenCharacter value)
                ? value
                : ChosenCharacter.Ventress; // <-- default
        set => AddOrUpdateSetting(nameof(ChosenCharacter), value.ToString());
    }

    private string? ReadSetting(string key)
    {
        try
        {
            var appSettings = ConfigurationManager.AppSettings;
            string? result = appSettings[key] ?? null;
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read {key} from config file. Exception: {ex}");
            return null;
        }
    }

    private void AddOrUpdateSetting(string key, string value)
    {
        try
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to write {key} with value {value} to config file. Exception: {ex}"
            );
        }
    }
}
