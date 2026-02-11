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
                if (!existingEntries.ContainsKey(key))
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
                existingData.Element("value")!.Value = value;
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
}
