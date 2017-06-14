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
using System.Reflection.Emit;
using Irony.Parsing;
using Irony.Utilities;

namespace Irony.Ast {

    public class AstBuilder {

        public AstBuilder(AstContext context) {
            Context = context;
        }

        public AstContext Context { get; }

        public virtual void BuildAst(ParseTree parseTree) {
            if (parseTree.Root == null) {
                return;
            }
            Context.Messages = parseTree.ParserMessages;
            if (!Context.Language.AstDataVerified) {
                VerifyLanguageData();
            }
            if (Context.Language.ErrorLevel == GrammarErrorLevel.Error) {
                return;
            }
            BuildAst(parseTree.Root);
        }

        public virtual void VerifyLanguageData() {
            var gd = Context.Language.GrammarData;
            //Collect all terminals and non-terminals
            var terms = new BnfTermSet();
            //SL does not understand co/contravariance, so doing merge one-by-one
            foreach (var t in gd.Terminals) {
                terms.Add(t);
            }
            foreach (var t in gd.NonTerminals) {
                terms.Add(t);
            }
            var missingList = new BnfTermList();
            foreach (var term in terms) {
                if (term is Terminal terminal && terminal.Category != TokenCategory.Content) {
                    continue; //only content terminals
                }
                if (term.Flags.IsSet(TermFlags.NoAstNode)) {
                    continue;
                }
                var config = term.AstConfig;
                if (config.NodeCreator != null || config.DefaultNodeCreator != null) {
                    continue;
                }
                //We must check NodeType
                if (config.NodeType == null) {
                    config.NodeType = GetDefaultNodeType(term);
                }
                if (config.NodeType == null) {
                    missingList.Add(term);
                } else {
                    config.DefaultNodeCreator = CompileDefaultNodeCreator(config.NodeType);
                }
            }
            if (missingList.Count > 0) {
                // AST node type is not specified for term {0}. Either assign Term.AstConfig.NodeType, or specify default type(s) in AstBuilder.  
                Context.AddMessage(ErrorLevel.Error, SourceLocation.Empty, Resources.ErrNodeTypeNotSetOn, string.Join(", ", missingList));
            }
            Context.Language.AstDataVerified = true;
        }

        public virtual void BuildAst(ParseTreeNode parseNode) {
            var term = parseNode.Term;
            if (term.Flags.IsSet(TermFlags.NoAstNode) || parseNode.AstNode != null) {
                return;
            }
            //children first
            var processChildren = !parseNode.Term.Flags.IsSet(TermFlags.AstDelayChildren) && parseNode.ChildNodes.Count > 0;
            if (processChildren) {
                var mappedChildNodes = parseNode.GetMappedChildNodes();
                foreach (var t in mappedChildNodes) {
                    BuildAst(t);
                }
            }
            //create the node
            //We know that either NodeCreator or DefaultNodeCreator is set; VerifyAstData create the DefaultNodeCreator
            var config = term.AstConfig;
            if (config.NodeCreator != null) {
                config.NodeCreator(Context, parseNode);
                // We assume that Node creator method creates node and initializes it, so parser does not need to call 
                // IAstNodeInit.Init() method on node object. But we do call AstNodeCreated custom event on term.
            } else {
                //Invoke the default creator compiled when we verified the data
                parseNode.AstNode = config.DefaultNodeCreator();
                //Initialize node
                if (parseNode.AstNode is IInitializableAstNode iInit) {
                    iInit.Initialize(Context, parseNode);
                }
            }
            //Invoke the event on term
            term.OnAstNodeCreated(parseNode);
        }

        //Contributed by William Horner (wmh)
        private DefaultAstNodeCreator CompileDefaultNodeCreator(Type nodeType) {
            var constructor = nodeType.GetConstructor(Type.EmptyTypes);
            var method = new DynamicMethod("CreateAstNode", nodeType, Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);
            var result = (DefaultAstNodeCreator)method.CreateDelegate(typeof(DefaultAstNodeCreator));
            return result;
        }

        protected virtual Type GetDefaultNodeType(BnfTerm term) {
            if (term is NumberLiteral || term is StringLiteral) {
                return Context.DefaultLiteralNodeType;
            } else if (term is IdentifierTerminal) {
                return Context.DefaultIdentifierNodeType;
            } else {
                return Context.DefaultNodeType;
            }
        }

    }

}
