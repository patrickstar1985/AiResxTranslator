using System.ComponentModel;

namespace AiResxTranslator.Models;

public class LanguageFile : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private int _missingCount;

    public string FilePath { get; init; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(FilePath);
    public string CultureCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public int MissingCount
    {
        get => _missingCount;
        set { _missingCount = value; OnPropertyChanged(nameof(MissingCount)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
