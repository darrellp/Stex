using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using RegexStringLibrary;
using System.Text.RegularExpressions;

namespace RegexStringTestProject
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class RegexStringTest
	{
	    /// <summary>
	    ///Gets or sets the test context which provides
	    ///information about and functionality for the current test run.
	    ///</summary>
	    public TestContext TestContext { get; set; }

	    #region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		[TestMethod]
		public void ConcatTest()
		{
			string output = Stex.Cat(Stex.Bell, Stex.CR, Stex.Digit, "Darrell");
			Assert.AreEqual(@"\a\r\dDarrell", output);
		}

		[TestMethod]
		public void HexTest()
		{
			string output = "FF".Hex();
			Assert.AreEqual(@"\xFF", output);
		}

		public void VerifyGoodDate(Regex rgx, string strDate, string strPrefix, string strMonth, string strMonthName, string strDay, string strYear, string strSuffix)
		{
			Match mtch = rgx.Match(strDate);
			Assert.IsTrue(mtch.Success);
			Assert.AreEqual(strPrefix, mtch.Groups["prefix"].Value);
			Assert.AreEqual(strMonth, mtch.Groups["month"].Value);
			Assert.AreEqual(strMonthName, mtch.Groups["mnthName"].Value);
			Assert.AreEqual(strDay, mtch.Groups["day"].Value);
			Assert.AreEqual(strYear, mtch.Groups["year"].Value);
			Assert.AreEqual(strSuffix, mtch.Groups["suffix"].Value);
		}

		[TestMethod]
		public void DateTest()
		{
			Regex rgxBet = Stex.AmericanDateBetRegExp;
			Regex rgx = Stex.AmericanDateRegExp;
			VerifyGoodDate(rgxBet, "11/04/1956", "", "11", "", "04", "1956", "");
			VerifyGoodDate(rgxBet, "NOV 4, 1956", "", "", "NOV", "4", "1956", "");
			VerifyGoodDate(rgxBet, "Nov 4, 1956", "", "", "Nov", "4", "1956", "");
			VerifyGoodDate(rgxBet, "11-04-1956", "", "11", "", "04", "1956", "");
			VerifyGoodDate(rgxBet, "4 November 1956", "", "", "November", "4", "1956", "");
			VerifyGoodDate(rgxBet, "ca 1956 11 4", "ca", "11", "", "4", "1956", "");
			VerifyGoodDate(rgxBet, "before 800 BC", "before", "", "", "", "800", "BC");
			VerifyGoodDate(rgxBet, "After Jan. 1, 1932", "After", "", "Jan", "1", "1932", "");
			Match mtch = rgxBet.Match("between nov 4, 1956 and ca sep 11, 1980");
			Assert.IsTrue(mtch.Success);
			Assert.AreEqual("between", mtch.Groups["betweenPrefix"].Value);
			Assert.AreEqual("nov", mtch.Groups["mnthName"].Value);
			Assert.AreEqual("4", mtch.Groups["day"].Value);
			Assert.AreEqual("1956", mtch.Groups["year"].Value);
			Assert.AreEqual("ca", mtch.Groups["prefix2"].Value);
			Assert.AreEqual("sep", mtch.Groups["mnthName2"].Value);
			Assert.AreEqual("11", mtch.Groups["day2"].Value);
			Assert.AreEqual("1980", mtch.Groups["year2"].Value);
			mtch = rgxBet.Match("nov 4, 1956 and ca sep 11, 1980");
			Assert.IsFalse(mtch.Success);
			mtch = rgxBet.Match("between nov 4, 1956");
			Assert.IsFalse(mtch.Success);
			mtch = rgx.Match("between nov 4, 1956 and ca sep 11, 1980");
			Assert.IsFalse(mtch.Success);
			mtch = rgx.Match("nov 4, 1956");
			Assert.IsTrue(mtch.Success);
			Stex.DateInfo di = Stex.GetDateInfo("between nov 4, 1956 and ca sep 11, 1980", true, true);
			Assert.IsTrue(di.Success);
			Assert.IsTrue(di.Between);
			Assert.AreEqual("", di.Prefix1);
			Assert.AreEqual("ca", di.Prefix2);
			Assert.AreEqual(11, di.Date1.Month);
			Assert.AreEqual(4, di.Date1.Day);
			Assert.AreEqual(1956, di.Date1.Year);
			Assert.AreEqual(9, di.Date2.Month);
			Assert.AreEqual(11, di.Date2.Day);
			Assert.AreEqual(1980, di.Date2.Year);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RepTest()
		{
			string output = Stex.Cat(
				"a".Rep(2, 3),
				"multi-char".Rep(4, 4),
				"c".Rep(0,1));
			Assert.AreEqual("a{2,3}(?:multi-char){4}c?", output);

			output = Stex.Cat(
				"a".Rep(0,-1),
				"multi-char".Rep(1,-1),
				"c".Rep(10,-1));
			Assert.AreEqual("a*(?:multi-char)+c{10,}", output);
			
			"a".Rep(2, 1);
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RepAtLeastTest()
		{
			string output = Stex.Cat(
				"a".RepAtLeast(0),
				"multi-char".RepAtLeast(1),
				"c".RepAtLeast(10));
			Assert.AreEqual("a*(?:multi-char)+c{10,}", output);

			"a".RepAtLeast(-1);
		}
		[TestMethod]
		public void NameTest()
		{
			string output = "darrell".Named("name");
			Assert.AreEqual("(?<name>darrell)", output);
		}
		[TestMethod]
		public void OrTest()
		{
			string output = Stex.AnyOf("Darrell", "Jim", "Bob", "Fred");
			Regex rgxOr = new Regex(output);
			Match match = rgxOr.Match("Jim", 0);
			Assert.IsTrue(match.Success);
			Assert.AreEqual("Jim", match.Value);
			Assert.AreEqual("(?:Darrell|Jim|Bob|Fred)", output);
			output = Stex.AnyOf("Darrell","Jim","Bob","Fred");
			Assert.AreEqual("(?:Darrell|Jim|Bob|Fred)", output);
		}

	    static void CheckAddParens(string str, bool fAdd)
		{
			if (!fAdd)
			{
				Assert.AreEqual(str, str.AsGroup());
			}
			else
			{
				Assert.AreEqual("(?:" + str + ")", str.AsGroup());
			}
		}

		[TestMethod]
		public void AddParensTest()
		{
			const string str0 = "a";
			const string str1 = @"\a";
			const string str2 = "[abc]";
			const string str3 = "(abc)";
			const string str4 = @"[a\]b]";
			const string str5 = "(ab[cd])";
			const string str6 = @"(a\)bc)";
			CheckAddParens(str0, false);
			CheckAddParens(str1, false);
			CheckAddParens(str2, false);
			CheckAddParens(str3, false);
			CheckAddParens(str4, false);
			CheckAddParens(str5, false);
			CheckAddParens(str6, false);

			const string str7 = "multi-char";
			const string str8 = "(abc)(def)";
			const string str9 = "[abc][def]";
			const string str10 = @"(abc\)";
			const string str11 = @"[abc\]";
			CheckAddParens(str7, true);
			CheckAddParens(str8, true);
			CheckAddParens(str9, true);
			CheckAddParens(str10, true);
			CheckAddParens(str11, true);
		}

		private static void TestNumericString(Regex rgx, string strTest, bool fMatch)
		{
			Match mtch = rgx.Match(strTest);
			Assert.AreEqual(mtch.Success, fMatch);
			if (fMatch)
			{
				Assert.AreEqual(mtch.Groups["value"].Value, strTest);
			}
		}

		[TestMethod]
		public void NumberTest()
		{
			Regex rgx = new Regex(Stex.StartAt + Stex.Integer("value") + Stex.End);

			TestNumericString(rgx, "143", true);
			TestNumericString(rgx, "-143", true);
			TestNumericString(rgx, "143A", false);
			TestNumericString(rgx, "a14", false);
			TestNumericString(rgx, "--143", false);
			TestNumericString(rgx, "-", false);

			rgx = new Regex(Stex.StartAt + Stex.UnsignedInteger("value") + Stex.End);
			TestNumericString(rgx, "143", true);
			TestNumericString(rgx, "-143", false);

			rgx = new Regex(Stex.StartAt + Stex.Float("value") + Stex.End);
			TestNumericString(rgx, "143", true);
			TestNumericString(rgx, "143.", true);
			TestNumericString(rgx, "-143.", true);
			TestNumericString(rgx, "-143", true);
			TestNumericString(rgx, "143.2", true);
			TestNumericString(rgx, ".3", true);
			TestNumericString(rgx, "-.3", true);
			TestNumericString(rgx, ".", false);
			TestNumericString(rgx, "-", false);
			TestNumericString(rgx, "143.a", false);
			TestNumericString(rgx, "a.2", false);
			TestNumericString(rgx, "-a", false);
			TestNumericString(rgx, "4 7", false);
			TestNumericString(rgx, "4 7 1", false);
		}

		[TestMethod]
		public void BalancingGroupTest()
		{
			string strBG = Stex.BalancingGroup("{", "}", Stex.NotCharIn("{}"));
		}
	}
}