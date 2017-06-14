using Irony.Parsing;

//Tests of Visual Studio integration functionality

namespace Irony.Tests {
#if USE_NUNIT
    using NUnit.Framework;
    using TestClass = NUnit.Framework.TestFixtureAttribute;
    using TestMethod = NUnit.Framework.TestAttribute;
    using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    [TestClass]
    public sealed class IntegrationTests {

        private sealed class IntegrationTestGrammar : Grammar {
            public IntegrationTestGrammar() {
                var comment = new CommentTerminal("comment", "/*", "*/");
                NonGrammarTerminals.Add(comment);
                var str = new StringLiteral("str", "'", StringOptions.AllowsLineBreak);
                var stmt = new NonTerminal("stmt") {
                    Rule = str | Empty
                };
                Root = stmt;
            }
        }

        [TestMethod]
        public void TestIntegration_VsScanningComment() {
            Init(new IntegrationTestGrammar());
            SetSource(" /*  ");
            var token = Read();
            Assert.IsTrue(token.IsSet(TokenFlags.IsIncomplete), "Expected incomplete token (line 1)");
            token = Read();
            Assert.IsNull(token, "NULL expected");
            SetSource(" comment ");
            token = Read();
            Assert.IsTrue(token.IsSet(TokenFlags.IsIncomplete), "Expected incomplete token (line 2)");
            token = Read();
            Assert.IsNull(token, "NULL expected");
            SetSource(" */ /*x*/");
            token = Read();
            Assert.IsFalse(token.IsSet(TokenFlags.IsIncomplete), "Expected complete token (line 3)");
            token = Read();
            Assert.IsFalse(token.IsSet(TokenFlags.IsIncomplete), "Expected complete token (line 3)");
            token = Read();
            Assert.IsNull(token, "Null expected.");
        }

        [TestMethod]
        public void TestIntegration_VsScanningString() {
            Init(new IntegrationTestGrammar());
            SetSource(" 'abc");
            var token = Read();
            Assert.IsTrue(token.ValueString == "abc", "Expected incomplete token 'abc' (line 1)");
            Assert.IsTrue(token.IsSet(TokenFlags.IsIncomplete), "Expected incomplete token (line 1)");
            token = Read();
            Assert.IsNull(token, "NULL expected");
            SetSource(" def ");
            token = Read();
            Assert.IsTrue(token.ValueString == " def ", "Expected incomplete token ' def ' (line 2)");
            Assert.IsTrue(token.IsSet(TokenFlags.IsIncomplete), "Expected incomplete token (line 2)");
            token = Read();
            Assert.IsNull(token, "NULL expected");
            SetSource("ghi' 'x'");
            token = Read();
            Assert.IsTrue(token.ValueString == "ghi", "Expected token 'ghi' (line 3)");
            Assert.IsFalse(token.IsSet(TokenFlags.IsIncomplete), "Expected complete token (line 3)");
            token = Read();
            Assert.IsTrue(token.ValueString == "x", "Expected token 'x' (line 3)");
            Assert.IsFalse(token.IsSet(TokenFlags.IsIncomplete), "Expected complete token (line 3)");
            token = Read();
            Assert.IsNull(token, "Null expected.");
        }

        private void Init(Grammar grammar) {
            _grammar = grammar;
            _language = new LanguageData(_grammar);
            var parser = new Parser(_language);
            _scanner = parser.Scanner;
            _context = parser.Context;
            _context.Mode = ParseMode.VsLineScan;
        }

        private void SetSource(string text) {
            _scanner.VsSetSource(text, 0);
        }

        private Token Read() {
            var token = _scanner.VsReadToken(ref _state);
            return token;
        }

        private Grammar _grammar;
        private LanguageData _language;
        private Scanner _scanner;
        private ParsingContext _context;
        private int _state;

    }
}