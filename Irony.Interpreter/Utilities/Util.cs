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
using JetBrains.Annotations;

namespace Irony.Interpreter.Utilities {

    internal static class Util {

        [AssertionMethod]
        [StringFormatMethod("messageTemplate")]
        public static void Ensure([AssertionCondition(AssertionConditionType.IS_TRUE)] bool condition, string messageTemplate, params object[] args) {
            if (condition) {
                return;
            }
            throw new Exception(SafeFormat(messageTemplate, args));
        }

        [StringFormatMethod("template")]
        private static string SafeFormat(string template, params object[] args) {
            if (args == null || args.Length == 0) {
                return template;
            }
            try {
                template = string.Format(template, args);
            } catch (Exception ex) {
                template = template + "(message formatting failed: " + ex.Message + " Args: " + string.Join(",", args) + ")";
            }
            return template;
        }

    }

}
