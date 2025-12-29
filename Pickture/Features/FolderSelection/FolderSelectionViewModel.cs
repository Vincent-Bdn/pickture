using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Storage;

namespace Pickture.Features.FolderSelection;

public class FolderSelectionViewModel : INotifyPropertyChanged
{
    private string _selectedFolderPath = string.Empty;

    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        set
        {
            if (_selectedFolderPath != value)
            {
                _selectedFolderPath = value;
                OnPropertyChanged();
            }
        }
    }

    public event Action<string>? FolderSelected;

    public async Task SelectFolderAsync()
    {
        try
        {
            
            var picked = await FolderPicker.PickAsync(default);
            if (picked.Folder == null)
                throw new InvalidOperationException("Invalid picked folder");
            SelectedFolderPath = picked.Folder.Path;
            FolderSelected?.Invoke(picked.Folder.Path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting folder: {ex.Message}\n{ex.StackTrace}");
            await Application.Current!.MainPage!.DisplayAlert("Error", $"Failed to select folder: {ex.Message}", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
