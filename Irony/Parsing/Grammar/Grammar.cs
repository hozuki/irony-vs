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
using System.Linq;
using Irony.Ast;
using Irony.Parsing.SpecialActionHints;
using Irony.Utilities;

namespace Irony.Parsing {

    public abstract class Grammar {

        #region constructors
        public Grammar()
            : this(true) {
            // Case sensitive by default.
        }

        public Grammar(bool caseSensitive) {
            _currentGrammar = this;
            CaseSensitive = caseSensitive;
            var ignoreCase = !CaseSensitive;
            var stringComparer = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase);
            KeyTerms = new KeyTermTable(stringComparer);
            //Initialize console attributes
            ConsoleTitle = Resources.MsgDefaultConsoleTitle;
            ConsoleGreeting = string.Format(Resources.MsgDefaultConsoleGreeting, GetType().Name);
            ConsolePrompt = ">";
            ConsolePromptMoreInput = ".";
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets case sensitivity of the grammar. Read-only, true by default. 
        /// Can be set to false only through a parameter to grammar constructor.
        /// </summary>
        public bool CaseSensitive { get; }

        public LanguageFlags LanguageFlags { get; set; } = LanguageFlags.Default;

        public TermReportGroupList TermReportGroups { get; } = new TermReportGroupList();

        //Terminals not present in grammar expressions and not reachable from the Root
        // (Comment terminal is usually one of them)
        // Tokens produced by these terminals will be ignored by parser input. 
        public TerminalSet NonGrammarTerminals { get; } = new TerminalSet();

        /// <summary>
        /// The main root entry for the grammar. 
        /// </summary>
        public NonTerminal Root { get; set; }

        /// <summary>
        /// Alternative roots for parsing code snippets.
        /// </summary>
        public NonTerminalSet SnippetRoots { get; } = new NonTerminalSet();

        // Shown in Grammar info tab
        public string GrammarComments { get; set; }

        public CultureInfo DefaultCulture { get; set; } = CultureInfo.InvariantCulture;

        //Console-related properties, initialized in grammar constructor
        public string ConsoleTitle { get; set; }

        public string ConsoleGreeting { get; set; }

        // Default prompt
        public string ConsolePrompt { get; set; }

        // Prompt to show when more input is expected
        public string ConsolePromptMoreInput { get; set; }
        #endregion

        #region Reserved words handling
        //Reserved words handling 
        public void MarkReservedWords(params string[] reservedWords) {
            foreach (var word in reservedWords) {
                var wdTerm = ToTerm(word);
                wdTerm.SetFlag(TermFlags.IsReservedWord);
            }
        }
        #endregion

        #region Register/Mark methods
        public void RegisterOperators(int precedence, params string[] opSymbols) {
            RegisterOperators(precedence, Associativity.Left, opSymbols);
        }

        public void RegisterOperators(int precedence, Associativity associativity, params string[] opSymbols) {
            foreach (var op in opSymbols) {
                var opSymbol = ToTerm(op);
                opSymbol.SetFlag(TermFlags.IsOperator);
                opSymbol.Precedence = precedence;
                opSymbol.Associativity = associativity;
            }
        }

        public void RegisterOperators(int precedence, params BnfTerm[] opTerms) {
            RegisterOperators(precedence, Associativity.Left, opTerms);
        }
        public void RegisterOperators(int precedence, Associativity associativity, params BnfTerm[] opTerms) {
            foreach (var term in opTerms) {
                term.SetFlag(TermFlags.IsOperator);
                term.Precedence = precedence;
                term.Associativity = associativity;
            }
        }

        public void RegisterBracePair(string openBrace, string closeBrace) {
            var openS = ToTerm(openBrace);
            var closeS = ToTerm(closeBrace);
            openS.SetFlag(TermFlags.IsOpenBrace);
            openS.IsPairFor = closeS;
            closeS.SetFlag(TermFlags.IsCloseBrace);
            closeS.IsPairFor = openS;
        }

        public void MarkPunctuation(params string[] symbols) {
            foreach (var symbol in symbols) {
                var term = ToTerm(symbol);
                term.SetFlag(TermFlags.IsPunctuation | TermFlags.NoAstNode);
            }
        }

        public void MarkPunctuation(params BnfTerm[] terms) {
            foreach (var term in terms)
                term.SetFlag(TermFlags.IsPunctuation | TermFlags.NoAstNode);
        }


