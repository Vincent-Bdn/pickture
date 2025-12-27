namespace Pickture;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

	private void OnMenuClicked(object sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			var action = await DisplayActionSheet(
				"File Menu",
				"Cancel",
				null,
				"Change Folder",
				"Exit Pickture");

			switch (action)
			{
				case "Change Folder":
					// Send event to current content
					if (Current?.CurrentPage is IFileMenuHandler handler)
					{
						handler.OnChangeFolder();
					}
					break;
				case "Exit Pickture":
					// Send event to current content
					if (Current?.CurrentPage is IFileMenuHandler handler2)
					{
						handler2.OnExit();
					}
					break;
			}
		});
	}
}

/// <summary>
/// Interface for pages that need to handle File menu actions
/// </summary>
public interface IFileMenuHandler
{
	void OnChangeFolder();
	void OnExit();
}
