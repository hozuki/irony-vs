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

using System.Linq;
using Irony.Parsing;

namespace Irony.Ast {
    public static class AstExtensions {

        public static ParseTreeNodeList GetMappedChildNodes(this ParseTreeNode node) {
            var term = node.Term;
            if (!term.HasAstConfig) {
                return node.ChildNodes;
            }

            var map = term.AstConfig.PartsMap;
            //If no map then mapped list is the same as original 
            if (map == null) {
                return node.ChildNodes;
            }

            //Create mapped list
            var result = new ParseTreeNodeList();
            result.AddRange(map.Select(index => node.ChildNodes[index]));
            return result;
        }


    }
}