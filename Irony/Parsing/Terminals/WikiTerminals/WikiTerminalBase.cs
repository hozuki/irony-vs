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

namespace Irony.Parsing.WikiTerminals {

    public abstract class WikiTerminalBase : Terminal {

        public WikiTerminalBase(string name, WikiTermType termType, string openTag, string closeTag, string htmlElementName) : base(name) {
            TermType = termType;
            OpenTag = openTag;
            CloseTag = closeTag;
            HtmlElementName = htmlElementName;
            Priority = TerminalPriority.Normal + OpenTag.Length; //longer tags have higher priority
        }

        public WikiTermType TermType { get; }

        public string OpenTag { get; }

        public string CloseTag { get; }

        public string HtmlElementName { get; }

        public string ContainerHtmlElementName { get; internal set; }

        public string OpenHtmlTag { get; private set; }

        public string CloseHtmlTag { get; private set; }

        public string ContainerOpenHtmlTag { get; private set; }

        public string ContainerCloseHtmlTag { get; private set; }

        public override IList<string> GetFirsts() {
            return new[] { OpenTag };
        }

        public override void Initialize(GrammarData grammarData) {
            base.Initialize(grammarData);
            if (!string.IsNullOrEmpty(HtmlElementName)) {
                if (string.IsNullOrEmpty(OpenHtmlTag)) OpenHtmlTag = "<" + HtmlElementName + ">";
                if (string.IsNullOrEmpty(CloseHtmlTag)) CloseHtmlTag = "</" + HtmlElementName + ">";
            }
            if (!string.IsNullOrEmpty(ContainerHtmlElementName)) {
                if (string.IsNullOrEmpty(ContainerOpenHtmlTag)) ContainerOpenHtmlTag = "<" + ContainerHtmlElementName + ">";
                if (string.IsNullOrEmpty(ContainerCloseHtmlTag)) ContainerCloseHtmlTag = "</" + ContainerHtmlElementName + ">";
            }

        }

    }

}
