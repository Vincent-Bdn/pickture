namespace Pickture;

using Pickture.Features.FolderSelection;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new FolderSelectionPage());
	}
}