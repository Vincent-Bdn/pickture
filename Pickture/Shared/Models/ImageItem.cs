using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pickture.Shared.Models;

public class ImageItem : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private bool _isFavorite;
    private bool _isSelected;
    private int _actualIndex = -1;
    private DateTime _modifiedDate;
    private long _fileSizeBytes;

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (_fileName != value)
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite != value)
            {
                _isFavorite = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime ModifiedDate
    {
        get => _modifiedDate;
        set
        {
            if (_modifiedDate != value)
            {
                _modifiedDate = value;
                OnPropertyChanged();
            }
        }
    }

    public long FileSizeBytes
    {
        get => _fileSizeBytes;
        set
        {
            if (_fileSizeBytes != value)
            {
                _fileSizeBytes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Internal property to track the actual index in the full collection.
    /// Used for virtualized views to map displayed items back to their position in the full list.
    /// </summary>
    public int ActualIndex
    {
        get => _actualIndex;
        set
        {
            if (_actualIndex != value)
            {
                _actualIndex = value;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
