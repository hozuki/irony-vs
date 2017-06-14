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

    public sealed class BinaryOperationNode : AstNode {

        public AstNode Left { get; private set; }

        public AstNode Right { get; private set; }

        public string OpSymbol { get; private set; }

        public ExpressionType Op { get; private set; }

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            Left = AddChild("Arg", nodes[0]);
            Right = AddChild("Arg", nodes[2]);
            var opToken = nodes[1].FindToken();
            OpSymbol = opToken.Text;
            var ictxt = context as InterpreterAstContext;
            Op = ictxt.OperatorHandler.GetOperatorExpressionType(OpSymbol);
            // Set error anchor to operator, so on error (Division by zero) the explorer will point to 
            // operator node as location, not to the very beginning of the first operand.
            ErrorAnchor = opToken.Location;
            AsString = Op + "(operator)";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            //assign implementation method
            switch (Op) {
                case ExpressionType.AndAlso:
                    Evaluate = EvaluateAndAlso;
                    break;
                case ExpressionType.OrElse:
                    Evaluate = EvaluateOrElse;
                    break;
                default:
                    Evaluate = DefaultEvaluateImplementation;
                    break;
            }
            // actually evaluate and get the result.
            var result = Evaluate(thread);
            // Check if result is constant - if yes, save the value and switch to method that directly returns the result.
            if (IsConstant) {
                _constValue = result;
                AsString = Op + "(operator) Const=" + _constValue;
                Evaluate = EvaluateConst;
            }
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        private object EvaluateAndAlso(ScriptThread thread) {
            var leftValue = Left.Evaluate(thread);
            return !thread.Runtime.IsTrue(leftValue) ? leftValue : Right.Evaluate(thread);
        }

        private object EvaluateOrElse(ScriptThread thread) {
            var leftValue = Left.Evaluate(thread);
            return thread.Runtime.IsTrue(leftValue) ? leftValue : Right.Evaluate(thread);
        }

        private object EvaluateFast(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var arg1 = Left.Evaluate(thread);
            var arg2 = Right.Evaluate(thread);
            //If we have _lastUsed, go straight for it; if types mismatch it will throw
            if (_lastUsed != null) {
                try {
                    var res = _lastUsed.EvaluateBinary(arg1, arg2);
                    thread.CurrentNode = Parent; //standard epilog
                    return res;
                } catch {
                    _lastUsed = null;
                    _failureCount++;
                    // if failed 3 times, change to method without direct try
                    if (_failureCount > 3) {
                        Evaluate = DefaultEvaluateImplementation;
                    }
                }
            }
            // go for normal evaluation
            var result = thread.Runtime.ExecuteBinaryOperator(Op, arg1, arg2, ref _lastUsed);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        private object DefaultEvaluateImplementation(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var arg1 = Left.Evaluate(thread);
            var arg2 = Right.Evaluate(thread);
            var result = thread.Runtime.ExecuteBinaryOperator(Op, arg1, arg2, ref _lastUsed);
            thread.CurrentNode = Parent; //standard epilog
            return result;
        }

        private object EvaluateConst(ScriptThread thread) {
            return _constValue;
        }

        public override bool IsConstant {
            get {
                if (_isConstant) {
                    return true;
                }
                _isConstant = Left.IsConstant && Right.IsConstant;
                return _isConstant;
            }
        }

        private bool _isConstant;

        private OperatorImplementation _lastUsed;
        private object _constValue;
        private int _failureCount;
    }

}
