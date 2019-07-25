using System.Threading.Tasks;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Mate
{
	#region RunningDocumentTable

		internal class RunningDocTableEvents
		:
			IVsRunningDocTableEvents3
		{
			#region Trash

			public int OnAfterFirstDocumentLock
			(
				uint Cookie,
				uint LockType,
				uint ReadLocksRemaining,
				uint EditLocksRemaining
			)
			{
				return VSConstants.S_OK;
			}

			public int OnBeforeLastDocumentUnlock
			(
				uint Cookie,
				uint LockType,
				uint ReadLocksRemaining,
				uint EditLocksRemaining
			)
			{
				return VSConstants.S_OK;
			}

			public int OnAfterSave(uint Cookie)
			{
				return VSConstants.S_OK;
			}

			public int OnAfterAttributeChange
			(
				uint Cookie,
				uint Attributes
			)
			{
				return VSConstants.S_OK;
			}

			public int OnAfterDocumentWindowHide
			(
				uint           Cookie,
				IVsWindowFrame Frame
			)
			{
				return VSConstants.S_OK;
			}

			public int OnAfterAttributeChangeEx
			(
				uint         Cookie,
				uint         Attributes,
				IVsHierarchy PreviouHierarchy,
				uint         PreviousItemID,
				string       PreviouDocumentName,
				IVsHierarchy NewHierarchy,
				uint         NewItemID,
				string       NewDocumentName
			)
			{
				return VSConstants.S_OK;
			}

			#endregion Trash

			public int OnBeforeDocumentWindowShow
			(
				uint           Cookie,
				int            FirstShow,
				IVsWindowFrame Frame
			)
			{
				if (FirstShow == 0) return VSConstants.S_OK;
				ThreadHelper.ThrowIfNotOnUIThread();

				Events.OnBeforeWindowCreate();

				return VSConstants.S_OK;
			}

			public int OnBeforeSave(uint Cookie)
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				Events.OnBeforeSave();

				return VSConstants.S_OK;
			}
		}

	#endregion

	internal static class Events
	{
		internal static void OnBeforeWindowCreate()
		{
			Parallel.Invoke(Window.Update);
		}

		internal static void OnAfterTextViewCreate()
		{
			Parallel.Invoke(Window.Update);
		}

		internal static void OnAfterWindowActivate()
		{
			Parallel.Invoke(Window.Update);
		}

		internal static void OnBeforeDocumentClose()
		{
			Parallel.Invoke(Window.RemoveAllEntries);
		}

		internal static void OnBeforeSave()
		{
			Parallel.Invoke(Window.Update);

			ThreadHelper.ThrowIfNotOnUIThread();
			Meta.RemoveTrailingWhitespaces();
			//Meta.FixHeadingSpaces();
		}

		internal static void OnAfterCaretPositionChange()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var CurrentLine = Utils.GetCurrentLine();
			Parallel.Invoke(() => Window.ScrollToLine(CurrentLine));
		}
	}
}