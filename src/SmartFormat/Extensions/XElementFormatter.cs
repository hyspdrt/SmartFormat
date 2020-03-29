
namespace SmartFormat.Extensions {

	using System.Collections.Generic;
	using System.Xml.Linq;
	using SmartFormat.Core.Extensions;

	public class XElementFormatter : IFormatter {

		public string[] Names { get; set; } = { "xelement", "xml", "x", "" };

		public bool TryEvaluateFormat(IFormattingInfo formattingInfo) {
			var format = formattingInfo.Format;
			var current = formattingInfo.CurrentValue;

			XElement currentXElement = null;
			if (format != null && format.HasNested) {
				return false;
			}

			// if we need to format list of XElements then we just take and format first
			if (current is IList<XElement> xElmentsAsList && xElmentsAsList.Count > 0) {
				currentXElement = xElmentsAsList[0];
			}

			var currentAsXElement = currentXElement ?? current as XElement;
			if (currentAsXElement != null) {
				formattingInfo.Write(currentAsXElement.Value);
				return true;
			}

			return false;

		}

	}

}