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

using System.Linq.Expressions;
using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    public sealed class IncDecNode : AstNode {

        public bool IsPostfix { get; private set; }

        public string OpSymbol { get; private set; }

        //corresponding binary operation: + for ++, - for --
        public string BinaryOpSymbol { get; private set; }

        public ExpressionType BinaryOp { get; private set; }

        public AstNode Argument { get; private set; }

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            FindOpAndDetectPostfix(nodes);
            var argIndex = IsPostfix ? 0 : 1;
            Argument = AddChild(NodeUseType.ValueReadWrite, "Arg", nodes[argIndex]);
            BinaryOpSymbol = OpSymbol[0].ToString(); //take a single char out of ++ or --
            var interpContext = (InterpreterAstContext)context;
            BinaryOp = interpContext.OperatorHandler.GetOperatorExpressionType(BinaryOpSymbol);
            AsString = OpSymbol + (IsPostfix ? "(postfix)" : "(prefix)");
        }

        private void FindOpAndDetectPostfix(ParseTreeNodeList mappedNodes) {
            IsPostfix = false; //assume it 
            OpSymbol = mappedNodes[0].FindTokenAndGetText();
            if (OpSymbol == "--" || OpSymbol == "++") {
                return;
            }
            IsPostfix = true;
            OpSymbol = mappedNodes[1].FindTokenAndGetText();
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var oldValue = Argument.Evaluate(thread);
            var newValue = thread.Runtime.ExecuteBinaryOperator(BinaryOp, oldValue, 1, ref _lastUsed);
            Argument.SetValue(thread, newValue);
            var result = IsPostfix ? oldValue : newValue;
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        public override void SetIsTail() {
            base.SetIsTail();
            Argument.SetIsTail();
        }

        private OperatorImplementation _lastUsed;

    }

}
