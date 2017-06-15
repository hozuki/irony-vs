using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Irony.Tests {
#if USE_NUNIT
    using NUnit.Framework;
    using TestClass = NUnit.Framework.TestFixtureAttribute;
    using TestMethod = NUnit.Framework.TestAttribute;
    using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
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
            Assert.IsTrue((string)token.Value == Environment.NewLine + "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
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
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
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
            Assert.IsTrue((string)token.Value == Environment.NewLine + "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
        }

        [TestMethod]
        public void TestHereDocIndentedStripBeginningLiteral() {
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.AllowIndentedEndToken | HereDocOptions.RemoveBeginningNewLine);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test
                        BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
        }

        [TestMethod]
        public void TestHereDocQuoted() {
            var quotes = new HashSet<string> { "'", "\"" };
            var term = new HereDocTerminal("Heredoc", "<<-", quotes, HereDocOptions.RemoveBeginningNewLine);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-'BEGIN'
test
BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
        }

        [TestMethod]
        public void TestHereDocQuotedButUnused() {
            var quotes = new HashSet<string> { "'", "\"" };
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.RemoveBeginningNewLine);
            term.AddSubType("<<-", quotes, HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test
BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            Assert.IsTrue((string)token.Value == "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
        }

        [TestMethod]
        public void TestHereDocUndented() {
            var quotes = new HashSet<string> { "'", "\"" };
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.RemoveBeginningNewLine | HereDocOptions.RemoveIndents | HereDocOptions.AllowIndentedEndToken);
            term.AddSubType("<<-", quotes, HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
        test0
test1
    test2
      test3
    BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsNotNull(token.Value, "Token Value field is null - should be string.");
            var nl = Environment.NewLine;
            Assert.IsTrue((string)token.Value == $"    test0{nl}test1{nl}test2{nl}  test3", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType());
        }

        [TestMethod]
        public void TestHereDocQuotedTemplateBehavior() {
            // TODO: something like this Ruby snippet:
            /*
             * num = 0;
             * str1 = <<-EOS
             * The number is #{num}.
             * EOS
             * str2 = <<-'EOS'
             * The number is #{num}.
             * EOS
             * 
             * The second one will not be formatted.
             */
        }

        [TestMethod]
        public void TestHereDocMultipleHereDocs() {
            // TODO: something like this Ruby snippet:
            // Yes there is a potential problem in quote matching, so this example will force updating the method.
            /*
             * num = 0;
             * str1 = <<-EOS1 <<-"EOS2"
             * E1 only contains this line.
             * EOS1
             * E2 only contains this line.
             * EOS
             * 
             * The second one will not be formatted.
             */
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
        public void TestHereDocUndentedNotAllowedError() {
            var quotes = new HashSet<string> { "'", "\"" };
            var term = new HereDocTerminal("Heredoc", "<<-", HereDocOptions.RemoveBeginningNewLine | HereDocOptions.RemoveIndents);
            term.AddSubType("<<-", quotes, HereDocOptions.None);
            var parser = TestHelper.CreateParser(term);
            var token = parser.ParseInput(@"<<-BEGIN
test1
    test2
      test3
    BEGIN");
            Assert.IsNotNull(token, "Failed to produce a token on valid string.");
            Assert.IsTrue(token.IsError, "Failed to detect error on invalid heredoc.");
        }

        private Parser _p;

    }

}