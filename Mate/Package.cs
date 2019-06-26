using System;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace Mate
{
	// ReSharper disable once ArrangeTypeModifiers
	// ReSharper disable once UnusedMember.Global

	[
		PackageRegistration
		(
			AllowsBackgroundLoading = true,
			UseManagedResourcesOnly = true
		),

		ProvideAutoLoad
		(
			VSConstants.UICONTEXT.ShellInitialized_string,
			PackageAutoLoadFlags.BackgroundLoad
		)
	]

	internal sealed class Package
	:
		AsyncPackage
	{
		private static EnvDTE.Events       Events;
		private static EnvDTE.WindowEvents WindowEvents;

		protected override async System.Threading.Tasks.Task InitializeAsync
		(
			CancellationToken              Token,
			IProgress<ServiceProgressData> Progress
		)
		{
			await base.InitializeAsync(Token, Progress);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Token);

			var Table = new RunningDocumentTable(this);
			Table.Advise(new RunningDocTableEvents());

			var DTE = Utils.GetDTE();
			Events = DTE.Events;

			WindowEvents = Events.WindowEvents;
			WindowEvents.WindowClosing += Window =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				if (Window.Document.Language != "C/C++") return;

				// Clear `File structure` window when closing window with `C/C++` source code.
				Mate.Window.RemoveAllEntries();
			};
		}
	}
}