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
using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Irony.Utilities;

namespace Irony.Interpreter.Ast {

    //  Implements Ruby-like active strings with embedded expressions

    /* Example usage:

          //String literal with embedded expressions  ------------------------------------------------------------------
          var stringLit = new StringLiteral("string", "\"", StringOptions.AllowsAllEscapes | StringOptions.IsTemplate);
          stringLit.AstNodeType = typeof(StringTemplateNode);
          var Expr = new NonTerminal("Expr"); 
          var templateSettings = new StringTemplateSettings(); //by default set to Ruby-style settings 
          templateSettings.ExpressionRoot = Expr; //this defines how to evaluate expressions inside template
          this.SnippetRoots.Add(Expr);
          stringLit.AstNodeConfig = templateSettings;

          //define Expr as an expression non-terminal in your grammar

     */
    public sealed class StringTemplateNode : AstNode {

        #region embedded classes
        private enum SegmentType {
            Text,
            Expression
        }

        private sealed class TemplateSegment {

            public TemplateSegment(string text, AstNode node, int position) {
                Type = node == null ? SegmentType.Text : SegmentType.Expression;
                Text = text;
                ExpressionNode = node;
                Position = position;
            }

            public SegmentType Type { get; }

            public string Text { get; }

            public AstNode ExpressionNode { get; }

            //Position in raw text of the token for error reporting
            public int Position { get; }

        }
        private sealed class SegmentList : List<TemplateSegment> { }
        #endregion

        public override void Initialize(AstContext context, ParseTreeNode treeNode) {
            base.Initialize(context, treeNode);
            _template = treeNode.Token.ValueString;
            _tokenText = treeNode.Token.Text;
            _templateSettings = treeNode.Term.AstConfig.Data as StringTemplateSettings;
            ParseSegments(context);
            AsString = "\"" + _template + "\" (templated string)";
        }

        protected override object DoEvaluate(ScriptThread thread) {
            thread.CurrentNode = this;  //standard prolog
            var value = BuildString(thread);
            thread.CurrentNode = Parent; //standard epilog
            return value;
        }

        private void ParseSegments(AstContext context) {
            var exprParser = new Parser(context.Language, _templateSettings.ExpressionRoot);
            // As we go along the "value text" (that has all escapes done), we track the position in raw token text  in the variable exprPosInTokenText.
            // This position is position in original text in source code, including original escaping sequences and open/close quotes. 
            // It will be passed to segment constructor, and maybe used later to compute the exact position of runtime error when it occurs. 
            int currentPos = 0, exprPosInTokenText = 0;
            while (true) {
                var startTagPos = _template.IndexOf(_templateSettings.StartTag, currentPos, StringComparison.InvariantCulture);
                if (startTagPos < 0) {
                    startTagPos = _template.Length;
                }

                var text = _template.Substring(currentPos, startTagPos - currentPos);
                if (!string.IsNullOrEmpty(text)) {
                    _segments.Add(new TemplateSegment(text, null, 0)); //for text segments position is not used
                }

                if (startTagPos >= _template.Length) {
                    break; //from while
                }

                //We have a real start tag, grab the expression
                currentPos = startTagPos + _templateSettings.StartTag.Length;
                var endTagPos = _template.IndexOf(_templateSettings.EndTag, currentPos, StringComparison.InvariantCulture);
                if (endTagPos < 0) {
                    //"No ending tag '{0}' found in embedded expression."
                    context.AddMessage(ErrorLevel.Error, Location, Resources.ErrNoEndTagInEmbExpr, _templateSettings.EndTag);
                    return;
                }

                var exprText = _template.Substring(currentPos, endTagPos - currentPos);
                if (!string.IsNullOrEmpty(exprText)) {
                    //parse the expression
                    //_expressionParser.context.Reset(); 

                    var exprTree = exprParser.Parse(exprText);
                    if (exprTree.HasErrors) {
                        //we use original search in token text instead of currentPos in template to avoid distortions caused by opening quote and escaped sequences
                        var baseLocation = Location + _tokenText.IndexOf(exprText, StringComparison.InvariantCulture);
                        CopyMessages(exprTree.ParserMessages, context.Messages, baseLocation, Resources.ErrInvalidEmbeddedPrefix);
                        return;
                    }
                    //add the expression segment
                    exprPosInTokenText = _tokenText.IndexOf(_templateSettings.StartTag, exprPosInTokenText, StringComparison.InvariantCulture) + _templateSettings.StartTag.Length;
                    var segmNode = exprTree.Root.AstNode as AstNode;
                    if (segmNode == null) {
                        throw new InvalidCastException();
                    }
                    segmNode.Parent = this; //important to attach the segm node to current Module
                    _segments.Add(new TemplateSegment(null, segmNode, exprPosInTokenText));
                    //advance position beyond the expression
                    exprPosInTokenText += exprText.Length + _templateSettings.EndTag.Length;

                }
                currentPos = endTagPos + _templateSettings.EndTag.Length;
            }
        }

        private static void CopyMessages(LogMessageList fromList, LogMessageList toList, SourceLocation baseLocation, string messagePrefix) {
            toList.AddRange(fromList.Select(other => new LogMessage(other.Level, baseLocation + other.Location, messagePrefix + other.Message, other.ParserState)));
        }


        private object BuildString(ScriptThread thread) {
            var values = new string[_segments.Count];
            for (var i = 0; i < _segments.Count; i++) {
                var segment = _segments[i];
                switch (segment.Type) {
                    case SegmentType.Text:
                        values[i] = segment.Text;
                        break;
                    case SegmentType.Expression:
                        values[i] = EvaluateExpression(thread, segment);
                        break;
                }
            }
            var result = string.Join(string.Empty, values);
            return result;
        }

        private string EvaluateExpression(ScriptThread thread, TemplateSegment segment) {
            try {
                var value = segment.ExpressionNode.Evaluate(thread);
                return value?.ToString() ?? string.Empty;
            } catch {
                //We need to catch here and set current node; ExpressionNode may have reset it, and location would be wrong
                //TODO: fix this - set error location to exact location inside string. 
                thread.CurrentNode = this;
                throw;
            }
        }

        private string _template;

        //used for locating error 
        private string _tokenText;

        //copied from Terminal.AstNodeConfig 
        private StringTemplateSettings _templateSettings;

        private readonly SegmentList _segments = new SegmentList();

    }

}
