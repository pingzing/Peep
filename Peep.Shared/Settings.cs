using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Peep.Shared;

public enum ChosenCharacter
{
    Ventress,
    KawKaw,
}

public class Settings
{
    private IConfigurationRoot _config;
    private string _filePath;

    public ChosenCharacter ChosenCharacter
    {
        get =>
            Enum.TryParse(_config[nameof(ChosenCharacter)], out ChosenCharacter value)
                ? value
                : ChosenCharacter.Ventress; // <-- default
        set => AddOrUpdateSetting(nameof(ChosenCharacter), value.ToString());
    }

    public Settings(string filePath)
    {
        _filePath = filePath;
        if (!File.Exists(_filePath))
        {
            using (var streamWriter = File.CreateText(_filePath))
            {
                streamWriter.WriteLine("{}");
            }
        }
        _config = new ConfigurationBuilder().AddJsonFile(filePath).Build();
    }

    private void AddOrUpdateSetting(string key, string value)
    {
        try
        {
            _config[key] = value;
            string updatedJson = JsonSerializer.Serialize(this);
            File.WriteAllText(_filePath, updatedJson);
            _config.Reload();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to write {key} with value {value} to config file. Exception: {ex}"
            );
        }
    }
}
