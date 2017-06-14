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
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using Irony.Parsing;
using Irony.Utilities;

namespace Irony.Interpreter {

    //Initialization of Runtime
    public partial class LanguageRuntime {

        // Note: ran some primitive tests, and it appears that use of smart boxing makes it slower 
        //  by about 5-10%; so disabling it for now
        public bool SmartBoxingEnabled { get; set; } = false;

        protected virtual void InitOperatorImplementations() {
            _supportsComplex = this.Language.Grammar.LanguageFlags.IsSet(LanguageFlags.SupportsComplex);
            _supportsBigInt = this.Language.Grammar.LanguageFlags.IsSet(LanguageFlags.SupportsBigInt);
            _supportsRational = this.Language.Grammar.LanguageFlags.IsSet(LanguageFlags.SupportsRational);
            // TODO: add support for Rational
            if (SmartBoxingEnabled) {
                InitBoxes();
            }

            InitTypeConverters();
            InitBinaryOperatorImplementationsForMatchedTypes();
            InitUnaryOperatorImplementations();
            CreateBinaryOperatorImplementationsForMismatchedTypes();
            CreateOverflowHandlers();
        }

        //The value of smart boxing is questionable - so far did not see perf improvements, so currently it is disabled
        private void InitBoxes() {
            for (var i = 0; i < _boxes.Length; i++) {
                _boxes[i] = i - BoxesMiddle;
            }
        }

        #region Utility methods for adding converters and binary implementations
        protected OperatorImplementation AddConverter(Type fromType, Type toType, UnaryOperatorMethod method) {
            var key = new OperatorDispatchKey(ExpressionType.ConvertChecked, fromType, toType);
            var impl = new OperatorImplementation(key, toType, method);
            OperatorImplementations[key] = impl;
            return impl;
        }

        protected OperatorImplementation AddBinaryBoxed(ExpressionType op, Type baseType,
            BinaryOperatorMethod boxedBinaryMethod, BinaryOperatorMethod noBoxMethod) {
            // first create implementation without boxing
            var noBoxImpl = AddBinary(op, baseType, noBoxMethod);
            if (!SmartBoxingEnabled) {
                return noBoxImpl;
            }
            //The boxedImpl will overwrite noBoxImpl in the dictionary
            var boxedImpl = AddBinary(op, baseType, boxedBinaryMethod);
            boxedImpl.NoBoxImplementation = noBoxImpl;
            return boxedImpl;
        }

        protected OperatorImplementation AddBinary(ExpressionType op, Type baseType, BinaryOperatorMethod binaryMethod) {
            return AddBinary(op, baseType, binaryMethod, null);
        }

        protected OperatorImplementation AddBinary(ExpressionType op, Type commonType,
            BinaryOperatorMethod binaryMethod, UnaryOperatorMethod resultConverter) {
            var key = new OperatorDispatchKey(op, commonType, commonType);
            var impl = new OperatorImplementation(key, commonType, binaryMethod, null, null, resultConverter);
            OperatorImplementations[key] = impl;
            return impl;
        }

        protected OperatorImplementation AddUnary(ExpressionType op, Type commonType, UnaryOperatorMethod unaryMethod) {
            var key = new OperatorDispatchKey(op, commonType);
            var impl = new OperatorImplementation(key, commonType, null, unaryMethod, null, null);
            OperatorImplementations[key] = impl;
            return impl;
        }

        #endregion

