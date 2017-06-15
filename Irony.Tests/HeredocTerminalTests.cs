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
            Assert.IsTrue((string)token.Value == Environment.NewLine + "test", "Token Value is wrong, got {0} of type {1}", token.Value, token.Value.GetType().ToString());
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

        private Parser _p;

    }

}