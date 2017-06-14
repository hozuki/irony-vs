#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Irony.Ast;
using Irony.Utilities;
using Complex64 = System.Numerics.Complex;

namespace Irony.Parsing {

    public sealed class NumberLiteral : CompoundTerminalBase {

        //nested helper class
        private sealed class ExponentsTable : Dictionary<char, TypeCode> { }

        #region Public Consts
        //currently using TypeCodes for identifying numeric types
        public const TypeCode TypeCodeBigInt = (TypeCode)30;

        public const TypeCode TypeCodeImaginary = (TypeCode)31;
        #endregion

        #region constructors and initialization
        public NumberLiteral(string name) : this(name, NumberOptions.Default) {
        }

        public NumberLiteral(string name, NumberOptions options, Type astNodeType)
            : this(name, options) {
            AstConfig.NodeType = astNodeType;
        }

        public NumberLiteral(string name, NumberOptions options, AstNodeCreator astNodeCreator)
            : this(name, options) {
            AstConfig.NodeCreator = astNodeCreator;
        }

        public NumberLiteral(string name, NumberOptions options)
            : base(name) {
            Options = options;
            SetFlag(TermFlags.IsLiteral);
        }

        public void AddPrefix(string prefix, NumberOptions options) {
            PrefixFlags.Add(prefix, (short)options);
            Prefixes.Add(prefix);
        }

        public void AddExponentSymbols(string symbols, TypeCode floatType) {
            foreach (var exp in symbols) {
                _exponentsTable[exp] = floatType;
            }
        }
        #endregion

        #region Public fields/properties: ExponentSymbols, Suffixes
        public NumberOptions Options { get; }

        public char DecimalSeparator { get; set; } = '.';

        //Default types are assigned to literals without suffixes; first matching type used
        public OrderedSet<TypeCode> DefaultIntTypes { get; } = new OrderedSet<TypeCode> { TypeCode.Int32 };

        public TypeCode DefaultFloatType { get; set; } = TypeCode.Double;

        private bool IsSet(NumberOptions option) {
            return (Options & option) != 0;
        }
        #endregion