        #region Initializing type converters
        protected virtual void InitTypeConverters() {
            //->string
            var targetType = typeof(string);
            AddConverter(typeof(char), targetType, ConvertAnyToString);
            AddConverter(typeof(sbyte), targetType, ConvertAnyToString);
            AddConverter(typeof(byte), targetType, ConvertAnyToString);
            AddConverter(typeof(short), targetType, ConvertAnyToString);
            AddConverter(typeof(ushort), targetType, ConvertAnyToString);
            AddConverter(typeof(int), targetType, ConvertAnyToString);
            AddConverter(typeof(uint), targetType, ConvertAnyToString);
            AddConverter(typeof(long), targetType, ConvertAnyToString);
            AddConverter(typeof(ulong), targetType, ConvertAnyToString);
            AddConverter(typeof(float), targetType, ConvertAnyToString);
            if (_supportsBigInt) {
                AddConverter(typeof(BigInteger), targetType, ConvertAnyToString);
            }

            if (_supportsComplex) {
                AddConverter(typeof(Complex), targetType, ConvertAnyToString);
            }

            //->Complex
            if (_supportsComplex) {
                targetType = typeof(Complex);
                AddConverter(typeof(sbyte), targetType, ConvertAnyToComplex);
                AddConverter(typeof(byte), targetType, ConvertAnyToComplex);
                AddConverter(typeof(short), targetType, ConvertAnyToComplex);
                AddConverter(typeof(ushort), targetType, ConvertAnyToComplex);
                AddConverter(typeof(int), targetType, ConvertAnyToComplex);
                AddConverter(typeof(uint), targetType, ConvertAnyToComplex);
                AddConverter(typeof(long), targetType, ConvertAnyToComplex);
                AddConverter(typeof(ulong), targetType, ConvertAnyToComplex);
                AddConverter(typeof(float), targetType, ConvertAnyToComplex);
                if (_supportsBigInt) {
                    AddConverter(typeof(BigInteger), targetType, ConvertBigIntToComplex);
                }
            }
            //->BigInteger
            if (_supportsBigInt) {
                targetType = typeof(BigInteger);
                AddConverter(typeof(sbyte), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(byte), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(short), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(ushort), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(int), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(uint), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(long), targetType, ConvertAnyIntToBigInteger);
                AddConverter(typeof(ulong), targetType, ConvertAnyIntToBigInteger);
            }

            //->Double
            targetType = typeof(double);
            AddConverter(typeof(sbyte), targetType, value => (double)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (double)(byte)value);
            AddConverter(typeof(short), targetType, value => (double)(short)value);
            AddConverter(typeof(ushort), targetType, value => (double)(ushort)value);
            AddConverter(typeof(int), targetType, value => (double)(int)value);
            AddConverter(typeof(uint), targetType, value => (double)(uint)value);
            AddConverter(typeof(long), targetType, value => (double)(long)value);
            AddConverter(typeof(ulong), targetType, value => (double)(ulong)value);
            AddConverter(typeof(float), targetType, value => (double)(float)value);
            if (_supportsBigInt) {
                AddConverter(typeof(BigInteger), targetType, value => ((double)(BigInteger)value));
            }

            //->Single
            targetType = typeof(float);
            AddConverter(typeof(sbyte), targetType, value => (float)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (float)(byte)value);
            AddConverter(typeof(short), targetType, value => (float)(short)value);
            AddConverter(typeof(ushort), targetType, value => (float)(ushort)value);
            AddConverter(typeof(int), targetType, value => (float)(int)value);
            AddConverter(typeof(uint), targetType, value => (float)(uint)value);
            AddConverter(typeof(long), targetType, value => (float)(long)value);
            AddConverter(typeof(ulong), targetType, value => (float)(ulong)value);
            if (_supportsBigInt) {
                AddConverter(typeof(BigInteger), targetType, value => (float)(BigInteger)value);
            }

            //->UInt64
            targetType = typeof(ulong);
            AddConverter(typeof(sbyte), targetType, value => (ulong)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (ulong)(byte)value);
            AddConverter(typeof(short), targetType, value => (ulong)(short)value);
            AddConverter(typeof(ushort), targetType, value => (ulong)(ushort)value);
            AddConverter(typeof(int), targetType, value => (ulong)(int)value);
            AddConverter(typeof(uint), targetType, value => (ulong)(uint)value);
            AddConverter(typeof(long), targetType, value => (ulong)(long)value);

            //->Int64
            targetType = typeof(long);
            AddConverter(typeof(sbyte), targetType, value => (long)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (long)(byte)value);
            AddConverter(typeof(short), targetType, value => (long)(short)value);
            AddConverter(typeof(ushort), targetType, value => (long)(ushort)value);
            AddConverter(typeof(int), targetType, value => (long)(int)value);
            AddConverter(typeof(uint), targetType, value => (long)(uint)value);

            //->UInt32
            targetType = typeof(uint);
            AddConverter(typeof(sbyte), targetType, value => (uint)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (uint)(byte)value);
            AddConverter(typeof(short), targetType, value => (uint)(short)value);
            AddConverter(typeof(ushort), targetType, value => (uint)(ushort)value);
            AddConverter(typeof(int), targetType, value => (uint)(int)value);

            //->Int32
            targetType = typeof(int);
            AddConverter(typeof(sbyte), targetType, value => (int)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (int)(byte)value);
            AddConverter(typeof(short), targetType, value => (int)(short)value);
            AddConverter(typeof(ushort), targetType, value => (int)(ushort)value);

            //->UInt16
            targetType = typeof(ushort);
            AddConverter(typeof(sbyte), targetType, value => (ushort)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (ushort)(byte)value);
            AddConverter(typeof(short), targetType, value => (ushort)(short)value);

            //->Int16
            targetType = typeof(short);
            AddConverter(typeof(sbyte), targetType, value => (short)(sbyte)value);
            AddConverter(typeof(byte), targetType, value => (short)(byte)value);

            //->byte
            targetType = typeof(byte);
            AddConverter(typeof(sbyte), targetType, value => (byte)(sbyte)value);
        }

