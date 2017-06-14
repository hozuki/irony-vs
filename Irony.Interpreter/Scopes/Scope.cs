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

namespace Irony.Interpreter {

    public sealed class Scope : ScopeBase {

        public Scope(ScopeInfo scopeInfo, Scope caller, Scope creator, object[] parameters) : base(scopeInfo) {
            Caller = caller;
            Creator = creator;
            Parameters = parameters;
        }

        public object[] Parameters { get; }

        public Scope Caller { get; }

        //either caller or closure parent
        public Scope Creator { get; }

        public object GetParameter(int index) {
            return Parameters[index];
        }
        public void SetParameter(int index, object value) {
            Parameters[index] = value;
        }

        // Lexical parent, computed on demand
        public Scope Parent {
            get => _parent ?? (_parent = GetParent());
            set => _parent = value;
        }

        private Scope GetParent() {
            // Walk along creators chain and find a scope with ScopeInfo matching this.ScopeInfo.Parent
            var parentScopeInfo = Info.Parent;
            if (parentScopeInfo == null) {
                return null;
            }

            var current = Creator;
            while (current != null) {
                if (current.Info == parentScopeInfo) {
                    return current;
                }

                current = current.Creator;
            }
            return null;
        }

        //computed on demand
        private Scope _parent;

    }

}
