﻿using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace RegexStringLibrary
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	Regular expression extensions for strings. </summary>
	///
	/// <remarks>	
	/// This set of extensions only deals with the string versions for regular expressions. That's
	/// where the complexity lies.  It also allows for cascading and combining of the results since
	/// they are all strings.  The only exception to this is the date info since this is a really
	/// complex string with vaious options available so we tailor the result for the situation and do
	/// the match in our code and interpret the results into a DateInfo object.  See the tests for
	/// examples of usage.  Darrellp, 10/1/2012. 
	/// </remarks>
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class Stex
	{
		public static string Bell { get { return @"\a"; } }
		public static string CR { get { return @"\r"; } }
		public static string LF { get { return @"\n"; } }
		public static string FormFeed { get { return @"\f"; } }
		public static string Digit { get { return @"\d"; } }
		public static string NonDigit { get { return @"\D"; } }
		public static string Word { get { return @"\w"; } }
		public static string Tab { get { return @"\t"; } }
		public static string White { get { return @"\s"; } }
		public static string NonWhite { get { return @"\S"; } }
		public static string VerticalTab { get { return @"\v"; } }
		public static string WordChar { get { return @"\w"; } }
		public static string NonWordChar { get { return @"\W"; } }
		public static string WordBoundary { get { return @"\b"; } }
		public static string NonWordBoundary { get { return @"\B"; } }
		public static string WhitePadding { get { return White.Rep(0); } }
		public static string CapLetterRange { get { return Range("A", "Z"); } }
		public static string LowerLetterRange { get { return Range("a","z"); } }
		public static string LetterRange { get { return CapLetterRange + LowerLetterRange; } }
		public static string AlphanumRange { get { return LetterRange + Digit; } }
		public static string Letter { get { return AnyCharFrom(LetterRange); } }
		public static string CapLetter { get { return AnyCharFrom(CapLetterRange); } }
		public static string LowerLetter { get { return AnyCharFrom(LowerLetterRange); } }
		public static string Alphanum { get { return AnyCharFrom(AlphanumRange); } }
		public static string StringStart { get { return "\\A"; } }
		public static string StringEnd { get { return "\\Z"; } }
		public static string Any { get { return "."; } }
		public static string Begin { get { return "^"; } }
		public static string End { get { return "$"; } }
		public static string Failure { get { return "(?!)";  } }
		public static string StartAt { get { return @"\G"; }}
		public static string Unsigned { get { return Digit.RepAtLeast(1); } }
		public static string DateAmerican { get; private set; }
		public static string DateEuropean { get; private set; }
		public static string DateAmericanBet { get; private set; }
		public static string DateEuropeanBet { get; private set; }

		// These are public mainly for testing purposes
		public static Regex AmericanDateRegExp { get; set; }
		public static Regex EuropeanDateRegExp { get; set; }
		public static Regex AmericanDateBetRegExp { get; set; }
		public static Regex EuropeanDateBetRegExp { get; set; }

		// Applied to rgx strings to determine whether they require parenthesization or not.
		// This keeps us from parenthesizing "(...)" and getting "((...))".
		private static readonly Regex RgxDontParenthesize;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Static constructor. </summary>
		///
		/// <remarks>	Initializes strings and values.  Darrellp, 10/1/2012. </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////
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
			RgxDontParenthesize = new Regex(strIgnore);
			DateAmerican = Date(true, false);
			DateEuropean = Date(false, false);
			DateAmericanBet = Date(true, true);
			DateEuropeanBet = Date(false, true);

			AmericanDateRegExp = new Regex(DateAmerican, RegexOptions.Compiled);
			EuropeanDateRegExp = new Regex(DateEuropean, RegexOptions.Compiled);
			AmericanDateBetRegExp = new Regex(DateAmericanBet, RegexOptions.Compiled);
			EuropeanDateBetRegExp = new Regex(DateEuropeanBet, RegexOptions.Compiled);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Determine whether the search is case sensitive or not. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">				pattern to be affected. </param>
		/// <param name="fCaseSensitive">	true to be case sensitive, false for case insensitive. </param>
		///
		/// <returns>	. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string CaseSensitive(this string str, bool fCaseSensitive)
		{
			return "(?" + (fCaseSensitive ? "-" : "") + "i:" + str + ")";
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Date information. </summary>
		///
		/// <remarks>	
		/// The dates are either an individual date or a period between two dates.  Either date can be
		/// prefixed with "about", "circa", "before", "after" or "calculated".  These can be abbreviated
		/// as follows:
		/// 
		/// About - "Abt", "A" 
		/// Before - "Bef", "B" 
		/// Calculated - "Cal" 
		/// Circa - "Cir", "Ca", "C"
		/// 
		/// Additionally, dates can be suffixed with "BC" or "B.C.".
		/// 
		/// Darrellp, 10/1/2012. 
		/// </remarks>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public class DateInfo
		{
			/// <summary> true if the match was a success, false if it failed </summary>
			public bool Success;
			/// <summary> true if this is between two dates </summary>
			public bool Between;
			/// <summary> The prefix on the first date </summary>
			public string Prefix1;
			/// <summary> The prefix on the second date </summary>
			public string Prefix2;
			/// <summary> The suffix on the first date </summary>
			public string Suffix1;
			/// <summary> The suffix on the second date </summary>
			public string Suffix2;
			/// <summary> First date </summary>
			public DateTime Date1;
			/// <summary> Second date </summary>
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

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets date information from a string. </summary>
		///
		/// <remarks>	
		/// Dates can be in American or European ordering.  They are either an individual date or a
		///  period between two dates.  Either date can be prefixed with "about", "circa", "before",
		/// "after" or "calculated".  These can be abbreviated as follows:
		/// 
		/// About - "Abt", "A" 
		/// Before - "Bef", "B" 
		/// Calculated - "Cal" 
		/// Circa - "Cir", "Ca", "C"
		/// 
		/// Additionally, dates can be suffixed with "BC" or "B.C.".
		/// 
		/// Some sample dates would include:
		/// 
		/// 10/12/2012
		/// February 10, 1912
		/// NOV 4, 1956
		/// Nov 4, 1956
		/// 1940
		/// between 1948 and 1950
		/// 11-4-1956
		/// 11-04-195
		/// 4 November 1956
		/// ca 1932
		/// after 2000
		/// before 800 BC
		/// After Jan. 1, 1932
		/// between nov 4, 1956 and ca sep 11, 1980
		/// 
		/// Darrellp, 10/1/2012. 
		/// </remarks>
		///
		/// <param name="strDate">			String representing the date. </param>
		/// <param name="fAmerican">		True for American date ordering. </param>
		/// <param name="fAllowBetween">	True to allow "between" in the date. </param>
		///
		/// <returns>	The date information. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static DateInfo GetDateInfo(string strDate, bool fAmerican, bool fAllowBetween)
		{
			if (strDate == null)
			{
				throw new ArgumentException("Null date in GetDateInfo");
			}

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

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	This is the regex string for the date. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="fAmerican">		True for American date ordering. </param>
		/// <param name="fAllowBetween">	True to allow "between" in the date. </param>
		///
		/// <returns>	String for regex which parses the date. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
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
					fAllowBetween ? "betweenPrefix".If(" AND " + strSingleDate2) : string.Empty,
					End)
				.CaseSensitive(false);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns hex character. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="strHex">	Hex value. </param>
		///
		/// <returns>	The hex character string. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Hex(this string strHex)
		{
			return @"\x" + strHex;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Escape a character. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="ch">	Character to escape. </param>
		///
		/// <returns>	Escaped character. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Esc(this char ch)
		{
			return @"\" + ch;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Escape a character. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="strch">	String with the char to escape. </param>
		///
		/// <returns>	Escaped character. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Esc(this string strch)
		{
			return @"\" + strch;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pattern for integers. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="strName">	Name for the match. </param>
		///
		/// <returns>	Pattern to recognize integers. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Integer(string strName = "")
		{
			string strSearch = "-".Optional() + UnsignedInteger();
			if (strName != String.Empty)
			{
				strSearch = strSearch.Named(strName);
			}
			return strSearch;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pattern for unsigned integers. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="strName">	Name for the match. </param>
		///
		/// <returns>	Pattern to recognize unsigned integers. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string UnsignedInteger(string strName = "")
		{
			string strSearch = Digit.RepAtLeast(1);
			if (strName != String.Empty)
			{
				strSearch = strSearch.Named(strName);
			}
			return strSearch;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pattern for floats. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="strName">	Name for the match. </param>
		///
		/// <returns>	Pattern to recognize floats. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Float(string strName = "")
		{
			string dot = '.'.Esc().Optional();
			string digits = Digit.Rep(0);
			string strSearch = "-".Optional() + AnyOf(UnsignedInteger() + dot + digits, digits + dot + UnsignedInteger());
			if (strName != String.Empty)
			{
				strSearch = strSearch.Named(strName);
			}
			return strSearch;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Forces a greedy search on a pattern. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">	pattern to be made greedy. </param>
		///
		/// <returns>	greedy pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Greedy(this string str)
		{
			return "(?>" + str + ")";
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns a range of chars for use in AnyChar. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="strLow">	Starting char. </param>
		/// <param name="strHigh">	Ending char. </param>
		///
		/// <returns>	The Range. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Range(string strLow, string strHigh)
		{
			return strLow + '-' + strHigh;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Concatenates strings. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="s">	strings to be concatenated. </param>
		///
		/// <returns>	concatenation of all the strings in s. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Cat(params string[] s)
		{
			return s.Aggregate((sAg, str) => sAg + str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Creates a pattern which matches either this or any of the parameters. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">	this. </param>
		/// <param name="s">	the other strings. </param>
		///
		/// <returns>	pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string OrAnyOf(this string str, params string[] s)
		{
			return "(?:" + s.Aggregate(str, (sAg, sNext) => sAg + "|" + sNext) + ")";
		}

		[Obsolete("Use OrAnyOf()")]
		public static string Or(this string str, params string[] s)
		{
			return str.OrAnyOf(s);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Creates a pattern which matches any of the parameters. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="s">	the other strings. </param>
		///
		/// <returns>	pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string AnyOf(params string[] s)
		{
			return s[0].OrAnyOf(s.Skip(1).ToArray());
		}

		[Obsolete("Use AnyOf")]
		public static string Or(params string[] s)
		{
			return AnyOf(s);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Accept any characters from any of the arguments. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="s">	Characters or ranges. </param>
		///
		/// <returns>	Pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
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

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Cosmetic version of NotCharIn which works better for single characters. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="s">	Characters or ranges. </param>
		///
		/// <returns>	Pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Not(params string[] s)
		{
			return NotCharIn(s);
		}

		[Obsolete("Use Not() or NotCharIn()")]
		public static string NoChar(params string[] s)
		{
			return NotCharIn(s);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Parenthesize a pattern properly.  If it's already parenthesized or is one character long,
		/// then it's merely returned.  Otherwise, it's surrounded by (?: ... ). 
		/// </summary>
		///
		/// <remarks>	
		/// This is a non-capturing group - use Capture() for a capturing group.  Darrellp, 10/1/2012. 
		/// </remarks>
		///
		/// <param name="str">	String to be parenthesized. </param>
		///
		/// <returns>	Properly parenthesized string. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string AsGroup(this string str)
		{
			string strRet;

			if (RgxDontParenthesize != null && RgxDontParenthesize.IsMatch(str))
			{
				strRet = str;
			}
			else
			{
				strRet = "(?:" + str + ")";
			}
			return strRet;
		}

		[Obsolete("Use AsGroup")]
		public static string AddParens(this string str)
		{
			return AsGroup(str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Returns pattern in which str is optional. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">	string to be made optional. </param>
		///
		/// <returns>	pattern which optionally matches string. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Optional(this string str)
		{
			return str.Rep(0, 1);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Repeat spec.  repeats at least "least" times and at most "most" times.  Most can be negative in
		/// which case it's considered to be "infinity" - i.e., it's repeated at least "least" times with
		/// no limit on the most. This is the default so Rep(3) means repeat three or more times.
		/// </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <exception cref="ArgumentException">	Thrown when one or more arguments have unsupported or
		/// 										illegal values. </exception>
		///
		/// <param name="str">		String to be repeated in search. </param>
		/// <param name="least">	Least number of times to repeat. </param>
		/// <param name="most">		Most number of times to repeat. </param>
		///
		/// <returns>	pattern which matches the original number of string repeated properly. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Rep(this string str, int least, int most = -1)
		{
			if (least < 0)
			{
				throw new ArgumentException("least must be >= 0 in Rep");
			}
			if (most >= 0 && least > most)
			{
				throw new ArgumentException("Invalid most value in Rep");
			}

			string strRep;

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
			else
			{
				strRep = string.Format("{{{0},{1}}}", least, most);
			}

			return str.AsGroup() + strRep;
		}

		private static string RepAtLeast(this string str, int count)
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
			return str.AsGroup() + strRep;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Positive look ahead. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">	pattern to look ahead for. </param>
		///
		/// <returns>	Positive lookahead pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string PosLookAhead(this string str)
		{
			return string.Format("(?={0})", str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Negative look ahead. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">	pattern to look ahead for. </param>
		///
		/// <returns>	Negative lookahead pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string NegLookAhead(this string str)
		{
			return string.Format("(?!{0})", str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Positive look behind. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">	pattern to look behind for. </param>
		///
		/// <returns>	Positive look behind pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string PosLookBehind(this string str)
		{
			return string.Format("(?<={0})", str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Negative look behind. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">	pattern to look behind for. </param>
		///
		/// <returns>	Negative look behind pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string NegLookBehind(this string str)
		{
			return string.Format("(?<!{0})", str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Puts the text in an unnamed group. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">	String to be matched. </param>
		///
		/// <returns>	Pattern which names the match. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Capture(this string str)
		{
			return string.Format("({0})", str);
		}

		[Obsolete("Use Capture")]
		public static string Group(this string str)
		{
			return Capture(str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Names the match made on the string. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">		String to be matched. </param>
		/// <param name="strName">	Name for the match. </param>
		///
		/// <returns>	Pattern which names the match. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Named(this string str, string strName)
		{
			return string.Format("(?<{0}>{1})", strName, str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Makes the search atomic. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="str">	pattern to be affected. </param>
		///
		/// <returns>	Atomic version of the search string. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Atomic(this string str)
		{
			return string.Format("(?>{0})", str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pattern which depends on whether a group has been matched. </summary>
		///
		/// <remarks>	Darrellp, 10/1/2012. </remarks>
		///
		/// <param name="strLabel">			Group to check whether it's matched. </param>
		/// <param name="strHasMatched">	Pattern to use if there was a match. </param>
		/// <param name="strDidntMatch">	Pattern to use if there wasn't a match. </param>
		///
		/// <returns>	Conditional pattern. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string If(this string strLabel, string strHasMatched, string strDidntMatch = "")
		{
			return string.Format("(?({0}){1}|{2})", strLabel, strHasMatched, strDidntMatch);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	
		/// Pushes the match onto the stack named strStack.  This is actually identical to the Named routine. 
		/// </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">		String to be matched. </param>
		/// <param name="strStack">	Stack to push the match onto. </param>
		///
		/// <returns>	Pattern which pushes the stack. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Push(this string strStack, string str = "")
		{
			return string.Format("(?<{0}>{1})", strStack, str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pops the named stack. </summary>
		///
		/// <remarks>	Darrellp, 8/29/2011. </remarks>
		///
		/// <param name="str">		String to be matched. </param>
		/// <param name="strStack">	Name for the match. </param>
		///
		/// <returns>	Pattern which names the match. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string Pop(this string strStack, string str = "")
		{
			return string.Format("(?<-{0}>{1})", strStack, str);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Pushes a stack while popping another. </summary>
		///
		/// <remarks>	Darrellp, 8/28/2011. </remarks>
		///
		/// <param name="str">			string to be matched. </param>
		/// <param name="strPushStack">	Stack to push match onto. </param>
		/// <param name="strPopStack">	Stack to pop. </param>
		///
		/// <returns>	Regex string to push and pop. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string PushPop(this string str, string strPushStack, string strPopStack)
		{
			return str.Push(strPushStack + "-" + strPopStack);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Matches only if the passed in stack is empty. </summary>
		///
		/// <remarks>	Darrellp, 8/28/2011. </remarks>
		///
		/// <param name="strStack">	Stack to test. </param>
		///
		/// <returns>	Regex pattern which matches if the stack is empty, fails otherwise. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string MatchEmptyStack(this string strStack)
		{
			return If(strStack, Failure);
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Balanced group. </summary>
		///
		/// <remarks>	
		/// Finally figured out what's going on here with the help of 
		/// http://www.codeproject.com/Articles/21080/In-Depth-with-RegEx-Matching-Nested-Constructions.
		/// Darrellp, 10/1/2012. 
		/// </remarks>
		///
		/// <param name="strOpen">	The string open. </param>
		/// <param name="strClose">	The string close. </param>
		///
		/// <returns>	A string matching a balanced group of opening strings and closing strings.. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static string BalancedGroup(string strOpen, string strClose)
		{
			string strPush = strOpen + "bgStack".Push();
			string strPop = strClose + "bgStack".Pop();
			string strAtomicContents = OrAnyOf(strPush, strPop, Any.Optional());
			string strAtomic = strAtomicContents.Atomic().Rep(0);

			return Cat(
				strOpen,
				strAtomic,
				"bgStack".MatchEmptyStack(),
				strClose);
		}
	}
}
