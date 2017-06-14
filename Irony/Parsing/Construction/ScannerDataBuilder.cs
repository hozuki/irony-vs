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

using System.Collections.Generic;
using Irony.Utilities;

namespace Irony.Parsing {

    internal sealed class ScannerDataBuilder {

        internal ScannerDataBuilder(LanguageData language) {
            _language = language;
            _grammar = _language.Grammar;
            _grammarData = language.GrammarData;
        }

        internal void Build() {
            _data = _language.ScannerData;
            InitMultilineTerminalsList();
            ProcessNonGrammarTerminals();
            BuildTerminalsLookupTable();
        }

        private void InitMultilineTerminalsList() {
            foreach (var terminal in _grammarData.Terminals) {
                if (terminal.Flags.IsSet(TermFlags.IsNonScanner)) {
                    continue;
                }
                if (terminal.Flags.IsSet(TermFlags.IsMultiline)) {
                    _data.MultilineTerminals.Add(terminal);
                    terminal.MultilineIndex = (byte)_data.MultilineTerminals.Count;
                }
            }
        }

        private void ProcessNonGrammarTerminals() {
            foreach (var term in _grammar.NonGrammarTerminals) {
                var firsts = term.GetFirsts();
                if (firsts == null || firsts.Count == 0) {
                    _language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrTerminalHasEmptyPrefix, term.Name);
                    continue;
                }
                AddTerminalToLookup(_data.NonGrammarTerminalsLookup, term, firsts);
            }

            //sort each list
            foreach (var list in _data.NonGrammarTerminalsLookup.Values) {
                if (list.Count > 1) {
                    list.Sort(Terminal.ByPriorityReverse);
                }
            }
        }

        private void BuildTerminalsLookupTable() {
            foreach (var term in _grammarData.Terminals) {
                //Non-grammar terminals are scanned in a separate step, before regular terminals; so we don't include them here
                if (term.Flags.IsSet(TermFlags.IsNonScanner | TermFlags.IsNonGrammar)) {
                    continue;
                }

                var firsts = term.GetFirsts();
                if (firsts == null || firsts.Count == 0) {
                    _grammarData.NoPrefixTerminals.Add(term);
                    continue; //foreach term
                }

                AddTerminalToLookup(_data.TerminalsLookup, term, firsts);
            }

            if (_grammarData.NoPrefixTerminals.Count > 0) {
                //copy them to Scanner data
                _data.NoPrefixTerminals.AddRange(_grammarData.NoPrefixTerminals);
                // Sort in reverse priority order
                _data.NoPrefixTerminals.Sort(Terminal.ByPriorityReverse);
                //Now add Fallback terminals to every list, then sort lists by reverse priority
                // so that terminal with higher priority comes first in the list
                foreach (var list in _data.TerminalsLookup.Values) {
                    foreach (var ft in _data.NoPrefixTerminals) {
                        if (!list.Contains(ft)) {
                            list.Add(ft);
                        }
                    }
                }
            }

            //Finally sort every list in terminals lookup table
            foreach (var list in _data.TerminalsLookup.Values) {
                if (list.Count > 1) {
                    list.Sort(Terminal.ByPriorityReverse);
                }
            }
        }

        private void AddTerminalToLookup(TerminalLookupTable lookup, Terminal term, IEnumerable<string> firsts) {
            foreach (var prefix in firsts) {
                if (string.IsNullOrEmpty(prefix)) {
                    _language.Errors.Add(GrammarErrorLevel.Error, null, Resources.ErrTerminalHasEmptyPrefix, term.Name);
                    continue;
                }
                //Calculate hash key for the prefix
                var firstChar = prefix[0];
                if (_grammar.CaseSensitive) {
                    AddTerminalToLookupByFirstChar(lookup, term, firstChar);
                } else {
                    AddTerminalToLookupByFirstChar(lookup, term, char.ToLower(firstChar));
                    AddTerminalToLookupByFirstChar(lookup, term, char.ToUpper(firstChar));
                }
            }

        }

        private void AddTerminalToLookupByFirstChar(TerminalLookupTable lookup, Terminal term, char firstChar) {
            if (!lookup.TryGetValue(firstChar, out TerminalList currentList)) {
                //if list does not exist yet, create it
                currentList = new TerminalList();
                lookup[firstChar] = currentList;
            }
            //add terminal to the list
            if (!currentList.Contains(term)) {
                currentList.Add(term);
            }
        }

        private readonly LanguageData _language;
        private readonly Grammar _grammar;
        private readonly GrammarData _grammarData;
        private ScannerData _data;

    }

}
