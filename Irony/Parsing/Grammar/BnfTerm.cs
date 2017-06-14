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
using Irony.Ast;

namespace Irony.Parsing {

    //Basic Backus-Naur Form element. Base class for Terminal, NonTerminal, BnfExpression, GrammarHint
    public abstract class BnfTerm {

        public BnfTerm(string name)
            : this(name, name) {
        }

        public BnfTerm(string name, string errorAlias, Type nodeType)
            : this(name, errorAlias) {
            AstConfig.NodeType = nodeType;
        }

        public BnfTerm(string name, string errorAlias, AstNodeCreator nodeCreator)
            : this(name, errorAlias) {
            AstConfig.NodeCreator = nodeCreator;
        }

        public BnfTerm(string name, string errorAlias) {
            Name = name;
            ErrorAlias = errorAlias;
            _hashCode = (_hashCounter++).GetHashCode();
        }

        public static readonly int NoPrecedence = 0;

        public virtual void Initialize(GrammarData grammarData) {
            _grammarData = grammarData;
        }

        public virtual string GetParseNodeCaption(ParseTreeNode node) {
            return _grammarData != null ? _grammarData.Grammar.GetParseNodeCaption(node) : Name;
        }

        public override string ToString() {
            return Name;
        }

        public override int GetHashCode() {
            return _hashCode;
        }

        #region Properties
        public string Name { get; internal set; }

        //ErrorAlias is used in error reporting, e.g. "Syntax error, expected <list-of-display-names>". 
        public string ErrorAlias { get; internal set; }

        public TermFlags Flags { get; internal set; }

        public int Precedence { get; set; } = NoPrecedence;

        public Associativity Associativity = Associativity.Neutral;

        public Grammar Grammar => _grammarData.Grammar;

        public void SetFlag(TermFlags flag) {
            SetFlag(flag, true);
        }

        public void SetFlag(TermFlags flag, bool value) {
            if (value) {
                Flags |= flag;
            } else {
                Flags &= ~flag;
            }
        }
        #endregion

        #region Events
        public event EventHandler<ParsingEventArgs> Shifting;

        //an event fired after AST node is created.
        public event EventHandler<AstNodeEventArgs> AstNodeCreated;

        protected internal void OnShifting(ParsingEventArgs args) {
            Shifting?.Invoke(this, args);
        }

        protected internal void OnAstNodeCreated(ParseTreeNode parseNode) {
            if (AstNodeCreated == null || parseNode.AstNode == null) {
                return;
            }
            var args = new AstNodeEventArgs(parseNode);
            AstNodeCreated(this, args);
        }

        #endregion

        //We autocreate AST config on first GET;
        public AstNodeConfig AstConfig {
            get => _astConfig ?? (_astConfig = new AstNodeConfig());
            internal set => _astConfig = value;
        }

        public bool HasAstConfig => _astConfig != null;

        #region Kleene operator Q()
        internal BnfExpression Q() {
            if (_q != null) {
                return _q;
            }
            _q = new NonTerminal(Name + "?") {
                Rule = this | Grammar.CurrentGrammar.Empty
            };
            return _q;
        }
        #endregion

        #region Operators: +, |, implicit
        public static BnfExpression operator +(BnfTerm term1, BnfTerm term2) {
            return Op_Plus(term1, term2);
        }

        public static BnfExpression operator +(BnfTerm term1, string symbol2) {
            return Op_Plus(term1, Grammar.CurrentGrammar.ToTerm(symbol2));
        }

        public static BnfExpression operator +(string symbol1, BnfTerm term2) {
            return Op_Plus(Grammar.CurrentGrammar.ToTerm(symbol1), term2);
        }

        //Alternative 
        public static BnfExpression operator |(BnfTerm term1, BnfTerm term2) {
            return Op_Pipe(term1, term2);
        }

        public static BnfExpression operator |(BnfTerm term1, string symbol2) {
            return Op_Pipe(term1, Grammar.CurrentGrammar.ToTerm(symbol2));
        }

        public static BnfExpression operator |(string symbol1, BnfTerm term2) {
            return Op_Pipe(Grammar.CurrentGrammar.ToTerm(symbol1), term2);
        }

        //BNF operations implementation -----------------------
        // Plus/sequence
        private static BnfExpression Op_Plus(BnfTerm term1, BnfTerm term2) {
            //Check term1 and see if we can use it as result, simply adding term2 as operand
            var expr1 = term1 as BnfExpression;
            if (expr1 == null || expr1.Data.Count > 1) {
                //either not expression at all, or Pipe-type expression (count > 1)
                expr1 = new BnfExpression(term1);
            }
            expr1.Data[expr1.Data.Count - 1].Add(term2);
            return expr1;
        }

        //Pipe/Alternative
        //New version proposed by the codeplex user bdaugherty
        private static BnfExpression Op_Pipe(BnfTerm term1, BnfTerm term2) {
            var expr1 = term1 as BnfExpression ?? new BnfExpression(term1);
            var expr2 = term2 as BnfExpression ?? new BnfExpression(term2);
            expr1.Data.AddRange(expr2.Data);
            return expr1;
        }
        #endregion

        private GrammarData _grammarData;

        //Hash code - we use static counter to generate hash codes
        private static int _hashCounter;
        private readonly int _hashCode;

        private AstNodeConfig _astConfig;

        private NonTerminal _q;

    }//class

}
