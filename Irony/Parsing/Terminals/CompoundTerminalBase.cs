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
using Irony.Utilities;

#region About compound terminals
/*
 As  it turns out, many terminal types in real-world languages have 3-part structure: prefix-body-suffix
 The body is essentially the terminal "value", while prefix and suffix are used to specify additional 
 information (options), while not  being a part of the terminal itself. 
 For example:
 1. c# numbers, may have 0x prefix for hex representation, and suffixes specifying 
   the exact data type of the literal (f, l, m, etc)
 2. c# string may have "@" prefix which disables escaping inside the string
 3. c# identifiers may have "@" prefix and escape sequences inside - just like strings
 4. Python string may have "u" and "r" prefixes, "r" working the same way as @ in c# strings
 5. VB string literals may have "c" suffix identifying that the literal is a character, not a string
 6. VB number literals and identifiers may have suffixes identifying data type

 So it seems like all these terminals have the format "prefix-body-suffix". 
 The CompoundTerminalBase base class implements base functionality supporting this multi-part structure.
 The IdentifierTerminal, NumberLiteral and StringLiteral classes inherit from this base class. 
 The methods in TerminalFactory static class demonstrate that with this architecture we can define the whole 
 variety of terminals for c#, Python and VB.NET languages. 
*/
#endregion

namespace Irony.Parsing {

    public abstract class CompoundTerminalBase : Terminal {

        #region Constructors and initialization
        public CompoundTerminalBase(string name)
            : this(name, TermFlags.None) {
        }

        public CompoundTerminalBase(string name, TermFlags flags)
            : base(name) {
            SetFlag(flags);
            Escapes = GetDefaultEscapes();
        }

        protected void AddPrefixFlag(string prefix, short flags) {
            PrefixFlags.Add(prefix, flags);
            Prefixes.Add(prefix);
        }

        public void AddSuffix(string suffix, params TypeCode[] typeCodes) {
            SuffixTypeCodes.Add(suffix, typeCodes);
            Suffixes.Add(suffix);
        }
        #endregion

        #region Nested classes
        protected sealed class ScanFlagTable : Dictionary<string, short> { }

        protected sealed class TypeCodeTable : Dictionary<string, TypeCode[]> { }

        protected sealed class CompoundTokenDetails {

            public string Prefix { get; set; }

            public string Body { get; set; }

            public string Suffix { get; set; }

            public string Sign { get; set; }

            public short Flags { get; set; }  //need to be short, because we need to save it in Scanner state for Vs integration

            public string Error { get; set; }

            public TypeCode[] TypeCodes { get; set; }

            public string ExponentSymbol { get; set; }  //exponent symbol for Number literal

            public string StartSymbol { get; set; }     //string start and end symbols

            public string EndSymbol { get; set; }

            public object Value { get; set; }

            //partial token info, used by VS integration
            public bool PartialOk { get; set; }

            public bool IsPartial { get; set; }

            public bool PartialContinues { get; set; }

            public byte SubTypeIndex { get; set; } //used for string literal kind

            // Token name. For example used in heredoc: <<LINE_END
            public string TokenName { get; set; }

            public string TokenQuote { get; set; }

            //Flags helper method
            public bool IsSet(short flag) {
                return (Flags & flag) != 0;
            }

            public string Text => Prefix + Body + Suffix;

            public override string ToString() => Text;

        }
        #endregion

        #region Properties/Fields
        public char EscapeChar { get; set; } = '\\';

        public EscapeTable Escapes { get; }

        //Case sensitivity for prefixes and suffixes
        public bool CaseSensitivePrefixesSuffixes { get; set; } = false;
        #endregion

        protected ScanFlagTable PrefixFlags { get; } = new ScanFlagTable();

        protected TypeCodeTable SuffixTypeCodes { get; } = new TypeCodeTable();

        protected StringList Prefixes { get; } = new StringList();

        protected StringList Suffixes { get; } = new StringList();

        #region Overrides: Init, TryMatch
        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            //collect all suffixes, prefixes in lists and create sets of first chars for both
            Prefixes.Sort(StringList.LongerFirst);
            Suffixes.Sort(StringList.LongerFirst);

