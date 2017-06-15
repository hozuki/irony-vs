using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;

namespace Irony.Tests {
#if USE_NUNIT
    using NUnit.Framework;
    using TestClass = NUnit.Framework.TestFixtureAttribute;
    using TestMethod = NUnit.Framework.TestAttribute;
    using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    //Currently not used, HereDocTerminal needs to be finished

    /// <summary>
    /// Summary description for HeredocTerminalTests
    /// </summary>
    [TestClass]
    public sealed class HeredocTerminalTests {

        private sealed class HereDocTestGrammar : Grammar {
            public HereDocTestGrammar()
                : base(true) {
                var heredoc = new HereDocTerminal("HereDoc", "<<", HereDocOptions.None);
                heredoc.AddSubType("<<-", HereDocOptions.AllowIndentedEndToken);
                var @string = new StringLiteral("string", "\"");
                var program = new NonTerminal("program") {
                    Rule = heredoc + @"+" + @string + NewLine + @"+" + @string
                           | heredoc + @"+" + heredoc + @"+" + @string + NewLine
                           | heredoc + @"+" + @string + NewLine
                           | heredoc + @"+" + @string + @"+" + heredoc
                           | heredoc + @"+" + heredoc
                           | heredoc
                };
                Root = program;
                MarkPunctuation("+");
            }
        }

        private string NormalizeParseTree(ParseTree tree) {
            var fullString = new StringBuilder();
            foreach (var node in tree.Root.ChildNodes) {
                fullString.Append(node.Token.Value);
            }
            fullString = fullString.Replace("\r\n", "\\n");
            fullString = fullString.Replace("\n", "\\n");
            return fullString.ToString();
        }

        [TestInitialize]
        public void HereDocSetup() {
            var grammar = new HereDocTestGrammar();
            _p = new Parser(grammar);
            _p.Context.SetOption(ParseOptions.TraceParser, true);
        }

        [TestMethod]
        public void TestHereDocLiteral() {
            var term = new HereDocTerminal("Heredoc", "<<", HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<BEGIN
test
BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == Environment.NewLine + "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType().ToString());
        }

        [TestMethod]
        public void TestHereDocStripBeginningLiteral() {
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.RemoveBeginningNewLine);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test
BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType().ToString());
        }

        [TestMethod]
        public void TestHereDocIndentedLiteral() {
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.AllowIndentedEndToken);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test
                        BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType().ToString());
        }

        [TestMethod]
        public void TestHereDocLiteralError() {
            var term = new HereDocTerminal("Heredoc", "<<", HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<BEGIN
test");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsTrue(token.IsError, "Failed to detect error on invalid heredoc.");
        }

        [TestMethod]
        public void TestHereDocIndentedLiteralError() {
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.AllowIndentedEndToken);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsTrue(token.IsError, "Failed to detect error on invalid heredoc.");
        }

        [TestMethod]
        public void TestHereDocLiteralErrorIndented() {
            var term = new HereDocTerminal("Heredoc", "<<", HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<BEGIN
test
     BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsTrue(token.IsError, "Failed to detect error on invalid heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDocStringString() {
            var tree = _p.Parse(@"<<HELLO + ""<--- this is the middle --->\n""
This is the beginning.
It is two lines long.
HELLO 
+ ""And now it's over!""");
            Assert.AreEqual(@"This is the beginning.\nIt is two lines long.\n<--- this is the middle --->\nAnd now it's over!", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDocStringHereDoc() {
            var tree = _p.Parse(@"<<HELLO + ""<--- this is the middle --->\n"" + <<END
This is the beginning.
It is more than two lines long.
It is three lines long.
HELLO 
And now it's over!
But we have three lines left.
Now two more lines.
Oops, last line! :(
END");
            Assert.AreEqual(@"This is the beginning.\nIt is more than two lines long.\nIt is three lines long.\n<--- this is the middle --->\nAnd now it's over!\nBut we have three lines left.\nNow two more lines.\nOops, last line! :(", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDocHereDoc() {
            var tree = _p.Parse(@"<<HELLO + <<END
This is the beginning.
How are you doing?
HELLO 
I'm fine.
And now it's over!
END");
            Assert.AreEqual(@"This is the beginning.\nHow are you doing?\nI'm fine.\nAnd now it's over!", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDoc() {
            var tree = _p.Parse(@"<<HELLO
This is the beginning.
I hope you enjoyed these tests.
HELLO");
            Assert.AreEqual(@"This is the beginning.\nI hope you enjoyed these tests.", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDocHereDocString() {
            var tree = _p.Parse(@"<<HELLO + <<MIDDLE + ""<--- And now it's over --->""
This is the beginning.
HELLO
And this is the middle.
MIDDLE");
            Assert.AreEqual(@"This is the beginning.\nAnd this is the middle.\n<--- And now it's over --->", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseHereDocString() {
            var tree = _p.Parse(@"<<HELLO + ""<--- this is the end --->""
This is the beginning.
HELLO");
            Assert.AreEqual(@"This is the beginning.\n<--- this is the end --->", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        [TestMethod]
        public void TestHereDocParseIndentHereDocStringHereDoc() {
            var tree = _p.Parse(@"<<-BEGIN + ""<--- middle --->\n"" + <<-END
This is the beginning:
		BEGIN
And now it is over!
		END");
            Assert.AreEqual(@"This is the beginning:\n<--- middle --->\nAnd now it is over!", NormalizeParseTree(tree), "Incorrectly parsed heredoc.");
        }

        private Parser _p;

    }

}