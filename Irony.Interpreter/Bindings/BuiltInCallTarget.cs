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
using Irony.Interpreter.Ast;

namespace Irony.Interpreter {

    //A wrapper to convert BuiltInMethod delegate (referencing some custom method in LanguageRuntime) into an ICallTarget instance (expected by FunctionCallNode)
    public sealed class BuiltInCallTarget : ICallTarget {

        public BuiltInCallTarget(BuiltInMethod method, string name, int minParamCount = 0, int maxParamCount = 0, string parameterNames = null) {
            Method = method;
            Name = name;
            MinParamCount = minParamCount;
            MaxParamCount = Math.Max(MinParamCount, maxParamCount);
            if (!string.IsNullOrEmpty(parameterNames)) {
                ParameterNames = parameterNames.Split(',');
            }
        }

        public string Name { get; }

        public BuiltInMethod Method { get; }

        public int MinParamCount { get; }

        public int MaxParamCount { get; }

        //Just for information purpose
        public string[] ParameterNames { get; }

        #region ICallTarget Members
        public object Call(ScriptThread thread, object[] parameters) {
            return Method(thread, parameters);
        }
        #endregion
    }

}
