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
using Irony.Interpreter.Ast;
using JetBrains.Annotations;

namespace Irony.Interpreter {

    /// <summary>Describes all variables (locals and parameters) defined in a scope of a function or module. </summary>
    /// <remarks>ScopeInfo is metadata, it does not contain variable values. The Scope object (described by ScopeInfo) is a container for values.</remarks>
    // Note that all access to SlotTable is done through "lock" operator, so it's thread safe
    public sealed class ScopeInfo {

        public ScopeInfo(AstNode ownerNode, bool caseSensitive) {
            OwnerNode = ownerNode ?? throw new Exception("ScopeInfo owner node may not be null.");
            _slots = new SlotInfoDictionary(caseSensitive);
            Level = Parent?.Level + 1 ?? 0;
            var sLevel = "level=" + Level;
            AsString = OwnerNode == null ? sLevel : OwnerNode.AsString + ", " + sLevel;
        }

        internal readonly object LockObject = new object();

        public int ValuesCount { get; private set; }

        public int ParametersCount { get; private set; }

        //might be null
        [CanBeNull]
        public AstNode OwnerNode { get; }

        // Static/singleton scopes only; for ex,  modules are singletons. Index in App.StaticScopes array  
        public int StaticIndex { get; internal set; } = -1;

        public int Level { get; }

        public string AsString { get; }

        //Experiment: reusable scope instance; see ScriptThread.cs class
        public Scope ScopeInstance { get; set; }

        //Lexical parent
        public ScopeInfo Parent => _parent ?? (_parent = GetParent());

        public ScopeInfo GetParent() {
            if (OwnerNode == null) {
                return null;
            }

            var currentParent = OwnerNode.Parent;
            while (currentParent != null) {
                var result = currentParent.DependentScopeInfo;
                if (result != null) {
                    return result;
                }

                currentParent = currentParent.Parent;
            }
            return null; //should never happen
        }

        #region Slot operations
        public SlotInfo AddSlot(string name, SlotType type) {
            lock (LockObject) {
                var index = type == SlotType.Value ? ValuesCount++ : ParametersCount++;
                var slot = new SlotInfo(this, type, name, index);
                _slots.Add(name, slot);
                return slot;
            }
        }

        //Returns null if slot not found.
        public SlotInfo GetSlot(string name) {
            lock (LockObject) {
                _slots.TryGetValue(name, out SlotInfo slot);
                return slot;
            }
        }

        public IList<SlotInfo> GetSlots() {
            lock (LockObject) {
                return new List<SlotInfo>(_slots.Values);
            }
        }

        public IList<string> GetNames() {
            lock (LockObject) {
                return new List<string>(_slots.Keys);
            }
        }

        public int GetSlotCount() {
            lock (LockObject) {
                return _slots.Count;
            }
        }
        #endregion

        public override string ToString() {
            return AsString;
        }

        private readonly SlotInfoDictionary _slots;
        private ScopeInfo _parent;

    }

}
