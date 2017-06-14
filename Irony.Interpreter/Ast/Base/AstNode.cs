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
using System.Linq.Expressions;
using Irony.Ast;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {

    //Base AST node class
    public class AstNode : IInitializableAstNode, IBrowsableAstNode, IVisitableNode {

        // Public default constructor
        // Note: its subtypes must have a public default constructor, for AstBuilder.CompileDefaultNodeCreator() to invoke.
        public AstNode() {
            Evaluate = DoEvaluate;
            SetValue = DoSetValue;
        }

        public AstNode Parent { get; internal set; }

        public BnfTerm Term { get; internal set; }

        public SourceSpan Span { get; private set; }

        public AstNodeFlags Flags { get; internal set; }

        protected ExpressionType ExpressionType { get; set; } = CustomExpressionTypes.NotAnExpression;

        protected readonly object LockObject = new object();

        //Used for pointing to error location. For most nodes it would be the location of the node itself.
        // One exception is BinExprNode: when we get "Division by zero" error evaluating 
        //  x = (5 + 3) / (2 - 2)
        // it is better to point to "/" as error location, rather than the first "(" - which is the start 
        // location of binary expression. 
        public SourceLocation ErrorAnchor { get; internal set; }

        //UseType is set by parent
        public NodeUseType UseType { get; internal set; } = NodeUseType.Unknown;

        // Role is a free-form string used as prefix in ToString() representation of the node. 
        // Node's parent can set it to "property name" or role of the child node in parent's node currentFrame.Context. 
        public string Role { get; private set; }

        // Default AstNode.ToString() returns 'Role: AsString', which is used for showing node in AST tree. 
        public string AsString { get; protected set; }

        public AstNodeList ChildNodes { get; } = new AstNodeList();  //List of child nodes

        //Reference to Evaluate method implementation. Initially set to DoEvaluate virtual method. 
        public EvaluateMethod Evaluate { get; internal set; }

        public ValueSetterMethod SetValue { get; }

        public SourceLocation Location => Span.Location;

        #region IAstNodeInit Members
        public virtual void Initialize(AstContext context, ParseTreeNode treeNode) {
            Term = treeNode.Term;
            Span = treeNode.Span;
            ErrorAnchor = Location;
            treeNode.AstNode = this;
            AsString = (Term == null ? GetType().Name : Term.Name);
        }
        #endregion

        //ModuleNode - computed on demand
        public AstNode ModuleNode {
            get => _moduleNode ?? (_moduleNode = (Parent == null) ? this : Parent.ModuleNode);
            internal set => _moduleNode = value;
        }

        #region virtual methods: DoEvaluate, SetValue, IsConstant, SetIsTail, GetDependentScopeInfo
        public virtual void Reset() {
            _moduleNode = null;
            Evaluate = DoEvaluate;
            foreach (var child in ChildNodes) {
                child.Reset();
            }
        }

        //By default the Evaluate field points to this method.
        protected virtual object DoEvaluate(ScriptThread thread) {
            //These 2 lines are standard prolog/epilog statements. Place them in every Evaluate and SetValue implementations.
            thread.CurrentNode = this;  //standard prolog
            thread.CurrentNode = Parent; //standard epilog
            return null;
        }

        public virtual void DoSetValue(ScriptThread thread, object value) {
            //Place the prolog/epilog lines in every implementation of SetValue method (see DoEvaluate above)
        }

        public virtual bool IsConstant => false;

        /// <summary>
        /// Sets a flag indicating that the node is in tail position. The value is propagated from parent to children. 
        /// Should propagate this call to appropriate children.
        /// </summary>
        public virtual void SetIsTail() {
            Flags |= AstNodeFlags.IsTail;
        }

        /// <summary>
        /// Dependent scope is a scope produced by the node. For ex, FunctionDefNode defines a scope
        /// </summary>
        public virtual ScopeInfo DependentScopeInfo {
            get => _dependentScope;
            set => _dependentScope = value;
        }
        #endregion

        #region IBrowsableAstNode Members
        public virtual IEnumerable<IBrowsableAstNode> GetChildNodes() {
            return ChildNodes;
        }

        public int Position => Span.Location.Position;
        #endregion

        #region Visitors, Iterators
        //the first primitive Visitor facility
        public virtual void AcceptVisitor(IAstVisitor visitor) {
            visitor.BeginVisit(this);
            if (ChildNodes.Count > 0) {
                foreach (AstNode node in ChildNodes) {
                    node.AcceptVisitor(visitor);
                }
            }

            visitor.EndVisit(this);
        }

        //Node traversal 
        public IEnumerable<AstNode> GetAll() {
            AstNodeList result = new AstNodeList();
            AddAll(result);
            return result;
        }
        private void AddAll(AstNodeList list) {
            list.Add(this);
            foreach (AstNode child in ChildNodes) {
                if (child != null) {
                    child.AddAll(list);
                }
            }
        }
        #endregion

        #region overrides: ToString
        public override string ToString() {
            return string.IsNullOrEmpty(Role) ? AsString : Role + ": " + AsString;
        }
        #endregion

        #region Utility methods: AddChild, HandleError
        protected AstNode AddChild(string role, ParseTreeNode childParseNode) {
            return AddChild(NodeUseType.Unknown, role, childParseNode);
        }

        protected AstNode AddChild(NodeUseType useType, string role, ParseTreeNode childParseNode) {
            //put a stub to throw an exception with clear message on attempt to evaluate. 
            var child = (AstNode)childParseNode.AstNode ?? new NullNode(childParseNode.Term);

            child.Role = role;
            child.Parent = this;
            ChildNodes.Add(child);
            return child;
        }
        #endregion

        protected ScopeInfo _dependentScope;

        private AstNode _moduleNode;

    }

}
