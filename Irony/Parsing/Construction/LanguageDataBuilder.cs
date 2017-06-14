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

using System.Diagnostics;

namespace Irony.Parsing {

    internal class LanguageDataBuilder {

        internal LanguageDataBuilder(LanguageData language) {
            _language = language;
            _grammar = _language.Grammar;
        }

        internal bool Build() {
            var stopwatch = new Stopwatch();
            try {
                if (_grammar.Root == null) {
                    _language.Errors.AddAndThrow(GrammarErrorLevel.Error, null, Resources.ErrRootNotSet);
                }

                stopwatch.Start();
                var gbld = new GrammarDataBuilder(_language);
                gbld.Build();
                //Just in case grammar author wants to customize something...
                _grammar.OnGrammarDataConstructed(_language);
                var sbld = new ScannerDataBuilder(_language);
                sbld.Build();
                var pbld = new ParserDataBuilder(_language);
                pbld.Build();
                Validate();
                //call grammar method, a chance to tweak the automaton
                _grammar.OnLanguageDataConstructed(_language);
                return true;
            } catch (GrammarErrorException) {
                //grammar error should be already added to Language.Errors collection
                return false;
            } finally {
                _language.ErrorLevel = _language.Errors.GetMaxLevel();
                stopwatch.Stop();
                _language.ConstructionTime = stopwatch.ElapsedMilliseconds;
            }
        }

        #region Language Data Validation

        private void Validate() {
        }

        #endregion

        private readonly LanguageData _language;
        private readonly Grammar _grammar;

    }

}
