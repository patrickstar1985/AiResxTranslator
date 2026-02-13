using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AiResxTranslator.Models;

namespace AiResxTranslator.Services;

public class ResxService
{
    public Dictionary<string, string> ReadResxEntries(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var entries = new Dictionary<string, string>();

        foreach (var dataElement in doc.Descendants("data"))
        {
            var name = dataElement.Attribute("name")?.Value;
            var value = dataElement.Element("value")?.Value;

            if (name is null)
                continue;

            // Skip entries that reference files (type or mimetype attributes indicate non-string resources)
            if (dataElement.Attribute("type") is not null || dataElement.Attribute("mimetype") is not null)
                continue;

            entries[name] = value ?? string.Empty;
        }

        return entries;
    }

    public List<LanguageFile> DiscoverLanguageFiles(string directoryPath, string anchorFileName)
    {
        var anchorBaseName = Path.GetFileNameWithoutExtension(anchorFileName); // e.g., "Strings"
        var files = new List<LanguageFile>();

        foreach (var file in Directory.GetFiles(directoryPath, $"{anchorBaseName}.*.resx"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file); // e.g., "Strings.de"
            var cultureCode = fileName[(anchorBaseName.Length + 1)..];  // e.g., "de"

            string displayName;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureCode);
                displayName = $"{culture.EnglishName} ({cultureCode})";
            }
            catch
            {
                displayName = cultureCode;
            }

            files.Add(new LanguageFile
            {
                FilePath = file,
                CultureCode = cultureCode,
                DisplayName = displayName
            });
        }

        return [.. files.OrderBy(f => f.DisplayName)];
    }

    public List<MissingTranslation> FindMissingTranslations(
        Dictionary<string, string> anchorEntries,
        List<LanguageFile> languageFiles)
    {
        var missing = new List<MissingTranslation>();

        foreach (var langFile in languageFiles)
        {
            var existingEntries = ReadResxEntries(langFile.FilePath);
            var missingCount = 0;

            foreach (var (key, value) in anchorEntries)
            {
                if (!existingEntries.TryGetValue(key, out var existingValue) || string.IsNullOrWhiteSpace(existingValue))
                {
                    missingCount++;
                    missing.Add(new MissingTranslation
                    {
                        Key = key,
                        AnchorValue = value,
                        TargetLanguageFile = langFile.FilePath,
                        TargetCultureCode = langFile.CultureCode
                    });
                }
            }

            langFile.MissingCount = missingCount;
        }

        return missing;
    }

    public void WriteTranslationsToResx(string filePath, Dictionary<string, string> translations)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root!;

        foreach (var (key, value) in translations)
        {
            var existingData = root.Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key);

            if (existingData is not null)
            {
                var valueElement = existingData.Element("value");
                if (valueElement is not null)
                {
                    valueElement.Value = value;
                }
                else
                {
                    existingData.Add(new XElement("value", value));
                }
            }
            else
            {
                var newData = new XElement("data",
                    new XAttribute("name", key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value));
                root.Add(newData);
            }
        }

        doc.Save(filePath);
    }

    public string CreateLanguageFile(string directoryPath, string anchorFileName, string cultureCode)
    {
        var anchorBaseName = Path.GetFileNameWithoutExtension(anchorFileName);
        var newFileName = $"{anchorBaseName}.{cultureCode}.resx";
        var newFilePath = Path.Combine(directoryPath, newFileName);

        if (File.Exists(newFilePath))
            throw new InvalidOperationException($"The file '{newFileName}' already exists.");

        var anchorPath = Path.Combine(directoryPath, anchorFileName);
        var anchorEntries = ReadResxEntries(anchorPath);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("root")
        );

        var root = doc.Root!;

        // Add standard resx headers
        root.Add(new XElement("resheader",
            new XAttribute("name", "resmimetype"),
            new XElement("value", "text/microsoft-resx")));
        root.Add(new XElement("resheader",
            new XAttribute("name", "version"),
            new XElement("value", "2.0")));
        root.Add(new XElement("resheader",
            new XAttribute("name", "reader"),
            new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));
        root.Add(new XElement("resheader",
            new XAttribute("name", "writer"),
            new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));

        // Add all keys with empty values
        foreach (var (key, _) in anchorEntries)
        {
            root.Add(new XElement("data",
                new XAttribute("name", key),
                new XAttribute(XNamespace.Xml + "space", "preserve")));
        }

        doc.Save(newFilePath);
        return newFilePath;
    }

    public List<CultureInfo> GetAvailableCultures(string directoryPath, string anchorFileName)
    {
        var existingFiles = DiscoverLanguageFiles(directoryPath, anchorFileName);
        var existingCultures = new HashSet<string>(existingFiles.Select(f => f.CultureCode), StringComparer.OrdinalIgnoreCase);

        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(c => !existingCultures.Contains(c.Name))
            .OrderBy(c => c.EnglishName)
            .ToList();
    }
}
