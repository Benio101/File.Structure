using EnvDTE;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace File.Structure
{
	internal static class Utils
	{
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr Object);

		/// Get DTE of EnvDTE (GuID "04A72314-32E9-48E2-9B87-A63603454F3E").
		internal static async Task<DTE> GetDTEAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			return Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE)) as DTE;
		}

		/// Get active document.
		private static async Task<Document> GetActiveDocumentAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var DTE = await GetDTEAsync();
			if (DTE == null) return null;

			return DTE.ActiveDocument;
		}

		/// Get active document as `TextDocument`.
		internal static async Task<TextDocument> GetTextDocumentAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var Document = await GetActiveDocumentAsync();
			if (Document == null) return null;

			return Document.Object() as TextDocument;
		}

		/// Get text of current active document.
		internal static async Task<string> GetTextAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var TextDocument = await GetTextDocumentAsync();
			if (TextDocument == null) return null;

			var Text = TextDocument.StartPoint.CreateEditPoint().GetText(TextDocument.EndPoint);
			return Text;
		}

		/// \short Get current line (where cursor is placed) of active document.
		/// \spare `1`.

		internal static async Task<int> GetCurrentLineAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var ActiveDocument = await GetActiveDocumentAsync();
			if (ActiveDocument == null) goto spare;

			var Selection = ActiveDocument.Selection as TextSelection;
			if (Selection == null) goto spare;

			return Selection.CurrentLine;
			spare: return -1;
		}

		/// \short           Get icon as `BitmapSource` from `ImageMoniker`.
		/// \param  Moniker  Moniker name of icon to get.
		/// \param  Size     Size of icon to get (render).

		internal static async Task<BitmapSource> GetIconFromMonikerAsync(ImageMoniker Moniker, int Size)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var Attributes = new ImageAttributes
			{
				Flags         = (uint) _ImageAttributesFlags.IAF_RequiredFlags,
				Format        = (uint) _UIDataFormat.DF_WPF,
				ImageType     = (uint) _UIImageType.IT_Bitmap,
				LogicalHeight = Size,
				LogicalWidth  = Size,
				StructSize    = Marshal.SizeOf(typeof(ImageAttributes))
			};

			if (!(Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsImageService)) is IVsImageService2 Service)) return null;

			var Result = Service.GetImage(Moniker, Attributes);
			Result.get_Data(out var Data);

			return Data as BitmapSource;
		}

		/// \short              Get icon as `BitmapSource` from `string` encoded in Base64.
		/// \param  DataBase64  string containing image data, encoded in Base64.
		/// \param  Size        Size of icon to get (render).

		internal static BitmapSource GetIconFromBase64(string DataBase64, int Size)
		{
			var Bytes = Convert.FromBase64String(DataBase64);

			Image Image;
			using (var Stream = new MemoryStream(Bytes))
			{
				Image = Image.FromStream(Stream);
			}

			var Bitmap = new Bitmap(Image, Size, Size);
			var BitmapPointer = Bitmap.GetHbitmap();
			var BitmapSource = Imaging.CreateBitmapSourceFromHBitmap
			(
				BitmapPointer,
				IntPtr.Zero,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions()
			);

			BitmapSource.Freeze();
			DeleteObject(BitmapPointer);
			Bitmap.Dispose();

			return BitmapSource;
		}
	}
}