using System.Globalization;

namespace Stumblr
{
	/// <summary>
	/// Numbers printed the same way they are parsed, so a reported value can be typed back in.
	/// Invariant culture throughout: these go to a console, not a locale.
	/// </summary>
	internal static class Format
	{
		internal static string Number(float _value)
		{
			return _value.ToString("0.###", CultureInfo.InvariantCulture);
		}

		internal static string Percent(float _value)
		{
			return Number(_value) + "%";
		}

		internal static string Seconds(float _value)
		{
			return Number(_value) + "s";
		}

		/// <summary>A multiplier, as "x2".</summary>
		internal static string Times(float _value)
		{
			return "x" + Number(_value);
		}
	}
}
