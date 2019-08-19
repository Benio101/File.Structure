using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace Mate
{
	internal sealed class WindowCommand
	{
		private const int CommandID = 0x0100;
		private static readonly Guid CommandSet = new Guid("ae2a58d7-9545-4fb7-aa43-db0d8c3a4cb8");
		private readonly AsyncPackage Package;

		private WindowCommand(AsyncPackage Package, IMenuCommandService CommandService)
		{
			if (Package        == null) return;
			if (CommandService == null) return;

			this.Package = Package;

			var MenuCommandID = new CommandID(CommandSet, CommandID);
			var MenuItem      = new MenuCommand(Execute, MenuCommandID);

			CommandService.AddCommand(MenuItem);
		}

		public static async Task InitializeAsync(AsyncPackage Package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);
			var CommandService = await Package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			// ReSharper disable once AssignmentIsFullyDiscarded
			_ = new WindowCommand(Package, CommandService);
		}

		private void Execute(object Sender, EventArgs Event)
		{
			Package.JoinableTaskFactory.RunAsync(async () =>
			{
				var Window = await Package.ShowToolWindowAsync(typeof(Window), 0, true, Package.DisposalToken);
				if (Window == null) return;

				await Package.JoinableTaskFactory.SwitchToMainThreadAsync();
				if (!(Window.Frame is IVsWindowFrame WndowFrame)) return;

				WndowFrame.Show();
			});
		}
	}
}