        public void MarkTransient(params NonTerminal[] nonTerminals) {
            foreach (var nt in nonTerminals)
                nt.Flags |= TermFlags.IsTransient | TermFlags.NoAstNode;
        }
        //MemberSelect are symbols invoking member list dropdowns in editor; for ex: . (dot), ::
        public void MarkMemberSelect(params string[] symbols) {
            foreach (var symbol in symbols)
                ToTerm(symbol).SetFlag(TermFlags.IsMemberSelect);
        }
        //Sets IsNotReported flag on terminals. As a result the terminal wouldn't appear in expected terminal list
        // in syntax error messages
        public void MarkNotReported(params BnfTerm[] terms) {
            foreach (var term in terms)
                term.SetFlag(TermFlags.IsNotReported);
        }
        public void MarkNotReported(params string[] symbols) {
            foreach (var symbol in symbols)
                ToTerm(symbol).SetFlag(TermFlags.IsNotReported);
        }

        #endregion

        #region Overridable methods for custom grammars
        protected internal virtual void CreateTokenFilters(LanguageData language, TokenFilterList filters) {
        }

        //This method is called if Scanner fails to produce a token; it offers custom method a chance to produce the token    
        protected internal virtual Token TryMatch(ParsingContext context, ISourceStream source) {
            return null;
        }

        //Gives a way to customize parse tree nodes captions in the tree view. 
        protected internal virtual string GetParseNodeCaption(ParseTreeNode node) {
            if (node.IsError) {
                return node.Term.Name + " (Syntax error)";
            }
            if (node.Token != null) {
                return node.Token.ToString();
            }
            if (node.Term == null) {
                // Special case for initial node pushed into the stack at parser start.
                return node.State == null ? string.Empty : "(State " + node.State.Name + ")"; //  Resources.LabelInitialState;
            }

            if (node.Term is NonTerminal ntTerm && !string.IsNullOrEmpty(ntTerm.NodeCaptionTemplate)) {
                return ntTerm.GetNodeCaption(node);
            }

            return node.Term.Name;
        }

        /// <summary>
        /// Override this method to help scanner select a terminal to create token when there are more than one candidates
        /// for an input char. context.CurrentTerminals contains candidate terminals; leave a single terminal in this list
        /// as the one to use.
        /// </summary>
        protected internal virtual void OnScannerSelectTerminal(ParsingContext context) {
        }

        internal void SkipWhitespace(ISourceStream source) {
            SkipWhitespace(source, false);
        }

        /// <summary>Skips whitespace characters in the input stream. </summary>
        /// <remarks>Override this method if your language has non-standard whitespace characters.</remarks>
        /// <param name="source">Source stream.</param>
        /// <param name="usesNewLineOverride">If this is set, new line characters must not be treated as whitespaces.</param>
        protected internal virtual void SkipWhitespace(ISourceStream source, bool usesNewLineOverride) {
            while (!source.EOF) {
                switch (source.PreviewChar) {
                    case ' ':
                    case '\t':
                        break;
                    case '\r':
                    case '\n':
                    case '\v':
                        if (UsesNewLine || usesNewLineOverride) {
                            return; //do not treat as whitespace if language is line-based
                        }
                        break;
                    default:
                        return;
                }
                source.PreviewPosition++;
            }
        }

        /// <summary>Returns true if a character is whitespace or delimiter. Used in quick-scanning versions of some terminals. </summary>
        /// <param name="ch">The character to check.</param>
        /// <returns>True if a character is whitespace or delimiter; otherwise, false.</returns>
        /// <remarks>Does not have to be completely accurate, should recognize most common characters that are special chars by themselves
        /// and may never be part of other multi-character tokens. </remarks>
        internal bool IsWhitespaceOrDelimiter(char ch) {
            return IsWhitespace(ch) || IsDelimiter(ch);
        }

        protected internal virtual bool IsWhitespace(char ch) {
            switch (ch) {
                case '(':
                case ')':
                case ',':
                case ';':
                case '[':
                case ']':
                case '{':
                case '}':
                case (char)0: //EOF
                    return true;
                default:
                    return false;
            }
        }

        protected internal virtual bool IsDelimiter(char ch) {
            switch (ch) {
                case '(':
                case ')':
                case ',':
                case ';':
                case '[':
                case ']':
                case '{':
                case '}':
                case (char)0: //EOF
                    return true;
                default:
                    return false;
            }
        }

