# Stex
There have been a few attempts to try to make a fluent interface for regular expressions in
.NET but most of them I've seen try to make the basic object being acted on either a Regex
object itself or some other class of their own devising.  It seems to me that the obvious
thing to act on is the regex string.  After all, it is the complicating factor in regular
expressions.  So Stex works by supplying string extension methods.  This negates the need
to convert a string into whatever type you're working with.  The string by itself is just
fine.  String concatenation via "+" works just fine (though there is a Cat routine supplied
if you like).

Pretty much all of regular expressions have been included plus some other common ones just
to make life easier.  I originally did this library because I had a crazy complicated
and general date format I needed to account for so there's a recognizer included for
that.  It recognizes dates such as:

```
  10/12/2012
  February 10, 1912
  NOV 4, 1956
  Nov 4, 1956
  1940
  between 1948 and 1950
  11-4-1956
  11-04-195
  4 November 1956
  ca 1932
  after 2000
  before 800 BC
  After Jan. 1, 1932
  between nov 4, 1956 and ca sep 11, 1980
```
  
and puts each of their elements into named parts of the match.  It allows for either British
or American style ordering and you can allow or disallow the "between" which produces two
dates.  The RGX string for the American dates allowing "between" is: 

```
(?i:^(?:(?<betweenPrefix>(?:Between|Bet)) )?(?:(?<prefix>(?:About|Abt|A|After|Aft|Before|Bef|B|Calculated|Cal|Circa|Cir|Ca|C)) )
?(?:(?:(?<mnthName>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|(?<mnthName>
(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<day>\d\d?), (?<year>\d\d\d\d?)|(?<day>\d\d?) (?:(?<mnthName>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|
(?<mnthName>(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<year>\d\d\d\d?)|(?<month>\d\d?)(?:-|/|.| )(?<day>\d\d?)(?:-|/|.| )(?<year>\d\d\d\d?)|(?<year>\d\d\d\d?) 
(?:(?<mnthName>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|(?<mnthName>
(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<day>\d\d?)|(?<year>\d\d\d\d?)(?:-|/|.| )(?<month>\d\d?)(?:-|/|.| )(?<day>\d\d?)|(?<year>\d\d\d\d?))(?: 
(?<suffix>(?:BC|B.C.)))?(?(betweenPrefix) AND (?:(?<prefix2>(?:About|Abt|A|After|Aft|Before|Bef|B|Calculated|Cal|Circa|Cir|Ca|C)) )?
(?:(?:(?<mnthName2>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|
(?<mnthName2>(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<day2>\d\d?), (?<year2>\d\d\d\d?)|(?<day2>\d\d?) (?:(?<mnthName2>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|
(?<mnthName2>(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<year2>\d\d\d\d?)|(?<month2>\d\d?)(?:-|/|.| )(?<day2>\d\d?)(?:-|/|.| )(?<year2>\d\d\d\d?)|(?<year2>\d\d\d\d?) 
(?:(?<mnthName2>(?:JAN|FEB|MAR|APR|JUN|JUL|AUG|SEP|OCT|NOV|DEC)).?|(?<mnthName2>
(?:JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER))) 
(?<day2>\d\d?)|(?<year2>\d\d\d\d?)(?:-|/|.| )(?<month2>\d\d?)(?:-|/|.| )(?<day2>\d\d?)|(?<year2>\d\d\d\d?))
(?: (?<suffix2>(?:BC|B.C.)))?|)$)
```

I don't think I ever could have come up with this string without Stex.

A couple of examples (from Linqpad which
supplies the "Dump" routine):

```C#
// Password check to ensure
//		8 characters length
//		2 letters in Upper Case
//		1 Special Character(!@#$&*)
//		2 numerals (0-9)
//		3 letters in Lower Case
var rgxString = 
Begin +
	(Any.Rep(0) + CapLetter + Any.Rep(0) + CapLetter).PosLookAhead() +
	(Any.Rep(0) + AnyCharFrom("!@#$&*")).PosLookAhead() +
	(Any.Rep(0) + Digit + Any.Rep(0) + Digit).PosLookAhead() +
	(Any.Rep(0) + LowerLetter + Any.Rep(0) + LowerLetter).PosLookAhead() +
	Any.Rep(8,8) +
	End;
rgxString.Dump();
var rgx = new Regex(rgxString);
rgx.Match("3AA!aa3a").Success.Dump("3AA!aa3a");
rgx.Match("3AA!aa@a").Success.Dump("3AA!aa@a");
rgx.Match("3AA!aa3ab").Success.Dump("3AA!aa3ab");
rgx.Match("Password").Success.Dump("Password");
```

The rgx string produced is:
```
  ^(?=.*[A-Z].*[A-Z])(?=.*[!@#$&*])(?=.*\d.*\d)(?=.*[a-z].*[a-z]).{8}$
```
  
The Rep() function takes either 1 or 2 arguments.  The first argument is the minimum number of repeats and the second
is the maximum.  If the second argument is omitted, then it's effectively infinity.  It's smart enough to use shortcuts
when they make sense.  So Rep(0) produces "*", Rep(1) produces "+", Rep(0,1) produces "?", Rep(3,3) produces "{3}" and
Rep(3,4) produces "{3,4}".  There are many static values and functions such as CapLetter, AnyCharFrom(), Digit, etc..
It used to be a bit tedious having to prefix all of these with "Stex" but with C# 4.6 life is much better by setting
  using static Stex;
Thus, these static values/methods are not prefixed by Stex above.
