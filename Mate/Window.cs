using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

using EnvDTE;

namespace Mate
{
	/// \short
	/// `File structure` window.
	/// 
	/// \details
	/// `File structure` window's content is !Scroll containing a vertical !Stack.
	/// !Stack contains !Entries for each of regions. !Entries are horizontal stacks, rows of `File structure` window wrapped in a !Scroll.
	/// !Entries consists of the following column: @LineNumberBlock,  optional indent blocks, @IconBlock and @NameBlock.

	[Guid("5f404ad9-a445-44ca-bae7-61fdadb5cb06")]
	internal sealed class Window
	:
		ToolWindowPane
	{
		/// `File structure` window's content. Contains !Stack with !Entries.
		private static readonly ScrollViewer Scroll = new ScrollViewer();

		/// Content of !Scroll of `File structure` window. Contains !Entries.
		private static readonly StackPanel Stack = new StackPanel();

		/// \short
		/// List of entries from `File structure` window.
		///
		/// \details
		/// - Key is a line number of entry.
		/// - Value is entry row of !Stack, which is content of !Scroll, which is content of `File structure` window.
		private static readonly SortedDictionary<int, StackPanel> Entries = new SortedDictionary<int, StackPanel>();

		/// Font size.
		private const int Size = 13;

		/// Margin size of `File structure` window.
		private const int MarginSize = 2;

		/// Width of @LineNumberBlock (coefficient of !Size).
		private const int LineNumberWidth = 3;

		/// Width of @NameBlock (coefficient of !Size).
		private const int NameWidth = 15;

		/// Entry height.
		private const int EntryHeight = Size + MarginSize * 4;

		/// Number of entries to display over focused entry when focusing it upon mouse click on active document.
		private const int EntriesOverFocus = 15;

		public Window()
		:
			base(null)
		{
			Stack.Orientation                    = Orientation.Vertical;
			Stack.CanHorizontallyScroll          = true;
			Stack.CanVerticallyScroll            = true;
			Stack.Margin                         = new Thickness(0);

			Scroll.Content                       = Stack;
			Scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

			Caption                              = "File structure";
			BitmapImageMoniker                   = KnownMonikers.Code;
			Content                              = Scroll;
		}

		/// Focus entry in `File structure` window by given $LineNumber.
		private static void FocusEntry(int LineNumber)
		{
			foreach (var EntryPair in Entries)
			{
				var EntryLineNumber  = EntryPair.Key;
				var Entry            = EntryPair.Value;
				var LineNumberBlock  = (TextBlock) Entry.Children[0];

				if (EntryLineNumber == LineNumber)
				{
					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
					Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
				}

				else
				{
					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
					Entry.Background = null;
				}
			}
		}

		/// \short               Add new entry to `File structure` window.
		/// \param  Region       Region type of entry to add.
		/// \param  LineNumber   Line number of beginning of entry (first line is 1).
		/// \param  Value        Text Value to show in `File structure` window's entry (`""` for some types of regions).
		/// \param  IndentLevel  Level of indentation (0 to 4) of entry (0 means no indentation, 4 means indent 4×).

		internal static void AddEntry
		(
			Meta.Region      Region,
			int              LineNumber,
			string           Value,
			int              IndentLevel = 0
		)
		{
			// Clamp $IndentLevel between 0 and 4.
			if (IndentLevel < 0) IndentLevel = 0;
			if (IndentLevel > 4) IndentLevel = 4;

			if (Entries.ContainsKey(LineNumber))
				RemoveEntry(LineNumber);

			// Entry @Stack, horizontal.
			var Stack = new StackPanel
			{
				CanHorizontallyScroll = false,
				CanVerticallyScroll   = false,
				Margin                = new Thickness(0),
				Orientation           = Orientation.Horizontal,
			};

			// Entry click handler.
			Stack.MouseLeftButtonDown += (sender, args) =>
			{
				// Set cursor in document window at the line corresponding to the line of `File structure` window's clicked entry.
				// Focus clicked entry in `File structure` window.

				ThreadHelper.ThrowIfNotOnUIThread();

				var DTE = Utils.GetDTE();
				if (DTE == null) return;

				var ActiveDocument = DTE.ActiveDocument;
				if (ActiveDocument == null) return;

				var Selection = (TextSelection) DTE.ActiveDocument.Selection;
				if (Selection == null) return;

				var ActiveDocumentTextLines = Utils.GetText().Split('\n').Length;
				if (ActiveDocumentTextLines < LineNumber) return;

				Selection.MoveToLineAndOffset(LineNumber, 1);
				FocusEntry(LineNumber);
			};

			// Entry's @Name: $Value to show in @NameBlock.
			var Name = Value;

			// Color of @Name.
			var NameColor = Color.FromRgb(224, 224, 224);

			// @Icon to show in @IconBlock.
			ImageSource Icon = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size);

			// Set @Name, @NameColor, and @Icon, depending on $Region.
			switch (Region)
			{
				case Meta.Region.Headers:

					Name = "Headers";
					NameColor = Color.FromRgb(224, 224, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedWhite, Size);

					break;

				case Meta.Region.Meta:

					Name = "Meta";
					NameColor = Color.FromRgb(224, 224, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullWhite, Size);

					break;

				case Meta.Region.Namespace:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGray, Size);

					break;

				case Meta.Region.Class:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Meta.Region.Struct:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Meta.Region.Union:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Meta.Region.Concept:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedBlue, Size);

