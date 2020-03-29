
namespace SmartFormat.Tests.Extensions {

	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using NUnit.Framework;
	using SmartFormat.Core.Extensions;
	using SmartFormat.Core.Formatting;
	using SmartFormat.Core.Parsing;
	using SmartFormat.Core.Settings;
	using SmartFormat.Extensions;
	using SmartFormat.Tests.TestUtils;

	[TestFixture]
	public class AutoServTests {

		[Test]
		public void TestBench() {

			var smart = Smart.CreateDefaultSmartFormat();
			smart.AddExtensions(new DnsCommandSource(smart));
			smart.AddExtensions(new PathFormatter());
			smart.Parser.UseAlternativeEscapeChar();
			RegisterTemplates(smart);

			var path = Directory.GetCurrentDirectory().ToUpper();
			var name = "chuck";
			var obj = new TestObject(path, name);
			var fmt = "Hey, {Person.Name.ToLower} your path is {$dns.hostName.ToUpper} @ {Source.ToLower:path.name}, with me!";
			var rst = $"Hey, {obj.Person.Name.ToLower()} your path is {Dns.GetHostName().ToUpper()} @ {Path.GetFileName(path.ToLower())}, with me!";
			smart.Test(fmt, new[] { obj }, rst);


			//Assert.Throws<FormattingException>(() => smart.Format("{:template:}", 5));

			//var parsed = smart.Parser.ParseFormat("{:template:}", smart.GetNotEmptyFormatterExtensionNames());
			//var parsed = smart.Parser.ParseFormat("{c1:{c2:{c3:path.name}}}", new[] { "path" });
			//var parsed = smart.Parser.ParseFormat("First dictionary: {0.Name}, second dictionary: {1.City}", new[] { "path" });
			//var parsed = smart.Parser.ParseFormat("{theKey:ismatch(^.+999.+$):{}|No match content}", new[] { "ismatch" });
			//var parsed = smart.Parser.ParseFormat(fmt, smart.GetNotEmptyFormatterExtensionNames());
			//OutputFormatObject(parsed);

			//smart.FormatterExtensions.Add(new IsMatchFormatter { RegexOptions = RegexOptions.None });
			//var _variable = new Dictionary<string, object>() { { "theKey", "Some123Content" } };
			//smart.Test("{theKey:ismatch(^.+999.+$):{}|No match content}", new[] { _variable }, "No match content");


		}

		#region Helpers

		private void RegisterTemplates(SmartFormatter smart) {

			var templates = new TemplateFormatter(smart);
			smart.AddExtensions(templates);

			templates.Register("firstLast", "{First} {Last}");
			templates.Register("lastFirst", "{Last}, {First}");
			templates.Register("FIRST", "{First.ToUpper}");
			templates.Register("last", "{Last.ToLower}");

			if (smart.Settings.CaseSensitivity == CaseSensitivityType.CaseSensitive) {
				templates.Register("LAST", "{Last.ToUpper}");
			}

			templates.Register("NESTED", "{:template:FIRST} {:template:last}");
		}

		private const char indentChar = '\t';

		private static void OutputFormatObject(Format format, int indent = 0) {

			var indentStr = string.Empty;
			if (indent > 0) {
				indentStr = new string(indentChar, indent);
			}

			Console.WriteLine($"{indentStr}Type: Format");
			Console.WriteLine($"{indentStr}HasNested: {format.HasNested}");
			Console.WriteLine($"{indentStr}RawText: {format.RawText}");
			Console.WriteLine($"{indentStr}LiteralText: {format.GetLiteralText()}");
			Console.WriteLine($"{indentStr}ItemsCount: {format.Items.Count}");

			indent++;
			var itemsCount = 0;
			foreach (var item in format.Items) {

				Console.WriteLine($"{indentStr}Summary for Item({++itemsCount}):");

				if (item is Placeholder placeholder) {
					OutputPlaceHolderObject(placeholder, indent);

				} else if (item is LiteralText literalText) {
					OutputLiteralObject(literalText, indent);

				} else if (item is Selector selector) {
					OutputSelectorObject(selector, indent);

				} else if (item is Format childFormat) {
					OutputFormatObject(childFormat, indent);
				}

			}

			Console.WriteLine($"{indentStr}*Exited Format*");

		}