            _prefixesFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);
            _suffixesFirsts = new CharHashSet(CaseSensitivePrefixesSuffixes);
            foreach (var pfx in Prefixes) {
                _prefixesFirsts.Add(pfx[0]);
            }
            foreach (var sfx in Suffixes) {
                _suffixesFirsts.Add(sfx[0]);
            }
        }

        public override IList<string> GetFirsts() {
            return Prefixes;
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source) {
            Token token;
            //Try quick parse first, but only if we're not continuing
            if (context.VsLineScanState.Value == 0) {
                token = QuickParse(context, source);
                if (token != null) {
                    return token;
                }
                source.PreviewPosition = source.Position; //revert the position
            }

            var details = new CompoundTokenDetails();
            InitDetails(context, details);

            if (context.VsLineScanState.Value == 0) {
                ReadPrefix(source, details);
            }

            if (!ReadBody(source, details)) {
                return null;
            }

            if (details.Error != null) {
                return context.CreateErrorToken(details.Error);
            }

            if (details.IsPartial) {
                details.Value = details.Body;
            } else {
                ReadSuffix(source, details);

                if (!ConvertValue(details)) {
                    if (string.IsNullOrEmpty(details.Error)) {
                        details.Error = Resources.ErrInvNumber;
                    }

                    return context.CreateErrorToken(details.Error); // "Failed to convert the value: {0}"
                }
            }
            token = CreateToken(context, source, details);

            if (details.IsPartial) {
                //Save terminal state so we can continue
                context.VsLineScanState.TokenSubType = details.SubTypeIndex;
                context.VsLineScanState.TerminalFlags = details.Flags;
                context.VsLineScanState.TerminalIndex = MultilineIndex;
            } else {
                context.VsLineScanState.Value = 0;
            }

            return token;
        }

        protected virtual Token CreateToken(ParsingContext context, ISourceStream source, CompoundTokenDetails details) {
            var token = source.CreateToken(OutputTerminal, details.Value);
            token.Details = details;
            if (details.IsPartial) {
                token.Flags |= TokenFlags.IsIncomplete;
            }

            return token;
        }

        protected virtual void InitDetails(ParsingContext context, CompoundTokenDetails details) {
            details.PartialOk = (context.Mode == ParseMode.VsLineScan);
            details.PartialContinues = (context.VsLineScanState.Value != 0);
        }

        protected virtual Token QuickParse(ParsingContext context, ISourceStream source) {
            return null;
        }

        protected virtual void ReadPrefix(ISourceStream source, CompoundTokenDetails details) {
            if (!_prefixesFirsts.Contains(source.PreviewChar)) {
                return;
            }

            var comparisonType = CaseSensitivePrefixesSuffixes ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            foreach (var pfx in Prefixes) {
                // Prefixes are usually case insensitive, even if language is case-sensitive. So we cannot use source.MatchSymbol here,
                // we need case-specific comparison
                if (string.Compare(source.Text, source.PreviewPosition, pfx, 0, pfx.Length, comparisonType) != 0) {
                    continue;
                }
                //We found prefix
                details.Prefix = pfx;
                source.PreviewPosition += pfx.Length;
                //Set flag from prefix
                if (!string.IsNullOrEmpty(details.Prefix) && PrefixFlags.TryGetValue(details.Prefix, out short pfxFlags)) {
                    details.Flags |= pfxFlags;
                }

                return;
            }
        }

        protected virtual bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
            return false;
        }

        protected virtual void ReadSuffix(ISourceStream source, CompoundTokenDetails details) {
            if (!_suffixesFirsts.Contains(source.PreviewChar)) {
                return;
            }

            var comparisonType = CaseSensitivePrefixesSuffixes ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            foreach (var sfx in Suffixes) {
                //Suffixes are usually case insensitive, even if language is case-sensitive. So we cannot use source.MatchSymbol here,
                // we need case-specific comparison
                if (string.Compare(source.Text, source.PreviewPosition, sfx, 0, sfx.Length, comparisonType) != 0) {
                    continue;
                }
                //We found suffix
                details.Suffix = sfx;
                source.PreviewPosition += sfx.Length;
                //Set TypeCode from suffix
                if (!string.IsNullOrEmpty(details.Suffix) && SuffixTypeCodes.TryGetValue(details.Suffix, out TypeCode[] codes)) {
                    details.TypeCodes = codes;
                }

                return;
            }
        }

        protected virtual bool ConvertValue(CompoundTokenDetails details) {
            details.Value = details.Body;
            return false;
        }
        #endregion

        #region utils: GetDefaultEscapes
        public static EscapeTable GetDefaultEscapes() {
            var escapes = new EscapeTable();
            escapes.Add('a', '\u0007');
            escapes.Add('b', '\b');
            escapes.Add('t', '\t');
            escapes.Add('n', '\n');
            escapes.Add('v', '\v');
            escapes.Add('f', '\f');
            escapes.Add('r', '\r');
            escapes.Add('"', '"');
            escapes.Add('\'', '\'');
            escapes.Add('\\', '\\');
            escapes.Add(' ', ' ');
            escapes.Add('\n', '\n'); //this is a special escape of the linebreak itself, 
            // when string ends with "\" char and continues on the next line
            return escapes;
        }
        #endregion

        private CharHashSet _prefixesFirsts; //first chars of all prefixes, for fast prefix detection
        private CharHashSet _suffixesFirsts; //first chars of all suffixes, for fast suffix detection

    }

}
