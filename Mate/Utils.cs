using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using EnvDTE;

namespace Mate
{
	internal static class Utils
	{
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr Object);

		/// Get DTE of EnvDTE (GuID "04A72314-32E9-48E2-9B87-A63603454F3E").
		internal static DTE GetDTE()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return (DTE) Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
		}

		/// Get active document.
		private static Document GetActiveDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return GetDTE()?.ActiveDocument;
		}

		/// Get active document as `TextDocument`.
		internal static TextDocument GetTextDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return GetActiveDocument()?.Object() as TextDocument;
		}

		/// Get text of current active document.
		internal static string GetText()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var TextDocument = GetTextDocument();
			var Text = TextDocument?.StartPoint.CreateEditPoint().GetText(TextDocument.EndPoint);

			return Text;
		}

		/// \short Get current line (where cursor is placed) of active document.
		/// \spare `1`.
		internal static int GetCurrentLine()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var DTE = GetDTE();
			if (DTE == null) return -1;

			var ActiveDocument = DTE.ActiveDocument;
			if (ActiveDocument == null) return -1;

			var Selection = (TextSelection) DTE.ActiveDocument.Selection;
			if (Selection == null) return 1;

			return Selection.CurrentLine;
		}

		/// \short           Get icon as `BitmapSource` from `ImageMoniker`.
		/// \param  Moniker  Moniker name of icon to get.
		/// \param  Size     Size of icon to get (render).

		internal static BitmapSource GetIconFromMoniker(ImageMoniker Moniker, int Size)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var Attributes = new ImageAttributes
			{
				Flags         = (uint) _ImageAttributesFlags.IAF_RequiredFlags,
				Format        = (uint) _UIDataFormat.DF_WPF,
				ImageType     = (uint) _UIImageType.IT_Bitmap,
				LogicalHeight = Size,
				LogicalWidth  = Size,
				StructSize    = Marshal.SizeOf(typeof(ImageAttributes))
			};

			var Service = (IVsImageService2) Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsImageService));
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

			return BitmapSource;
		}
	}
}