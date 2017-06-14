using System;
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

    //Authors: Roman Ivantsov, Philipp Serr

    [TestClass]
    public sealed class NumberLiteralTests {

        [TestMethod]
        public void TestNumber_General() {
            var number = new NumberLiteral("Number");
            number.DefaultIntTypes.AddRange(TypeCode.Int32, TypeCode.Int64, NumberLiteral.TypeCodeBigInt);
            var parser = TestHelper.CreateParser(number);
            var token = parser.ParseInput("123");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");
            token = parser.ParseInput("123.4");
            Assert.IsTrue(Math.Abs(Convert.ToDouble(token.Value) - 123.4) < 0.000001, "Failed to read float value");
            //100 digits
            const string sbig = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
            token = parser.ParseInput(sbig);
            Assert.IsTrue(token.Value.ToString() == sbig, "Failed to read big integer value");
        }//method

        //The following "sign" test methods and a fix are contributed by ashmind codeplex user
        [TestMethod]
        public void TestNumber_SignedDoesNotMatchSingleMinus() {
            var number = new NumberLiteral("number", NumberOptions.AllowSign);
            var parser = TestHelper.CreateParser(number);
            var token = parser.ParseInput("-");
            Assert.IsTrue(token.IsError, "Parsed single '-' as a number value.");
        }

        [TestMethod]
        public void TestNumber_SignedDoesNotMatchSinglePlus() {
            var number = new NumberLiteral("number", NumberOptions.AllowSign);
            var parser = TestHelper.CreateParser(number);
            var token = parser.ParseInput("+");
            Assert.IsTrue(token.IsError, "Parsed single '+' as a number value.");
        }

        [TestMethod]
        public void TestNumber_SignedMatchesNegativeCorrectly() {
            var number = new NumberLiteral("number", NumberOptions.AllowSign);
            var parser = TestHelper.CreateParser(number);
            var token = parser.ParseInput("-500");
            Assert.AreEqual(-500, token.Value, "Negative number was parsed incorrectly; expected: {0}, scanned: {1}", "-500", token.Value);
        }

        [TestMethod]
        public void TestNumber_CSharp() {
            const double eps = 0.0001;
            var parser = TestHelper.CreateParser(TerminalFactory.CreateCSharpNumber("Number"));

            //Simple integers and suffixes
            var token = parser.ParseInput("123 ");
            CheckType(token, typeof(int));
            Assert.IsTrue(token.Details != null, "ScanDetails object not found in token.");
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");

            token = parser.ParseInput(int.MaxValue.ToString());
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == int.MaxValue, "Failed to read Int32.MaxValue.");

            token = parser.ParseInput(ulong.MaxValue.ToString());
            CheckType(token, typeof(ulong));
            Assert.IsTrue((ulong)token.Value == ulong.MaxValue, "Failed to read uint64.MaxValue value");

            token = parser.ParseInput("123U ");
            CheckType(token, typeof(uint));
            Assert.IsTrue((uint)token.Value == 123, "Failed to read uint value");

            token = parser.ParseInput("123L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 123, "Failed to read long value");

            token = parser.ParseInput("123uL ");
            CheckType(token, typeof(ulong));
            Assert.IsTrue((ulong)token.Value == 123, "Failed to read ulong value");

            //Hex representation
            token = parser.ParseInput("0x012 ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 0x012, "Failed to read hex int value");

            token = parser.ParseInput("0x12U ");
            CheckType(token, typeof(uint));
            Assert.IsTrue((uint)token.Value == 0x012, "Failed to read hex uint value");

            token = parser.ParseInput("0x012L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 0x012, "Failed to read hex long value");

            token = parser.ParseInput("0x012uL ");
            CheckType(token, typeof(ulong));
            Assert.IsTrue((ulong)token.Value == 0x012, "Failed to read hex ulong value");

            //Floating point types
            token = parser.ParseInput("123.4 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #1");

            token = parser.ParseInput("1234e-1 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 1234e-1) < eps, "Failed to read double value #2");

            token = parser.ParseInput("12.34e+01 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #3");

            token = parser.ParseInput("0.1234E3 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #4");

            token = parser.ParseInput("123.4f ");
            CheckType(token, typeof(float));
            Assert.IsTrue(Math.Abs((float)token.Value - 123.4) < eps, "Failed to read float(single) value");

            token = parser.ParseInput("123.4m ");
            CheckType(token, typeof(decimal));
            Assert.IsTrue(Math.Abs((decimal)token.Value - 123.4m) < Convert.ToDecimal(eps), "Failed to read decimal value");

            token = parser.ParseInput("123. ", false); //should ignore dot and read number as int. compare it to python numbers - see below
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value with trailing dot");

            //Quick parse
            token = parser.ParseInput("1 ");
            CheckType(token, typeof(int));
            //When going through quick parse path (for one-digit numbers), the NumberScanInfo record is not created and hence is absent in Attributes
            Assert.IsTrue(token.Details == null, "Quick parse test failed: ScanDetails object is found in token - quick parse path should not produce this object.");
            Assert.IsTrue((int)token.Value == 1, "Failed to read quick-parse value");
        }

        [TestMethod]
        public void TestNumber_VB() {
            const double eps = 0.0001;
            var parser = TestHelper.CreateParser(TerminalFactory.CreateVbNumber("Number"));

            //Simple integer
            var token = parser.ParseInput("123 ");
            CheckType(token, typeof(int));
            Assert.IsTrue(token.Details != null, "ScanDetails object not found in token.");
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");

            //Test all suffixes
            token = parser.ParseInput("123S ");
            CheckType(token, typeof(short));
            Assert.IsTrue((short)token.Value == 123, "Failed to read short value");

            token = parser.ParseInput("123I ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");

            token = parser.ParseInput("123% ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");

            token = parser.ParseInput("123L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 123, "Failed to read long value");

            token = parser.ParseInput("123& ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 123, "Failed to read long value");

            token = parser.ParseInput("123us ");
            CheckType(token, typeof(ushort));
            Assert.IsTrue((ushort)token.Value == 123, "Failed to read ushort value");

            token = parser.ParseInput("123ui ");
            CheckType(token, typeof(uint));
            Assert.IsTrue((uint)token.Value == 123, "Failed to read uint value");

            token = parser.ParseInput("123ul ");
            CheckType(token, typeof(ulong));
            Assert.IsTrue((ulong)token.Value == 123, "Failed to read ulong value");

            //Hex and octal 
            token = parser.ParseInput("&H012 ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 0x012, "Failed to read hex int value");

            token = parser.ParseInput("&H012L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 0x012, "Failed to read hex long value");

            token = parser.ParseInput("&O012 ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 10, "Failed to read octal int value"); //12(oct) = 10(dec)

            token = parser.ParseInput("&o012L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 10, "Failed to read octal long value");

            //Floating point types
            token = parser.ParseInput("123.4 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #1");

            token = parser.ParseInput("1234e-1 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 1234e-1) < eps, "Failed to read double value #2");

            token = parser.ParseInput("12.34e+01 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #3");

            token = parser.ParseInput("0.1234E3 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #4");

            token = parser.ParseInput("123.4R ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #5");

            token = parser.ParseInput("123.4# ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #6");

            token = parser.ParseInput("123.4f ");
            CheckType(token, typeof(float));
            Assert.IsTrue(Math.Abs((float)token.Value - 123.4) < eps, "Failed to read float(single) value");

            token = parser.ParseInput("123.4! ");
            CheckType(token, typeof(float));
            Assert.IsTrue(Math.Abs((float)token.Value - 123.4) < eps, "Failed to read float(single) value");

            token = parser.ParseInput("123.4D ");
            CheckType(token, typeof(decimal));
            Assert.IsTrue(Math.Abs((decimal)token.Value - 123.4m) < Convert.ToDecimal(eps), "Failed to read decimal value");

            token = parser.ParseInput("123.4@ ");
            CheckType(token, typeof(decimal));
            Assert.IsTrue(Math.Abs((decimal)token.Value - 123.4m) < Convert.ToDecimal(eps), "Failed to read decimal value");

            //Quick parse
            token = parser.ParseInput("1 ");
            CheckType(token, typeof(int));
            //When going through quick parse path (for one-digit numbers), the NumberScanInfo record is not created and hence is absent in Attributes
            Assert.IsTrue(token.Details == null, "Quick parse test failed: ScanDetails object is found in token - quick parse path should not produce this object.");
            Assert.IsTrue((int)token.Value == 1, "Failed to read quick-parse value");
        }


        [TestMethod]
        public void TestNumber_Python() {
            const double eps = 0.0001;
            var parser = TestHelper.CreateParser(TerminalFactory.CreatePythonNumber("Number"));

            //Simple integers and suffixes
            var token = parser.ParseInput("123 ");
            CheckType(token, typeof(int));
            Assert.IsTrue(token.Details != null, "ScanDetails object not found in token.");
            Assert.IsTrue((int)token.Value == 123, "Failed to read int value");

            token = parser.ParseInput("123L ");
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 123, "Failed to read long value");

            //Hex representation
            token = parser.ParseInput("0x012 ");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 0x012, "Failed to read hex int value");

            token = parser.ParseInput("0x012l "); //with small "L"
            CheckType(token, typeof(long));
            Assert.IsTrue((long)token.Value == 0x012, "Failed to read hex long value");

            //Floating point types
            token = parser.ParseInput("123.4 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #1");

            token = parser.ParseInput("1234e-1 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 1234e-1) < eps, "Failed to read double value #2");

            token = parser.ParseInput("12.34e+01 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #3");

            token = parser.ParseInput("0.1234E3 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #4");

            token = parser.ParseInput(".1234 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 0.1234) < eps, "Failed to read double value with leading dot");

            token = parser.ParseInput("123. ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.0) < eps, "Failed to read double value with trailing dot");

            //Big integer
            var sbig = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"; //100 digits
            token = parser.ParseInput(sbig);
            Assert.IsTrue(token.Value.ToString() == sbig, "Failed to read big integer value");

            //Quick parse
            token = parser.ParseInput("1 ");
            CheckType(token, typeof(int));
            Assert.IsTrue(token.Details == null, "Quick parse test failed: ScanDetails object is found in token - quick parse path should produce this object.");
            Assert.IsTrue((int)token.Value == 1, "Failed to read quick-parse value");
        }

        [TestMethod]
        public void TestNumber_Scheme() {
            const double eps = 0.0001;
            var parser = TestHelper.CreateParser(TerminalFactory.CreateSchemeNumber("Number"));

            //Just test default float value (double), and exp symbols (e->double, s->single, d -> double)
            var token = parser.ParseInput("123.4 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value #1");

            token = parser.ParseInput("1234e-1 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 1234e-1) < eps, "Failed to read single value #2");

            token = parser.ParseInput("1234s-1 ");
            CheckType(token, typeof(float));
            Assert.IsTrue(Math.Abs((float)token.Value - 1234e-1) < eps, "Failed to read single value #3");

            token = parser.ParseInput("12.34d+01 ");
            CheckType(token, typeof(double));
            Assert.IsTrue(Math.Abs((double)token.Value - 123.4) < eps, "Failed to read double value  #4");
        }

        [TestMethod]
        public void TestNumber_WithUnderscore() {
            var number = new NumberLiteral("number", NumberOptions.AllowUnderscore);
            var parser = TestHelper.CreateParser(number);

            //Simple integers and suffixes
            var token = parser.ParseInput("1_234_567");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == 1234567, "Failed to read int value with underscores.");
        }


        //There was a bug discovered in NumberLiteral - it cannot parse appropriately the int.MinValue value.
        // This test ensures that the issue is fixed.
        [TestMethod]
        public void TestNumber_MinMaxValues() {
            var number = new NumberLiteral("number", NumberOptions.AllowSign);
            number.DefaultIntTypes.AddRange(TypeCode.Int32);
            var parser = TestHelper.CreateParser(number);
            var s = int.MinValue.ToString();
            var token = parser.ParseInput(s);
            Assert.IsFalse(token.IsError, "Failed to scan int.MinValue, scanner returned an error.");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == int.MinValue, "Failed to scan int.MinValue, scanned value does not match.");
            s = int.MaxValue.ToString();
            token = parser.ParseInput(s);
            Assert.IsFalse(token.IsError, "Failed to scan int.MaxValue, scanner returned an error.");
            CheckType(token, typeof(int));
            Assert.IsTrue((int)token.Value == int.MaxValue, "Failed to read int.MaxValue");
        }

        private static void CheckType(Token token, Type type) {
            Assert.IsNotNull(token, "TryMatch returned null, while token was expected.");
            var vtype = token.Value.GetType();
            Assert.IsTrue(vtype == type, "Invalid target type, expected " + type + ", found:  " + vtype);
        }

    }

}