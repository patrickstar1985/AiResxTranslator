using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace AiResxTranslator.Services;

public class SettingsService
{
    private const string RegistryKeyPath = @"SOFTWARE\AiResxTranslator";

    public string? LoadApiKey()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        return key?.GetValue("ApiKey") as string;
    }

    public void SaveApiKey(string apiKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue("ApiKey", apiKey);
    }

    public string? LoadFolderPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        return key?.GetValue("FolderPath") as string;
    }

    public void SaveFolderPath(string folderPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue("FolderPath", folderPath);
    }

    public string? LoadAnchorFileName()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        return key?.GetValue("AnchorFileName") as string;
    }

    public void SaveAnchorFileName(string anchorFileName)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue("AnchorFileName", anchorFileName);
    }

    public string? LoadSelectedModel()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
        return key?.GetValue("SelectedModel") as string;
    }

    public void SaveSelectedModel(string model)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue("SelectedModel", model);
    }
}
