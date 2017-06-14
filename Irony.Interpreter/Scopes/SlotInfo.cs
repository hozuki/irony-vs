﻿#region License
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

    /// <summary> Describes a variable. </summary>
    public sealed class SlotInfo {

        internal SlotInfo(ScopeInfo scopeInfo, SlotType type, string name, int index) {
            ScopeInfo = scopeInfo;
            Type = type;
            Name = name;
            Index = index;
        }

        public ScopeInfo ScopeInfo { get; }

        public SlotType Type { get; }

        public string Name { get; }

        public int Index { get; }

        //for module-level slots, indicator that symbol is "exported" and visible by code that imports the module
        public bool IsPublic { get; internal set; } = true;

    }

}
