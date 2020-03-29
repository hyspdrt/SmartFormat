
namespace SmartFormat.Extensions {

	using System;
	using SmartFormat.Core.Extensions;

	#region CommandSource<T>
	/// <summary>
	/// A base implementation of a Command based <see cref="ISource"/>.
	/// </summary>
	/// <typeparam name="T">The Type of object an implementation resolves.</typeparam>
	public abstract class CommandSource<T> : ISource where T : class {

		/// <summary>
		/// The Prefix (selector), that identifies a <see cref="CommandSource{T}"/> = $.
		/// </summary>
		protected static string Prefix = "$";

		/// <summary>
		/// Gets the Key that identifies this command source.
		/// </summary>
		protected string Key { get; }

		/// <summary>
		/// Construct a new instance with the specified formatter and key.
		/// </summary>
		/// <param name="formatter">The <see cref="SmartFormatter"/> that can be used to prepare the source for consumption.</param>
		/// <param name="key">The value that identifies this command source instance.</param>
		public CommandSource(SmartFormatter formatter, string key) {
			formatter.Parser.AddAdditionalSelectorChars(Prefix);
			this.Key = Prefix + key.TrimStart(Prefix.ToCharArray());
		}

		/// <summary>
		/// Validates the string specified is not null and matches this instances <see cref="Key"/>.
		/// </summary>
		/// <param name="selector">The selector text that could match this instances <see cref="Key"/>.</param>
		/// <returns>True if the selector is meant for this instance; otherwise false.</returns>
		protected virtual bool IsCommandSource(string selector) {
			return
				!string.IsNullOrWhiteSpace(selector) &&
				string.Equals(selector, this.Key, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Implementors of this class should place their command(s) value-resolution here...
		/// </summary>
		/// <param name="command">The name of the command to resolve a value.</param>
		/// <returns>The value this source returns if the command was found; otherwise NULL.</returns>
		protected abstract T ResolveValue(string command);

		/// <summary>
		/// The evaluator implemtation from <see cref="ISource"/>.
		/// </summary>
		/// <param name="selectorInfo">A <see cref="ISelectorInfo"/> from the current place holder.</param>
		/// <returns>True if handled; otherwise false.</returns>
		public virtual bool TryEvaluateSelector(ISelectorInfo selectorInfo) {

			if (selectorInfo.SelectorIndex == 0 &&
				IsCommandSource(selectorInfo.SelectorText)) {
				selectorInfo.Result = selectorInfo.CurrentValue;
				return true;
			}

			if (selectorInfo.SelectorIndex == 1) {

				var rootSelector = selectorInfo?.Placeholder?.Selectors[0]?.RawText;
				if (IsCommandSource(rootSelector)) {

					var val = this.ResolveValue(selectorInfo.SelectorText);

					if (val != null) {
						selectorInfo.Result = val;
						return true;
					}

				}

			}

			return false;

		}

	}
	#endregion

}