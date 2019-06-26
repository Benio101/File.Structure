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
	/// !Entries consists of the following column: @LineNumberBlock,  optional indent blocks, @ImageBlock and @NameBlock.

	[Guid("5f404ad9-a445-44ca-bae7-61fdadb5cb06")]
	internal sealed class Window
	:
		ToolWindowPane
	{
		private static readonly ScrollViewer                      Scroll      = new ScrollViewer();
		private static readonly StackPanel                        Stack       = new StackPanel();
		private static readonly SortedDictionary<int, StackPanel> Entries     = new SortedDictionary<int, StackPanel>();

		/// Font size.
		private const int Size = 13;

		/// Entry height.
		private const int EntryHeight = Size + 8;

		public Window()
		:
			base(null)
		{
			Stack.Orientation                    = Orientation.Vertical;
			Stack.CanHorizontallyScroll          = true;
			Stack.CanVerticallyScroll            = true;
			Stack.Margin                         = new Thickness(10);

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
				}

				else
				{
					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
				}
			}
		}

		/// \short               Add new entry to `File structure` window.
		/// \param  Region       Region type of entry to add.
		/// \param  LineNumber   Line number of beginning of entry (first line is 1).
		/// \param  Value        Text Value to show in `File structure` window's entry (`""` for some types of regions).
		/// \param  AccessLevel  Unused. Pass `Meta.AccessLevel.None`.
		/// \param  IndentLevel  Level of indentation (0 to 3) of entry (0 means no indentation, 3 means indent 3×).
		/// \todo                Remove $AccessLevel.

		internal static void AddEntry
		(
			Meta.Region      Region,
			int              LineNumber,
			string           Value,
			Meta.AccessLevel AccessLevel = Meta.AccessLevel.None,
			int              IndentLevel = 0
		)
		{
			if (Entries.ContainsKey(LineNumber))
				RemoveEntry(LineNumber);

			// Entry @Stack, horizontal.
			var Stack = new StackPanel
			{
				CanHorizontallyScroll = false,
				CanVerticallyScroll   = false,
				Margin                = new Thickness(2),
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

			// @Icon to show in @ImageBlock.
			ImageSource Icon = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size);

			// Unused.
			// \todo Remove.
			ImageSource AccessLevelIcon = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size);

			// Unused.
			// \todo Remove.
			var Indent = Meta.IndentType.Global;

			// If @Icon should be shown.
			// \todo Remove? Can icon ever not be shown?
			var ShowIcon = true;

			// Set @Name, @NameColor, and @Icon, depending on $Region.
			switch (Region)
			{
				case Meta.Region.Headers:

					Name = "Headers";
					NameColor = Color.FromRgb(224, 224, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedWhite, Size);
					Indent = Meta.IndentType.Global;

					break;

				case Meta.Region.Meta:

					Name = "Meta";
					NameColor = Color.FromRgb(224, 224, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullWhite, Size);
					Indent = Meta.IndentType.Global;

					break;

				case Meta.Region.Namespace:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGray, Size);
					Indent = Meta.IndentType.Global;

					break;

				case Meta.Region.Class:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);
					Indent = Meta.IndentType.Object;

					break;

				case Meta.Region.Struct:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);
					Indent = Meta.IndentType.Object;

					break;

				case Meta.Region.Union:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);
					Indent = Meta.IndentType.Object;

					break;

				case Meta.Region.Concept:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedBlue, Size);
					Indent = Meta.IndentType.Object;

					break;

				case Meta.Region.Macro:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPurple, Size);
					Indent = Meta.IndentType.Object;

					break;

				case Meta.Region.Public:

					Name = "Public";
					NameColor = Color.FromRgb(128, 176, 96);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);
					Indent = Meta.IndentType.Access;

					break;

				case Meta.Region.Protected:

					Name = "Protected";
					NameColor = Color.FromRgb(128, 176, 96);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);
					Indent = Meta.IndentType.Access;

					break;

				case Meta.Region.Private:

					Name = "Private";
					NameColor = Color.FromRgb(128, 176, 96);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallGreen, Size);
					Indent = Meta.IndentType.Access;

					break;

				case Meta.Region.Usings:

					Name = "Usings";
					NameColor = Color.FromRgb(128, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Friends:

					Name = "Friends";
					NameColor = Color.FromRgb(128, 176, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Enums:

					Name = "Enums";
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Components:

					Name = "Components";
					NameColor = Color.FromRgb(128, 224, 176);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Members:

					Name = "Members";
					NameColor = Color.FromRgb(224, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Delegates:

					Name = "Delegates";
					NameColor = Color.FromRgb(224, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Fields:

					Name = "Fields";
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Specials:

					Name = "Specials";
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Constructors:

					Name = "Constructors";
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Operators:

					Name = "Operators";
					NameColor = Color.FromRgb(224, 176, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Conversions:

					Name = "Conversions";
					NameColor = Color.FromRgb(224, 176, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Overrides:

					Name = "Overrides";
					NameColor = Color.FromRgb(128, 224, 176);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Methods:

					Name = "Methods";
					NameColor = Color.FromRgb(224, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Events:

					Name = "Events";
					NameColor = Color.FromRgb(224, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Getters:

					Name = "Getters";
					NameColor = Color.FromRgb(128, 176, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Setters:

					Name = "Setters";
					NameColor = Color.FromRgb(176, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Functions:

					Name = "Functions";
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);
					Indent = Meta.IndentType.Region;

					break;

				case Meta.Region.Using:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Friend:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Enum:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Component:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Member:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Delegate:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Field:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Special:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Constructor:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Operator:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Conversion:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Override:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Method:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Event:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Getter:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Setter:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);
					Indent = Meta.IndentType.Item;

					break;

				case Meta.Region.Function:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					ShowIcon = true;
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);
					Indent = Meta.IndentType.Item;

					break;
			}

			var LineNumberBlock = new TextBlock
			{
				Background    = new SolidColorBrush(Colors.Transparent),
				Focusable     = false,
				FontSize      = Size,
				FontWeight    = FontWeights.Normal,
				Foreground    = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
				Padding       = new Thickness(0, 0, Size, 0),
				Text          = LineNumber.ToString(),
				TextAlignment = TextAlignment.Right,
				Width         = Size * 3 + Size,
			};

			var GlobalIndentBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
			};

			var ObjectIndentBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
			};

			var RegionIndentBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
			};

			var AccessIndentBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = Utils.GetIconFromMoniker(KnownMonikers.Blank, Size),
			};

			// \todo Remove? Is it ever used?
			switch (AccessLevel)
			{
				case Meta.AccessLevel.Public:    AccessLevelIcon = Utils.GetIconFromBase64(Icons.KeyGreen,  Size); break;
				case Meta.AccessLevel.Protected: AccessLevelIcon = Utils.GetIconFromBase64(Icons.KeyYellow, Size); break;
				case Meta.AccessLevel.Private:   AccessLevelIcon = Utils.GetIconFromBase64(Icons.KeyRed,    Size); break;
			}

			// Unused.
			// \todo Remove.
			var AccessLevelBlock = new TextBlock
			{
				Background = new SolidColorBrush(Colors.Transparent),
				Focusable = false,
				FontSize = Size,
				FontWeight = FontWeights.Normal,
				Foreground = new SolidColorBrush(Color.FromRgb(128, 176, 96)),
				Padding = new Thickness(0, 0, Size, 0),
				Text = Meta.AccessLevelToString(AccessLevel),
				TextAlignment = TextAlignment.Left,
			};

			var AccessLevelIconBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = AccessLevelIcon,
			};

			var ImageBlock = new Image
			{
				Margin = new Thickness(0, 0, Size, 0),
				Source = Icon,
			};

			var NameBlock = new TextBlock
			{
				Background = new SolidColorBrush(Colors.Transparent),
				Focusable = false,
				FontSize = Size,
				FontWeight = FontWeights.Normal,
				Foreground = new SolidColorBrush(NameColor),
				Padding = new Thickness(0, 0, 0, 0),
				Text = Name,
				TextAlignment = TextAlignment.Left,
				Width = Size * 15,
			};

			Stack.Children.Add(LineNumberBlock);

			//if (Indent != Meta.IndentType.Global)
			//{
			//	//Stack.Children.Add(GlobalIndentBlock);
			//	if (Indent != Meta.IndentType.Object)
			//	{
			//		Stack.Children.Add(ObjectIndentBlock);
			//		if (Indent != Meta.IndentType.Region && Indent != Meta.IndentType.Access)
			//		{
			//			Stack.Children.Add(RegionIndentBlock);
			//			if (Indent != Meta.IndentType.Access)
			//			{
			//				//Stack.Children.Add(AccessIndentBlock);
			//			}
			//		}
			//	}
			//}

			if (IndentLevel > 0)
			{
				Stack.Children.Add(GlobalIndentBlock);
				if (IndentLevel > 1)
				{
					Stack.Children.Add(ObjectIndentBlock);
					if (IndentLevel > 2)
					{
						Stack.Children.Add(RegionIndentBlock);
						if (IndentLevel > 3)
						{
							Stack.Children.Add(AccessIndentBlock);
						}
					}
				}
			}

			// Unused.
			// \todo Remove.
			// {

				if (ShowIcon)
				Stack.Children.Add(ImageBlock);

				if (AccessLevel != Meta.AccessLevel.None)
				Stack.Children.Add(AccessLevelIconBlock);

				//if (AccessLevel != Meta.AccessLevel.None)
				//Stack.Children.Add(AccessLevelBlock);

			// }

			Stack.Children.Add(NameBlock);

			// \todo perf: Rewrite.
			// Mate.Window.Stack.Children.Insert insted of .Clear and foreach .Add.

			Entries.Add(LineNumber, Stack);
			Mate.Window.Stack.Children.Clear();
			foreach (var Entry in Entries)
			{
				Mate.Window.Stack.Children.Add(Entry.Value);
			}
		}

		/// Remove entry from `File structure` window at $Line.
		private static void RemoveEntry(int Line)
		{
			if (!Entries.ContainsKey(Line))
				return;

			Stack.Children.Remove(Entries[Line]);
			Entries.Remove(Line);
		}

		/// Remove all !Entries from `File structure` window.
		public static void RemoveAllEntries()
		{
			Entries.Clear();
		}

		// Scroll `File structure` window to $Line (or the nearest entry with line number smaller than $Line).
		public static void ScrollToLine(int Line)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var EntryID = 0;
			foreach (var Entry in Entries)
			{
				var EntryLine = Entry.Key;

				if (EntryLine == Line) goto end;
				if (EntryLine >  Line) break;

				++EntryID;
			}	--EntryID; end:;

			// `10` is Stack.Margin's Thickness.
			// \todo Make it one variable. Is it always pixel?
			Scroll.ScrollToVerticalOffset(EntryID * EntryHeight + 10);

			// \todo Rename.
			// \todo Add note !ScrollToLine also focuses element.
			var EntryID2 = 0;
			foreach (var EntryPair in Entries)
			{
				var LineNumber = EntryPair.Key;
				var Entry      = EntryPair.Value;
				var LineBlock  = (TextBlock) Entry.Children[0];

				if (EntryID2 == EntryID)
				{
					LineBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
				}

				else
				{
					LineBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
				}

				++EntryID2;
			}
		}
	}
}