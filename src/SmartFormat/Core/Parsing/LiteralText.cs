
namespace SmartFormat.Core.Parsing {

	using System;
	using SmartFormat.Core.Settings;

	/// <summary>
	/// Represents the literal text that is found
	/// in a parsed format string.
	/// </summary>
	public class LiteralText : FormatItem {

		public LiteralText(
			SmartSettings smartSettings,
			Format parent,
			int startIndex) :
			base(smartSettings, parent, startIndex) {

		}

		public LiteralText(SmartSettings smartSettings, Format parent) :
			base(smartSettings, parent, parent.startIndex) {

		}

		public override string ToString() {
			return SmartSettings.ConvertCharacterStringLiterals
				? ConvertCharacterLiteralsToUnicode()
				: baseString[this.startIndex..this.endIndex];
		}

		private string ConvertCharacterLiteralsToUnicode() {

			var source = baseString[this.startIndex..this.endIndex];

			// No character literal escaping - nothing to do
			if (source[0] != Parser.CharLiteralEscapeChar) {
				return source;
			}

			// The string length should be 2: espace character \ and literal character
			if (source.Length < 2) {
				throw new ArgumentException($"Missing escape sequence in literal: \"{source}\"");
			}

			var c = (source[1]) switch
			{
				'\'' => '\'',
				'\"' => '\"',
				'\\' => '\\',
				'0' => '\0',
				'a' => '\a',
				'b' => '\b',
				'f' => '\f',
				'n' => '\n',
				'r' => '\r',
				't' => '\t',
				'v' => '\v',
				_ => throw new ArgumentException($"Unrecognized escape sequence in literal: \"{source}\""),
			};

			return c.ToString();

		}

	}

}