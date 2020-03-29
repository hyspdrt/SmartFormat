﻿
namespace SmartFormat.Extensions {

	using System.Linq;
	using System.Xml.Linq;
	using SmartFormat.Core.Extensions;

	public class XmlSource : ISource {

		public XmlSource(SmartFormatter formatter) {
			// Add some special info to the parser:
			formatter.Parser.AddAlphanumericSelectors(); // (A-Z + a-z)
			formatter.Parser.AddAdditionalSelectorChars("_");
			formatter.Parser.AddOperators(".");
		}

		public bool TryEvaluateSelector(ISelectorInfo selectorInfo) {

			if (selectorInfo.CurrentValue is XElement element) {

				var selector = selectorInfo.SelectorText;

				// Find elements that match a selector
				var selectorMatchedElements = element
					.Elements()
					.Where(x => x.Name.LocalName == selector)
					.ToList();

				if (selectorMatchedElements.Any()) {
					selectorInfo.Result = selectorMatchedElements;
					return true;
				}

			}

			return false;

		}

	}

}