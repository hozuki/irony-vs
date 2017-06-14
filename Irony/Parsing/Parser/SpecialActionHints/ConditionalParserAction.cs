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
using Irony.Parsing.ParserActions;

namespace Irony.Parsing.SpecialActionHints {

    public class ConditionalParserAction : ParserAction {

        #region Embedded types
        public delegate bool ConditionChecker(ParsingContext context);

        public sealed class ConditionalEntry {

            public ConditionalEntry(ConditionChecker condition, ParserAction action, string description) {
                Condition = condition;
                Action = action;
                Description = description;
            }

            public ConditionChecker Condition { get; }

            public ParserAction Action { get; }

            //for tracing
            public string Description { get; }

            public override string ToString() {
                return Description + "; action: " + Action.ToString();
            }

        }

        public sealed class ConditionalEntryList : List<ConditionalEntry> { }
        #endregion

        public ConditionalEntryList ConditionalEntries { get; } = new ConditionalEntryList();

        public ParserAction DefaultAction { get; internal set; }

        public override void Execute(ParsingContext context) {
            var traceEnabled = context.TracingEnabled;
            if (traceEnabled) context.AddTrace("Conditional Parser Action.");
            foreach (var entry in ConditionalEntries) {
                if (traceEnabled) context.AddTrace("  Checking condition: " + entry.Description);
                if (!entry.Condition(context)) {
                    continue;
                }
                if (traceEnabled) context.AddTrace("  Condition is TRUE, executing action: " + entry.Action.ToString());
                entry.Action.Execute(context);
                return;
            }
            //if no conditions matched, execute default action
            if (DefaultAction == null) {
                context.AddParserError("Fatal parser error: no conditions matched in conditional parser action, and default action is null." +
                                       " State: {0}", context.CurrentParserState.Name);
                context.Parser.RecoverFromError();
                return;
            }
            if (traceEnabled) {
                context.AddTrace("  All conditions failed, executing default action: " + DefaultAction);
            }
            DefaultAction.Execute(context);
        }

    }

}