        #region overrides
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            //Default Exponent symbols if table is empty 
            if (_exponentsTable.Count == 0 && !IsSet(NumberOptions.IntOnly)) {
                _exponentsTable['e'] = DefaultFloatType;
                _exponentsTable['E'] = DefaultFloatType;
            }
            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.Literal, TokenColor.Number, TokenTriggers.None);
            }
        }

        public override IList<string> GetFirsts() {
            var result = new StringList();
            result.AddRange(Prefixes);
            //we assume that prefix is always optional, so number can always start with plain digit
            result.AddRange(new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" });
            // Python float numbers can start with a dot
            if (IsSet(NumberOptions.AllowStartEndDot)) {
                result.Add(DecimalSeparator.ToString());
            }

            if (IsSet(NumberOptions.AllowSign)) {
                result.AddRange(new[] { "-", "+" });
            }

            return result;
        }

        //Most numbers in source programs are just one-digit instances of 0, 1, 2, and maybe others until 9
        // so we try to do a quick parse for these, without starting the whole general process
        protected override Token QuickParse(ParsingContext context, ISourceStream source) {
            if (IsSet(NumberOptions.DisableQuickParse)) {
                return null;
            }

            var current = source.PreviewChar;
            //it must be a digit followed by a whitespace or delimiter
            if (!char.IsDigit(current)) {
                return null;
            }

            if (!Grammar.IsWhitespaceOrDelimiter(source.NextPreviewChar)) {
                return null;
            }

            var iValue = current - '0';
            object value;
            switch (DefaultIntTypes[0]) {
                case TypeCode.Int32:
                    value = iValue;
                    break;
                case TypeCode.UInt32:
                    value = (uint)iValue;
                    break;
                case TypeCode.Byte:
                    value = (byte)iValue;
                    break;
                case TypeCode.SByte:
                    value = (sbyte)iValue;
                    break;
                case TypeCode.Int16:
                    value = (short)iValue;
                    break;
                case TypeCode.UInt16:
                    value = (ushort)iValue;
                    break;
                case TypeCode.Int64:
                    value = (long)iValue;
                    break;
                case TypeCode.UInt64:
                    value = (ulong)iValue;
                    break;
                default:
                    return null;
            }
            source.PreviewPosition++;
            return source.CreateToken(OutputTerminal, value);
        }

        protected override void InitDetails(ParsingContext context, CompoundTokenDetails details) {
            base.InitDetails(context, details);
            details.Flags = (short)Options;
        }

        protected override void ReadPrefix(ISourceStream source, CompoundTokenDetails details) {
            //check that is not a  0 followed by dot; 
            //this may happen in Python for number "0.123" - we can mistakenly take "0" as octal prefix
            if (source.PreviewChar == '0' && source.NextPreviewChar == '.') {
                return;
            }

            base.ReadPrefix(source, details);
        }

        protected override bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
            //remember start - it may be different from source.TokenStart, we may have skipped prefix
            var start = source.PreviewPosition;
            var current = source.PreviewChar;
            if (IsSet(NumberOptions.AllowSign) && (current == '-' || current == '+')) {
                details.Sign = current.ToString();
                source.PreviewPosition++;
            }
            //Figure out digits set
            var digits = GetDigits(details);
            var isDecimal = !details.IsSet((short)(NumberOptions.Binary | NumberOptions.Octal | NumberOptions.Hex));
            var allowFloat = !IsSet(NumberOptions.IntOnly);
            var foundDigits = false;

            while (!source.EOF) {
                current = source.PreviewChar;

                //1. If it is a digit, just continue going; the same for '_' if it is allowed
                if (digits.IndexOf(current) >= 0 || IsSet(NumberOptions.AllowUnderscore) && current == '_') {
                    source.PreviewPosition++;
                    foundDigits = true;
                    continue;
                }

                //2. Check if it is a dot in float number
                var isDot = current == DecimalSeparator;
                if (allowFloat && isDot) {
                    //If we had seen already a dot or exponent, don't accept this one;
                    var hasDotOrExp = details.IsSet((short)(NumberFlagsInternal.HasDot | NumberFlagsInternal.HasExp));
                    if (hasDotOrExp) {
                        break; //from while loop
                    }
                    //In python number literals (NumberAllowPointFloat) a point can be the first and last character,
                    //We accept dot only if it is followed by a digit
                    if (digits.IndexOf(source.NextPreviewChar) < 0 && !IsSet(NumberOptions.AllowStartEndDot)) {
                        break; //from while loop
                    }

                    details.Flags |= (int)NumberFlagsInternal.HasDot;
                    source.PreviewPosition++;
                    continue;
                }

                //3. Check if it is int number followed by dot or exp symbol
                var isExpSymbol = (details.ExponentSymbol == null) && _exponentsTable.ContainsKey(current);
                if (!allowFloat && foundDigits && (isDot || isExpSymbol)) {
                    //If no partial float allowed then return false - it is not integer, let float terminal recognize it as float
                    if (IsSet(NumberOptions.NoDotAfterInt)) {
                        return false;
                    }
                    //otherwise break, it is integer and we're done reading digits
                    break;
                }


                //4. Only for decimals - check if it is (the first) exponent symbol
                if (allowFloat && isDecimal && isExpSymbol) {
                    var next = source.NextPreviewChar;
                    var nextIsSign = next == '-' || next == '+';
                    var nextIsDigit = digits.IndexOf(next) >= 0;
                    if (!nextIsSign && !nextIsDigit) {
                        break;  //Exponent should be followed by either sign or digit
                    }
                    //ok, we've got real exponent
                    details.ExponentSymbol = current.ToString(); //remember the exp char
                    details.Flags |= (int)NumberFlagsInternal.HasExp;
                    source.PreviewPosition++;
                    if (nextIsSign) {
                        source.PreviewPosition++; //skip +/- explicitly so we don't have to deal with them on the next iteration
                    }
                    continue;
                }

                //4. It is something else (not digit, not dot or exponent) - we're done
                break; //from while loop
            }
            var end = source.PreviewPosition;
            if (!foundDigits) {
                return false;
            }

            details.Body = source.Text.Substring(start, end - start);
            return true;
        }

        protected internal override void OnValidateToken(ParsingContext context) {
            if (!IsSet(NumberOptions.AllowLetterAfter)) {
                var current = context.Source.PreviewChar;
                if (char.IsLetter(current) || current == '_') {
                    context.CurrentToken = context.CreateErrorToken(Resources.ErrNoLetterAfterNum); // "Number cannot be followed by a letter." 
                }
            }
            base.OnValidateToken(context);
        }

        protected override bool ConvertValue(CompoundTokenDetails details) {
            if (string.IsNullOrEmpty(details.Body)) {
                details.Error = Resources.ErrInvNumber;  // "Invalid number.";
                return false;
            }

            AssignTypeCodes(details);
            //check for underscore
            if (IsSet(NumberOptions.AllowUnderscore) && details.Body.Contains("_")) {
                details.Body = details.Body.Replace("_", string.Empty);
            }

            //Try quick paths
            switch (details.TypeCodes[0]) {
                case TypeCode.Int32:
                    if (QuickConvertToInt32(details)) {
                        return true;
                    }
                    break;
                case TypeCode.Double:
                    if (QuickConvertToDouble(details)) {
                        return true;
                    }
                    break;
            }

            //Go full cycle
            details.Value = null;
            foreach (var typeCode in details.TypeCodes) {
                switch (typeCode) {
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    case TypeCodeImaginary:
                        return ConvertToFloat(typeCode, details);
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        if (details.Value == null) {
                            //if it is not done yet
                            TryConvertToLong(details, typeCode == TypeCode.UInt64); //try to convert to Long/Ulong and place the result into details.Value field;
                        }
                        if (TryCastToIntegerType(typeCode, details)) {
                            //now try to cast the ULong value to the target type 
                            return true;
                        }
                        break;
                    case TypeCodeBigInt:
                        if (ConvertToBigInteger(details)) {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private void AssignTypeCodes(CompoundTokenDetails details) {
            //Type could be assigned when we read suffix; if so, just exit
            if (details.TypeCodes != null) {
                return;
            }
            //Decide on float types
            var hasDot = details.IsSet((short)(NumberFlagsInternal.HasDot));
            var hasExp = details.IsSet((short)(NumberFlagsInternal.HasExp));
            var isFloat = (hasDot || hasExp);
            if (!isFloat) {
                details.TypeCodes = DefaultIntTypes.ToArray();
                return;
            }
            //so we have a float. If we have exponent symbol then use it to select type
            if (hasExp) {
                if (_exponentsTable.TryGetValue(details.ExponentSymbol[0], out TypeCode code)) {
                    details.TypeCodes = new[] { code };
                    return;
                }
            }
            //Finally assign default float type
            details.TypeCodes = new[] { DefaultFloatType };
        }

        #endregion

        #region private utilities
        private bool QuickConvertToInt32(CompoundTokenDetails details) {
            var radix = GetRadix(details);
            if (radix == 10 && details.Body.Length > 10) {
                return false;    //10 digits is maximum for int32; int32.MaxValue = 2 147 483 647
            }

            try {
                //workaround for .Net FX bug: http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=278448
                int iValue;
                if (radix == 10) {
                    iValue = Convert.ToInt32(details.Body, CultureInfo.InvariantCulture);
                } else {
                    iValue = Convert.ToInt32(details.Body, radix);
                }

                details.Value = iValue;
                return true;
            } catch {
                return false;
            }
        }

        private bool QuickConvertToDouble(CompoundTokenDetails details) {
            if (details.IsSet((short)(NumberOptions.Binary | NumberOptions.Octal | NumberOptions.Hex))) {
                return false;
            }
            if (details.IsSet((short)(NumberFlagsInternal.HasExp))) {
                return false;
            }
            if (DecimalSeparator != '.') {
                return false;
            }
            if (!double.TryParse(details.Body, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double dvalue)) {
                return false;
            }
            details.Value = dvalue;
            return true;
        }


        private bool ConvertToFloat(TypeCode typeCode, CompoundTokenDetails details) {
            //only decimal numbers can be fractions
            if (details.IsSet((short)(NumberOptions.Binary | NumberOptions.Octal | NumberOptions.Hex))) {
                details.Error = Resources.ErrInvNumber; //  "Invalid number.";
                return false;
            }
            var body = details.Body;
            //Some languages allow exp symbols other than E. Check if it is the case, and change it to E
            // - otherwise .NET conversion methods may fail
            if (details.IsSet((short)NumberFlagsInternal.HasExp) && details.ExponentSymbol.ToUpper() != "E") {
                body = body.Replace(details.ExponentSymbol, "E");
            }

            //'.' decimal seperator required by invariant culture
            if (details.IsSet((short)NumberFlagsInternal.HasDot) && DecimalSeparator != '.') {
                body = body.Replace(DecimalSeparator, '.');
            }

            switch (typeCode) {
                case TypeCode.Double:
                case TypeCodeImaginary:
                    double dValue;
                    if (!double.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue)) {
                        return false;
                    }

                    if (typeCode == TypeCodeImaginary) {
                        details.Value = new Complex64(0, dValue);
                    } else {
                        details.Value = dValue;
                    }

                    return true;
                case TypeCode.Single:
                    float fValue;
                    if (!float.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out fValue)) {
                        return false;
                    }

                    details.Value = fValue;
                    return true;
                case TypeCode.Decimal:
                    decimal decValue;
                    if (!decimal.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out decValue)) {
                        return false;
                    }

                    details.Value = decValue;
                    return true;
            }
            return false;
        }

        private static bool TryCastToIntegerType(TypeCode typeCode, CompoundTokenDetails details) {
            if (details.Value == null) {
                return false;
            }

            try {
                if (typeCode != TypeCode.UInt64) {
                    details.Value = Convert.ChangeType(details.Value, typeCode, CultureInfo.InvariantCulture);
                }

                return true;
            } catch (Exception) {
                details.Error = string.Format(Resources.ErrCannotConvertValueToType, details.Value, typeCode);
                return false;
            }
        }

        private bool TryConvertToLong(CompoundTokenDetails details, bool useULong) {
            try {
                var radix = GetRadix(details);
                //workaround for .Net FX bug: http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=278448
                if (radix == 10) {
                    if (useULong) {
                        details.Value = Convert.ToUInt64(details.Body, CultureInfo.InvariantCulture);
                    } else {
                        details.Value = Convert.ToInt64(details.Body, CultureInfo.InvariantCulture);
                    }
                } else {
                    if (useULong) {
                        details.Value = Convert.ToUInt64(details.Body, radix);
                    } else {
                        details.Value = Convert.ToInt64(details.Body, radix);
                    }
                }

                return true;
            } catch (OverflowException) {
                details.Error = string.Format(Resources.ErrCannotConvertValueToType, details.Value, TypeCode.Int64);
                return false;
            }
        }

        private bool ConvertToBigInteger(CompoundTokenDetails details) {
            //ignore leading zeros and sign
            details.Body = details.Body.TrimStart('+').TrimStart('-').TrimStart('0');
            if (string.IsNullOrEmpty(details.Body)) {
                details.Body = "0";
            }

            var bodyLength = details.Body.Length;
            var radix = GetRadix(details);
            var wordLength = GetSafeWordLength(details);
            var sectionCount = GetSectionCount(bodyLength, wordLength);
            var numberSections = new ulong[sectionCount]; //big endian

            try {
                var startIndex = details.Body.Length - wordLength;
                for (var sectionIndex = sectionCount - 1; sectionIndex >= 0; sectionIndex--) {
                    if (startIndex < 0) {
                        wordLength += startIndex;
                        startIndex = 0;
                    }
                    //workaround for .Net FX bug: http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=278448
                    if (radix == 10) {
                        numberSections[sectionIndex] = Convert.ToUInt64(details.Body.Substring(startIndex, wordLength));
                    } else {
                        numberSections[sectionIndex] = Convert.ToUInt64(details.Body.Substring(startIndex, wordLength), radix);
                    }

                    startIndex -= wordLength;
                }
            } catch {
                details.Error = Resources.ErrInvNumber;//  "Invalid number.";
                return false;
            }
            //produce big integer
            var safeWordRadix = GetSafeWordRadix(details);
            BigInteger bigIntegerValue = numberSections[0];
            for (var i = 1; i < sectionCount; i++) {
                bigIntegerValue = bigIntegerValue * safeWordRadix + numberSections[i];
            }

            if (details.Sign == "-") {
                bigIntegerValue = -bigIntegerValue;
            }

            details.Value = bigIntegerValue;
            return true;
        }

        private static int GetRadix(CompoundTokenDetails details) {
            if (details.IsSet((short)NumberOptions.Hex)) {
                return 16;
            }
            if (details.IsSet((short)NumberOptions.Octal)) {
                return 8;
            }
            if (details.IsSet((short)NumberOptions.Binary)) {
                return 2;
            }
            return 10;
        }

        private string GetDigits(CompoundTokenDetails details) {
            if (details.IsSet((short)NumberOptions.Hex)) {
                return Strings.HexDigits;
            }

            if (details.IsSet((short)NumberOptions.Octal)) {
                return Strings.OctalDigits;
            }

            if (details.IsSet((short)NumberOptions.Binary)) {
                return Strings.BinaryDigits;
            }

            return Strings.DecimalDigits;
        }

        private static int GetSafeWordLength(CompoundTokenDetails details) {
            if (details.IsSet((short)NumberOptions.Hex)) {
                return 15;
            }

            if (details.IsSet((short)NumberOptions.Octal)) {
                return 21; //maxWordLength 22
            }

            if (details.IsSet((short)NumberOptions.Binary)) {
                return 63;
            }

            return 19; //maxWordLength 20
        }
        private int GetSectionCount(int stringLength, int safeWordLength) {
            var quotient = stringLength / safeWordLength;
            var remainder = stringLength - quotient * safeWordLength;
            return remainder == 0 ? quotient : quotient + 1;
        }

        //radix^safeWordLength
        private static ulong GetSafeWordRadix(CompoundTokenDetails details) {
            if (details.IsSet((short)NumberOptions.Hex)) {
                return 1152921504606846976;
            }

            if (details.IsSet((short)NumberOptions.Octal)) {
                return 9223372036854775808;
            }

            if (details.IsSet((short)NumberOptions.Binary)) {
                return 9223372036854775808;
            }

            return 10000000000000000000;
        }
        private static bool IsIntegerCode(TypeCode code) {
            return (code >= TypeCode.SByte && code <= TypeCode.UInt64);
        }
        #endregion

        private readonly ExponentsTable _exponentsTable = new ExponentsTable();

    }

}