        //The method is called after GrammarData is constructed 
        protected internal virtual void OnGrammarDataConstructed(LanguageData language) {
        }

        protected internal virtual void OnLanguageDataConstructed(LanguageData language) {
        }

        //Constructs the error message in situation when parser has no available action for current input.
        // override this method if you want to change this message
        protected virtual string ConstructParserErrorMessage(ParsingContext context, StringSet expectedTerms) {
            if (expectedTerms.Count > 0) {
                return string.Format(Resources.ErrSyntaxErrorExpected, expectedTerms.ToString(", "));
            }
            return Resources.ErrParserUnexpectedInput;
        }

        // Override this method to perform custom error processing
        protected internal virtual void ReportParseError(ParsingContext context) {
            string error;
            if (context.CurrentParserInput.Term == SyntaxError) {
                //scanner error
                error = context.CurrentParserInput.Token.Value as string;
            } else if (context.CurrentParserInput.Term == Indent) {
                error = Resources.ErrUnexpIndent;
            } else if (context.CurrentParserInput.Term == Eof && context.OpenBraces.Count > 0) {
                if (context.OpenBraces.Count > 0) {
                    //report unclosed braces/parenthesis
                    var openBrace = context.OpenBraces.Peek();
                    error = string.Format(Resources.ErrNoClosingBrace, openBrace.Text);
                } else
                    error = Resources.ErrUnexpEof;
            } else {
                var expectedTerms = context.GetExpectedTermSet();
                error = ConstructParserErrorMessage(context, expectedTerms);
            }
            context.AddParserError(error);
        }
        #endregion

        #region MakePlusRule, MakeStarRule methods
        public BnfExpression MakePlusRule(NonTerminal listNonTerminal, BnfTerm listMember) {
            return MakeListRule(listNonTerminal, null, listMember);
        }

        public BnfExpression MakePlusRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember) {
            return MakeListRule(listNonTerminal, delimiter, listMember);
        }

        public BnfExpression MakeStarRule(NonTerminal listNonTerminal, BnfTerm listMember) {
            return MakeListRule(listNonTerminal, null, listMember, TermListOptions.StarList);
        }

