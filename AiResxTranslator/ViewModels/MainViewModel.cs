using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AiResxTranslator.Models;
using AiResxTranslator.Services;

namespace AiResxTranslator.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService = new();
    private readonly ResxService _resxService = new();
    private CancellationTokenSource? _cts;

    private string _apiKey = string.Empty;
    private string _folderPath = string.Empty;
    private string _anchorFileName = "Strings.resx";
    private string _selectedModel = "gpt-4o-mini";
    private string _statusText = "Ready";
    private bool _isBusy;
    private double _progressValue;
    private double _progressMax = 100;
    private bool _selectAllMissing = true;

    public MainViewModel()
    {
        _apiKey = _settingsService.LoadApiKey() ?? string.Empty;
        _folderPath = _settingsService.LoadFolderPath() ?? string.Empty;
        _anchorFileName = _settingsService.LoadAnchorFileName() ?? "Strings.resx";
        _selectedModel = _settingsService.LoadSelectedModel() ?? "gpt-4o-mini";

        ScanCommand = new RelayCommand(async () => await ScanAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(FolderPath));
        TranslateCommand = new RelayCommand(async () => await TranslateAsync(), () => !IsBusy && MissingTranslations.Count > 0);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        SelectAllMissingCommand = new RelayCommand(ToggleSelectAllMissing);
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;
            _settingsService.SaveApiKey(value);
            OnPropertyChanged();
        }
    }

    public string FolderPath
    {
        get => _folderPath;
        set
        {
            _folderPath = value;
            _settingsService.SaveFolderPath(value);
            OnPropertyChanged();
        }
    }

    public string AnchorFileName
    {
        get => _anchorFileName;
        set
        {
            _anchorFileName = value;
            _settingsService.SaveAnchorFileName(value);
            OnPropertyChanged();
        }
    }

    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            _selectedModel = value;
            _settingsService.SaveSelectedModel(value);
            OnPropertyChanged();
        }
    }

    public List<string> AvailableModels { get; } =
    [
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1-nano",
        "gpt-4.1-mini",
        "gpt-4.1",
    ];

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); }
    }

    public bool IsNotBusy => !IsBusy;

    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    public double ProgressMax
    {
        get => _progressMax;
        set { _progressMax = value; OnPropertyChanged(); }
    }

    public bool SelectAllMissing
    {
        get => _selectAllMissing;
        set
        {
            _selectAllMissing = value;
            foreach (var m in MissingTranslations)
                m.IsSelected = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LanguageFile> LanguageFiles { get; } = [];
    public ObservableCollection<MissingTranslation> MissingTranslations { get; } = [];

    public ICommand ScanCommand { get; }
    public ICommand TranslateCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand SelectAllMissingCommand { get; }

    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select resource folder"
        };

        if (!string.IsNullOrWhiteSpace(FolderPath) && Directory.Exists(FolderPath))
            dialog.InitialDirectory = FolderPath;

        if (dialog.ShowDialog() == true)
        {
            FolderPath = dialog.FolderName;
        }
    }

    private void ToggleSelectAllMissing()
    {
        SelectAllMissing = !SelectAllMissing;
    }

    private async Task ScanAsync()
    {
        if (!Directory.Exists(FolderPath))
        {
            StatusText = "Folder path does not exist.";
            return;
        }

        var anchorPath = Path.Combine(FolderPath, AnchorFileName);
        if (!File.Exists(anchorPath))
        {
            StatusText = $"Anchor file '{AnchorFileName}' not found in the selected folder.";
            return;
        }

        IsBusy = true;
        StatusText = "Scanning...";
        LanguageFiles.Clear();
        MissingTranslations.Clear();

        try
        {
            await Task.Run(() =>
            {
                var anchorEntries = _resxService.ReadResxEntries(anchorPath);
                var langFiles = _resxService.DiscoverLanguageFiles(FolderPath, AnchorFileName);
                var missing = _resxService.FindMissingTranslations(anchorEntries, langFiles);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var lf in langFiles)
                        LanguageFiles.Add(lf);
                    foreach (var m in missing)
                        MissingTranslations.Add(m);
                });
            });

            StatusText = $"Found {LanguageFiles.Count} language file(s), {MissingTranslations.Count} missing translation(s).";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusText = "Please enter your OpenAI API key.";
            return;
        }

        var selectedLanguages = LanguageFiles.Where(l => l.IsSelected).ToList();
        if (selectedLanguages.Count == 0)
        {
            StatusText = "No language files selected.";
            return;
        }

        var selectedMissing = MissingTranslations
            .Where(m => m.IsSelected)
            .Where(m => selectedLanguages.Any(l => l.FilePath == m.TargetLanguageFile))
            .ToList();

        if (selectedMissing.Count == 0)
        {
            StatusText = "No translations selected.";
            return;
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();
        ProgressValue = 0;

        // Group by target language file
        var grouped = selectedMissing
            .GroupBy(m => m.TargetLanguageFile)
            .ToList();

        ProgressMax = grouped.Count;

        var translationService = new TranslationService(ApiKey, SelectedModel);
        var totalTranslated = 0;

        try
        {
            foreach (var group in grouped)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var langFile = LanguageFiles.First(l => l.FilePath == group.Key);
                StatusText = $"Translating {langFile.DisplayName}...";

                var keysAndValues = group.ToDictionary(m => m.Key, m => m.AnchorValue);

                // Batch in chunks of 50 to avoid token limits
                var chunks = keysAndValues
                    .Chunk(50)
                    .Select(c => c.ToDictionary(kv => kv.Key, kv => kv.Value))
                    .ToList();

                var allTranslations = new Dictionary<string, string>();

                foreach (var chunk in chunks)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var translations = await translationService.TranslateBatchAsync(
                        chunk, langFile.CultureCode, _cts.Token);

                    foreach (var kv in translations)
                        allTranslations[kv.Key] = kv.Value;
                }

                // Write to resx file
                _resxService.WriteTranslationsToResx(group.Key, allTranslations);
                totalTranslated += allTranslations.Count;

                // Update UI - mark translated items
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var m in group)
                    {
                        if (allTranslations.TryGetValue(m.Key, out var translated))
                        {
                            m.TranslatedValue = translated;
                        }
                    }
                    langFile.MissingCount -= allTranslations.Count;
                });

                ProgressValue++;
            }

            StatusText = $"Done! Translated {totalTranslated} string(s) across {grouped.Count} language file(s).";
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Cancelled. Translated {totalTranslated} string(s) before cancellation.";
        }
        catch (Exception ex)
        {
            StatusText = $"Translation error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "Cancelling...";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
