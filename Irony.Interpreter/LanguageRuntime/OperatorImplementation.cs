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

    ///<summary>
    ///The OperatorImplementation class represents an implementation of an operator for specific argument types.
    ///</summary>
    ///<remarks>
    /// The OperatorImplementation is used for holding implementation for binary operators, unary operators, 
    /// and type converters (special case of unary operators)
    /// it holds 4 method references for binary operators:
    /// converters for both arguments, implementation method and converter for the result.
    /// For unary operators (and type converters) the implementation is in Arg1Converter
    /// operator (arg1 is used); the converter method is stored in Arg1Converter; the target type is in CommonType
    ///</remarks>
    public sealed class OperatorImplementation {

        //Constructor for binary operators
        public OperatorImplementation(OperatorDispatchKey key, Type resultType, BinaryOperatorMethod baseBinaryMethod,
            UnaryOperatorMethod arg1Converter, UnaryOperatorMethod arg2Converter, UnaryOperatorMethod resultConverter) {
            Key = key;
            CommonType = resultType;
            Arg1Converter = arg1Converter;
            Arg2Converter = arg2Converter;
            ResultConverter = resultConverter;
            BaseBinaryMethod = baseBinaryMethod;
            SetupEvaluationMethod();
        }

        //Constructor for unary operators and type converters
        public OperatorImplementation(OperatorDispatchKey key, Type type, UnaryOperatorMethod method) {
            Key = key;
            CommonType = type;
            Arg1Converter = method;
            Arg2Converter = null;
            ResultConverter = null;
            BaseBinaryMethod = null;
        }

        public OperatorDispatchKey Key { get; }

        // The type to which arguments are converted and no-conversion method for this type. 
        public Type CommonType { get; }

        public BinaryOperatorMethod BaseBinaryMethod { get; }

        //converters
        internal UnaryOperatorMethod Arg1Converter { get; }

        internal UnaryOperatorMethod Arg2Converter { get; }

        internal UnaryOperatorMethod ResultConverter { get; }

        //A reference to the actual binary evaluator method - one of EvaluateConvXXX 
        public BinaryOperatorMethod EvaluateBinary { get; private set; }

        // An overflow handler - the implementation to handle arithmetic overflow
        public OperatorImplementation OverflowHandler { get; internal set; }

        // No-box counterpart for implementations with auto-boxed output. If this field <> null, then this is 
        // implementation with auto-boxed output
        public OperatorImplementation NoBoxImplementation { get; internal set; }

        public override string ToString() {
            return "[OpImpl for " + Key + "]";
        }

        public void SetupEvaluationMethod() {
            if (BaseBinaryMethod == null) {
                //special case - it is unary method, the method itself in Arg1Converter; LanguageRuntime.ExecuteUnaryOperator will handle this properly
                return;
            }
            // Binary operator
            if (ResultConverter == null) {
                //without ResultConverter
                if (Arg1Converter == null && Arg2Converter == null) {
                    EvaluateBinary = EvaluateConvNone;
                } else if (Arg1Converter != null && Arg2Converter == null) {
                    EvaluateBinary = EvaluateConvLeft;
                } else if (Arg1Converter == null && Arg2Converter != null) {
                    EvaluateBinary = EvaluateConvRight;
                } else {
                    // if (Arg1Converter != null && arg2Converter != null)
                    EvaluateBinary = EvaluateConvBoth;
                }
            } else {
                //with result converter
                if (Arg1Converter == null && Arg2Converter == null) {
                    EvaluateBinary = EvaluateConvNoneConvResult;
                } else if (Arg1Converter != null && Arg2Converter == null) {
                    EvaluateBinary = EvaluateConvLeftConvResult;
                } else if (Arg1Converter == null && Arg2Converter != null) {
                    EvaluateBinary = EvaluateConvRightConvResult;
                } else {
                    // if (Arg1Converter != null && Arg2Converter != null)
                    EvaluateBinary = EvaluateConvBothConvResult;
                }
            }
        }

        private object EvaluateConvNone(object arg1, object arg2) {
            return BaseBinaryMethod(arg1, arg2);
        }

        private object EvaluateConvLeft(object arg1, object arg2) {
            return BaseBinaryMethod(Arg1Converter(arg1), arg2);
        }

        private object EvaluateConvRight(object arg1, object arg2) {
            return BaseBinaryMethod(arg1, Arg2Converter(arg2));
        }

        private object EvaluateConvBoth(object arg1, object arg2) {
            return BaseBinaryMethod(Arg1Converter(arg1), Arg2Converter(arg2));
        }

        private object EvaluateConvNoneConvResult(object arg1, object arg2) {
            return ResultConverter(BaseBinaryMethod(arg1, arg2));
        }

        private object EvaluateConvLeftConvResult(object arg1, object arg2) {
            return ResultConverter(BaseBinaryMethod(Arg1Converter(arg1), arg2));
        }

        private object EvaluateConvRightConvResult(object arg1, object arg2) {
            return ResultConverter(BaseBinaryMethod(arg1, Arg2Converter(arg2)));
        }

        private object EvaluateConvBothConvResult(object arg1, object arg2) {
            return ResultConverter(BaseBinaryMethod(Arg1Converter(arg1), Arg2Converter(arg2)));
        }

    }

}
