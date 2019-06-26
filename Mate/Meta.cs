using System.IO;
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.Shell;

using EnvDTE;

namespace Mate
{
	internal static class Meta
	{
		internal enum Region
		{
			None = 0,

			Headers,
			Meta,
			Namespace,

			Class,
			Struct,
			Union,
			Concept,
			Macro,

			Public,
			Protected,
			Private,

			Usings,
			Friends,
			Enums,
			Components,
			Members,
			Delegates,
			Fields,
			Specials,
			Constructors,
			Operators,
			Conversions,
			Overrides,
			Methods,
			Events,
			Getters,
			Setters,
			Functions,

			Using,
			Friend,
			Enum,
			Component,
			Member,
			Delegate,
			Field,
			Special,
			Constructor,
			Operator,
			Conversion,
			Override,
			Method,
			Event,
			Getter,
			Setter,
			Function,
		}

		/// Remove trailing whitespaces from active document.
		internal static void RemoveTrailingWhitespaces()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var TextDocument = Utils.GetTextDocument();
			if (TextDocument == null) return;

			TextDocument.ReplacePattern
			(
				"(?<!///( //)?)[ \t\v\f\r]+(?=(\r|\n|$))", "",
				(int) vsFindOptions.vsFindOptionsRegularExpression
			);
		}

		/// Replace heading spaces with tabs (each pack of heading 4 spaces are replaced to a single tab character).
		internal static void FixHeadingSpaces()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var TextDocument = Utils.GetTextDocument();
			if (TextDocument == null) return;

