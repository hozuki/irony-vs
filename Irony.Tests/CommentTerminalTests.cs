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
    public sealed class CommentTerminalTests {

        [TestMethod]
        public void TestCommentTerminal() {
            var parser = TestHelper.CreateParser(new CommentTerminal("Comment", "/*", "*/"));
            var token = parser.ParseInput("/* abc  */");
            Assert.IsTrue(token.Category == TokenCategory.Comment, "Failed to read comment");

            parser = TestHelper.CreateParser(new CommentTerminal("Comment", "//", "\n"));
            token = parser.ParseInput("// abc  \n   ");
            Assert.IsTrue(token.Category == TokenCategory.Comment, "Failed to read line comment");
        }

    }
}