					break;

				case Meta.Region.Macro:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPurple, Size);

					break;

				case Meta.Region.Public:

					Name = "Public";
					NameColor = Color.FromRgb(128, 176, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);

					break;

				case Meta.Region.Protected:

					Name = "Protected";
					NameColor = Color.FromRgb(128, 176, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);

					break;

				case Meta.Region.Private:

					Name = "Private";
					NameColor = Color.FromRgb(128, 176, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);

					break;

				case Meta.Region.Usings:

					Name = "Usings";
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);

					break;

				case Meta.Region.Friends:

					Name = "Friends";
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);

					break;

				case Meta.Region.Enums:

					Name = "Enums";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Meta.Region.Components:

					Name = "Components";
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);

					break;

				case Meta.Region.Members:

					Name = "Members";
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);

					break;

				case Meta.Region.Delegates:

					Name = "Delegates";
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);

					break;

				case Meta.Region.Fields:

					Name = "Fields";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);

					break;

				case Meta.Region.Specials:

					Name = "Specials";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);

					break;

				case Meta.Region.Constructors:

					Name = "Constructors";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);

					break;

				case Meta.Region.Operators:

					Name = "Operators";
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);

					break;

				case Meta.Region.Conversions:

					Name = "Conversions";
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);

					break;

				case Meta.Region.Overrides:

					Name = "Overrides";
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);

					break;

				case Meta.Region.Methods:

					Name = "Methods";
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);

					break;

				case Meta.Region.Events:

					Name = "Events";
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);

					break;

				case Meta.Region.Getters:

					Name = "Getters";
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);

					break;

				case Meta.Region.Setters:

					Name = "Setters";
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);

					break;

				case Meta.Region.Functions:

					Name = "Functions";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);

					break;

				case Meta.Region.Using:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);

					break;

				case Meta.Region.Friend:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);

					break;

				case Meta.Region.Enum:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Meta.Region.Component:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);

					break;

				case Meta.Region.Member:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);

					break;

				case Meta.Region.Delegate:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);

					break;

				case Meta.Region.Field:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);

					break;

				case Meta.Region.Special:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);

					break;

				case Meta.Region.Constructor:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);

					break;

				case Meta.Region.Operator:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);

					break;

				case Meta.Region.Conversion:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);

					break;

				case Meta.Region.Override:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);

					break;

				case Meta.Region.Method:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);

					break;

				case Meta.Region.Event:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);

					break;

				case Meta.Region.Getter:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);

					break;

				case Meta.Region.Setter:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);

					break;

				case Meta.Region.Function:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);

					break;
			}

			#region Line number

				var LineNumberBlock = new TextBlock
				{
					Background    = new SolidColorBrush(Colors.Transparent),
					Focusable     = false,
					FontSize      = Size,
					FontWeight    = FontWeights.Normal,
					Foreground    = new SolidColorBrush(Color.FromRgb(96, 96, 96)),
					Padding       = new Thickness(0, 0, Size, 0),
					Text          = LineNumber.ToString(),
					TextAlignment = TextAlignment.Right,
					Width         = Size * LineNumberWidth + Size,
					Margin        = new Thickness(0, MarginSize, 0, MarginSize),
				};

				Stack.Children.Add(LineNumberBlock);

			#endregion
			#region Indent

				var IndentBlock0 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
				};

				var IndentBlock1 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
				};

				var IndentBlock2 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
				};

				var IndentBlock3 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
				};

				if (IndentLevel > 0)
				{
					Stack.Children.Add(IndentBlock0);
					if (IndentLevel > 1)
					{
						Stack.Children.Add(IndentBlock1);
						if (IndentLevel > 2)
						{
							Stack.Children.Add(IndentBlock2);
							if (IndentLevel > 3)
							{
								Stack.Children.Add(IndentBlock3);
							}
						}
					}
				}

			#endregion
			#region Icon

				var IconBlock = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = Icon,
				};

				Stack.Children.Add(IconBlock);

			#endregion
			#region Name

				var NameBlock = new TextBlock
				{
					Background = new SolidColorBrush(Colors.Transparent),
					Focusable = false,
					FontSize = Size,
					FontWeight = FontWeights.Normal,
					Foreground = new SolidColorBrush(NameColor),
					Padding = new Thickness(0, MarginSize, 0, MarginSize),
					Text = Name,
					TextAlignment = TextAlignment.Left,
					Width = Size * NameWidth,
				};

				Stack.Children.Add(NameBlock);

			#endregion

			// \todo perf: Rewrite.
			// Mate.Window.Stack.Children.Insert insted of .Clear and foreach .Add.

			Entries.Add(LineNumber, Stack);
			Mate.Window.Stack.Children.Clear();
			foreach (var Entry in Entries)
			{
				Mate.Window.Stack.Children.Add(Entry.Value);
			}
		}

		/// Remove entry from `File structure` window at $LineNumber.
		private static void RemoveEntry(int LineNumber)
		{
			if (!Entries.ContainsKey(LineNumber))
				return;

			Stack.Children.Remove(Entries[LineNumber]);
			Entries.Remove(LineNumber);
		}

		/// Remove all !Entries from `File structure` window.
		public static void RemoveAllEntries()
		{
			Entries.Clear();
			Stack.Children.Clear();
		}

		/// - Scroll `File structure` window to $LineNumber (or the nearest entry with line number smaller than $LineNumber).
		/// - Focus entry in `File structure` window by given $LineNumber.
		public static void ScrollToLine(int LineNumber)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			#region Scroll

				var EntryID = 0;
				foreach (var Entry in Entries)
				{
					var EntryLineNumber = Entry.Key;

					if (EntryLineNumber == LineNumber) goto end;
					if (EntryLineNumber >  LineNumber) break;

					++EntryID;
				}	--EntryID; end:;

				var TargetEntryID = EntryID - EntriesOverFocus;
				if (TargetEntryID < 0) TargetEntryID = 0;

				Scroll.ScrollToVerticalOffset(TargetEntryID * EntryHeight);

			#endregion
			#region Focus

				var CurrentEntryID = 0;
				foreach (var EntryPair in Entries)
				{
					var Entry     = EntryPair.Value;
					var LineBlock = (TextBlock) Entry.Children[0];

					if (CurrentEntryID == EntryID)
					{
						LineBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
						Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
					}

					else
					{
						LineBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
						Entry.Background = null;
					}

					++CurrentEntryID;
				}

			#endregion
		}
	}
}