			while (TextDocument.ReplacePattern
			(
				"(?<=(^|\n)\t*)(    )", "\t",
				(int) vsFindOptions.vsFindOptionsRegularExpression
			));
		}

		/// \short    Update content of `File structure` window.
		/// \details  Read active document, regenerate entries and replace them with current ones.

		internal static void UpdateWindow()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var Text = Utils.GetText();
			if (Text == null) return;

			var Reader = new StringReader(Text);
			var LineNumber = 0;
			string Line;
			var IndentLevel = 0;
			var bAccessLevelIndent = -1;

			Window.RemoveAllEntries();

			while ((Line = Reader.ReadLine()) != null)
			{
				++LineNumber;
				Match Match;

				// #pragma region
				{
					Match = Regex.Match(Line, "^[ \t\v\f]*#pragma[ \t\v\f]+region[ \t\v\f]+(?<Region>[^ \t\v\f]+)[ \t\v\f]*(?<Desc>.*)");
					if (Match.Length > 0)
					{
						var Region = Match.Groups["Region"].Value;
						var Desc = Match.Groups["Desc"].Value;
						var Value = "";

						// ReSharper disable once ConvertIfStatementToSwitchStatement
						if (Region == "enum" && Desc.StartsWith("class "))
						{
							Desc = Desc.Substring("class ".Length);
						}

						if (Region == "Enum" && Desc == "Classes")
						{
							Region = "Enums";
							Desc = "";
						}

						var CurrentRegion = Meta.Region.None;

						switch (Region)
						{
							case "Headers":      CurrentRegion = Meta.Region.Headers;      goto AddEntry;
							case "Meta":         CurrentRegion = Meta.Region.Meta;         goto AddEntry;
							case "Usings":       CurrentRegion = Meta.Region.Usings;       goto AddEntry;
							case "Friends":      CurrentRegion = Meta.Region.Friends;      goto AddEntry;
							case "Enums":        CurrentRegion = Meta.Region.Enums;        goto Return;
							case "Components":   CurrentRegion = Meta.Region.Components;   goto Return;
							case "Members":      CurrentRegion = Meta.Region.Members;      goto Return;
							case "Delegates":    CurrentRegion = Meta.Region.Delegates;    goto Return;
							case "Fields":       CurrentRegion = Meta.Region.Fields;       goto Return;
							case "Specials":     CurrentRegion = Meta.Region.Specials;     goto Return;
							case "Constructors": CurrentRegion = Meta.Region.Constructors; goto Return;
							case "Operators":    CurrentRegion = Meta.Region.Operators;    goto Return;
							case "Conversions":  CurrentRegion = Meta.Region.Conversions;  goto Return;
							case "Overrides":    CurrentRegion = Meta.Region.Overrides;    goto Return;
							case "Methods":      CurrentRegion = Meta.Region.Methods;      goto Return;
							case "Events":       CurrentRegion = Meta.Region.Events;       goto Return;
							case "Getters":      CurrentRegion = Meta.Region.Getters;      goto Return;
							case "Setters":      CurrentRegion = Meta.Region.Setters;      goto Return;
							case "Functions":    CurrentRegion = Meta.Region.Functions;    goto Return;
						}

						switch (Region)
						{
							case "namespace":    CurrentRegion = Meta.Region.Namespace;    goto Match;
							case "class":        CurrentRegion = Meta.Region.Class;        goto Match;
							case "struct":       CurrentRegion = Meta.Region.Struct;       goto Match;
							case "union":        CurrentRegion = Meta.Region.Union;        goto Match;
							case "concept":      CurrentRegion = Meta.Region.Concept;      goto Match;
							case "macro":        CurrentRegion = Meta.Region.Macro;        goto Match;
							case "using":        CurrentRegion = Meta.Region.Using;        goto Match;
							case "friend":       CurrentRegion = Meta.Region.Friend;       goto Match;
							case "enum":         CurrentRegion = Meta.Region.Enum;         goto Match;
							case "component":    CurrentRegion = Meta.Region.Component;    goto Match;
							case "member":       CurrentRegion = Meta.Region.Member;       goto Match;
							case "delegate":     CurrentRegion = Meta.Region.Delegate;     goto Match;
							case "field":        CurrentRegion = Meta.Region.Field;        goto Match;
							case "special":      CurrentRegion = Meta.Region.Special;      goto Match;
							case "constructor":  CurrentRegion = Meta.Region.Constructor;  goto Match;
							case "operator":     CurrentRegion = Meta.Region.Operator;     goto Match;
							case "conversion":   CurrentRegion = Meta.Region.Conversion;   goto Match;
							case "override":     CurrentRegion = Meta.Region.Override;     goto Match;
							case "method":       CurrentRegion = Meta.Region.Method;       goto Match;
							case "event":        CurrentRegion = Meta.Region.Event;        goto Match;
							case "getter":       CurrentRegion = Meta.Region.Getter;       goto Match;
							case "setter":       CurrentRegion = Meta.Region.Setter;       goto Match;
							case "function":     CurrentRegion = Meta.Region.Function;     goto Match;

							default:

								Value = Region + " " + Desc;
								break;

							Match:

								Value = Desc;
								break;
						}

						AddEntry:
						Return:

							Window.AddEntry(CurrentRegion, LineNumber, Value, IndentLevel);
							++IndentLevel;
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

				// access:
				{
					Match = Regex.Match(Line, "^[ \t\v\f]*(?<Access>(public|protected|private))[ \t\v\f]*:[ \t\v\f]*$");
					if (Match.Length > 0)
					{
						var Access = Match.Groups["Access"].Value;
						var AIndentLevel = bAccessLevelIndent == -1 ? IndentLevel : IndentLevel - 1;
						switch (Access)
						{
							case "public":

								Window.AddEntry(Region.Public, LineNumber, "", AIndentLevel);
								goto Match;

							case "protected":

								Window.AddEntry(Region.Protected, LineNumber, "", AIndentLevel);
								goto Match;

							case "private":

								Window.AddEntry(Region.Private, LineNumber, "", AIndentLevel);

								goto Match;

							default:

								break;

							Match:

								if (bAccessLevelIndent == -1)
								{
									bAccessLevelIndent = IndentLevel;
									++IndentLevel;
								}

								break;
						}
					}
				}
			}
		}
	}

	/// Icons used by extension, as strings encoded in Base64.
	internal static class Icons
	{
		// ReSharper disable UnusedMember.Global

		public const string SquareFullGray                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAJ0lEQVRIx+3NQQEAMAgEIF3ya64l5g8K0EmmDr06JhAIBAKBQPDHApCdAq9P4twiAAAAAElFTkSuQmCC";
		public const string SquareFullWhite                = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2N88ODBfwYaAiYGGoNRC0YtGLVg1IJRC0YtGLVg1ALqAADJ8wPPiy50nQAAAABJRU5ErkJggg==";
		public const string SquareFullYellow               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2N88KDhPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AABh5gNvWtN7lwAAAABJRU5ErkJggg==";
		public const string SquareFullGreen                = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2Pc8KDhPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AACteAM/mda+NwAAAABJRU5ErkJggg==";
		public const string SquareFullRed                  = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2N80NDwn4GGgImBxmDUglELRi0YtWDUglELRi0YtYA6AAD5agMPtp2M4AAAAABJRU5ErkJggg==";
		public const string SquareFullPurple               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2Pc0PDgPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AACtGAM/RxK1/gAAAABJRU5ErkJggg==";
		public const string SquareFullPink                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2N80PDgPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AABhhgNvhBdwXgAAAABJRU5ErkJggg==";
		public const string SquareFullBlue                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2Ns2PDgPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AACs6AM/O44cyAAAAABJRU5ErkJggg==";
		public const string SquareFullTurquoise            = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAKElEQVRIx2NseLDhPwMNARMDjcGoBaMWjFowasGoBaMWjFowagF1AACtGAM/iNCHOwAAAABJRU5ErkJggg==";
		public const string SquareDottedGray               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2NsaGj4z0BDwMRAYzBqwQiwgAWfZENDAyMxhuBL6qNxMGrBqAV0sIBxtMIZtWDwWwAAGr0JLzYhKdQAAAAASUVORK5CYII=";
		public const string SquareDottedWhite              = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2N88ODBfwYaAiYGGoNRC0aABSz4JBUUFBiJMQRfUh+Ng1ELRi2ggwWMoxXOqAWD3wIAIOALb2gUCzsAAAAASUVORK5CYII=";
		public const string SquareDottedYellow             = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2N88KDhPwMNARMDjcGoBSPAAhZ8kgoKDYzEGIIvqY/GwagFoxbQwQLG0Qpn1ILBbwEAyjUKrz6Syg8AAAAASUVORK5CYII=";
		public const string SquareDottedGreen              = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2Pc8KDhPwMNARMDjcGoBSPAAhZ8kgEKDYzEGIIvqY/GwagFoxbQwQLG0Qpn1ILBbwEAnicKT/AWNh8AAAAASUVORK5CYII=";
		public const string SquareDottedRed                = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2N80NDwn4GGgImBxmDUghFgAQs+SYWGBkZiDMGX1EfjYNSCUQvoYAHjaIUzasHgtwAActkJ72PEFV0AAAAASUVORK5CYII=";
		public const string SquareDottedBlue               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAOElEQVRIx2Ns2PDgPwMNARMDjcGoBSPAAhZ8kg0BCozEGIIvqY/GwagFoxbQwQLG0Qpn1ILBbwEAnQcKT4fmK6UAAAAASUVORK5CYII=";
		public const string CircleFullGray                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABKElEQVRIx7WWMU7DQBBFnzfiBEi5ATQWdxgpDQ1lKOgpkG8AzRwhFFT0FlKOQDNS+nRucJ8iV9iGgnFkOcZyEu8r19o/uyPP/5sxgKreAEtAgByY+6c9UAEGrFW1/k8jGxBW4IlxlH/bjgtlPeLPwAdwxWlEoFDVz/birCP+Cqy66yOZAQ8iEs1sc1TAT77ichYisjOz7aFF3vPqjLYMtStX1To0F5hQHNdSgMxP/0MaboP/56lYBh+iVEjwCU1FHlrjn4J5IDHBjSsV++ADlooquOWmwgKwTlhgHdzDywTiZdeL4oTisfGi4G5aA8WEBYom3Q55YGZbEYnA4kLxN1V97000M9uIyA64PyPVIvDSFqdPxG/yBVwDdyeE/qOqfo96VUz5bPkFWhdjWA1FDbwAAAAASUVORK5CYII=";
		public const string CircleFullWhite                = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA+UlEQVRIx72WrRHCQBBG3+0MLaDXgEkTNIHBg0kHYNIEJviYNEETMWCiaSEGs8fcAGEgc8uTydy3P8l+e4EP9H2/ANbACiiAub26AR1wBlpVvY5phA/CFbDhOxqgehcovBHfAkdgxm8MQKmqp/ShPInvgXqCOHamNo3XCizzmjzsYiUh6Xk3MfOxdhWqeo0tqjKKx3ZVAMGyv+DDUuw/92ItNkRerMQm1ItCkvH3YC44I2ZcXtzEBsyLTsxyvTgL0DoGaMU8vHEQb569aMgoPkQvEgCroswYoIzb7TEH5t+HDOKHdKv9d2UmlRQ/fvjGFszpq1tFzmvLHdHnUfL+xUUvAAAAAElFTkSuQmCC";
		public const string CircleFullYellow               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA+UlEQVRIx72WLRLCMBBGX3aGK6BjwPQSvQQGD6Y3AJNLYFpf00twiRowaK5Qg9kwGWgZymR5sp18+9Put3F84HYLK2ADlEABLPXVHeiBM9B5H65TGu6DcAC2fEcLhLFAbkR8B5yABfMYgMr70KQP5UX8ANQ/iKNnatV4r0Azr8nDPlbikp73P2Y+1a7C+3CNLQoZxWO7AoDT7C/YsBb9z63YiA6RFaXohFpRSDL+FiwFY0SNy4q76IBZ0YtarhVnATrDAJ2oh7cG4u2rFw0ZxYfoRQKgVVQZA1Rxuz3nQP37mEH8mG61/67MpJJi5odvdcE0X90qcl5bHn3rU9KvJc86AAAAAElFTkSuQmCC";
		public const string CircleFullOrange               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA/ElEQVRIx72WqxHCQBCGv9sZWkDHgEkTlBATgweTDsBcE5jEx8RQAk3EgEHTQgxmj7nhNSRzyyeTuX8fyf57ji9cj34BlMAKyIG5vroBPXACuqzwl08a7ouwB9b8Rgv4d4HcG/ENcABmjGMAqqzwTfxQnsR3QD1BHD1Tq8ZrBZp5TRq2oRIX9byfmPmnduVZ4S+hRT6heGiXB3Ca/RkblqL/uRWl6BBZsRKdUCtyicbfgrlgjKhxWXETHTArelHLteIkQGcYoBP18NZAvH32oiGh+BC8SAC0iiphgCpst8ccqH/vE4jv463235UZVZKP/PCtLpjmp1tFymvLHVTdVMJeEVksAAAAAElFTkSuQmCC";
		public const string CircleFullGreen                = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA/ElEQVRIx72WqxHCQBCGv9sZWkDHgEkTlBATgweTDsBcE5jEx8RQAk3EgEHTQgxmj7nhNSRzyyeTuX8fyf57ji8cr34BlMAKyIG5vroBPXACuiLzl08a7ouwB9b8Rgv4d4HcG/ENcABmjGMAqiLzTfxQnsR3QD1BHD1Tq8ZrBZp5TRq2oRIX9byfmPmnduVF5i+hRT6heGiXB3Ca/RkblqL/uRWl6BBZsRKdUCtyicbfgrlgjKhxWXETHTArelHLteIkQGcYoBP18NZAvH32oiGh+BC8SAC0iiphgCpst8ccqH/vE4jv463235UZVZKP/PCtLpjmp1tFymvLHVXNVMJOE/XZAAAAAElFTkSuQmCC";
		public const string CircleFullRed                  = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA+ElEQVRIx72WrRHCQBBG3+0MLaBjwKQJmsDgwaQDYrYJTPAxaYImYsCgaSEGs8fcQMIAc8uTydy3P8l+e4E3XFUXwBpYASUwt1c3oAdOQFeoXqY0whthBTZ8RgvoWKAwIr4FDsCM7xiAqlA9pg/lSXwPND+IY2ca03itwDJvyMMuVhKSnvc/Zj7VrrJQvcQWaUbx2C4FCJb9GR+WYv+5F2uxIfJiJTahXpSSjL8Hc8EZMePy4iY2YF70YpbrxUmAzjFAJ+bhrYN4++xFQ0bxIXqRAFgVVcYAVdxujzkw/64ziNfpVvvvykwqKb/88K0tmONHt4qc15Y7K89Vsp37ZBYAAAAASUVORK5CYII=";
		public const string CircleFullPurple               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA/ElEQVRIx72WqxHCQBCGv9sZWkDHgEkTlBATgweTDsBcE5jEx8RQAk3EgEHTQgxmj7nhNSRzyyeTuX8fyf57ji8c/XUBlMAKyIG5vroBPXACusJnl08a7ouwB9b8Rgv4d4HcG/ENcABmjGMAqsJnTfxQnsR3QD1BHD1Tq8ZrBZp5TRq2oRIX9byfmPmnduWFzy6hRT6heGiXB3Ca/RkblqL/uRWl6BBZsRKdUCtyicbfgrlgjKhxWXETHTArelHLteIkQGcYoBP18NZAvH32oiGh+BC8SAC0iiphgCpst8ccqH/vE4jv463235UZVZKP/PCtLpjmp1tFymvLHVetVMJTR85+AAAAAElFTkSuQmCC";
		public const string CircleFullPink                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA+UlEQVRIx72WLRLCMBBGX3aGK6BjwPQSvQQGD6Y3AJNLYFpf00twiRowaK5Qg9kwGWgZymR5sp18+9Put3F84BZuK2ADlEABLPXVHeiBM9D54K9TGu6DcAC2fEcLhLFAbkR8B5yABfMYgMoH36QP5UX8ANQ/iKNnatV4r0Azr8nDPlbikp73P2Y+1a7CB3+NLQoZxWO7AoDT7C/YsBb9z63YiA6RFaXohFpRSDL+FiwFY0SNy4q76IBZ0YtarhVnATrDAJ2oh7cG4u2rFw0ZxYfoRQKgVVQZA1Rxuz3nQP37mEH8mG61/67MpJJi5odvdcE0X90qcl5bHn/LU9JNInm9AAAAAElFTkSuQmCC";
		public const string CircleFullBlue                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA/ElEQVRIx72WqxHCQBCGv9sZWkDHgEkTlBATgweTDsBcE5jEx8RQAk3EgEHTQgxmj7nhNSRzyyeTuX8fyf57ji/443UBlMAKyIG5vroBPXACOl9kl08a7ouwB9b8Rgv4d4HcG/ENcABmjGMAKl9kTfxQnsR3QD1BHD1Tq8ZrBZp5TRq2oRIX9byfmPmnduW+yC6hRT6heGiXB3Ca/RkblqL/uRWl6BBZsRKdUCtyicbfgrlgjKhxWXETHTArelHLteIkQGcYoBP18NZAvH32oiGh+BC8SAC0iiphgCpst8ccqH/vE4jv463235UZVZKP/PCtLpjmp1tFymvLHVidVMKl0ca2AAAAAElFTkSuQmCC";
		public const string CircleFullTurquoise            = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA/ElEQVRIx72WqxHCQBCGv9sZWkDHgEkTlBATgweTDsBcE5jEx8RQAk3EgEHTQgxmj7nhNSRzyyeTuX8fyf57ji/463EBlMAKyIG5vroBPXACOp8Vl08a7ouwB9b8Rgv4d4HcG/ENcABmjGMAKp8VTfxQnsR3QD1BHD1Tq8ZrBZp5TRq2oRIX9byfmPmnduU+Ky6hRT6heGiXB3Ca/RkblqL/uRWl6BBZsRKdUCtyicbfgrlgjKhxWXETHTArelHLteIkQGcYoBP18NZAvH32oiGh+BC8SAC0iiphgCpst8ccqH/vE4jv463235UZVZKP/PCtLpjmp1tFymvLHVetVMImG2qvAAAAAElFTkSuQmCC";
		public const string CircleSmallGreen               = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAqElEQVRIx+2UsRHCMAwAHzqmICVHqJgiZeowQbbwFp7AfUqmoPRRhilSQiPuaOxYCqW/tGW9fZIFlcpWdmsB0+wOwAjcgLMsRyAAvm/cYhZMszsCd+CUCHkCXd+4l1ogN39kkv9KrqmX7DMHx4LkSMyY2swJBkUtB4ugVQguFoGGt0UQFYJoEQSFIFgEXlqQgjb1aoH0dbci+X605V+jopWCFo+KSmU7H2N1KiP3JMzrAAAAAElFTkSuQmCC";
		public const string CircleSmallYellow              = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAApklEQVRIx+2UsRECIRAAVzOrkNDxjazia3groAu6oAJ6sApDxhCr+FCTc8bkee7ekA3huIW546DT2cpuLaCUcAA8cAPOspyBBETnwmwWlBKOwB04LYQ8gdG58FIL5OaPSvJfyXXpJfvKQd+QHInxS5s1waSo5WQRDArBxSLQ8LYIskKQLYKkECSLIEoL0tCmUS2Qvh5XJN+PNv9rVAxS0OZR0els5wNuhinDsq3kgwAAAABJRU5ErkJggg==";
		public const string CircleSmallRed                 = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAApklEQVRIx+2UsRHCMAwAHzqmICVHqJgiM4QJvIW28ATegSkofZRhipTQiDuaOJZC6S9tWW+fZEGjsZXdWsAkcgACcAPOupyBBMROZHYLJpEjcAdOCyFPYOhEXmaB3vxRSP4ruS69ZF84GCqSozFhabMkGA21HD2C3iC4eAQW3h5BNgiyR5AMguQRRG1BKto0mgXa18OK5PvR5n+Nil4LWj0qGo3tfABXpCqDgocl4wAAAABJRU5ErkJggg==";
		public const string CircleDottedGreen              = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABRElEQVRIx72WLXLDMBCFP2nqe5i4xJfIEUxMUuyQHKFElwipuYlJjpBLiFQk9wgpWeBRd+UkjfuQR6t9z9o/yVHA+RoaoAd2QJuZI3AB5q4OyeJwBeIA7LkPExA0IaeQD8AJqHgMN+DY1WE0BYT8i7/hsBRxLyb/JeIWMY8rYUnALN890KyEq+3qkN5kIRTIkyRwWqx9nq9hL36aUCW2Dyd//22Qj5K4m1FtlRTEYPi/ezmu9ecmOYDYjrJXQ++liTSEEnkmEgzzzisdCpCymK+JTMYpWm/4zE+Upurj2RiWQP8EV28JRGW9kTq/C7JX64foZeSqVSR1vkZeFaro4gsJbYBTSWTRaNbYmL3McKskByBq4ZK1WOjiqatD2nzY/c+43vLC8VnLj8BBjvgobjm52miyoS0k3rr025zcfFW88tnyA435jfdQLevYAAAAAElFTkSuQmCC";
		public const string CircleDottedOrange             = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABRElEQVRIx72WLXLDMBCFP2nqe5i4xJfIEUxMUuyQHKFElwipuYlJjpBLiFQk9wgpWeBRd+UkjfuQR6t9z9o/yVHA9RwaoAd2QJuZI3AB5roLyeJwBeIA7LkPExA0IaeQD8AJqHgMN+BYd2E0BYT8i7/hsBRxLyb/JeIWMY8rYUnALN890KyEq627kN5kIRTIkyRwWqx9Xs9hL36aUCW2Dyd//22Qj5K4m1FtlRTEYPi/ezmu9ecmOYDYjrJXQ++liTSEEnkmEgzzzisdCpCymK+JTMYpWm/4zE+Upurj2RiWQP8EV28JRGW9kTq/C7JX64foZeSqVSR1vkZeFaro4gsJbYBTSWTRaNbYmL3McKskByBq4ZK1WOjiqe5C2nzY/c+43vLC8VnLj8BBjvgobjm52miyoS0k3rr025zcfFW88tnyA41pjfesQX+fAAAAAElFTkSuQmCC";
		public const string CircledTriangleLeftFullYellow  = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABOUlEQVRIx7WWPU7DQBSEv11DwQWoX0XDgUhBg4AoKKIgUEGzFyBQoWBDBaThDiiXSJVmS5QrWAKaNVop/lk76ymt9TcaW+/NKgJlrVHAJXAPrIElsAA+Rcyq6j3VAj4DhhVH5oApM1KB8GfgvOFoDoxFTOY/1A1wHQgH2AVSa81tUAIPfkZ7DYskqgaeAqd0Uw4cipiVroBnW8CLz2U2Ejj4C3BCHB0kHjwBXiPCAb4TD/4OHBNXP4m1Zgf4AAbE154G3oAj+tG+Br6A354M0G4gRj2ZrDWAiEl7Mln+D5ozuYhsslAlkzwCnkJXedOgbawKETMDJhHg89Jd5Ewegast4Hmxiyr7wJl0TTIu2q22cETMA3DdEn7nt1poJ09c2cetTC/JFLip+6GuYLJOtwovycDtruBryx+l1mottTvN/AAAAABJRU5ErkJggg==";
		public const string CircledTriangleRightFullYellow = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABQ0lEQVRIx7WWPU7DQBBG31pwBeppqHKJiDukAVJBEgkaSKigmQuEICEgcvirUBCX4BI00EzNFQKExkbBiu1NvHn9vm9m7d1ZRwFmugk0gDpQAzaApoiO8cQViBXYzll3IqL9pQLMtAVcAesla3siel4WEGXkp0DsIQfom2nXu4Ok8pjF6YrooDAg2fM3z8rncSyiF0VbpBXkAAMzPZrbQVL9O9WZAgciOsx20CAMDrg20042oE44HHBjpu3ZgBphccAwDXFmOgHWCM8U6ESsDgdsRcDnigKegWaUHLDQjIEdEf2KgNfA8kdgV0S/07/oJaD8AdhL5QCRiH4ATwHk98C+iP7k3UWTCvI7oJWV/wUkXRwuKb8F2vPk/waOiI6AswXlI6CTJ686MlP51HtkznRSK/nwsY8891VR8mzpAZc+coBfthRo0kSARdwAAAAASUVORK5CYII=";
		public const string TriangleTopFullYellow          = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAsklEQVRIx7XPwQ3CMAxA0W/TMzN0Dw4gMRELQMRmSAySDViBS0EqbYOdOL5EkWLlfXBMzumQczp6dgZ8c5/Os3VBPHrgOV1P45gelj2t0ANcQwt+9HgqtELvqpBKvblCK/XmCmnQmyq0QW+qkEb93wpt1H/m5ipw6osVGqAvVkiQfrNCg/SbFRKoX63QQP1qhQTrFxUarF9USAf9rEI76GcV0kn/rRiAC/AqPNoB+8oP0hsXIkddD5dS6wAAAABJRU5ErkJggg==";
		public const string TriangleBottomFullYellow       = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAA0UlEQVRIx7XPuQ3CQBCF4R/LMTVsKVQ2WtwAFdlA4D62BmKQSIyF7b2PifbQ6L3vZIy+Axfy5gV8PP9TD2jgkRlwDvzfOqX0E7hTfyal9Nwtl2uDAAHoABooJqX0vAY0UMjvsAZUVKzt94JaCvm/bAIqKDbtbYJShewfDgEFikN7lyBXIbZHa0CGwtreJ0hViOvDGbAoppL2IQHAUNI+GBCh8LaPEYQUEloOBngUwfaxApdCYhajAiyKqPYpgr1CaDHG6NEYPabs9IkZA/BOWfgCkpVGh1/BFhEAAAAASUVORK5CYII=";
		public const string TriangleLeftFullYellow         = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAmElEQVRIx7XVyw1AQBCA4X+05SB0pAFEpTqYxNnBBRHPfcxMAf+XzLIrOM00DRUwiFO4B2oA8QrvI17hbOAvnAyEhqOB2HAwsIU7oElZpXiFXwGr8A2wDh+AV3ifAlhwnPOKSmA0X9HDIZtCX5+pBTSH/Gg5kMZcFSmQplx2MZDmXNchkFo8OF+QWj6ZT5B6PPpnyB64QO0KaZVSp+p8teMAAAAASUVORK5CYII=";
		public const string TriangleRightFullYellow        = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAApElEQVRIx7XSzQ3CMAxA4YfJINmDG4MhwzysAAUJ9vAmcAIVKQr5sX2qmuh9SVXM9GymO4JGgD3wMNNLBCSr5xBICu9cIamsuUDSsGcKko69Q5AM3LoLEmA7+HmboI2Zvpz+yCtwyFmfUUARigB+ICF2Ugo6+SlnvQOkqPD3ClFhD6AangEW4PgvPAJ0hXuAoXALMBWuAS7hEuAaXgM3QL3Dn3kD0mloZtedSuEAAAAASUVORK5CYII=";
		public const string KeyGreen                       = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAArUlEQVRIx+3SwQ3CMAyF4Q/EGVZhBGCCdINu0rBJNyATIEboKmUBuLgXhLikCCH1l3yILdnxe2ZhoZYV5NLu0eEU+SvOOfVD9YBofsPupTbiUDtkHT/fYUATMUSum2ODEVs0OfUlJEu4zOHB+tsmb8LQhC6XdspP0pSc+uZTg1zaB+TUr969NzjjiP2LLGPU6iSKKzmg4B5R5rigaQPRqPmWB1VMWv/sihYW/oAnDEM0byN8q5AAAAAASUVORK5CYII=";
		public const string KeyYellow                      = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAr0lEQVRIx+3SsQ3CQAyF4Q9EDavcCEAWIKNkgogJMgpZIIgRbpWwADROgxDNBSGk/JKLsyX7/J5ZWChlBUNuElocI3/FuUpdLh4QzW/YvdRG7EuHrOPnO2TUETly7RwbjNiirlLXh2QnXObwYP1tkzdh6AntkJspP0nTV6mrPzUYcvOAKnWrd+8NzjggvcgyRq1MoriSPXrcI/o5LmjaQDSqv+VBEZPWP7uihYU/4AkfwzMlCtyGNQAAAABJRU5ErkJggg==";
		public const string KeyRed                         = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAArElEQVRIx+3SwQ3CMAyF4Q/EGVbJCMAEdJRMUDFBRqETIEbIKmUBuLgXhLikCCH1l3yILdnxe2ZhoZUV1JwTehwjf8U5lVKbB0TzG3YvtRH71iHr+PkOFV1EjVw/xwYjtuhSKUNIdsJlDg/W3zZ5E4ae0Necp/wkzZBK6T41qDk/IJWyevfe4IwD0ossY9TaJIor2WPAPWKY44KmDUSj7lseNDFp/bMrWlj4A57VKDQJoZg0hgAAAABJRU5ErkJggg==";
		public const string StarGreen                      = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABiUlEQVRIx7XVsWtUQRAG8N+FIGqRHCLaaFDBBBNObSyFt52ptJOHlQiCoCCWoiBYCHY2Fva65D+Iory1t1EsYmPONoggoqiF2Cxynpd4ez6nnJn9vvlmdnYpsLqpunVTbS85M6XMruFiyYFOQfWzeIdvOBhD+tK2gquYxZ4SFZ2C6vvoZtd7HIghfW5LwZUBcNiNS60oqJtqJve+OxT6kFV8+lcFl0eAw64cG09B3VQ7sIQe5nEYC9m3ldI+1vEGr/EW6zGkNejUTXULF7BPu/YD96ZwN1fQtj3F9U5uz06s4mRL4E9wOob0dXAGbZH8Av/tFuXVP4XnbYH/cU0zyTKeTQD+eBh800Wrm2ouL1eJHYshvRp30fZPoGChZJOXJiA4UkJwdAKCxRKC+U38a3jxPwj6OI/FGNIJnMHwQHtjPdd1U23L3yJs4DYexJC+j8g9i5sDMzseQ3o5mDM9grSX3/o7uL/V3xtDWsFK3VTncCO36a8EGzgUQ/o47nRjSA/rpnqEvcOxn/Sab3zklA7IAAAAAElFTkSuQmCC";
		public const string StarYellow                     = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABhklEQVRIx7XVMWtUQRQF4O+FIGphFhGxMKKFCSas2lgK8zotHrGzsBJBEBTEUhQEC8HOxsJ/kHoaFWWevU3EIjbJ2gaxEUUtxGaQdd3EnfV5yjsz59xzZ+5cCpBi6KUYdpecmVGGW7hacqAqyH4O7/ENx+qm/dK1g5uYw8ESF1VB9gP0cugDjtZN+7krBzeGyOEArnXiIMWwL9e+N7L0Mbv49K8Oro8hh/15bTIHKYY9WEYfCziOxRzbyekAm3iHt9jAZt2061ClGO7hCg7rFj/waAYPcwZd4wVuV7k8e/EUZzsif46Vumm/Dt9BVyK/yH97Rbn1z+FVV+R/PNMsch4vpyB/Nkq+baOlGI7k5irBqbpp30zaaPNTOFgs6eTlKQROlAicnEJgqURgYZv4Ol7/D4EBLmOpbtozuIDRC+1P9F2nGHblsQhbuI8nddN+H7P3Iu4O3dnpumnXhvfMjhHt57/+AR7vNHvrpl3FaorhEu7kMq39bcDM5xFZhBRDlWI4NBr/CayKbXBJqusWAAAAAElFTkSuQmCC";
		public const string StarRed                        = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAABgElEQVRIx7XVMWtUQRQF4O8FEU1hFhGxMEELE0xYtbEU7uu0ip2FlQiCoCCWoiBYCHY2Fv6D/AMVZcbeJmKRNGZtRWxEUQuxGWRdN3FnfZ7yzsw599yZO5cKpIheithTc2ZGHW7iSs2BpiL7ObzDNxxtc/7StYMbmMPBGhdNRfYD9EroA460OX/uysH1IXI4gKudOEgR+0rteyNLH4uLT//q4NoYcthf1iZzkCL2YgV9LOIYlkpsJ6cDbGETb/AWW23OG9CkiLu4jMO6xQ88nMGDkkHXeI5bTSnPLJ7gTEfkz7Da5vx1+A66EvlF/tsrKq1/Fi+7Iv/jmRaRc3gxBfnTUfJtGy1FLJTmqsHJNufXkzba/BQOlmo6eWUKgeM1AiemEFiuEVjcJr6BV/9DYIBLWG5zPo3zGL3Q/kTfdYrYXcYivMc9PG5z/j5m7wXcGbqzU23O68N7do0R7Ze//j4e7TR725zXsJYiLuJ2KdP63wbMfBmRVUgRTYo4NBr/Cb2bbP0yzc/uAAAAAElFTkSuQmCC";

		// ReSharper restore UnusedMember.Global
	}
}