		private static void OutputLiteralObject(LiteralText literalText, int indent) {
			var indentStr = new string(indentChar, indent);
			Console.WriteLine($"{indentStr}Type: LiteralText");
			Console.WriteLine($"{indentStr}Raw Text: {literalText.RawText}");

		}

		private static void OutputSelectorObject(Selector selector, int indent) {
			var indentStr = new string(indentChar, indent);

			Console.WriteLine($"{indentStr}Selector: {selector.RawText}");

			indentStr = new string(indentChar, indent + 1);
			Console.WriteLine($"{indentStr}Operator: {selector.Operator}");
			Console.WriteLine($"{indentStr}SelectorIndex: {selector.SelectorIndex}");

		}

		private static void OutputPlaceHolderObject(Placeholder placeHolder, int indent) {
			var indentStr = new string(indentChar, indent);

			Console.WriteLine($"{indentStr}Type: PlaceHolder");
			Console.WriteLine($"{indentStr}Raw Text: {placeHolder.RawText}");
			Console.WriteLine($"{indentStr}Detail:");

			indentStr = new string(indentChar, ++indent);
			Console.WriteLine($"{indentStr}Alignment: {placeHolder.Alignment}");
			Console.WriteLine($"{indentStr}NestedDepth: {placeHolder.NestedDepth}");

			Console.WriteLine($"{indentStr}Selectors: IsNullOrEmpty({placeHolder.Selectors == null || placeHolder.Selectors.Count == 0})");
			if (placeHolder.Selectors != null) {
				foreach (var selector in placeHolder.Selectors) {
					OutputSelectorObject(selector, indent + 1);
				}
			}

			Console.WriteLine($"{indentStr}FormatterName: {placeHolder.FormatterName}");
			Console.WriteLine($"{indentStr}FormatterOptions: {placeHolder.FormatterOptions}");
			Console.WriteLine($"{indentStr}Format: IsNull({placeHolder.Format == null})");
			if (placeHolder.Format != null) {
				OutputFormatObject(placeHolder.Format, indent + 1);
			}

		}

		#endregion

	}



	#region TestObject

	public class TestObject {

		public TestObject(string path, string personName) {
			this.Source = path;
			this.Person = new Person { Name = personName };
		}

		public string Source { get; set; }

		public Person Person { get; set; }

	}

	public class Person {

		public Person() {
		}

		public string Name { get; set; }
	}

	#endregion

	#region DnsCommandSource
	/// <summary>
	/// A custom command source, that resolve various IPAddress values.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The commands supported are (non-case sensitive)
	/// <list type="table">
	/// <listheader>
	/// <term>Command</term>
	/// <description>Details</description>
	/// </listheader>
	/// <item>
	/// <term>IPAddress (or ipaddress)</term>
	/// <description>Resolves the first or null, IPAddress returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
	/// </item>
	/// <item>
	/// <term>IP4Address (or ip4address)</term>
	/// <description>Resolves the first or null, IP v4 Address returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
	/// </item>
	/// <item>
	/// <term>IP6Address (or ip6address)</term>
	/// <description>Resolves the first or null, IP v6 Address returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
	/// </item>
	/// <item>
	/// <term>HostName (or hostname)</term>
	/// <description>Resolves host name of the local computer from Dns.GetHostName()</description>
	/// </item>
	/// </list>
	/// </para>
	/// </remarks>
	public class DnsCommandSource : CommandSource<string> {

		/// <summary>
		/// The command prefix Key used to identify this command source
		/// in a format string.
		/// </summary>
		private const string dnsKey = "dns";

