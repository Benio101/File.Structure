﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace File.Structure
{
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
		),

		InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400),
		Guid("2e11dc29-f231-4423-a329-5485cdb144bd"),

		ProvideMenuResource("Menus.ctmenu", 1),
		ProvideToolWindow(typeof(Window))
	]

	internal sealed class Package
	:
		AsyncPackage
	{
		private static EnvDTE.Events         Events;
		private static EnvDTE.DocumentEvents DocumentEvents;
		private static EnvDTE.WindowEvents   WindowEvents;
		private static EnvDTE.Window         LastWindowThatGotFocus;

		protected override async Task InitializeAsync
		(
			CancellationToken              Token,
			IProgress<ServiceProgressData> Progress
		)
		{
			await base.InitializeAsync(Token, Progress);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Token);

			#region Events
				#region RunningDocumentTable

					var Table = new RunningDocumentTable(this);
					Table.Advise(new RunningDocTableEvents());

				#endregion
				#region EnvDTE.Events

					var DTE = await Utils.GetDTEAsync();
					Events = DTE.Events;

					DocumentEvents = Events.DocumentEvents;
					DocumentEvents.DocumentClosing += Document =>
					{
						_ = Task.Run(async () =>
						{
							await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
							if (Document.Language != "C/C++") return;

							await TaskScheduler.Default;
							await File.Structure.Events.OnBeforeDocumentCloseAsync();
						}, Token);
					};

					WindowEvents = Events.WindowEvents;
					WindowEvents.WindowActivated += (GotFocus, LostFocus) =>
					{
						if (GotFocus == LastWindowThatGotFocus) return;
						_ = Task.Run(async () =>
						{
							await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

							if (GotFocus.Document == null) return;
							if (GotFocus.Document.Language != "C/C++") return;

							await TaskScheduler.Default;
							LastWindowThatGotFocus = GotFocus;
							await File.Structure.Events.OnAfterWindowActivateAsync();
						}, Token);
					};

				#endregion

				await WindowCommand.InitializeAsync(this);

			#endregion
		}

		public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid ToolWindowType)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (ToolWindowType == typeof(Window).GUID) return this;
			return base.GetAsyncToolWindowFactory(ToolWindowType);
		}

		protected override string GetToolWindowTitle(Type toolWindowType, int id)
		{
			if (toolWindowType == typeof(Window)) return "Loading…";
			return base.GetToolWindowTitle(toolWindowType, id);
		}
	}
}