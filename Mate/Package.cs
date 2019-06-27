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
		private static EnvDTE.Events         Events;
		private static EnvDTE.DocumentEvents DocumentEvents;
		private static EnvDTE.WindowEvents   WindowEvents;

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

			DocumentEvents = Events.DocumentEvents;
			DocumentEvents.DocumentClosing += Document =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				if (Document.Language != "C/C++") return;

				// Clear `File structure` window when closing window with `C/C++` source code.
				Window.RemoveAllEntries();
			};

			WindowEvents = Events.WindowEvents;
			WindowEvents.WindowActivated += (GotFocus, LostFocus) =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				if (GotFocus.Document.Language != "C/C++") return;

				Meta.UpdateWindow();
			};
		}
	}
}