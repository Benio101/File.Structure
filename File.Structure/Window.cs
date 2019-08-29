using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace File.Structure
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
		private static async Task FocusEntryAsync
		(
			int               LineNumber,
			CancellationToken Token
		)
		{
			if (Token.IsCancellationRequested) return;
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var Entries = File.Structure.Window.Entries;

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

			foreach (var EntryPair in Entries)
			{
				var EntryLineNumber  = EntryPair.Key;
				var Entry            = EntryPair.Value;

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				if (Token.IsCancellationRequested) return;

				if (!(Entry.Children[0] is TextBlock LineNumberBlock)) return;

				await TaskScheduler.Default;
				if (Token.IsCancellationRequested) return;

				if (EntryLineNumber == LineNumber)
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					if (Token.IsCancellationRequested) return;

					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
					Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
					await TaskScheduler.Default;
				}
				else
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					if (Token.IsCancellationRequested) return;

					LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
					Entry.Background = null;
					await TaskScheduler.Default;
				}

				if (Token.IsCancellationRequested) return;
			}
		}

		/// \short               Add new entry to `File structure` window.
		/// \param  Region       Region type of entry to add.
		/// \param  LineNumber   Line number of beginning of entry (first line is 1).
		/// \param  Value        Text Value to show in `File structure` window's entry (`""` for some types of regions).
		/// \param  IndentLevel  Level of indentation (0 to 4) of entry (0 means no indentation, 4 means indent 4×).
		
		private static async Task AddEntryAsync
		(
			Region            Region,
			int               LineNumber,
			string            Value,
			CancellationToken Token,
			int               IndentLevel
		)
		{
			if (Token.IsCancellationRequested) return;

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

			// Clamp $IndentLevel between 0 and 4.
			if (IndentLevel < 0) IndentLevel = 0;
			if (IndentLevel > 4) IndentLevel = 4;

			if (Entries.ContainsKey(LineNumber))
				await RemoveEntryAsync(LineNumber, Token);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (Token.IsCancellationRequested) return;

			// Entry @Stack, horizontal.
			var Stack = new StackPanel
			{
				CanHorizontallyScroll = false,
				CanVerticallyScroll   = false,
				Margin                = new Thickness(0),
				Orientation           = Orientation.Horizontal,
			};

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

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
					await FocusEntryAsync(LineNumber, Token);
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
			if (Token.IsCancellationRequested) return;

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
			if (Token.IsCancellationRequested) return;

			Entries.Add(LineNumber, Stack);

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (Token.IsCancellationRequested) return;

			File.Structure.Window.Stack.Children.Clear();
			foreach (var Entry in Entries)
			{
				File.Structure.Window.Stack.Children.Add(Entry.Value);
			}
		}

		/// Remove entry from `File structure` window at $LineNumber.
		private static async Task RemoveEntryAsync
		(
			int               LineNumber,
			CancellationToken Token
		)
		{
			if (Token.IsCancellationRequested) return;

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

			if (!Entries.ContainsKey(LineNumber))
				return;

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (Token.IsCancellationRequested) return;

			Stack.Children.Remove(Entries[LineNumber]);

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

			Entries.Remove(LineNumber);
		}

		/// Remove all !Entries from `File structure` window.
		private static async Task RemoveAllEntriesAsync
		(
			CancellationToken Token
		)
		{
			if (Token.IsCancellationRequested) return;

			await TaskScheduler.Default;
			if (Token.IsCancellationRequested) return;

			Entries.Clear();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			if (Token.IsCancellationRequested) return;

			Stack.Children.Clear();
		}

		/// Remove all !Entries from `File structure` window.
		internal static async Task ClearAsync
		(
			CancellationToken Token
		)
		{
			if (Token.IsCancellationRequested) return;
			await RemoveAllEntriesAsync(Token);
		}

		/// \short    Update content of `File structure` window.
		/// \details  Read active document, regenerate entries and replace them with current ones.
		
		internal static async Task UpdateAsync
		(
			CancellationToken Token
		)
		{
			await TaskScheduler.Default;

			var Text = await Utils.GetTextAsync();
			if (Text == null) return;

			#pragma warning disable IDE0068
			var Reader = new StringReader(Text);
			#pragma warning restore IDE0068

			var LineNumber = 0;
			string Line;
			var IndentLevel = 0;
			var bAccessLevelIndent = -1;

			if (Token.IsCancellationRequested) return;
			await RemoveAllEntriesAsync(Token);

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

								if (Token.IsCancellationRequested) {Reader.Dispose(); return;}
								await AddEntryAsync(Region.Public, LineNumber, "", Token, AIndentLevel);
								goto Match;

							case "protected":

								if (Token.IsCancellationRequested) {Reader.Dispose(); return;}
								await AddEntryAsync(Region.Protected, LineNumber, "", Token, AIndentLevel);
								goto Match;

							case "private":

								if (Token.IsCancellationRequested) {Reader.Dispose(); return;}
								await AddEntryAsync(Region.Private, LineNumber, "", Token, AIndentLevel);
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

						var CurrentRegion = File.Structure.Window.Region.None;

						switch (Region)
						{
							case "Headers":          CurrentRegion = File.Structure.Window.Region.Headers;       goto Return;
							case "Meta":             CurrentRegion = File.Structure.Window.Region.Meta;          goto Return;
							case "Usings":           CurrentRegion = File.Structure.Window.Region.Usings;        goto Return;
							case "Macros":           CurrentRegion = File.Structure.Window.Region.Macros;        goto Return;
							case "Friends":          CurrentRegion = File.Structure.Window.Region.Friends;       goto Return;
							case "Components":       CurrentRegion = File.Structure.Window.Region.Components;    goto Return;
							case "Concepts":         CurrentRegion = File.Structure.Window.Region.Concepts;      goto Return;
							case "Classes":          CurrentRegion = File.Structure.Window.Region.Classes;       goto Return;
							case "Structs":          CurrentRegion = File.Structure.Window.Region.Structs;       goto Return;
							case "Unions":           CurrentRegion = File.Structure.Window.Region.Unions;        goto Return;
							case "Members":          CurrentRegion = File.Structure.Window.Region.Members;       goto Return;
							case "Properties":       CurrentRegion = File.Structure.Window.Region.Properties;    goto Return;
							case "Fields":           CurrentRegion = File.Structure.Window.Region.Fields;        goto Return;
							case "Enums":            CurrentRegion = File.Structure.Window.Region.Enums;         goto Return;
							case "Enums (unscoped)": CurrentRegion = File.Structure.Window.Region.EnumsUnscoped; goto Return;
							case "Delegates":        CurrentRegion = File.Structure.Window.Region.Delegates;     goto Return;
							case "Setters":          CurrentRegion = File.Structure.Window.Region.Setters;       goto Return;
							case "Getters":          CurrentRegion = File.Structure.Window.Region.Getters;       goto Return;
							case "Overrides":        CurrentRegion = File.Structure.Window.Region.Overrides;     goto Return;
							case "Specials":         CurrentRegion = File.Structure.Window.Region.Specials;      goto Return;
							case "Constructors":     CurrentRegion = File.Structure.Window.Region.Constructors;  goto Return;
							case "Methods":          CurrentRegion = File.Structure.Window.Region.Methods;       goto Return;
							case "Operators":        CurrentRegion = File.Structure.Window.Region.Operators;     goto Return;
							case "Conversions":      CurrentRegion = File.Structure.Window.Region.Conversions;   goto Return;
							case "Functions":        CurrentRegion = File.Structure.Window.Region.Functions;     goto Return;
							case "Events":           CurrentRegion = File.Structure.Window.Region.Events;        goto Return;
						}

						switch (Region)
						{
							case "namespace":        CurrentRegion = File.Structure.Window.Region.Namespace;     goto Match;
							case "using":            CurrentRegion = File.Structure.Window.Region.Using;         goto Match;
							case "macro":            CurrentRegion = File.Structure.Window.Region.Macro;         goto Match;
							case "friend":           CurrentRegion = File.Structure.Window.Region.Friend;        goto Match;
							case "component":        CurrentRegion = File.Structure.Window.Region.Component;     goto Match;
							case "concept":          CurrentRegion = File.Structure.Window.Region.Concept;       goto Match;
							case "class":            CurrentRegion = File.Structure.Window.Region.Class;         goto Match;
							case "struct":           CurrentRegion = File.Structure.Window.Region.Struct;        goto Match;
							case "union":            CurrentRegion = File.Structure.Window.Region.Union;         goto Match;
							case "member":           CurrentRegion = File.Structure.Window.Region.Member;        goto Match;
							case "property":         CurrentRegion = File.Structure.Window.Region.Property;      goto Match;
							case "field":            CurrentRegion = File.Structure.Window.Region.Field;         goto Match;
							case "enum class":       CurrentRegion = File.Structure.Window.Region.Enum;          goto Match;
							case "enum":             CurrentRegion = File.Structure.Window.Region.EnumUnscoped;  goto Match;
							case "delegate":         CurrentRegion = File.Structure.Window.Region.Delegate;      goto Match;
							case "setter":           CurrentRegion = File.Structure.Window.Region.Setter;        goto Match;
							case "getter":           CurrentRegion = File.Structure.Window.Region.Getter;        goto Match;
							case "override":         CurrentRegion = File.Structure.Window.Region.Override;      goto Match;
							case "special":          CurrentRegion = File.Structure.Window.Region.Special;       goto Match;
							case "constructor":      CurrentRegion = File.Structure.Window.Region.Constructor;   goto Match;
							case "method":           CurrentRegion = File.Structure.Window.Region.Method;        goto Match;
							case "operator":         CurrentRegion = File.Structure.Window.Region.Operator;      goto Match;
							case "conversion":       CurrentRegion = File.Structure.Window.Region.Conversion;    goto Match;
							case "function":         CurrentRegion = File.Structure.Window.Region.Function;      goto Match;
							case "event":            CurrentRegion = File.Structure.Window.Region.Event;         goto Match;

							default:

								Value = Region + " " + Desc;
								break;

							Match:

								Value = Desc;
								break;
						}

						Return:

							if (Token.IsCancellationRequested) {Reader.Dispose(); return;}
							await AddEntryAsync(CurrentRegion, LineNumber, Value, Token, IndentLevel);
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

			if (Token.IsCancellationRequested) return;
			var CurrentLine = await Utils.GetCurrentLineAsync();
			// ReSharper disable once ArrangeStaticMemberQualifier
			if (Token.IsCancellationRequested) return;
			await ScrollToLineAsync(CurrentLine, Token);
		}

		/// - Scroll `File structure` window to $LineNumber (or the nearest entry with line number smaller than $LineNumber).
		/// - Focus entry in `File structure` window by given $LineNumber.
		
		internal static async Task ScrollToLineAsync
		(
			int               LineNumber,
			CancellationToken Token
		)
		{
			if (Token.IsCancellationRequested) return;

			#region Scroll

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				if (Token.IsCancellationRequested) return;

				var Entries = File.Structure.Window.Entries;

				await TaskScheduler.Default;
				if (Token.IsCancellationRequested) return;

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
				if (Token.IsCancellationRequested) return;

				Scroll.ScrollToVerticalOffset(TargetEntryID * EntryHeight);

				await TaskScheduler.Default;
				if (Token.IsCancellationRequested) return;

			#endregion
			#region Focus

				var CurrentEntryID = 0;
				foreach (var EntryPair in Entries)
				{
					var Entry = EntryPair.Value;

					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					if (Token.IsCancellationRequested) return;

					if (!(Entry.Children[0] is TextBlock LineNumberBlock)) return;

					await TaskScheduler.Default;
					if (Token.IsCancellationRequested) return;

					if (CurrentEntryID == EntryID)
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						if (Token.IsCancellationRequested) return;

						LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
						Entry.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
						await TaskScheduler.Default;
					}
					else
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
						if (Token.IsCancellationRequested) return;

						LineNumberBlock.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));
						Entry.Background = null;
						await TaskScheduler.Default;
					}

					if (Token.IsCancellationRequested) return;
					++CurrentEntryID;
				}

			#endregion
		}
	}
}