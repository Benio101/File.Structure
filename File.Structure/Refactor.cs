using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace File.Structure
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

			_ = Events.OnAfterTextViewCreateAsync();
		}

		private static void OnCaretPositionChanged
		(
			object                        Sender,
			CaretPositionChangedEventArgs Event
		)
		{
			_ = Events.OnAfterCaretPositionChangeAsync();
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
		}

		private void PostTextBufferChanged(object Sender, EventArgs Event)
		{
			IsTextChanging = false;
		}
	}
}