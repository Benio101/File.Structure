using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

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
		private const int EntriesOverFocus = 17;

		/// Types of regions supported by this extension.
		private enum Region
		{
			None = 0,

			Headers,
			Meta,
			Namespace,

			Usings,
			Macros,
			Friends,
			Components,
			Concepts,
			Classes,
			Structs,
			Unions,
			Members,
			Properties,
			Fields,
			Enums,
			EnumsUnscoped,
			Delegates,

			Setters,
			Getters,
			Overrides,
			Specials,
			Constructors,
			Methods,
			Operators,
			Conversions,
			Functions,
			Events,

			Using,
			Macro,
			Friend,
			Component,
			Concept,
			Class,
			Struct,
			Union,
			Member,
			Property,
			Field,
			Enum,
			EnumUnscoped,
			Delegate,

			Setter,
			Getter,
			Override,
			Special,
			Constructor,
			Method,
			Operator,
			Conversion,
			Function,
			Event,

			Public,
			Protected,
			Private,
		}

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

		#pragma warning disable IDE0060

		public Window(string Message)
		:
			this()
		{}

		#pragma warning restore IDE0060

		/// Focus entry in `File structure` window by given $LineNumber.
		private static async Task FocusEntryAsync(int LineNumber)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var Entries = Mate.Window.Entries;
			await TaskScheduler.Default;

			foreach (var EntryPair in Entries)
			{
				var EntryLineNumber  = EntryPair.Key;
				var Entry            = EntryPair.Value;

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				if (!(Entry.Children[0] is TextBlock LineNumberBlock)) return;
				await TaskScheduler.Default;

				if (EntryLineNumber == LineNumber)
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
					Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
					await TaskScheduler.Default;
				}
				else
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
					Entry.Background = null;
					await TaskScheduler.Default;
				}
			}
		}

		/// \short               Add new entry to `File structure` window.
		/// \param  Region       Region type of entry to add.
		/// \param  LineNumber   Line number of beginning of entry (first line is 1).
		/// \param  Value        Text Value to show in `File structure` window's entry (`""` for some types of regions).
		/// \param  IndentLevel  Level of indentation (0 to 4) of entry (0 means no indentation, 4 means indent 4×).

		private static async Task AddEntryAsync
		(
			Region  Region,
			int     LineNumber,
			string  Value,
			int     IndentLevel = 0
		)
		{
			await TaskScheduler.Default;

			// Clamp $IndentLevel between 0 and 4.
			if (IndentLevel < 0) IndentLevel = 0;
			if (IndentLevel > 4) IndentLevel = 4;

			if (Entries.ContainsKey(LineNumber))
				await RemoveEntryAsync(LineNumber);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			// Entry @Stack, horizontal.
			var Stack = new StackPanel
			{
				CanHorizontallyScroll = false,
				CanVerticallyScroll   = false,
				Margin                = new Thickness(0),
				Orientation           = Orientation.Horizontal,
			};

			await TaskScheduler.Default;

			// Entry click handler.
			Stack.MouseLeftButtonDown += (sender, args) =>
			{
				// Set cursor in document window at the line corresponding to the line of `File structure` window's clicked entry.
				// Focus clicked entry in `File structure` window.

				_ = Task.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					var DTE = await Utils.GetDTEAsync();
					if (DTE == null) return;

					var ActiveDocument = DTE.ActiveDocument;
					if (ActiveDocument == null) return;

					var Selection = DTE.ActiveDocument.Selection as TextSelection;
					if (Selection == null) return;

					await TaskScheduler.Default;

					var ActiveDocumentText = await Utils.GetTextAsync();
					var ActiveDocumentTextLines = ActiveDocumentText.Split('\n').Length;
					if (ActiveDocumentTextLines < LineNumber) return;

					Selection.MoveToLineAndOffset(LineNumber, 1);
					await FocusEntryAsync(LineNumber);
				});
			};

			// Entry's @Name: $Value to show in @NameBlock.
			var Name = Value;

			// Color of @Name.
			var NameColor = Color.FromRgb(224, 224, 224);

			// @Icon to show in @IconBlock.
			ImageSource Icon = await Utils.GetIconFromMonikerAsync(KnownMonikers.Blank, Size);

			// Set @Name, @NameColor, and @Icon, depending on $Region.
			switch (Region)
			{
				case Region.Headers:

					Name = "Headers";
					NameColor = Color.FromRgb(224, 224, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedWhite, Size);

					break;

				case Region.Meta:

					Name = "Meta";
					NameColor = Color.FromRgb(224, 224, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullWhite, Size);

					break;

				case Region.Namespace:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGray, Size);

					break;

				case Region.Usings:

					Name = "Usings";
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);

					break;

				case Region.Macros:

					Name = "Macros";
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPurple, Size);

					break;

				case Region.Friends:

					Name = "Friends";
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);

					break;

				case Region.Components:

					Name = "Components";
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);

					break;

				case Region.Concepts:

					Name = "Concepts";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGreen, Size);

					break;

				case Region.Classes:

					Name = "Classes";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Structs:

					Name = "Structs";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Unions:

					Name = "Unions";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Members:

					Name = "Members";
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);

					break;

				case Region.Properties:

					Name = "Properties";
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullOrange, Size);

					break;

				case Region.Fields:

					Name = "Fields";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);

					break;

				case Region.Enums:

					Name = "Enums";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Region.EnumsUnscoped:

					Name = "Enums (unscoped)";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Region.Delegates:

					Name = "Delegates";
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);

					break;

				case Region.Setters:

					Name = "Setters";
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);

					break;

				case Region.Getters:

					Name = "Getters";
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);

					break;

				case Region.Overrides:

					Name = "Overrides";
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);

					break;

				case Region.Specials:

					Name = "Specials";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);

					break;

				case Region.Constructors:

					Name = "Constructors";
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);

					break;

				case Region.Methods:

					Name = "Methods";
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);

					break;

				case Region.Operators:

					Name = "Operators";
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);

					break;

				case Region.Conversions:

					Name = "Conversions";
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);

					break;

				case Region.Functions:

					Name = "Functions";
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);

					break;

				case Region.Events:

					Name = "Events";
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);

					break;

				case Region.Using:

					Name = Value;
					NameColor = Color.FromRgb(128, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGray, Size);

					break;

				case Region.Macro:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPurple, Size);

					break;

				case Region.Friend:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullBlue, Size);

					break;

				case Region.Component:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullTurquoise, Size);

					break;

				case Region.Concept:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullGreen, Size);

					break;

				case Region.Class:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Struct:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Union:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedGreen, Size);

					break;

				case Region.Member:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullYellow, Size);

					break;

				case Region.Property:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullOrange, Size);

					break;

				case Region.Field:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullRed, Size);

					break;

				case Region.Enum:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Region.EnumUnscoped:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.SquareDottedRed, Size);

					break;

				case Region.Delegate:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.SquareFullPink, Size);

					break;

				case Region.Setter:

					Name = Value;
					NameColor = Color.FromRgb(176, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPurple, Size);

					break;

				case Region.Getter:

					Name = Value;
					NameColor = Color.FromRgb(128, 176, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullBlue, Size);

					break;

				case Region.Override:

					Name = Value;
					NameColor = Color.FromRgb(128, 224, 176);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullTurquoise, Size);

					break;

				case Region.Special:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullGreen, Size);

					break;

				case Region.Constructor:

					Name = Value;
					NameColor = Color.FromRgb(176, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedGreen, Size);

					break;

				case Region.Method:

					Name = Value;
					NameColor = Color.FromRgb(224, 224, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullYellow, Size);

					break;

				case Region.Operator:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullOrange, Size);

					break;

				case Region.Conversion:

					Name = Value;
					NameColor = Color.FromRgb(224, 176, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleDottedOrange, Size);

					break;

				case Region.Function:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 128);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullRed, Size);

					break;

				case Region.Event:

					Name = Value;
					NameColor = Color.FromRgb(224, 128, 224);
					Icon = Utils.GetIconFromBase64(Icons.CircleFullPink, Size);

					break;

				case Region.Public:

					Name = "Public";
					NameColor = Color.FromRgb(128, 176, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallDarkGreen, Size);

					break;

				case Region.Protected:

					Name = "Protected";
					NameColor = Color.FromRgb(152, 152, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallDarkYellow, Size);

					break;

				case Region.Private:

					Name = "Private";
					NameColor = Color.FromRgb(176, 128, 96);
					Icon = Utils.GetIconFromBase64(Icons.CircleSmallDarkRed, Size);

					break;
			}

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
					Source = await Utils.GetIconFromMonikerAsync(KnownMonikers.Blank, Size),
				};

				var IndentBlock1 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = await Utils.GetIconFromMonikerAsync(KnownMonikers.Blank, Size),
				};

				var IndentBlock2 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = await Utils.GetIconFromMonikerAsync(KnownMonikers.Blank, Size),
				};

				var IndentBlock3 = new Image
				{
					Margin = new Thickness(0, MarginSize, Size, MarginSize),
					Source = await Utils.GetIconFromMonikerAsync(KnownMonikers.Blank, Size),
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

				if (Region != Region.None)
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
					MaxWidth = Size * NameWidth,
				};

				Stack.Children.Add(NameBlock);

			#endregion
			#region Indent

				if (IndentLevel <= 3)
				{
					Stack.Children.Add(IndentBlock3);
					if (IndentLevel <= 2)
					{
						Stack.Children.Add(IndentBlock2);
						if (IndentLevel <= 1)
						{
							Stack.Children.Add(IndentBlock1);
							if (IndentLevel <= 0)
							{
								Stack.Children.Add(IndentBlock0);
							}
						}
					}
				}

			#endregion
			#region Icon

				if (Region == Region.None)
				Stack.Children.Add(IconBlock);

			#endregion

			await TaskScheduler.Default;
			Entries.Add(LineNumber, Stack);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Mate.Window.Stack.Children.Clear();
			foreach (var Entry in Entries)
			{
				Mate.Window.Stack.Children.Add(Entry.Value);
			}
		}

		/// Remove entry from `File structure` window at $LineNumber.
		private static async Task RemoveEntryAsync(int LineNumber)
		{
			await TaskScheduler.Default;
			if (!Entries.ContainsKey(LineNumber))
				return;

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Stack.Children.Remove(Entries[LineNumber]);

			await TaskScheduler.Default;
			Entries.Remove(LineNumber);
		}

		/// Remove all !Entries from `File structure` window.
		internal static async Task RemoveAllEntriesAsync()
		{
			await TaskScheduler.Default;
			Entries.Clear();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			Stack.Children.Clear();
		}

		/// \short    Update content of `File structure` window.
		/// \details  Read active document, regenerate entries and replace them with current ones.

		internal static async Task UpdateAsync()
		{
			await TaskScheduler.Default;

			var Text = await Utils.GetTextAsync();
			if (Text == null) return;

			var Reader = new StringReader(Text);
			var LineNumber = 0;
			string Line;
			var IndentLevel = 0;
			var bAccessLevelIndent = -1;

			await RemoveAllEntriesAsync();
			while ((Line = await Reader.ReadLineAsync()) != null)
			{
				++LineNumber;
				Match Match;

				// access:
				{
					var IsSpecifier = false;

					Match = Regex.Match(Line, "^[ \t\v\f]*(?<Access>(public|protected|private))[ \t\v\f]*:[ \t\v\f]*$");

					if (Match.Length == 0)
					{
						Match = Regex.Match(Line, "^[ \t\v\f]*#pragma[ \t\v\f]+region[ \t\v\f]+(?<Access>([Pp]ublic|[Pp]rivate|[Pp]rotected))");
					}
					else
					{
						IsSpecifier = true;
					}

					if (Match.Length > 0)
					{
						var Access = Match.Groups["Access"].Value.ToLower();
						var AIndentLevel = bAccessLevelIndent == -1 ? IndentLevel : IndentLevel - 1;
						switch (Access)
						{
							case "public":

								await AddEntryAsync(Region.Public, LineNumber, "", AIndentLevel);
								goto Match;

							case "protected":

								await AddEntryAsync(Region.Protected, LineNumber, "", AIndentLevel);
								goto Match;

							case "private":

								await AddEntryAsync(Region.Private, LineNumber, "", AIndentLevel);
								goto Match;

							default:

								break;

							Match:

								if (IsSpecifier)
								{
									if (bAccessLevelIndent == -1)
									{
										bAccessLevelIndent = IndentLevel;
										++IndentLevel;
									}
								}
								else
								{
									++IndentLevel;
								}

								break;
						}

						continue;
					}
				}

				// #pragma region
				{
					Match = Regex.Match(Line, "^[ \t\v\f]*#pragma[ \t\v\f]+region[ \t\v\f]+(?<Region>[^ \t\v\f]+)[ \t\v\f]*(?<Desc>.*)");
					if (Match.Length > 0)
					{
						var Region = Match.Groups["Region"].Value;
						var Desc = Match.Groups["Desc"].Value;
						var Value = "";

						// ReSharper disable once ConvertIfStatementToSwitchStatement
						if (Region == "enum")
						{
							if (Desc.StartsWith("class "))
							{
								Region = "enum class";
								Desc = Desc.Substring("class ".Length);
							}

							else if (Region == "enum" && Desc.StartsWith("struct "))
							{
								Region = "enum class";
								Desc = Desc.Substring("struct ".Length);
							}

							else
							{
								Region = "enum";
							}
						}

						else if (Region == "Enums")
						{
							Region = "Enums (unscoped)";
						}

						// ReSharper disable once ConvertIfStatementToSwitchStatement
						else if
						(
								Region == "Enum"
							&&	(
										Desc == "Classes"
									||	Desc == "classes"
									||	Desc == "Structs"
									||	Desc == "structs"
								)
						)
						{
							Region = "Enums";
							Desc = "";
						}

						var CurrentRegion = Mate.Window.Region.None;

						switch (Region)
						{
							case "Headers":          CurrentRegion = Mate.Window.Region.Headers;       goto Return;
							case "Meta":             CurrentRegion = Mate.Window.Region.Meta;          goto Return;
							case "Usings":           CurrentRegion = Mate.Window.Region.Usings;        goto Return;
							case "Macros":           CurrentRegion = Mate.Window.Region.Macros;        goto Return;
							case "Friends":          CurrentRegion = Mate.Window.Region.Friends;       goto Return;
							case "Components":       CurrentRegion = Mate.Window.Region.Components;    goto Return;
							case "Concepts":         CurrentRegion = Mate.Window.Region.Concepts;      goto Return;
							case "Classes":          CurrentRegion = Mate.Window.Region.Classes;       goto Return;
							case "Structs":          CurrentRegion = Mate.Window.Region.Structs;       goto Return;
							case "Unions":           CurrentRegion = Mate.Window.Region.Unions;        goto Return;
							case "Members":          CurrentRegion = Mate.Window.Region.Members;       goto Return;
							case "Properties":       CurrentRegion = Mate.Window.Region.Properties;    goto Return;
							case "Fields":           CurrentRegion = Mate.Window.Region.Fields;        goto Return;
							case "Enums":            CurrentRegion = Mate.Window.Region.Enums;         goto Return;
							case "Enums (unscoped)": CurrentRegion = Mate.Window.Region.EnumsUnscoped; goto Return;
							case "Delegates":        CurrentRegion = Mate.Window.Region.Delegates;     goto Return;
							case "Setters":          CurrentRegion = Mate.Window.Region.Setters;       goto Return;
							case "Getters":          CurrentRegion = Mate.Window.Region.Getters;       goto Return;
							case "Overrides":        CurrentRegion = Mate.Window.Region.Overrides;     goto Return;
							case "Specials":         CurrentRegion = Mate.Window.Region.Specials;      goto Return;
							case "Constructors":     CurrentRegion = Mate.Window.Region.Constructors;  goto Return;
							case "Methods":          CurrentRegion = Mate.Window.Region.Methods;       goto Return;
							case "Operators":        CurrentRegion = Mate.Window.Region.Operators;     goto Return;
							case "Conversions":      CurrentRegion = Mate.Window.Region.Conversions;   goto Return;
							case "Functions":        CurrentRegion = Mate.Window.Region.Functions;     goto Return;
							case "Events":           CurrentRegion = Mate.Window.Region.Events;        goto Return;
						}

						switch (Region)
						{
							case "namespace":        CurrentRegion = Mate.Window.Region.Namespace;     goto Match;
							case "using":            CurrentRegion = Mate.Window.Region.Using;         goto Match;
							case "macro":            CurrentRegion = Mate.Window.Region.Macro;         goto Match;
							case "friend":           CurrentRegion = Mate.Window.Region.Friend;        goto Match;
							case "component":        CurrentRegion = Mate.Window.Region.Component;     goto Match;
							case "concept":          CurrentRegion = Mate.Window.Region.Concept;       goto Match;
							case "class":            CurrentRegion = Mate.Window.Region.Class;         goto Match;
							case "struct":           CurrentRegion = Mate.Window.Region.Struct;        goto Match;
							case "union":            CurrentRegion = Mate.Window.Region.Union;         goto Match;
							case "member":           CurrentRegion = Mate.Window.Region.Member;        goto Match;
							case "property":         CurrentRegion = Mate.Window.Region.Property;      goto Match;
							case "field":            CurrentRegion = Mate.Window.Region.Field;         goto Match;
							case "enum class":       CurrentRegion = Mate.Window.Region.Enum;          goto Match;
							case "enum":             CurrentRegion = Mate.Window.Region.EnumUnscoped;  goto Match;
							case "delegate":         CurrentRegion = Mate.Window.Region.Delegate;      goto Match;
							case "setter":           CurrentRegion = Mate.Window.Region.Setter;        goto Match;
							case "getter":           CurrentRegion = Mate.Window.Region.Getter;        goto Match;
							case "override":         CurrentRegion = Mate.Window.Region.Override;      goto Match;
							case "special":          CurrentRegion = Mate.Window.Region.Special;       goto Match;
							case "constructor":      CurrentRegion = Mate.Window.Region.Constructor;   goto Match;
							case "method":           CurrentRegion = Mate.Window.Region.Method;        goto Match;
							case "operator":         CurrentRegion = Mate.Window.Region.Operator;      goto Match;
							case "conversion":       CurrentRegion = Mate.Window.Region.Conversion;    goto Match;
							case "function":         CurrentRegion = Mate.Window.Region.Function;      goto Match;
							case "event":            CurrentRegion = Mate.Window.Region.Event;         goto Match;

							default:

								Value = Region + " " + Desc;
								break;

							Match:

								Value = Desc;
								break;
						}

						Return:

							await AddEntryAsync(CurrentRegion, LineNumber, Value, IndentLevel);
							++IndentLevel;

						continue;
					}
				}

				// #pragma endregion
				{
					Match = Regex.Match(Line, "^[ \t\v\f]*#pragma[ \t\v\f]+endregion[ \t\v\f]*$");
					if (Match.Length > 0)
					{
						--IndentLevel;

						if (bAccessLevelIndent == IndentLevel)
						{
							bAccessLevelIndent = -1;
							--IndentLevel;
						}
					}
				}
			}

			Reader.Dispose();

			var CurrentLine = await Utils.GetCurrentLineAsync();
			await Mate.Window.ScrollToLineAsync(CurrentLine);
		}

		/// - Scroll `File structure` window to $LineNumber (or the nearest entry with line number smaller than $LineNumber).
		/// - Focus entry in `File structure` window by given $LineNumber.
		internal static async Task ScrollToLineAsync(int LineNumber)
		{
			#region Scroll

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				var Entries = Mate.Window.Entries;
				await TaskScheduler.Default;

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

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Scroll.ScrollToVerticalOffset(TargetEntryID * EntryHeight);
				await TaskScheduler.Default;

			#endregion
			#region Focus

				var CurrentEntryID = 0;
				foreach (var EntryPair in Entries)
				{
					var Entry = EntryPair.Value;

					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					if (!(Entry.Children[0] is TextBlock LineNumberBlock)) return;
					await TaskScheduler.Default;

					if (CurrentEntryID == EntryID)
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
						Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
						await TaskScheduler.Default;
					}
					else
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
						Entry.Background = null;
						await TaskScheduler.Default;
					}

					++CurrentEntryID;
				}

			#endregion
		}
	}
}