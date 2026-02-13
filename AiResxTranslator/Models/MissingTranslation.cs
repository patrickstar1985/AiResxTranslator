using System.ComponentModel;

namespace AiResxTranslator.Models;

public class MissingTranslation : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private string? _translatedValue;

    public string Key { get; init; } = string.Empty;
    public string AnchorValue { get; init; } = string.Empty;
    public string TargetLanguageFile { get; init; } = string.Empty;
    public string TargetCultureCode { get; init; } = string.Empty;

    public string? TranslatedValue
    {
        get => _translatedValue;
        set { _translatedValue = value; OnPropertyChanged(nameof(TranslatedValue)); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
