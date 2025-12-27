using System.Collections.ObjectModel;
using Pickture.Shared.Models;

namespace Pickture.Features.ImageGallery;

/// <summary>
/// Manages virtual loading of thumbnails - only keeping loaded thumbnails for items 
/// within the visible window (current ± buffer size)
/// </summary>
public class VirtualizedThumbnailLoader
{
    private readonly ObservableCollection<ImageItem> _visibleThumbnails;
    private readonly List<ImageItem> _allImages;
    private readonly int _bufferSize;
    private int _currentViewIndex = -1;
    private int _firstLoadedIndex = -1;
    private int _lastLoadedIndex = -1;

    public VirtualizedThumbnailLoader(ObservableCollection<ImageItem> visibleThumbnails, List<ImageItem> allImages, int bufferSize = 15)
    {
        _visibleThumbnails = visibleThumbnails;
        _allImages = allImages;
        _bufferSize = bufferSize;
    }

    /// <summary>
    /// Updates the visible window of thumbnails based on current index
    /// </summary>
    public void UpdateWindow(int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= _allImages.Count)
            return;

        // Only update if we've scrolled significantly
        if (_currentViewIndex == currentIndex)
            return;

        _currentViewIndex = currentIndex;

        // Calculate the range we want to load
        int startIndex = Math.Max(0, currentIndex - _bufferSize);
        int endIndex = Math.Min(_allImages.Count - 1, currentIndex + _bufferSize);

        // Check if we need to update
        if (_firstLoadedIndex == startIndex && _lastLoadedIndex == endIndex)
            return;

        _firstLoadedIndex = startIndex;
        _lastLoadedIndex = endIndex;

        // Update the visible collection
        _visibleThumbnails.Clear();
        for (int i = startIndex; i <= endIndex; i++)
        {
            _visibleThumbnails.Add(_allImages[i]);
        }
    }

    /// <summary>
    /// Get the actual index in the full list of the item at position in visible collection
    /// </summary>
    public int GetActualIndexFromVisibleIndex(int visibleIndex)
    {
        if (_firstLoadedIndex < 0 || visibleIndex < 0 || visibleIndex >= _visibleThumbnails.Count)
            return -1;

        return _firstLoadedIndex + visibleIndex;
    }

    /// <summary>
    /// Get the position in the visible collection of an item from the full list
    /// </summary>
    public int GetVisibleIndexFromActualIndex(int actualIndex)
    {
        if (_firstLoadedIndex < 0 || actualIndex < _firstLoadedIndex || actualIndex > _lastLoadedIndex)
            return -1;

        return actualIndex - _firstLoadedIndex;
    }
}
