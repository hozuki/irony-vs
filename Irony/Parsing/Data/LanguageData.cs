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

namespace Irony.Parsing {

    public sealed class LanguageData {

        public LanguageData(Grammar grammar) {
            Grammar = grammar;
            GrammarData = new GrammarData(this);
            ParserData = new ParserData(this);
            ScannerData = new ScannerData(this);
            ConstructAll();
        }

        public Grammar Grammar { get; }

        public GrammarData GrammarData { get; }

        public ParserData ParserData { get; }

        public ScannerData ScannerData { get; }

        public GrammarErrorList Errors { get; } = new GrammarErrorList();

        public GrammarErrorLevel ErrorLevel { get; internal set; } = GrammarErrorLevel.NoError;

        public long ConstructionTime { get; internal set; }

        public bool AstDataVerified { get; internal set; }

        public void ConstructAll() {
            var builder = new LanguageDataBuilder(this);
            builder.Build();
        }

        public bool CanParse => ErrorLevel < GrammarErrorLevel.Error;

    }

}
