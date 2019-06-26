using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Mate
{
	[
		ContentType("C/C++"),
		Export(typeof(ITextViewCreationListener)),
		TextViewRole(PredefinedTextViewRoles.Editable)
	]

	internal class Refactor
	:
		ITextViewCreationListener
	{
		private ITextView TextView;
		private bool IsTextChanging;
		private static IClassifier Classifier;

		#pragma warning disable 649

		[Import]
		private readonly IClassifierAggregatorService ClassifierAggregatorService;

		#pragma warning restore 649

		public void TextViewCreated(ITextView TextView)
		{
			this.TextView = TextView;

			TextView.TextBuffer.Changed     += OnTextBufferChanged;
			TextView.TextBuffer.PostChanged += PostTextBufferChanged;

			Classifier = ClassifierAggregatorService.GetClassifier(this.TextView.TextBuffer);
			this.TextView.Caret.PositionChanged += OnCaretPositionChanged;

			ThreadHelper.ThrowIfNotOnUIThread();
			Meta.UpdateWindow();
		}

		private static void OnCaretPositionChanged
		(
			object                        Sender,
			CaretPositionChangedEventArgs Event
		)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var CurrentLine = Utils.GetCurrentLine();
			Window.ScrollToLine(CurrentLine);
		}

		private void OnTextBufferChanged
		(
			object                      Sender,
			TextContentChangedEventArgs Event
		)
		{
			if (IsTextChanging) return;
			if (Event.Changes == null) return;

			ThreadHelper.ThrowIfNotOnUIThread();
			IsTextChanging = true;

			foreach (var Change in Event.Changes)
			{
				if
				(
						Change.OldLength == 0
					&&	Change.NewText   == "꞉"
				)
				{
					var Line          = Event.After.GetLineFromPosition(Change.NewPosition);
					var LineNumber    = Event.After.GetLineNumberFromPosition(Change.NewPosition);
					var Intersections = Classifier.GetClassificationSpans(new SnapshotSpan(Line.Start, Line.End));

					foreach (var Intersection in Intersections)
					{
						Debug.Print(LineNumber + ": " + Intersection.ClassificationType.Classification);
					}

					using (var Edit = Event.After.TextBuffer.CreateEdit())
					{
						Edit.Replace(new Span(Change.NewPosition, Change.NewText.Length), "");
						Edit.Apply();
					}
				}

				if (Change.OldLength == 0)
				{
					// Text is being inserted.

					if
					(
							Change.NewText == "\n"
						||	Change.NewText == "\r\n"
					)
					{
						// New line is being inserted.

						var Line               = Event.Before.GetLineFromPosition(Change.NewPosition);
						var LineText           = Line.GetText();
						var LineTextWithoutTab = LineText.TrimStart('\t');

						if (LineTextWithoutTab.StartsWith("///"))
						{
							// Documentation comment is being extended.
							var NewText = Change.NewText;
							Copy:

							while (LineText.StartsWith("\t"))
							{
								LineText = LineText.Substring(1);
								NewText += "\t";
							}

							if (LineText.StartsWith(" "))
							{
								LineText = LineText.Substring(1);
								NewText += " ";

								goto Copy;
							}

							if (LineText.StartsWith("///"))
							{
								LineText = LineText.Substring(3);
								NewText += "///";

								goto Copy;
							}

							if (LineText.StartsWith("//"))
							{
								LineText = LineText.Substring(2);
								NewText += "//";

								goto Copy;
							}

							using (var Edit = Event.After.TextBuffer.CreateEdit())
							{
								Edit.Replace(new Span(Change.NewPosition, Change.NewText.Length), NewText);
								Edit.Apply();
							}
						}
					}
				}

				if
				(
						(
								Change.OldText == " "
							||	Change.OldText == "\t"
						)
					&&	Change.NewLength == 0
				)
				{
					var Line = Event.After.GetLineFromPosition(Change.NewPosition);
					var LineText = Line.GetText();
					var LineTextWithoutTab = LineText.TrimStart('\t');

					switch (LineTextWithoutTab)
					{
						case "///":

							using (var Edit = Event.After.TextBuffer.CreateEdit())
							{
								Edit.Delete(new Span(Change.NewPosition - 3, 3));
								Edit.Apply();
							}

							break;

						case "/// //":

							using (var Edit = Event.After.TextBuffer.CreateEdit())
							{
								Edit.Delete(new Span(Change.NewPosition - 2, 2));
								Edit.Apply();
							}

							break;
					}
				}
			}
		}

		private void PostTextBufferChanged(object Sender, EventArgs Event)
		{
			IsTextChanging = false;
		}
	}
}