using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RegexStringLibrary
{
	public static class Stex
	{
		public static string Bell { get { return @"\a"; } }
		public static string CR { get { return @"\r"; } }
		public static string LF { get { return @"\n"; } }
		public static string Digit { get { return @"\d"; } }
		public static string Word { get { return @"\w"; } }
		public static string Tab { get { return @"\t"; } }
		public static string White { get { return @"\s"; } }
		public static string CapLetterRange { get { return Range("A", "Z"); } }
		public static string LowerLetterRange { get { return Range("a","z"); } }
		public static string LetterRange { get { return CapLetterRange + LowerLetterRange; } }
		public static string AlphanumRange { get { return LetterRange + Digit; } }
		public static string Letter { get { return AnyCharFrom(LetterRange); } }
		public static string CapLetter { get { return AnyCharFrom(CapLetterRange); } }
		public static string LowerLetter { get { return AnyCharFrom(LowerLetterRange); } }
		public static string Alphanum { get { return AnyCharFrom(AlphanumRange); } }
		public static string Any { get { return "."; } }
		public static string Begin { get { return "^"; } }
		public static string End { get { return "$"; } }
		public static string StartAt { get { return @"\G"; }}
		public static string Unsigned { get { return Digit.RepAtLeast(1); } }
		public static string DateAmerican { get; private set; }
		public static string DateEuropean { get; private set; }
		public static string DateAmericanBet { get; private set; }
		public static string DateEuropeanBet { get; private set; }
		public static Regex AmericanDateRegExp { get; private set; }
		public static Regex EuropeanDateRegExp { get; private set; }
		public static Regex AmericanDateBetRegExp { get; private set; }
		public static Regex EuropeanDateBetRegExp { get; private set; }

		private static readonly Regex RgxIgnore;

		static Stex()
		{
			string strEscape = Esc('\\') + Any;
			string strEscapedParen = Esc('\\') + Esc(')');
			string strEscapedBracket = Esc('\\') + Esc(']');
			string strInBrackets = Cat(
				Esc('['),
				strEscapedBracket.OrAnyOf(
					NotCharIn(Esc(']'))).RepAtLeast(0),
				NotCharIn(Esc('\\')),
				Esc(']'));
			string strInParens = Cat(
				Esc('('),
				strEscapedParen.OrAnyOf(
					Not(Esc(')'))).RepAtLeast(0),
					Not(Esc('\\')),
				Esc(')'));
			string strIgnore = Begin + Any.OrAnyOf(strEscape, strInBrackets, strInParens) + End;
			RgxIgnore = new Regex(strIgnore);
			DateAmerican = Date(true, false);
			DateEuropean = Date(false, false);
			DateAmericanBet = Date(true, true);
			DateEuropeanBet = Date(false, true);

			AmericanDateRegExp = new Regex(DateAmerican, RegexOptions.Compiled);
			EuropeanDateRegExp = new Regex(DateEuropean, RegexOptions.Compiled);
			AmericanDateBetRegExp = new Regex(DateAmericanBet, RegexOptions.Compiled);
			EuropeanDateBetRegExp = new Regex(DateEuropeanBet, RegexOptions.Compiled);
		}

		public static string CaseSensitive(this string str, bool fCaseSensitive)
		{
			return "(?" + (fCaseSensitive ? "-" : "") + "i:" + str + ")";
		}

		public class DateInfo
		{
			public bool Success;
			public bool Between;
			public string Prefix1;
			public string Prefix2;
			public string Suffix1;
			public string Suffix2;
			public DateTime Date1;
			public DateTime Date2;
			static readonly DateTime DateUninitialized = new DateTime(1,DateTimeKind.Utc);
			internal DateInfo(bool successParm, bool betweenParm, string prefix1Parm, string prefix2Parm, DateTime date1Parm, DateTime date2Parm, string suffix1Parm, string suffix2Parm)
			{
				Success = successParm;
				Between = betweenParm;
				Prefix1 = prefix1Parm;
				Prefix2 = prefix2Parm;
				Date1 = date1Parm;
				Date2 = date2Parm;
				Suffix1 = suffix1Parm;
				Suffix2 = suffix2Parm;
			}
			public DateInfo()
			{
				Date1 = DateUninitialized;
			}
			public bool Initialized()
			{
				return Date1 != DateUninitialized;
			}
		}

		static int MonthNameToIndex(string mnthName)
		{
			int iMonth = 0;
			if (mnthName != "")
			{
				switch (mnthName.ToUpper())
				{
					case "JAN":
					case "JANUARY":
						iMonth = 1;
						break;
					case "FEB":
					case "FEBRUARY":
						iMonth = 2;
						break;
					case "MAR":
					case "MARCH":
						iMonth = 3;
						break;
					case "APR":
					case "APRIL":
						iMonth = 4;
						break;
					case "MAY":
						iMonth = 5;
						break;
					case "JUN":
					case "JUNE":
						iMonth = 6;
						break;
					case "JUL":
					case "JULY":
						iMonth = 7;
						break;
					case "AUG":
					case "AUGUST":
						iMonth = 8;
						break;
					case "SEP":
					case "SEPTEMBER":
						iMonth = 9;
						break;
					case "OCT":
					case "OCTOBER":
						iMonth = 10;
						break;
					case "NOV":
					case "NOVEMBER":
						iMonth = 11;
						break;
					case "DEC":
					case "DECEMBER":
						iMonth = 12;
						break;
				}
			}
			return iMonth;
		}

		public static DateInfo GetDateInfo(string strDate, bool fAmerican, bool fAllowBetween)
		{
			int iType = (fAmerican ? 2 : 0) + (fAllowBetween ? 1 : 0);
			Regex rgx = null;
			switch (iType)
			{
				case 0:
					rgx = EuropeanDateRegExp;
					break;
				case 1:
					rgx = EuropeanDateBetRegExp;
					break;
				case 2:
					rgx = AmericanDateRegExp;
					break;
				case 3:
					rgx = AmericanDateBetRegExp;
					break;
			}
// ReSharper disable PossibleNullReferenceException
			Match mtch = rgx.Match(strDate);
// ReSharper restore PossibleNullReferenceException
			bool success = mtch.Success;
			bool between = false;
			string prefix1 = string.Empty;
			string prefix2 = string.Empty;
			string suffix1 = string.Empty;
			string suffix2 = string.Empty;
			DateTime dt1 = new DateTime();
			DateTime dt2 = new DateTime();

			if (success)
			{
				between = mtch.Groups["betweenPrefix"].Value != "";
				string tmp;
				if ((tmp = mtch.Groups["prefix"].Value.ToLower()) != "")
				{
					prefix1 = tmp;
				}
				if ((tmp = mtch.Groups["suffix"].Value.ToLower()) != "")
				{
					suffix1 = tmp;
				}
				string mnthName = mtch.Groups["mnthName"].Value;
				int iMonth1 = mnthName != "" ? MonthNameToIndex(mnthName) : int.Parse(mtch.Groups["month"].Value);
				int iDay1 = int.Parse(mtch.Groups["day"].Value);
				int iYear1 = int.Parse(mtch.Groups["year"].Value);
				dt1 = new DateTime(iYear1, iMonth1, iDay1);
				if (between)
				{
					if ((tmp = mtch.Groups["prefix2"].Value.ToLower()) != "")
					{
						prefix2 = tmp;
					}
					if ((tmp = mtch.Groups["suffix2"].Value.ToLower()) != "")
					{
						suffix2 = tmp;
					}
					string mnthName2 = mtch.Groups["mnthName2"].Value;
					int iMonth2 = mnthName2 != "" ? MonthNameToIndex(mnthName2) : int.Parse(mtch.Groups["month2"].Value);
					int iDay2 = int.Parse(mtch.Groups["day2"].Value);
					int iYear2 = int.Parse(mtch.Groups["year2"].Value);
					dt2 = new DateTime(iYear2, iMonth2, iDay2);
				}
			}
			return new DateInfo(success, between, prefix1, prefix2, dt1, dt2, suffix1, suffix2);
		}

		static string Date(bool fAmerican, bool fAllowBetween)
		{
			string strMonthAbbr = AnyOf("JAN", "FEB", "MAR", "APR", "JUN", "JUL",
				"AUG", "SEP", "OCT", "NOV", "DEC").Named("mnthName") + ".".Optional();
			string strMonthSpelled = AnyOf("JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE",
				"JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER").Named("mnthName");
			string strMonthName = strMonthAbbr.OrAnyOf(strMonthSpelled);
			string strSeparator = AnyOf("-", "/", ".", " ");
			string strBetween = AnyOf("Between", "Bet").Named("betweenPrefix");
			string strPrefix = AnyOf("About", "Abt", "A",
								  "After", "Aft",
								  "Before", "Bef", "B",
								  "Calculated", "Cal",
								  "Circa", "Cir", "Ca", "C").Named("prefix");
			string strSuffix = AnyOf("BC", "B.C.").Named("suffix");

			string strTwoDigits = Cat(Digit, Digit.Optional());
			string strDay = strTwoDigits.Named("day");
			string strMonth = strTwoDigits.Named("month");
			string strYear = Cat(Digit, Digit, Digit, Digit.Optional()).Named("year");
			string strDate1 = strMonthName + " " + strDay + ", " + strYear;
			string strDate2 = strDay + " " + strMonthName + " " + strYear;
			string strDateAmerican = strMonth + strSeparator + strDay + strSeparator + strYear;
			string strDateEuropean = strDay + strSeparator + strMonth + strSeparator + strYear;
			string strDate3 = strYear + " " + strMonthName + " " + strDay;
			string strDate4 = strYear + strSeparator + strMonth + strSeparator + strDay;
			string strDate5 = strYear;
			string strSingleDate = (strPrefix + " ").Optional() + AnyOf(
				strDate1,
				strDate2,
				fAmerican ? strDateAmerican : strDateEuropean,
				strDate3,
				strDate4,
				strDate5) + (" " + strSuffix).Optional();
			string strTags = AnyOf("mnthName", "prefix", "suffix", "day", "month", "year");
			// Performing regex replacements on our regex string!  Kinky!
			string strSingleDate2 = Regex.Replace(strSingleDate, strTags, "$&2");
			return Cat(
					Begin,
					fAllowBetween ? (strBetween + " ").Optional() : string.Empty,
					strSingleDate,
					fAllowBetween ? "betweenPrefix".If(" AND " + strSingleDate2, "") : string.Empty,
					End)
				.CaseSensitive(false);
		}

		/// <summary>
		/// Returns hex character
		/// </summary>
		/// <param name="strHex">Hex value</param>
		/// <returns>The hex character string</returns>
		public static string Hex(this string strHex)
		{
			return @"\x" + strHex;
		}

		/// <summary>
		/// Escape a character
		/// </summary>
		/// <param name="ch">Character to escape</param>
		/// <returns>Escaped character</returns>
		public static string Esc(this char ch)
		{
			return @"\" + ch;
		}

		/// <summary>
		/// Escape a character
		/// </summary>
		/// <param name="strch">String with the char to escape</param>
		/// <returns>Escaped character</returns>
		public static string Esc(this string strch)
		{
			return @"\" + strch;
		}

		/// <summary>
		/// Pattern which depends on whether a group has been matched
		/// </summary>
		/// <param name="strLabel">Group to check whether it's matched</param>
		/// <param name="strHasMatched">Pattern to use if there was a match</param>
		/// <param name="strDidntMatch">Pattern to use if there wasn't a match</param>
		/// <returns>Conditional pattern</returns>
		public static string If(this string strLabel, string strHasMatched, string strDidntMatch)
		{
			return string.Format("(?({0}){1}|{2})", strLabel, strHasMatched, strDidntMatch);
		}

		/// <summary>
		/// Forces a greedy search on a pattern
		/// </summary>
		/// <param name="str">pattern to be made greedy</param>
		/// <returns>greedy pattern</returns>
		public static string Greedy(this string str)
		{
			return "(?>" + str + ")";
		}

		/// <summary>
		/// Returns a range of chars for use in AnyChar
		/// </summary>
		/// <param name="strLow">Starting char</param>
		/// <param name="strHigh">Ending char</param>
		/// <returns>The Range</returns>
		public static string Range(string strLow, string strHigh)
		{
			return strLow + '-' + strHigh;
		}

		/// <summary>
		/// Concatenates strings
		/// </summary>
		/// <param name="s">strings to be concatenated</param>
		/// <returns>concatenation of all the strings in s</returns>
		public static string Cat(params string[] s)
		{
			return s.Aggregate((sAg, str) => sAg + str);
		}

		/// <summary>
		/// Creates a pattern which matches either this or any of the parameters
		/// </summary>
		/// <param name="str">this</param>
		/// <param name="s">the other strings</param>
		/// <returns>pattern</returns>
		public static string OrAnyOf(this string str, params string[] s)
		{
			return "(?:" + s.Aggregate(str, (sAg, sNext) => sAg + "|" + sNext) + ")";
		}

		[Obsolete("Use OrAnyOf()")]
		public static string Or(this string str, params string[] s)
		{
			return str.OrAnyOf(s);
		}

		/// <summary>
		/// Creates a pattern which matches any of the parameters
		/// </summary>
		/// <param name="s">the other strings</param>
		/// <returns>pattern</returns>
		public static string AnyOf(params string[] s)
		{
			return s[0].OrAnyOf(s.Skip(1).ToArray());
		}

		[Obsolete("Use AnyOf")]
		public static string Or(params string[] s)
		{
			return AnyOf(s);
		}

		/// <summary>
		/// Accept any characters from any of the arguments
		/// </summary>
		/// <param name="s">Characters or ranges</param>
		/// <returns>Pattern</returns>
		public static string AnyCharFrom(params string[] s)
		{
			return "[" + Cat(s) + "]";
		}

		[Obsolete("Use AnyCharFrom()")]
		public static string AnyChar(params string[] s)
		{
			return AnyCharFrom(s);
		}

		/// <summary>
		/// Accept any characters not from any of the arguments
		/// </summary>
		/// <param name="s">Characters or ranges</param>
		/// <returns>Pattern</returns>
		public static string NotCharIn(params string[] s)
		{
			return "[^" + Cat(s) + "]";
		}


		/// <summary>
		/// Cosmetic version of NotCharIn which works better for single characters
		/// </summary>
		/// <param name="s">Characters or ranges</param>
		/// <returns>Pattern</returns>
		public static string Not(params string[] s)
		{
			return NotCharIn(s);
		}

		[Obsolete("Use Not() or NotCharIn()")]
		public static string NoChar(params string[] s)
		{
			return NotCharIn(s);
		}

		/// <summary>
		/// Parenthesize a pattern properly.  If it's already parenthesized or is one character
		/// long, then it's merely returned.  Otherwise, it's surrounded by (?: ... ).
		/// </summary>
		/// <param name="str">String to be parenthesized</param>
		/// <returns>Properly parenthesized string</returns>
		public static string AddParens(this string str)
		{
			string strRet;

			if (RgxIgnore != null && RgxIgnore.IsMatch(str))
			{
				strRet = str;
			}
			else
			{
				strRet = "(?:" + str + ")";
			}
			return strRet;
		}

		/// <summary>
		/// Returns pattern in which str is optional
		/// </summary>
		/// <param name="str">string to be made optional</param>
		/// <returns>pattern which optionally matches string</returns>
		public static string Optional(this string str)
		{
			return str.Rep(0, 1);
		}

		/// <summary>
		/// Repeat spec.  repeats at least least times and at most most times.  Most can be
		/// negative in which case it's considered to be "infinity" - i.e., it's repeated
		/// at least "least" times with no limit on the most.
		/// </summary>
		/// <param name="str">String to be repeated in search</param>
		/// <param name="least">Least number of times to repeat</param>
		/// <param name="most">Most number of times to repeat</param>
		/// <returns>pattern which matches the original number of string repeated properly</returns>
		public static string Rep(this string str, int least, int most)
		{
			string strRep;

			if (least < 0)
			{
				throw new ArgumentException("least is negative in Rep");
			}
			if (most < 0)
			{
				return str.RepAtLeast(least);
			}
			if (least == most)
			{
				strRep = string.Format("{{{0}}}", least);
			}
			else if (least == 0 && most == 1)
			{
				strRep = "?";
			}
			else if (least < most)
			{
				strRep = string.Format("{{{0},{1}}}", least, most);
			}
			else
			{
				throw new ArgumentException("least must be less than most in Rep");
			}
			return str.AddParens() + strRep;
		}

		/// <summary>
		/// Creates pattern which matches str at least count times
		/// </summary>
		/// <param name="str">String to be repeated</param>
		/// <param name="count">Count of times it must be repeated</param>
		/// <returns>Proper pattern</returns>
		public static string RepAtLeast(this string str, int count)
		{
			string strRep;
			if (count < 0)
			{
				throw new ArgumentException("count is negative in RepAtLeast");
			}
			if (count == 0)
			{
				strRep = "*";
			}
			else if (count == 1)
			{
				strRep = "+";
			}
			else
			{
				strRep = string.Format("{{{0},}}", count);
			}
			return str.AddParens() + strRep;
		}

		/// <summary>
		/// Puts the text in an unnamed group
		/// </summary>
		/// <param name="str">String to be matched</param>
		/// <returns>Pattern which names the match</returns>
		public static string Group(this string str)
		{
			return string.Format("({0})", str);
		}

		/// <summary>
		/// Names the match made on the string
		/// </summary>
		/// <param name="str">String to be matched</param>
		/// <param name="strName">Name for the match</param>
		/// <returns>Pattern which names the match</returns>
		public static string Named(this string str, string strName)
		{
			return string.Format("(?<{0}>{1})", strName, str);
		}
	}
}
