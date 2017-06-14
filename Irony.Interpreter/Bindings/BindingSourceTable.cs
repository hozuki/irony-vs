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

namespace Irony.Interpreter {

    public sealed class BindingSourceTable : Dictionary<string, IBindingSource>, IBindingSource {

        public BindingSourceTable(bool caseSensitive)
            : base(caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase) {
        }

        //IBindingSource Members
        public Binding Bind(BindingRequest request) {
            return TryGetValue(request.Symbol, out IBindingSource target) ? target.Bind(request) : null;
        }

    }

}
