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

namespace Irony.Interpreter {

    public class SpecialFormBindingInfo : BindingTargetInfo, IBindingSource {

        public SpecialFormBindingInfo(string symbol, SpecialForm form, int minChildCount = 0, int maxChildCount = 0, string childRoles = null)
            : base(symbol, BindingTargetType.SpecialForm) {
            Binding = new ConstantBinding(form, this);
            MinChildCount = minChildCount;
            MaxChildCount = Math.Max(minChildCount, maxChildCount); //if maxParamCount=0 then set it equal to minParamCount
            if (!string.IsNullOrEmpty(childRoles)) {
                ChildRoles = childRoles.Split(',');
                //TODO: add check that paramNames array is in accord with min/max param counts        
            }
        }

        public ConstantBinding Binding { get; }

        public int MinChildCount { get; }

        public int MaxChildCount { get; }

        public string[] ChildRoles { get; }

        #region IBindingSource Members
        public Binding Bind(BindingRequest request) {
            return Binding;
        }
        #endregion

    }


}
