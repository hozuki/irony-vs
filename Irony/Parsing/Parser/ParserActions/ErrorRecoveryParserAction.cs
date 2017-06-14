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

namespace Irony.Parsing.ParserActions {

    //TODO: Improve recovery by adding automatic injection of missing tokens.
    // Make sure we ALWAYS have output parse tree, even if it is messed up
    public sealed class ErrorRecoveryParserAction : ParserAction {

        public override void Execute(ParsingContext context) {
            context.Status = ParserStatus.Error;
            var grammar = context.Language.Grammar;
            grammar.ReportParseError(context);
            // Do not recover if we're already at EOF, or if we're in command line mode
            if (context.CurrentParserInput.Term == grammar.Eof || context.Mode == ParseMode.CommandLine) {
                return;
            }
            //Try to recover from error
            context.Status = ParserStatus.Recovering;
            context.AddTrace(Resources.MsgTraceRecovering); // *** RECOVERING - searching for state with error shift *** 
            var recovered = TryRecoverFromError(context);
            if (recovered) {
                context.AddTrace(Resources.MsgTraceRecoverSuccess); //add new trace entry
                context.Status = ParserStatus.Parsing;
            } else {
                context.AddTrace(Resources.MsgTraceRecoverFailed);
                context.Status = ParserStatus.Error;
            }
        }

        private bool TryRecoverFromError(ParsingContext context) {
            var grammar = context.Language.Grammar;
            var parser = context.Parser;

            //1. We need to find a state in the stack that has a shift item based on error production (with error token), 
            // and error terminal is current. This state would have a shift action on error token. 
            var errorShiftAction = FindErrorShiftActionInStack(context);
            if (errorShiftAction == null) {
                //we failed to recover
                return false;
            }
            context.AddTrace(Resources.MsgTraceRecoverFoundState, context.CurrentParserState);

            //2. Shift error token - execute shift action
            context.AddTrace(Resources.MsgTraceRecoverShiftError, errorShiftAction);
            errorShiftAction.Execute(context);

            //4. Now we need to go along error production until the end, shifting tokens that CAN be shifted and ignoring others.
            //   We shift until we can reduce
            context.AddTrace(Resources.MsgTraceRecoverShiftTillEnd);

            while (true) {
                if (context.CurrentParserInput == null) {
                    parser.ReadInput();
                }
                Debug.Assert(context.CurrentParserInput != null);
                if (context.CurrentParserInput.Term == grammar.Eof) {
                    return false;
                }
                //Check if we can reduce
                var nextAction = parser.GetNextAction();
                if (nextAction == null) {
                    parser.ReadInput();
                    continue;
                }
                if (nextAction is ReduceParserAction) {
                    //We are reducing a fragment containing error - this is the end of recovery
                    //Clear all input token queues and buffered input, reset location back to input position token queues; 
                    context.SetSourceLocation(context.CurrentParserInput.Span.Location);

                    //Reduce error production - it creates parent non-terminal that "hides" error inside
                    context.AddTrace(Resources.MsgTraceRecoverReducing);
                    context.AddTrace(Resources.MsgTraceRecoverAction, nextAction);
                    nextAction.Execute(context); //execute reduce
                    return true; //we recovered 
                }
                // If it is not reduce, simply execute it (it is most likely shift)
                context.AddTrace(Resources.MsgTraceRecoverAction, nextAction);
                nextAction.Execute(context); //shift input token
            }
        }

        private static ParserAction FindErrorShiftActionInStack(ParsingContext context) {
            var grammar = context.Language.Grammar;
            while (context.ParserStack.Count >= 1) {
                var actions = context.CurrentParserState.Actions;
                if (actions.TryGetValue(grammar.SyntaxError, out ParserAction errorShiftAction)
                    && errorShiftAction is ShiftParserAction) {
                    return errorShiftAction;
                }
                //pop next state from stack
                if (context.ParserStack.Count == 1) {
                    // Don't pop the initial state.
                    return null;
                }

                context.ParserStack.Pop();
                context.CurrentParserState = context.ParserStack.Top.State;
            }
            return null;
        }

    }

}