		/// <summary>
		/// KeyValue Pairs containing the supported command names and their associated resolver functions.
		/// </summary>
		private readonly static Dictionary<string, Func<string>> Commands =
			new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase) {
			{ "IPAddress", () => GetIpAddress(null) },
			{ "IP4Address", () => GetIpAddress(false) },
			{ "IP6Address", () => GetIpAddress(true) },
			{ "HostName", () => Dns.GetHostName() }
		};

		/// <summary>
		/// Helper method to get the first or default IP Address
		/// </summary>
		/// <param name="isIP6">
		/// <c>null</c> for the first IPAddress; <c>True</c> for the first IP v6
		/// Address, or <c>False</c> for the first IP v4 Address.</param>
		/// <returns>The value resolved or null.</returns>
		private static string GetIpAddress(bool? isIP6 = null) {

			if (isIP6 == false) {
				var ip4 = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => !a.IsIPv6LinkLocal);
				if (ip4 != null) {
					return ip4.ToString();
				}
			}

			if (isIP6 == true) {
				var ip6 = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => a.IsIPv6LinkLocal);
				if (ip6 != null) {
					return ip6.ToString();
				}
			}

			var ip = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
			if (ip != null) {
				return ip.ToString();
			}

			return string.Empty;

		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="formatter">The in scope <see cref="SmartFormatter"/> instance.</param>
		public DnsCommandSource(SmartFormatter formatter) :
			base(formatter, dnsKey) {

		}

		/// <summary>
		/// Resolve a command for the first IPAddress, the first IP4Address, the first IP6Address or HostName
		/// using the <see cref="Dns"/> object.
		/// </summary>
		/// <param name="command"></param>
		/// <returns>The resolve value; or null.</returns>
		protected override string ResolveValue(string command) {

			if (Commands.TryGetValue(command, out var cmd)) {
				return cmd();
			};

			return null;

		}

	}
	#endregion

	#region PathFormatter
	/// <summary>
	/// 
	/// </summary>
	public class PathFormatter : IFormatter {

		private static readonly string name = "path";

		/// <summary>
		/// Collection of supported and available commands.
		/// </summary>
		readonly Dictionary<string, Func<string, string>> commands;

		public string[] Names {
			get;
			set;
		}

		public bool TryEvaluateFormat(IFormattingInfo formattingInfo) {

			var format = formattingInfo.Format;
			var current = formattingInfo.CurrentValue?.ToString();

			if (format != null && format.HasNested) {
				return false;
			}

			if (string.IsNullOrWhiteSpace(current)) {
				return false;
			}

			var options =
				formattingInfo.FormatterOptions != "" ?
				formattingInfo.FormatterOptions :
				format != null ?
				format.GetLiteralText() :
				"";

			if (this.commands.TryGetValue(options, out var func)) {
				var v = func(current);
				foreach (var itemFormat in format.Items) {
					v = formattingInfo.FormatDetails.Formatter.Format("{0:" + itemFormat.RawText + "}", v);
				}
				formattingInfo.Write(v);
				return true;
			}

			return false;

		}

		/// <summary>
		/// 
		/// </summary>
		public PathFormatter() {

			this.Names = new[] { name };

			this.commands = new Dictionary<string, Func<string, string>>(12, StringComparer.OrdinalIgnoreCase) {

				// Directory
				//1
				{
					"dir",
					(value) => {
						return Path.GetDirectoryName(value);
					}
				},


				// Directory + FileNameWithoutExtension
				//2
				{
					"dirName",
					(value) => {
						return Path.GetFileNameWithoutExtension(Path.GetDirectoryName(value));
					}
				},


				// GetFileNameWithoutExtension
				//3
				{
					"nameWithoutExtension",
					(value) => {
						return Path.GetFileNameWithoutExtension(value);
					}
				},
				//4
				{
					"nameNoExtension",
					(value) => {
						return Path.GetFileNameWithoutExtension(value);
					}
				},
				//5
				{
					"nameNoExt",
					(value) => {
						return Path.GetFileNameWithoutExtension(value);
					}
				},
				//6
				{
					"nameOnly",
					(value) => {
						return Path.GetFileNameWithoutExtension(value);
					}
				},


				// GetFileName
				//7
				{
					"name",
					(value) => {
						return Path.GetFileName(value);
					}
				},


				// GetExtension
				//8
				{
					"extension",
					(value) => {
						return Path.GetExtension(value);
					}
				},
				//9
				{
					"ext",
					(value) => {
						return Path.GetExtension(value);
					}
				},

				// GetFullPath
				//10
				{
					"fullPath",
					(value) => {
						return Path.GetFullPath(value);
					}
				},
				//11
				{
					"full",
					(value) => {
						return Path.GetFullPath(value);
					}
				},

				// GetPathRoot
				//12
				{
					"root",
					(value) => {
						return Path.GetPathRoot(value);
					}
				}
			};

		}

	}
	#endregion

	#region CasingStringExtensions
	/// <summary>
	/// String Extension Methods to manipulate character casing.
	/// </summary>
	public static class CasingStringExtensions {

		#region Casing

		/// <summary>
		/// Convert the string value to Pascal case.
		/// </summary>
		public static string ToPascalCase(this string value) {

			// If there are 0 or 1 characters, just return the string.
			if (value == null) {
				return value;
			}
			if (value.Length < 2) {
				return value.ToUpper();
			}

			// Split the string into words.
			var words = value.Split(
				new char[] { },
				StringSplitOptions.RemoveEmptyEntries);

			// Combine the words.
			var result = new StringBuilder(value.Length);
			foreach (var word in words) {
				result.Append(word.Substring(0, 1).ToUpper());
				result.Append(word.Substring(1));
			}

			return result.ToString();

		}

		/// <summary>
		/// Convert the string value to camel case.
		/// </summary>
		public static string ToCamelCase(this string value) {

			// If there are 0 or 1 characters, just return the string.
			if (value == null || value.Length < 2) {
				return value;
			}

			// Split the string into words.
			var words = value.Split(
				new char[] { },
				StringSplitOptions.RemoveEmptyEntries);

			// Combine the words.
			var result = new StringBuilder();
			result.Append(words[0].Substring(0, 1).ToLower());
			result.Append(words[0].Substring(1));
			for (var i = 1; i < words.Length; i++) {
				result.Append(words[i].Substring(0, 1).ToUpper());
				result.Append(words[i].Substring(1));
			}

			return result.ToString();

		}

		/// <summary>
		/// Convert the <c>Path</c> to camel case.
		/// </summary>
		public static string ToCamelCaseForPath(this string path) {

			// If there are 0 or 1 characters, just return the string.
			if (path == null || path.Length < 2) {
				return path;
			}

			// Split the string into words.
			var words = path.Split(
				new char[] { Path.DirectorySeparatorChar },
				StringSplitOptions.RemoveEmptyEntries);

			// Combine the words.
			var result = new StringBuilder();
			result.Append(words[0].Substring(0, 1).ToLower());
			result.Append(words[0].Substring(1));
			for (var i = 1; i < words.Length; i++) {
				result.Append(Path.DirectorySeparatorChar);
				result.Append(words[i].Substring(0, 1).ToLower());
				result.Append(words[i].Substring(1));
			}

			return result.ToString();

		}

		/// <summary>
		/// Converts the string value to Proper Case.
		/// </summary>
		public static string ToProperCase(this string value) {

			// If there are 0 or 1 characters, just return the string.
			if (value == null) {
				return value;
			}
			if (value.Length < 2) {
				return value.ToUpper();
			}

			return new string(CharsToTitleCase(value.ToLower()).ToArray());

		}

		/// <summary>
		/// Converts the string value to Title Case.
		/// </summary>
		public static string ToTitleCase(this string value) {

			return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);

		}

		#endregion

		#region Utils

		private static IEnumerable<char> CharsToTitleCase(string s) {

			var newWord = true;

			foreach (var c in s) {

				if (newWord) {
					yield return char.ToUpper(c);
					newWord = false;
				} else {
					yield return char.ToLower(c);
				}

				if (c == ' ') {
					newWord = true;
				}

			}

		}

		#endregion

	}
	#endregion

}