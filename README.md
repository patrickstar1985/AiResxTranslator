# AI Resx Translator

A WPF desktop application that uses OpenAI to automatically translate .NET `.resx` resource files into multiple languages. Point it at a folder containing your resource files, and it will scan for missing translations and fill them in using AI — preserving format placeholders, escape sequences, and resx structure.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-MahApps.Metro-blue)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-green?logo=openai)

## Features

- **Automatic scanning** — Detects all culture-specific `.resx` files relative to an anchor file (e.g., `Strings.resx`) and identifies missing translation keys.
- **Batch translation** — Translates multiple keys at once using the OpenAI Chat API, grouped by target language file.
- **Configurable batch size** — Adjust the number of entries per API request (default: 50) to optimize for large translation files or API token limits.
- **Incremental saving** — Each translated batch is immediately written to the `.resx` file, so progress is preserved even if the translation is cancelled or interrupted mid-way. A subsequent scan will only pick up the remaining untranslated keys.
- **Per-entry progress tracking** — The progress bar and status text update based on individual translated entries, not just language files, giving accurate feedback for large translation runs.
- **Multiple OpenAI models** — Choose between `gpt-4o-mini`, `gpt-4o`, `gpt-4.1-nano`, `gpt-4.1-mini`, and `gpt-4.1`.
- **Selective translation** — Pick which language files and individual keys to translate before running.
- **Create new language files** — Add new culture-specific `.resx` files directly from the UI by selecting from a list of available cultures.
- **Direct .resx writing** — Translated strings are written back into the `.resx` files, ready to use.
- **Persistent settings** — API key, folder path, anchor file name, selected model, and batch size are saved to the Windows Registry and restored on next launch.
- **Modern UI** — Built with [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) for a clean, modern look with Material Design icons.
- **Cancellation support** — Long-running translations can be cancelled at any time. Already translated batches remain saved in the `.resx` files.

## Prerequisites

- **Windows** (WPF application)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- An [OpenAI API key](https://platform.openai.com/api-keys)

## Getting Started

### Clone the repository

```bash
git clone https://github.com/patrickstar1985/AiResxTranslator.git
cd AiResxTranslator
```

### Build and run

```bash
dotnet build
dotnet run --project AiResxTranslator
```

Or open `AiResxTranslator.sln` in Visual Studio 2022 and press **F5**.

## Usage

1. **Enter your OpenAI API key** in the settings panel.
2. **Browse to the folder** containing your `.resx` resource files.
3. **Set the anchor file name** — this is your base/English resource file (default: `Strings.resx`). The tool looks for matching culture-specific files like `Strings.de.resx`, `Strings.fr.resx`, etc.
4. **Select the AI model** you want to use for translation.
5. **Adjust the batch size** if needed — smaller batches reduce the risk of token limit issues, larger batches reduce the number of API calls.
6. Click **Scan for Missing Translations** to discover which keys are missing in each language file.
7. **Select/deselect** language files and individual translation entries as needed.
8. Optionally, **create a new language file** by selecting a culture from the dropdown and clicking **Create**.
9. Click **Translate Selected** to start the AI-powered translation.
10. Translated values are displayed in the grid and **immediately written** to the corresponding `.resx` files after each batch. You can safely cancel and resume later — only untranslated keys will be picked up on the next scan.

## How It Works

The application uses a straightforward approach:

1. **Scan** — Reads all `<data>` entries from the anchor `.resx` file, discovers culture-specific variants (e.g., `Strings.de.resx`), and compares keys to find missing translations.
2. **Translate** — Sends batches of key-value pairs (configurable batch size, default 50) to the OpenAI Chat API with a system prompt instructing it to return a JSON object with translated values while preserving format placeholders like `{0}`, `{1}`, etc.
3. **Write** — After each batch, merges translated entries back into the target `.resx` files, adding new `<data>` elements or updating existing ones. This ensures no progress is lost on cancellation or errors.

## Project Structure

```
AiResxTranslator/
    Converters/
        BoolConverters.cs          # Visibility converters for XAML bindings
    Models/
        LanguageFile.cs            # Represents a culture-specific .resx file
        MissingTranslation.cs      # Represents a single missing translation entry
        ResxEntry.cs               # Represents a .resx key-value entry
    Services/
        ResxService.cs             # .resx file reading, scanning, and writing
        SettingsService.cs         # Persistent settings via Windows Registry
        TranslationService.cs     # OpenAI API integration for translations
    ViewModels/
        MainViewModel.cs          # Main application logic and data bindings
    App.xaml                       # Application resources and MahApps.Metro theming
    MainWindow.xaml                # Main window UI layout
    MainWindow.xaml.cs             # Code-behind
    RelayCommand.cs                # ICommand implementation
```

## Dependencies

| Package | Purpose |
|---------|---------|
| [MahApps.Metro](https://www.nuget.org/packages/MahApps.Metro) | Modern Metro-style WPF controls and theming |
| [MahApps.Metro.IconPacks.Material](https://www.nuget.org/packages/MahApps.Metro.IconPacks.Material) | Material Design icons |
| [OpenAI](https://www.nuget.org/packages/OpenAI) | Official OpenAI .NET client library |

## License

This project is provided as-is. See the repository for license details.