        public BnfExpression MakeStarRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember) {
            return MakeListRule(listNonTerminal, delimiter, listMember, TermListOptions.StarList);
        }

        protected BnfExpression MakeListRule(NonTerminal list, BnfTerm delimiter, BnfTerm listMember, TermListOptions options = TermListOptions.PlusList) {
            //If it is a star-list (allows empty), then we first build plus-list
            var isPlusList = !options.IsSet(TermListOptions.AllowEmpty);
            var allowTrailingDelim = options.IsSet(TermListOptions.AllowTrailingDelimiter) && delimiter != null;
            //"plusList" is the list for which we will construct expression - it is either extra plus-list or original list. 
            // In the former case (extra plus-list) we will use it later to construct expression for list
            var plusList = isPlusList ? list : new NonTerminal(listMember.Name + "+");
            plusList.SetFlag(TermFlags.IsList);
            plusList.Rule = plusList;  // rule => list
            if (delimiter != null) {
                plusList.Rule += delimiter;  // rule => list + delim
            }
            if (options.IsSet(TermListOptions.AddPreferShiftHint)) {
                plusList.Rule += PreferShiftHere(); // rule => list + delim + PreferShiftHere()
            }
            plusList.Rule += listMember;          // rule => list + delim + PreferShiftHere() + elem
            plusList.Rule |= listMember;        // rule => list + delim + PreferShiftHere() + elem | elem
            if (isPlusList) {
                // if we build plus list - we're almost done; plusList == list
                // add trailing delimiter if necessary; for star list we'll add it to final expression
                if (allowTrailingDelim)
                    plusList.Rule |= list + delimiter; // rule => list + delim + PreferShiftHere() + elem | elem | list + delim
            } else {
                // Setup list.Rule using plus-list we just created
                list.Rule = Empty | plusList;
                if (allowTrailingDelim) {
                    list.Rule |= plusList + delimiter | delimiter;
                }
                plusList.SetFlag(TermFlags.NoAstNode);
                list.SetFlag(TermFlags.IsListContainer); //indicates that real list is one level lower
            }
            return list.Rule;
        }
        #endregion

        #region Hint utilities
        protected GrammarHint PreferShiftHere() {
            return new PreferredActionHint(PreferredActionType.Shift);
        }

        protected GrammarHint ReduceHere() {
            return new PreferredActionHint(PreferredActionType.Reduce);
        }

        protected TokenPreviewHint ReduceIf(string thisSymbol, params string[] comesBefore) {
            return new TokenPreviewHint(PreferredActionType.Reduce, thisSymbol, comesBefore);
        }

        protected TokenPreviewHint ReduceIf(Terminal thisSymbol, params Terminal[] comesBefore) {
            return new TokenPreviewHint(PreferredActionType.Reduce, thisSymbol, comesBefore);
        }

        protected TokenPreviewHint ShiftIf(string thisSymbol, params string[] comesBefore) {
            return new TokenPreviewHint(PreferredActionType.Shift, thisSymbol, comesBefore);
        }

        protected TokenPreviewHint ShiftIf(Terminal thisSymbol, params Terminal[] comesBefore) {
            return new TokenPreviewHint(PreferredActionType.Shift, thisSymbol, comesBefore);
        }

        protected GrammarHint ImplyPrecedenceHere(int precedence) {
            return ImplyPrecedenceHere(precedence, Associativity.Left);
        }

        protected GrammarHint ImplyPrecedenceHere(int precedence, Associativity associativity) {
            return new ImpliedPrecedenceHint(precedence, associativity);
        }

        protected CustomActionHint CustomActionHere(ExecuteActionMethod executeMethod, PreviewActionMethod previewMethod = null) {
            return new CustomActionHint(executeMethod, previewMethod);
        }
        #endregion

        #region Term report group methods
        /// <summary>
        /// Creates a terminal reporting group, so all terminals in the group will be reported as a single "alias" in syntex error messages like
        /// "Syntax error, expected: [list of terms]"
        /// </summary>
        /// <param name="alias">An alias for all terminals in the group.</param>
        /// <param name="symbols">Symbols to be included into the group.</param>
        protected void AddTermsReportGroup(string alias, params string[] symbols) {
            TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Normal, SymbolsToTerms(symbols)));
        }

        /// <summary>
        /// Creates a terminal reporting group, so all terminals in the group will be reported as a single "alias" in syntex error messages like
        /// "Syntax error, expected: [list of terms]"
        /// </summary>
        /// <param name="alias">An alias for all terminals in the group.</param>
        /// <param name="terminals">Terminals to be included into the group.</param>
        protected void AddTermsReportGroup(string alias, params Terminal[] terminals) {
            TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Normal, terminals));
        }

        /// <summary>
        /// Adds symbols to a group with no-report type, so symbols will not be shown in expected lists in syntax error messages. 
        /// </summary>
        /// <param name="symbols">Symbols to exclude.</param>
        protected void AddToNoReportGroup(params string[] symbols) {
            TermReportGroups.Add(new TermReportGroup(string.Empty, TermReportGroupType.DoNotReport, SymbolsToTerms(symbols)));
        }

        /// <summary>
        /// Adds symbols to a group with no-report type, so symbols will not be shown in expected lists in syntax error messages. 
        /// </summary>
        /// <param name="terminals">Symbols to exclude.</param>
        protected void AddToNoReportGroup(params Terminal[] terminals) {
            TermReportGroups.Add(new TermReportGroup(string.Empty, TermReportGroupType.DoNotReport, terminals));
        }

        /// <summary>
        /// Adds a group and an alias for all operator symbols used in the grammar.
        /// </summary>
        /// <param name="alias">An alias for operator symbols.</param>
        protected void AddOperatorReportGroup(string alias) {
            TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Operator, null)); //operators will be filled later
        }

        private IEnumerable<Terminal> SymbolsToTerms(IEnumerable<string> symbols) {
            var termList = new TerminalList();
            termList.AddRange(symbols.Select(ToTerm));
            return termList;
        }
        #endregion

        #region Standard terminals: EOF, Empty, NewLine, Indent, Dedent
        // Empty object is used to identify optional element: 
        //    term.Rule = term1 | Empty;
        public readonly Terminal Empty = new Terminal("EMPTY");

        public readonly NewLineTerminal NewLine = new NewLineTerminal("LF");

        //set to true automatically by NewLine terminal; prevents treating new-line characters as whitespaces
        public bool UsesNewLine { get; internal set; }

        // The following terminals are used in indent-sensitive languages like Python;
        // they are not produced by scanner but are produced by CodeOutlineFilter after scanning
        public readonly Terminal Indent = new Terminal("INDENT", TokenCategory.Outline, TermFlags.IsNonScanner);

        public readonly Terminal Dedent = new Terminal("DEDENT", TokenCategory.Outline, TermFlags.IsNonScanner);

        //End-of-Statement terminal - used in indentation-sensitive language to signal end-of-statement;
        // it is not always synced with CRLF chars, and CodeOutlineFilter carefully produces Eos tokens
        // (as well as Indent and Dedent) based on line/col information in incoming content tokens.
        public readonly Terminal Eos = new Terminal("EOS", Resources.LabelEosLabel, TokenCategory.Outline, TermFlags.IsNonScanner);

        // Identifies end of file
        // Note: using Eof in grammar rules is optional. Parser automatically adds this symbol 
        // as a lookahead to Root non-terminal
        public readonly Terminal Eof = new Terminal("EOF", TokenCategory.Outline);

        //Artificial terminal to use for injected/replaced tokens that must be ignored by parser. 
        public readonly Terminal Skip = new Terminal("(SKIP)", TokenCategory.Outline, TermFlags.IsNonGrammar);

        //Used as a "line-start" indicator
        public readonly Terminal LineStartTerminal = new Terminal("LINE_START", TokenCategory.Outline);

        //Used for error tokens
        public readonly Terminal SyntaxError = new Terminal("SYNTAX_ERROR", TokenCategory.Error, TermFlags.IsNonScanner);

        public NonTerminal NewLinePlus {
            get {
                if (_newLinePlus != null) {
                    return _newLinePlus;
                }
                _newLinePlus = new NonTerminal("LF+");
                //We do no use MakePlusRule method; we specify the rule explicitly to add PrefereShiftHere call - this solves some unintended shift-reduce conflicts
                // when using NewLinePlus 
                _newLinePlus.Rule = NewLine | _newLinePlus + PreferShiftHere() + NewLine;
                MarkPunctuation(_newLinePlus);
                _newLinePlus.SetFlag(TermFlags.IsList);
                return _newLinePlus;
            }
        }

        public NonTerminal NewLineStar {
            get {
                if (_newLineStar != null) {
                    return _newLineStar;
                }
                _newLineStar = new NonTerminal("LF*");
                MarkPunctuation(_newLineStar);
                _newLineStar.Rule = MakeStarRule(_newLineStar, NewLine);
                return _newLineStar;
            }
        }

        #endregion

        #region KeyTerms (keywords + special symbols)
        public KeyTermTable KeyTerms { get; }

        protected internal KeyTerm ToTerm(string text) {
            return ToTerm(text, text);
        }

        protected KeyTerm ToTerm(string text, string name) {
            if (KeyTerms.TryGetValue(text, out KeyTerm term)) {
                //update name if it was specified now and not before
                if (string.IsNullOrEmpty(term.Name) && !string.IsNullOrEmpty(name)) {
                    term.Name = name;
                }
                return term;
            }
            //create new term
            if (!CaseSensitive) {
                text = text.ToLower(CultureInfo.InvariantCulture);
            }
            string.Intern(text);
            term = new KeyTerm(text, name);
            KeyTerms[text] = term;
            return term;
        }
        #endregion

        #region CurrentGrammar static field
        public static Grammar CurrentGrammar => _currentGrammar;

        internal static void ClearCurrentGrammar() {
            _currentGrammar = null;
        }
        #endregion

        #region AST construction
        public virtual void BuildAst(LanguageData language, ParseTree parseTree) {
            if (!LanguageFlags.IsSet(LanguageFlags.CreateAst)) {
                return;
            }
            var astContext = new AstContext(language);
            var astBuilder = new AstBuilder(astContext);
            astBuilder.BuildAst(parseTree);
        }
        #endregion

        //Static per-thread instance; Grammar constructor sets it to self (this). 
        // This field/property is used by operator overloads (which are static) to access Grammar's predefined terminals like Empty,
        //  and SymbolTerms dictionary to convert string literals to symbol terminals and add them to the SymbolTerms dictionary
        [ThreadStatic]
        private static Grammar _currentGrammar;

        private NonTerminal _newLinePlus;
        private NonTerminal _newLineStar;

    }

}
