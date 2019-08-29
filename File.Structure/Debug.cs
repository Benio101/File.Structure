using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using Task = System.Threading.Tasks.Task;

namespace File.Structure
{
	internal static class Debug
	{
		#pragma warning disable 169
		private static readonly IVsOutputWindowPane Pane;
		#pragma warning restore 169

		static Debug()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (!(Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow Window)) return;

			#region File.Structure.Debug

				// Create `File.Structure.Debug` output window.

				var GuID = new Guid();
				Window.CreatePane(ref GuID, "File.Structure.Debug", 1, 1);
				Window.GetPane(ref GuID, out Pane);

			#endregion
		}

		/// \short           Print $Message in `File.Structure.Debug` output window.
		/// \param  Message  Message to print.

		internal static void Print(string Message)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Pane.OutputString(Message + "\n");
			Pane.Activate();
		}

		/// \short           Print $Message in `File.Structure.Debug` output window.
		/// \param  Message  Message to print.

		internal static async Task PrintAsync(string Message)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Print(Message);
		}
	}
}