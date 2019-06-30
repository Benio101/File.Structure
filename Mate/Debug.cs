using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Mate
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

			#region Mate.Debug

				// Create `Mate.Debug` output window.

				var GuID = new Guid();
				Window.CreatePane(ref GuID, "Mate.Debug", 1, 1);
				Window.GetPane(ref GuID, out Pane);

			#endregion
		}

		/// \short           Print $Message in `Mate.Debug` output window.
		/// \param  Message  Message to print.

		internal static void Print(string Message)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Pane.OutputString(Message + "\n");
			Pane.Activate();
		}
	}
}