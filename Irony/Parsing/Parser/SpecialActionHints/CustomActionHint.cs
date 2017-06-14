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

namespace Irony.Parsing.SpecialActionHints {

    public class CustomActionHint : GrammarHint {

        public CustomActionHint(ExecuteActionMethod executeMethod)
            : this(executeMethod, null) {
        }

        public CustomActionHint(ExecuteActionMethod executeMethod, PreviewActionMethod previewMethod) {
            _executeMethod = executeMethod;
            _previewMethod = previewMethod;
        }

        public override void Apply(LanguageData language, LRItem owner) {
            //Create custom action and put it into state.Actions table
            var state = owner.State;
            var action = new CustomParserAction(language, state, _executeMethod);
            _previewMethod?.Invoke(action);
            if (!state.BuilderData.IsInadequate) {
                // adequate state, with a single possible action which is DefaultAction
                state.DefaultAction = action;
            } else if (owner.Core.Current != null) {
                //shift action
                state.Actions[owner.Core.Current] = action;
            } else {
                foreach (var lkh in owner.Lookaheads) {
                    state.Actions[lkh] = action;
                }
            }
            //We consider all conflicts handled by the action
            state.BuilderData.Conflicts.Clear();
        }

        private readonly ExecuteActionMethod _executeMethod;
        private readonly PreviewActionMethod _previewMethod;

    }

}
