﻿
namespace SmartFormat.Core.Formatting {

	using System;
	using SmartFormat.Core.Parsing;

	/// <summary>
	/// An exception caused while attempting to output the format.
	/// </summary>
	public class FormattingException : Exception {

		public FormattingException(FormatItem errorItem, Exception formatException, int index) {
			Format = errorItem.baseString;
			ErrorItem = errorItem;
			Issue = formatException.Message;
			Index = index;
		}

		public FormattingException(FormatItem errorItem, string issue, int index) {
			Format = errorItem.baseString;
			ErrorItem = errorItem;
			Issue = issue;
			Index = index;
		}

		public string Format { get; }
		public FormatItem ErrorItem { get; }
		public string Issue { get; }
		public int Index { get; }

		public override string Message => string.Format("Error parsing format string: {0} at {1}\n{2}\n{3}",
			Issue,
			Index,
			Format,
			new string('-', Index) + "^");

	}

}