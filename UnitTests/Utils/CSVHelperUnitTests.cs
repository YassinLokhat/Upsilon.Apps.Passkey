using FluentAssertions;
using Upsilon.Apps.Passkey.Core.Utils;

namespace Upsilon.Apps.Passkey.UnitTests.Utils
{
   [TestClass]
   public sealed class CSVHelperUnitTests
   {
      [TestMethod]
      /*
       * Tab-separated rows (the export format) split on each tab.
      */
      public void Case01_TabDelimited()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("a\tb\tc");

         _ = fields.Should().Equal("a", "b", "c");
      }

      [TestMethod]
      /*
       * Comma-separated rows (Excel CSV) split on each comma.
      */
      public void Case02_CommaDelimited()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("a,b,c");

         _ = fields.Should().Equal("a", "b", "c");
      }

      [TestMethod]
      /*
       * A comma inside a quoted JSON cell is data, not a delimiter.
      */
      public void Case03_QuotedCommaIsKept()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("\"hello, world\",\"next\"");

         _ = fields.Should().Equal("\"hello, world\"", "\"next\"");
      }

      [TestMethod]
      /*
       * A tab inside a quoted JSON cell is data, not a delimiter.
      */
      public void Case04_QuotedTabIsKept()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("\"hello\tworld\",\"next\"");

         _ = fields.Should().Equal("\"hello\tworld\"", "\"next\"");
      }

      [TestMethod]
      /*
       * A backslash-escaped comma or tab is not a delimiter.
      */
      public void Case05_EscapedSeparatorsAreKept()
      {
         string[] commaFields = CSVHelper.SplitTabOrCommaDelimited(@"a\,b,c");
         string[] tabFields = CSVHelper.SplitTabOrCommaDelimited("a\\\tb\tc");

         _ = commaFields.Should().Equal(@"a\,b", "c");
         _ = tabFields.Should().Equal("a\\\tb", "c");
      }

      [TestMethod]
      /*
       * JSON-escaped quotes (\" ) do not end the quoted cell, so a comma
       * after them still belongs to that field.
      */
      public void Case06_EscapedQuotesDoNotEndCell()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("\"say \\\"hi\\\", please\",\"next\"");

         _ = fields.Should().Equal("\"say \\\"hi\\\", please\"", "\"next\"");
      }

      [TestMethod]
      /*
       * Consecutive delimiters produce empty fields, matching string.Split.
      */
      public void Case07_EmptyFields()
      {
         string[] fields = CSVHelper.SplitTabOrCommaDelimited("a,,b,\t,c,");

         _ = fields.Should().Equal("a", "", "b", "", "", "c", "");
      }

      [TestMethod]
      /*
       * A real export row (JSON cells, tab separators) keeps nine columns.
      */
      public void Case08_ExportedJsonCells()
      {
         const string line = "\"Service0\"\t\"http://service0.xyz\"\t\"notes, with comma\"\t\"Account0\"\t\"a@x.yz|b@x.yz\"\t\"0000\"\t\"Account0's notes\"\t\"None\"\t3";

         string[] fields = CSVHelper.SplitTabOrCommaDelimited(line);

         _ = fields.Should().HaveCount(9);
         _ = fields[0].Should().Be("\"Service0\"");
         _ = fields[2].Should().Be("\"notes, with comma\"");
         _ = fields[8].Should().Be("3");
      }
   }
}
