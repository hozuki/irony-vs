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

namespace Irony.Ast {
    public class AstNodeConfig {

        public Type NodeType { get; internal set; }

        /// <summary>
        /// Config data passed to AstNode.
        /// </summary>
        public object Data { get; internal set; }

        /// <summary>
        /// A custom method for creating AST nodes.
        /// </summary>
        public AstNodeCreator NodeCreator { get; internal set; }

        /// <summary>
        /// Default method for creating AST nodes; compiled dynamic method, wrapper around "new nodeType();"
        /// </summary>
        public DefaultAstNodeCreator DefaultNodeCreator { get; internal set; }

        // An optional map (selector, filter) of child AST nodes. This facility provides a way to adjust the "map" of child nodes in various languages to 
        // the structure of a standard AST nodes (that can be shared betweeen languages). 
        // ParseTreeNode object has two properties containing list nodes: ChildNodes and MappedChildNodes.
        //  If term.AstPartsMap is null, these two child node lists are identical and contain all child nodes. 
        // If AstParts is not null, then MappedChildNodes will contain child nodes identified by indexes in the map. 
        // For example, if we set  
        //           term.AstPartsMap = new int[] {1, 4, 2}; 
        // then MappedChildNodes will contain 3 child nodes, which are under indexes 1, 4, 2 in ChildNodes list.
        // The mapping is performed in CoreParser.cs, method CheckCreateMappedChildNodeList.
        public int[] PartsMap { get; internal set; }

        public bool CanCreateNode => NodeCreator != null || NodeType != null;

    }
}
