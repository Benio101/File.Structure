using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using EnvDTE;

// \todo Review.

namespace Mate
{
	internal static class Utils
	{
		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr Object);

		internal static DTE GetDTE()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return (DTE) Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
		}

		private static Document GetActiveDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return GetDTE()?.ActiveDocument;
		}

		internal static TextDocument GetTextDocument()
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			return GetActiveDocument()?.Object() as TextDocument;
		}

		internal static string GetText()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var TextDocument = GetTextDocument();
			var Text = TextDocument?.StartPoint.CreateEditPoint().GetText(TextDocument.EndPoint);

			return Text;
		}

		/// Move Cursor by $Offset positions (relative).
		internal static void MoveCursor(int Offset)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var DTE = GetDTE();
			if (DTE == null) return;

			var ActiveDocument = DTE.ActiveDocument;
			if (ActiveDocument == null) return;

			var Selection = (TextSelection) DTE.ActiveDocument.Selection;
			Selection.MoveToAbsoluteOffset(Selection.ActivePoint.AbsoluteCharOffset + Offset);
		}

		internal static int GetCurrentLine()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var DTE = GetDTE();
			if (DTE == null) return -1;

			var ActiveDocument = DTE.ActiveDocument;
			if (ActiveDocument == null) return -1;

			var Selection = (TextSelection) DTE.ActiveDocument.Selection;
			return Selection.CurrentLine;
		}

		// Check if $Source classifications contains any classification from $Search.
		internal static bool IsClassifiedAs
		(
			IReadOnlyCollection<string> Source,
			IReadOnlyCollection<string> Search
		)
		{
			return
			(
					Source.Count > 0
				&&	Search.Count > 0
				&&	(
						from SourceClassification in Source
						from SearchClassification in Search

						let SourceEntry = SourceClassification.ToLower()
						let SearchEntry = SearchClassification.ToLower()

						where
						(
								SourceEntry == SearchEntry
							||	SourceEntry.StartsWith(SearchEntry + ".")
						)

						select SourceEntry
					)

					.Any()
			);
		}

		// Check if $Source classifications contains $Search classification.
		internal static bool IsClassifiedAs
		(
			string[] Source,
			string   Search
		)
		{
			// ReSharper disable ConvertIfStatementToReturnStatement

			if (Source.Length == 0) return false;
			if (Search.Length == 0) return false;

			// ReSharper restore ConvertIfStatementToReturnStatement

			return IsClassifiedAs(Source, new[]{Search});
		}

		// Check if $Source classifications contains classification that matches $Search.
		internal static bool IsClassifiedAs
		(
			string[] Source,
			Regex    Search
		)
		{
			return
			(
					Source.Length > 0
				&&	Source.Select(SourceClassification => SourceClassification.ToLower())

					.Any(Search.IsMatch)
			);
		}

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