        // Some specialized convert implementation methods
        public static object ConvertAnyToString(object value) {
            return value?.ToString() ?? string.Empty;
        }

        public static object ConvertBigIntToComplex(object value) {
            var bi = (BigInteger)value;
            return new Complex((double)bi, 0);
        }

        public static object ConvertAnyToComplex(object value) {
            var d = Convert.ToDouble(value);
            return new Complex(d, 0);
        }

        public static object ConvertAnyIntToBigInteger(object value) {
            var l = Convert.ToInt64(value);
            return new BigInteger(l);
        }
        #endregion

        #region Binary operators implementations
        // Generates of binary implementations for matched argument types
        protected virtual void InitBinaryOperatorImplementationsForMatchedTypes() {

            // For each operator, we add a series of implementation methods for same-type operands. They are saved as OperatorImplementation
            // records in OperatorImplementations table. This happens at initialization time. 
            // After this initialization (for same-type operands), system adds implementations for all type pairs (ex: int + double), 
            // using these same-type implementations and appropriate type converters.
            // Note that arithmetics on byte, sbyte, int16, uint16 are performed in Int32 format (the way it's done in c# I guess)
            // so the result is always Int32. We do not define operators for sbyte, byte, int16 and UInt16 types - they will 
            // be processed using Int32 implementation, with appropriate type converters.

            var op = ExpressionType.AddChecked;
            AddBinaryBoxed(op, typeof(int), (x, y) => _boxes[checked((int)x + (int)y) + BoxesMiddle],
                (x, y) => checked((int)x + (int)y));
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x + (uint)y));
            AddBinary(op, typeof(long), (x, y) => checked((long)x + (long)y));
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x + (ulong)y));
            AddBinary(op, typeof(float), (x, y) => (float)x + (float)y);
            AddBinary(op, typeof(double), (x, y) => (double)x + (double)y);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x + (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x + (BigInteger)y);
            }

            if (_supportsComplex) {
                AddBinary(op, typeof(Complex), (x, y) => (Complex)x + (Complex)y);
            }

            AddBinary(op, typeof(string), (x, y) => (string)x + (string)y);
            AddBinary(op, typeof(char), (x, y) => ((char)x).ToString() + (char)y); //force to concatenate as strings

            op = ExpressionType.SubtractChecked;
            AddBinaryBoxed(op, typeof(int), (x, y) => _boxes[checked((int)x - (int)y) + BoxesMiddle],
                (x, y) => checked((int)x - (int)y));
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x - (uint)y));
            AddBinary(op, typeof(long), (x, y) => checked((long)x - (long)y));
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x - (ulong)y));
            AddBinary(op, typeof(float), (x, y) => (float)x - (float)y);
            AddBinary(op, typeof(double), (x, y) => (double)x - (double)y);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x - (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x - (BigInteger)y);
            }

            if (_supportsComplex) {
                AddBinary(op, typeof(Complex), (x, y) => (Complex)x - (Complex)y);
            }

            op = ExpressionType.MultiplyChecked;
            AddBinaryBoxed(op, typeof(int), (x, y) => _boxes[checked((int)x * (int)y) + BoxesMiddle],
                (x, y) => checked((int)x * (int)y));
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x * (uint)y));
            AddBinary(op, typeof(long), (x, y) => checked((long)x * (long)y));
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x * (ulong)y));
            AddBinary(op, typeof(float), (x, y) => (float)x * (float)y);
            AddBinary(op, typeof(double), (x, y) => (double)x * (double)y);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x * (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x * (BigInteger)y);
            }

            if (_supportsComplex) {
                AddBinary(op, typeof(Complex), (x, y) => (Complex)x * (Complex)y);
            }

            op = ExpressionType.Divide;
            AddBinary(op, typeof(int), (x, y) => checked((int)x / (int)y));
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x / (uint)y));
            AddBinary(op, typeof(long), (x, y) => checked((long)x / (long)y));
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x / (ulong)y));
            AddBinary(op, typeof(float), (x, y) => (float)x / (float)y);
            AddBinary(op, typeof(double), (x, y) => (double)x / (double)y);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x / (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x / (BigInteger)y);
            }

            if (_supportsComplex) {
                AddBinary(op, typeof(Complex), (x, y) => (Complex)x / (Complex)y);
            }

            op = ExpressionType.Modulo;
            AddBinary(op, typeof(int), (x, y) => checked((int)x % (int)y));
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x % (uint)y));
            AddBinary(op, typeof(long), (x, y) => checked((long)x % (long)y));
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x % (ulong)y));
            AddBinary(op, typeof(float), (x, y) => (float)x % (float)y);
            AddBinary(op, typeof(double), (x, y) => (double)x % (double)y);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x % (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x % (BigInteger)y);
            }

            // For bitwise operator, we provide explicit implementations for "small" integer types
            op = ExpressionType.And;
            AddBinary(op, typeof(bool), (x, y) => (bool)x & (bool)y);
            AddBinary(op, typeof(sbyte), (x, y) => (sbyte)x & (sbyte)y);
            AddBinary(op, typeof(byte), (x, y) => (byte)x & (byte)y);
            AddBinary(op, typeof(short), (x, y) => (short)x & (short)y);
            AddBinary(op, typeof(ushort), (x, y) => (ushort)x & (ushort)y);
            AddBinary(op, typeof(int), (x, y) => (int)x & (int)y);
            AddBinary(op, typeof(uint), (x, y) => (uint)x & (uint)y);
            AddBinary(op, typeof(long), (x, y) => (long)x & (long)y);
            AddBinary(op, typeof(ulong), (x, y) => (ulong)x & (ulong)y);

            op = ExpressionType.Or;
            AddBinary(op, typeof(bool), (x, y) => (bool)x | (bool)y);
            AddBinary(op, typeof(sbyte), (x, y) => (sbyte)x | (sbyte)y);
            AddBinary(op, typeof(byte), (x, y) => (byte)x | (byte)y);
            AddBinary(op, typeof(short), (x, y) => (short)x | (short)y);
            AddBinary(op, typeof(ushort), (x, y) => (ushort)x | (ushort)y);
            AddBinary(op, typeof(int), (x, y) => (int)x | (int)y);
            AddBinary(op, typeof(uint), (x, y) => (uint)x | (uint)y);
            AddBinary(op, typeof(long), (x, y) => (long)x | (long)y);
            AddBinary(op, typeof(ulong), (x, y) => (ulong)x | (ulong)y);

            op = ExpressionType.ExclusiveOr;
            AddBinary(op, typeof(bool), (x, y) => (bool)x ^ (bool)y);
            AddBinary(op, typeof(sbyte), (x, y) => (sbyte)x ^ (sbyte)y);
            AddBinary(op, typeof(byte), (x, y) => (byte)x ^ (byte)y);
            AddBinary(op, typeof(short), (x, y) => (short)x ^ (short)y);
            AddBinary(op, typeof(ushort), (x, y) => (ushort)x ^ (ushort)y);
            AddBinary(op, typeof(int), (x, y) => (int)x ^ (int)y);
            AddBinary(op, typeof(uint), (x, y) => (uint)x ^ (uint)y);
            AddBinary(op, typeof(long), (x, y) => (long)x ^ (long)y);
            AddBinary(op, typeof(ulong), (x, y) => (ulong)x ^ (ulong)y);

            op = ExpressionType.LessThan;
            AddBinary(op, typeof(int), (x, y) => checked((int)x < (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x < (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x < (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x < (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => (float)x < (float)y, BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => (double)x < (double)y, BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x < (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x < (BigInteger)y, BoolResultConverter);
            }

            op = ExpressionType.GreaterThan;
            AddBinary(op, typeof(int), (x, y) => checked((int)x > (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x > (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x > (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x > (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => (float)x > (float)y, BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => (double)x > (double)y, BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x > (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x > (BigInteger)y, BoolResultConverter);
            }

            op = ExpressionType.LessThanOrEqual;
            AddBinary(op, typeof(int), (x, y) => checked((int)x <= (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x <= (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x <= (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x <= (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => (float)x <= (float)y, BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => (double)x <= (double)y, BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x <= (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x <= (BigInteger)y, BoolResultConverter);
            }

            op = ExpressionType.GreaterThanOrEqual;
            AddBinary(op, typeof(int), (x, y) => checked((int)x >= (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x >= (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x >= (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x >= (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => (float)x >= (float)y, BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => (double)x >= (double)y, BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x >= (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x >= (BigInteger)y, BoolResultConverter);
            }

            op = ExpressionType.Equal;
            AddBinary(op, typeof(int), (x, y) => checked((int)x == (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x == (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x == (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x == (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => ((float)x).Equals((float)y), BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => ((double)x).Equals((double)y), BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x == (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x == (BigInteger)y, BoolResultConverter);
            }

            op = ExpressionType.NotEqual;
            AddBinary(op, typeof(int), (x, y) => checked((int)x != (int)y), BoolResultConverter);
            AddBinary(op, typeof(uint), (x, y) => checked((uint)x != (uint)y), BoolResultConverter);
            AddBinary(op, typeof(long), (x, y) => checked((long)x != (long)y), BoolResultConverter);
            AddBinary(op, typeof(ulong), (x, y) => checked((ulong)x != (ulong)y), BoolResultConverter);
            AddBinary(op, typeof(float), (x, y) => !((float)x).Equals((float)y), BoolResultConverter);
            AddBinary(op, typeof(double), (x, y) => !((double)x).Equals((double)y), BoolResultConverter);
            AddBinary(op, typeof(decimal), (x, y) => (decimal)x != (decimal)y);
            if (_supportsBigInt) {
                AddBinary(op, typeof(BigInteger), (x, y) => (BigInteger)x != (BigInteger)y, BoolResultConverter);
            }
        }

        public virtual void InitUnaryOperatorImplementations() {
            var op = ExpressionType.UnaryPlus;
            AddUnary(op, typeof(sbyte), x => +(sbyte)x);
            AddUnary(op, typeof(byte), x => +(byte)x);
            AddUnary(op, typeof(short), x => +(short)x);
            AddUnary(op, typeof(ushort), x => +(ushort)x);
            AddUnary(op, typeof(int), x => +(int)x);
            AddUnary(op, typeof(uint), x => +(uint)x);
            AddUnary(op, typeof(long), x => +(long)x);
            AddUnary(op, typeof(ulong), x => +(ulong)x);
            AddUnary(op, typeof(float), x => +(float)x);
            AddUnary(op, typeof(double), x => +(double)x);
            AddUnary(op, typeof(decimal), x => +(decimal)x);
            if (_supportsBigInt) {
                AddUnary(op, typeof(BigInteger), x => +(BigInteger)x);
            }

            op = ExpressionType.Negate;
            AddUnary(op, typeof(sbyte), x => -(sbyte)x);
            AddUnary(op, typeof(byte), x => -(byte)x);
            AddUnary(op, typeof(short), x => -(short)x);
            AddUnary(op, typeof(ushort), x => -(ushort)x);
            AddUnary(op, typeof(int), x => -(int)x);
            AddUnary(op, typeof(uint), x => -(uint)x);
            AddUnary(op, typeof(long), x => -(long)x);
            AddUnary(op, typeof(float), x => -(float)x);
            AddUnary(op, typeof(double), x => -(double)x);
            AddUnary(op, typeof(decimal), x => -(decimal)x);
            if (_supportsBigInt) {
                AddUnary(op, typeof(BigInteger), x => -(BigInteger)x);
            }

            if (_supportsComplex) {
                AddUnary(op, typeof(Complex), x => -(Complex)x);
            }

            op = ExpressionType.Not;
            AddUnary(op, typeof(bool), x => !(bool)x);
            AddUnary(op, typeof(sbyte), x => ~(sbyte)x);
            AddUnary(op, typeof(byte), x => ~(byte)x);
            AddUnary(op, typeof(short), x => ~(short)x);
            AddUnary(op, typeof(ushort), x => ~(ushort)x);
            AddUnary(op, typeof(int), x => ~(int)x);
            AddUnary(op, typeof(uint), x => ~(uint)x);
            AddUnary(op, typeof(long), x => ~(long)x);

        }

        // Generates binary implementations for mismatched argument types
        protected virtual void CreateBinaryOperatorImplementationsForMismatchedTypes() {
            // find all data types are there
            var allTypes = new HashSet<Type>();
            var allBinOps = new HashSet<ExpressionType>();
            foreach (var kv in OperatorImplementations) {
                allTypes.Add(kv.Key.Arg1Type);
                if (kv.Value.BaseBinaryMethod != null) {
                    allBinOps.Add(kv.Key.Op);
                }
            }
            foreach (var arg1Type in allTypes) {
                foreach (var arg2Type in allTypes) {
                    if (arg1Type != arg2Type) {
                        foreach (var op in allBinOps) {
                            CreateBinaryOperatorImplementation(op, arg1Type, arg2Type);
                        }
                    }
                }
            }
        }

        // Creates a binary implementations for an operator with mismatched argument types.
        // Determines common type, retrieves implementation for operator with both args of common type, then creates
        // implementation for mismatched types using type converters (by converting to common type)
        protected OperatorImplementation CreateBinaryOperatorImplementation(ExpressionType op, Type arg1Type, Type arg2Type) {
            var commonType = GetCommonTypeForOperator(op, arg1Type, arg2Type);
            if (commonType == null) {
                return null;
            }
            //Get base method for the operator and common type 
            var baseImpl = FindBaseImplementation(op, commonType);
            if (baseImpl == null) { //Try up-type
                commonType = GetUpType(commonType);
                if (commonType == null) {
                    return null;
                }

                baseImpl = FindBaseImplementation(op, commonType);
            }
            if (baseImpl == null) {
                return null;
            }
            //Create implementation and save it in implementations table
            var impl = CreateBinaryOperatorImplementation(op, arg1Type, arg2Type, commonType, baseImpl.BaseBinaryMethod, baseImpl.ResultConverter);
            OperatorImplementations[impl.Key] = impl;
            return impl;
        }

        protected virtual OperatorImplementation CreateBinaryOperatorImplementation(ExpressionType op, Type arg1Type, Type arg2Type,
            Type commonType, BinaryOperatorMethod method, UnaryOperatorMethod resultConverter) {
            var key = new OperatorDispatchKey(op, arg1Type, arg2Type);
            var arg1Converter = arg1Type == commonType ? null : GetConverter(arg1Type, commonType);
            var arg2Converter = arg2Type == commonType ? null : GetConverter(arg2Type, commonType);
            var impl = new OperatorImplementation(key, commonType, method, arg1Converter, arg2Converter, resultConverter);
            return impl;
        }

        // Creates overflow handlers. For each implementation, checks if operator can overflow; 
        // if yes, creates and sets an overflow handler - another implementation that performs
        // operation using "upper" type that wouldn't overflow. For ex: (int * int) has overflow handler (int64 * int64) 
        protected virtual void CreateOverflowHandlers() {
            foreach (var impl in OperatorImplementations.Values) {
                if (!CanOverflow(impl)) {
                    continue;
                }

                var key = impl.Key;
                var upType = GetUpType(impl.CommonType);
                if (upType == null) {
                    continue;
                }

                var upBaseImpl = FindBaseImplementation(key.Op, upType);
                if (upBaseImpl == null) {
                    continue;
                }

                impl.OverflowHandler = CreateBinaryOperatorImplementation(key.Op, key.Arg1Type, key.Arg2Type, upType,
                    upBaseImpl.BaseBinaryMethod, upBaseImpl.ResultConverter);
                // Do not put OverflowHandler into OperatoImplementations table! - it will override some other, non-overflow impl
            }
        }

        private OperatorImplementation FindBaseImplementation(ExpressionType op, Type commonType) {
            var baseKey = new OperatorDispatchKey(op, commonType, commonType);
            OperatorImplementations.TryGetValue(baseKey, out OperatorImplementation baseImpl);
            return baseImpl;
        }

        // Important: returns null if fromType == toType
        public virtual UnaryOperatorMethod GetConverter(Type fromType, Type toType) {
            if (fromType == toType) {
                return (x => x);
            }

            var key = new OperatorDispatchKey(ExpressionType.ConvertChecked, fromType, toType);
            return !OperatorImplementations.TryGetValue(key, out OperatorImplementation impl) ? null : impl.Arg1Converter;
        }
        #endregion

        #region Utilities
        private static bool CanOverflow(OperatorImplementation impl) {
            if (!CanOverflow(impl.Key.Op)) {
                return false;
            }
            if (impl.CommonType == typeof(int) && IsSmallInt(impl.Key.Arg1Type) && IsSmallInt(impl.Key.Arg2Type)) {
                return false;
            }
            if (impl.CommonType == typeof(double) || impl.CommonType == typeof(float)) {
                return false;
            }
            if (impl.CommonType == typeof(BigInteger)) {
                return false;
            }
            return true;
        }

        private static bool CanOverflow(ExpressionType expression) {
            return OverflowOperators.Contains(expression);
        }

        private static bool IsSmallInt(Type type) {
            return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort);
        }

        /// <summary>
        /// Returns the type to which arguments should be converted to perform the operation
        /// for a given operator and arguments types.
        /// </summary>
        /// <param name="op">Operator.</param>
        /// <param name="argType1">The type of the first argument.</param>
        /// <param name="argType2">The type of the second argument</param>
        /// <returns>A common type for operation.</returns>
        protected virtual Type GetCommonTypeForOperator(ExpressionType op, Type argType1, Type argType2) {
            if (argType1 == argType2) {
                return argType1;
            }

            //TODO: see how to handle properly null/NoneValue in expressions
            // var noneType = typeof(NoneClass);
            // if (argType1 == noneType || argType2 == noneType) return noneType; 

            // Check for unsigned types and convert to signed versions
            var t1 = GetSignedTypeForUnsigned(argType1);
            var t2 = GetSignedTypeForUnsigned(argType2);
            // The type with higher index in _typesSequence is the commont type 
            var index1 = TypesSequence.IndexOf(t1);
            var index2 = TypesSequence.IndexOf(t2);
            if (index1 >= 0 && index2 >= 0) {
                return TypesSequence[Math.Max(index1, index2)];
            }
            //If we have some custom type, 
            return null;
        } 

        // If a type is one of "unsigned" int types, returns next bigger signed type
        protected virtual Type GetSignedTypeForUnsigned(Type type) {
            if (!UnsignedTypes.Contains(type)) {
                return type;
            }
            if (type == typeof(byte) || type == typeof(ushort)) {
                return typeof(int);
            }
            if (type == typeof(uint)) {
                return typeof(long);
            }
            if (type == typeof(ulong)) {
                return typeof(long); //let's remain in Int64
            }
            return typeof(BigInteger);
        }

        /// <summary>
        /// Returns the "up-type" to use in operation instead of the type that caused overflow.
        /// </summary>
        /// <param name="type">The base type for operation that caused overflow.</param>
        /// <returns>The type to use for operation.</returns>
        /// <remarks>
        /// Can be overwritten in language implementation to implement different type-conversion policy.
        /// </remarks>
        protected virtual Type GetUpType(Type type) {
            // In fact we do not need to care about unsigned types - they are eliminated from common types for operations,
            //  so "type" parameter can never be unsigned type. But just in case...
            if (UnsignedTypes.Contains(type)) {
                return GetSignedTypeForUnsigned(type); //it will return "upped" type in fact
            }
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(short)) {
                return typeof(int);
            }
            if (type == typeof(int)) {
                return typeof(long);
            }
            if (type == typeof(long)) {
                return typeof(BigInteger);
            }
            return null;
        }

        //Note bool type at the end - if any of operands is of bool type, convert the other to bool as well
        private static readonly TypeList TypesSequence = new TypeList(
            typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(BigInteger), // typeof(Rational)
            typeof(float), typeof(double), typeof(Complex),
            typeof(bool), typeof(char), typeof(string)
        );

        private static readonly TypeList UnsignedTypes = new TypeList(
            typeof(byte), typeof(ushort), typeof(uint), typeof(ulong)
        );
        #endregion

        private static readonly ExpressionType[] OverflowOperators = {
            ExpressionType.Add, ExpressionType.AddChecked, ExpressionType.Subtract, ExpressionType.SubtractChecked,
            ExpressionType.Multiply, ExpressionType.MultiplyChecked, ExpressionType.Power
        };

        // Smart boxing: boxes for a bunch of integers are preallocated
        private const int BoxesMiddle = 2048;
        private readonly object[] _boxes = new object[BoxesMiddle * 2];

        private bool _supportsComplex;
        private bool _supportsBigInt;
        private bool _supportsRational;

    }

}
