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

using Irony.Utilities;

namespace Irony.Parsing.ParserActions {

    /// <summary>
    /// Base class for more specific reduce actions. 
    /// </summary>
    public class ReduceParserAction : ParserAction {

        public ReduceParserAction(Production production) {
            Production = production;
        }

        public Production Production { get; }

        /// <summary>Factory method for creating a proper type of reduce parser action. </summary>
        /// <param name="production">A Production to reduce.</param>
        /// <returns>Reduce action.</returns>
        public static ReduceParserAction Create(Production production) {
            var nonTerm = production.LValue;
            //List builder (non-empty production for list non-terminal) is a special case 
            var isList = nonTerm.Flags.IsSet(TermFlags.IsList);
            var isListBuilderProduction = isList && production.RValues.Count > 0 && production.RValues[0] == production.LValue;
            if (isListBuilderProduction) {
                return new ReduceListBuilderParserAction(production);
            } else if (nonTerm.Flags.IsSet(TermFlags.IsListContainer)) {
                return new ReduceListContainerParserAction(production);
            } else if (nonTerm.Flags.IsSet(TermFlags.IsTransient)) {
                return new ReduceTransientParserAction(production);
            } else {
                return new ReduceParserAction(production);
            }
        }

        public override void Execute(ParsingContext context) {
            var savedParserInput = context.CurrentParserInput;
            context.CurrentParserInput = GetResultNode(context);
            CompleteReduce(context);
            context.CurrentParserInput = savedParserInput;
        }

        public override string ToString() {
            return string.Format(Resources.LabelActionReduce, Production.ToStringQuoted());
        }

        protected virtual ParseTreeNode GetResultNode(ParsingContext context) {
            var childCount = Production.RValues.Count;
            var firstChildIndex = context.ParserStack.Count - childCount;
            var span = context.ComputeStackRangeSpan(childCount);
            var newNode = new ParseTreeNode(Production.LValue, span);
            for (var i = 0; i < childCount; i++) {
                var childNode = context.ParserStack[firstChildIndex + i];
                if (childNode.IsPunctuationOrEmptyTransient) {
                    // Skip punctuation or empty transient nodes.
                    continue;
                }
                newNode.ChildNodes.Add(childNode);
            }//for i
            return newNode;
        }
        //Completes reduce: pops child nodes from the stack and pushes result node into the stack
        private void CompleteReduce(ParsingContext context) {
            var resultNode = context.CurrentParserInput;
            var childCount = Production.RValues.Count;
            //Pop stack
            context.ParserStack.Pop(childCount);
            //Copy comment block from first child; if comments precede child node, they precede the parent as well. 
            if (resultNode.ChildNodes.Count > 0) {
                resultNode.Comments = resultNode.ChildNodes[0].Comments;
            }
            //Inherit precedence and associativity, to cover a standard case: BinOp->+|-|*|/; 
            // BinOp node should inherit precedence from underlying operator symbol. 
            //TODO: this special case will be handled differently. A ToTerm method should be expanded to allow "combined" terms like "NOT LIKE".
            // OLD COMMENT: A special case is SQL operator "NOT LIKE" which consists of 2 tokens. We therefore inherit "max" precedence from any children
            if (Production.LValue.Flags.IsSet(TermFlags.InheritPrecedence)) {
                InheritPrecedence(resultNode);
            }
            //Push new node into stack and move to new state
            //First read the state from top of the stack 
            context.CurrentParserState = context.ParserStack.Top.State;
            if (context.TracingEnabled) {
                context.AddTrace(Resources.MsgTracePoppedState, Production.LValue.Name);
            }
            #region comments on special case
            //Special case: if a non-terminal is Transient (ex: BinOp), then result node is not this NonTerminal, but its its child (ex: symbol). 
            // Shift action will invoke OnShifting on actual term being shifted (symbol); we need to invoke Shifting even on NonTerminal itself
            // - this would be more expected behavior in general. ImpliedPrecHint relies on this
            #endregion
            if (resultNode.Term != Production.LValue) {
                //special case
                Production.LValue.OnShifting(context.SharedParsingEventArgs);
            }
            // Shift to new state - execute shift over the non-terminal of the production. 
            var shift = context.CurrentParserState.Actions[Production.LValue];
            // Execute shift to new state
            shift.Execute(context);
            //Invoke Reduce event
            Production.LValue.OnReduced(context, Production, resultNode);
        }

        //This operation helps in situation when Bin expression is declared as BinExpr.Rule = expr + BinOp + expr; 
        // where BinOp is an OR-combination of operators. 
        // During parsing, when 'expr, BinOp, expr' is on the top of the stack, 
        // and incoming symbol is operator, we need to use precedence rule for deciding on the action. 
        private static void InheritPrecedence(ParseTreeNode node) {
            foreach (var child in node.ChildNodes) {
                if (child.Precedence == BnfTerm.NoPrecedence) {
                    continue;
                }
                node.Precedence = child.Precedence;
                node.Associativity = child.Associativity;
                return;
            }
        }

    }

}
