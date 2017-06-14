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

    [TestClass]
    public sealed class ErrorRecoveryTests {

        #region Grammars
        //A simple grammar for language consisting of simple assignment statements: x=y + z; z= t + m;
        private sealed class ErrorRecoveryGrammar : Grammar {

            public ErrorRecoveryGrammar() {
                var id = new IdentifierTerminal("id");
                var expr = new NonTerminal("expr");
                var stmt = new NonTerminal("stmt");
                var stmtList = new NonTerminal("stmt");

                Root = stmtList;
                stmtList.Rule = MakeStarRule(stmtList, stmt);
                stmt.Rule = id + "=" + expr + ";";
                stmt.ErrorRule = SyntaxError + ";";
                expr.Rule = id | id + "+" + id;
            }

        }
        #endregion

        [TestMethod]
        public void TestErrorRecovery() {
            var grammar = new ErrorRecoveryGrammar();
            var parser = new Parser(grammar);
            TestHelper.CheckGrammarErrors(parser);

            //correct sample
            var parseTree = parser.Parse("x = y; y = z + m; m = n;");
            Assert.IsFalse(parseTree.HasErrors, "Unexpected parse errors in correct source sample.");

            parseTree = parser.Parse("x = y; m = = d ; y = z + m; x = z z; m = n;");
            Assert.AreEqual(2, parseTree.ParserMessages.Count, "Invalid # of errors.");
        }

    }

}
