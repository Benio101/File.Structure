using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Mate
{
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

			Meta.UpdateWindow();

			return VSConstants.S_OK;
		}

		public int OnBeforeSave(uint Cookie)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Meta.RemoveTrailingWhitespaces();
			Meta.FixHeadingSpaces();
			Meta.UpdateWindow();

			return VSConstants.S_OK;
		}
	}
}