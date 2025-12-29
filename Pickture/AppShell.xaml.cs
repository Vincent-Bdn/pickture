namespace Pickture;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
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
