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
using Irony.Parsing.ParserActions;

namespace Irony.Parsing.SpecialActionHints {

    // CustomParserAction is in fact action selector: it allows custom Grammar code to select the action to execute from a set of 
    // shift/reduce actions available in this state.
    public class CustomParserAction : ParserAction {

        public CustomParserAction(LanguageData language, ParserState state, ExecuteActionMethod executeRef) {
            Language = language;
            State = state;
            ExecuteRef = executeRef ?? throw new ArgumentNullException(nameof(executeRef));
            Conflicts.UnionWith(state.BuilderData.Conflicts);

            // Create default shift and reduce actions
            foreach (var shiftItem in state.BuilderData.ShiftItems) {
                ShiftActions.Add(new ShiftParserAction(shiftItem));
            }
            foreach (var item in state.BuilderData.ReduceItems) {
                ReduceActions.Add(ReduceParserAction.Create(item.Core.Production));
            }
        }

        public LanguageData Language { get; }

        public ParserState State { get; }

        public ExecuteActionMethod ExecuteRef { get; }

        public TerminalSet Conflicts { get; } = new TerminalSet();

        public List<ShiftParserAction> ShiftActions { get; } = new List<ShiftParserAction>();

        public List<ReduceParserAction> ReduceActions { get; } = new List<ReduceParserAction>();

        public object Tag { get; set; }

        public override void Execute(ParsingContext context) {
            if (context.TracingEnabled) {
                context.AddTrace(Resources.MsgTraceExecCustomAction);
            }
            //States with DefaultAction do NOT read input, so we read it here
            if (context.CurrentParserInput == null) {
                context.Parser.ReadInput();
            }
            // Remember old state and input; if they don't change after custom action - it is error, we may fall into an endless loop
            var oldState = context.CurrentParserState;
            var oldInput = context.CurrentParserInput;
            ExecuteRef(context, this);
            //Prevent from falling into an infinite loop 
            if (context.CurrentParserState == oldState && context.CurrentParserInput == oldInput) {
                context.AddParserError(Resources.MsgErrorCustomActionDidNotAdvance);
                context.Parser.RecoverFromError();
            }
        }

        public override string ToString() {
            return "CustomParserAction";
        }

    }

}
