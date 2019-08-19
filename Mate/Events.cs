using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

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
				if (FirstShow == 0)
					return VSConstants.S_OK;

				_ = Events.OnBeforeWindowCreateAsync();
				return VSConstants.S_OK;
			}

			public int OnBeforeSave(uint Cookie)
			{
				_ = Events.OnBeforeSaveAsync();
				return VSConstants.S_OK;
			}
		}

	#endregion

	internal static class Events
	{
		internal static async Task OnBeforeWindowCreateAsync()
		{
			await TaskScheduler.Default;
			await Window.UpdateAsync();
		}

		internal static async Task OnAfterTextViewCreateAsync()
		{
			await TaskScheduler.Default;
			await Window.UpdateAsync();
		}

		internal static async Task OnAfterWindowActivateAsync()
		{
			await TaskScheduler.Default;
			await Window.UpdateAsync();
		}

		internal static async Task OnBeforeDocumentCloseAsync()
		{
			await TaskScheduler.Default;
			await Window.RemoveAllEntriesAsync();
		}

		internal static async Task OnBeforeSaveAsync()
		{
			await TaskScheduler.Default;
			await Window.UpdateAsync();
			await Meta.RemoveTrailingWhitespacesAsync();
			//await Meta.FixHeadingSpacesAsync();
		}

		internal static async Task OnAfterCaretPositionChangeAsync()
		{
			await TaskScheduler.Default;
			var CurrentLine = await Utils.GetCurrentLineAsync();
			await Window.ScrollToLineAsync(CurrentLine);
		}
	}
}