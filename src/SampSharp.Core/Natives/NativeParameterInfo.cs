// SampSharp
// Copyright 2017 Tim Potze
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using SampSharp.Core.Communication;

namespace SampSharp.Core.Natives
{
    /// <summary>
    ///     Contains information about a native's parameter.
    /// </summary>
    public struct NativeParameterInfo
    {
        /// <summary>
        ///     A mask for all supported argument value types.
        /// </summary>
        private const NativeParameterType ArgumentMask = NativeParameterType.Int32 |
                                                         NativeParameterType.Single |
                                                         NativeParameterType.Bool |
                                                         NativeParameterType.String;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NativeParameterInfo" /> struct.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="lengthIndex">Index of the length.</param>
        public NativeParameterInfo(NativeParameterType type, uint lengthIndex)
        {
            Type = type;
            LengthIndex = lengthIndex;
            RequiresLength = CalcRequiresLength(type);
            ArgumentType = CalcCommandArgument(type);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NativeParameterInfo" /> struct.
        /// </summary>
        /// <param name="type">The type.</param>
        public NativeParameterInfo(NativeParameterType type)
        {
            Type = type;
            LengthIndex = 0;
            RequiresLength = CalcRequiresLength(type);
            ArgumentType = CalcCommandArgument(type);
        }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public NativeParameterType Type { get; }

        /// <summary>
        ///     Gets the type as a <see cref="ServerCommandArgument" />.
        /// </summary>
        public ServerCommandArgument ArgumentType { get; }

        private static ServerCommandArgument CalcCommandArgument(NativeParameterType type)
        {
            var value = ServerCommandArgument.Terminator;

            switch (type & ArgumentMask)
            {
                case NativeParameterType.Int32:
                case NativeParameterType.Single:
                case NativeParameterType.Bool:
                    value = ServerCommandArgument.Value;
                    break;
                case NativeParameterType.String:
                    value = ServerCommandArgument.String;
                    break;
            }

            if (type.HasFlag(NativeParameterType.Array))
                value = ServerCommandArgument.Array;

            if (type.HasFlag(NativeParameterType.Reference))
                value |= ServerCommandArgument.Reference;

            return value;
        }

        /// <summary>
        ///     Returns a <see cref="NativeParameterInfo" /> for the specified <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A struct for the type.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     Thrown if <paramref name="type" /> is not a valid native parameter
        ///     type.
        /// </exception>
        public static NativeParameterInfo ForType(Type type)
        {
            var isByRef = type.IsByRef;
            var elementType = isByRef ? type.GetElementType() : type;
            var isArray = elementType.IsArray;
            elementType = isArray ? elementType.GetElementType() : elementType;

            NativeParameterType parameterType;
            if (elementType == typeof(int)) parameterType = NativeParameterType.Int32;
            else if (elementType == typeof(float)) parameterType = NativeParameterType.Single;
            else if (elementType == typeof(bool)) parameterType = NativeParameterType.Bool;
            else if (elementType == typeof(string)) parameterType = NativeParameterType.String;
            else throw new ArgumentOutOfRangeException(nameof(type));

            if (isArray) parameterType |= NativeParameterType.Array;
            if (isByRef) parameterType |= NativeParameterType.Reference;

            return new NativeParameterInfo(parameterType);
        }

        /// <summary>
        ///     Gets a value indicating whether the parameter info requires length information.
        /// </summary>
        public bool RequiresLength { get; }

        private static bool CalcRequiresLength(NativeParameterType type)
        {
            var isArray = type.HasFlag(NativeParameterType.Array);
            var isReference = type.HasFlag(NativeParameterType.Reference);
            var isValue = (type & (NativeParameterType.Int32 | NativeParameterType.Single | NativeParameterType.Bool)) != 0;
            return isArray || isReference && !isValue;
        }

        /// <summary>
        ///     Gets the index of the length parameter specifying the length of this parameter.
        /// </summary>
        public uint LengthIndex { get; }

        /// <summary>
        ///     Returns the referenced value returned by a native.
        /// </summary>
        /// <param name="response">The response to extract the value from.</param>
        /// <param name="index">The current top of the response.</param>
        /// <param name="length">The length of the argument.</param>
        /// <param name="nativeResult">The result of the invoked native</param>
        /// <param name="gameModeClient">The game mode client.</param>
        /// <returns>The referenced value.</returns>
        public object GetReferenceArgument(byte[] response, ref int index, int length, int nativeResult, IGameModeClient gameModeClient)
        {
            object result = null;
            switch (Type)
            {
                case NativeParameterType.Int32Reference:
                    result = ValueConverter.ToInt32(response, index);
                    index += 4;
                    break;
                case NativeParameterType.SingleReference:
                    result = ValueConverter.ToSingle(response, index);
                    index += 4;
                    break;
                case NativeParameterType.BoolReference:
                    result = ValueConverter.ToBoolean(response, index);
                    index += 4;
                    break;
                case NativeParameterType.StringReference:
                    var str = ValueConverter.ToString(response, index, gameModeClient.Encoding);
                    result = str;
                    index += str.Length + 1;

                    if (nativeResult == 0) 
                        result = string.Empty;
                    break;
                case NativeParameterType.Int32ArrayReference:
                {
                    //var len =  ValueConverter.ToInt32(response, index);
                    //index += 4;
                    var arr = new int[length];
                    for (var i = 0; i < length; i++)
                    {
                        arr[i] = ValueConverter.ToInt32(response, index);
                        index += 4;
                    }

                    result = arr;
                    break;
                }
                case NativeParameterType.SingleArrayReference:
                {
                    //var len = ValueConverter.ToInt32(response, index);
                    //index += 4;
                    var arr = new float[length];
                    for (var i = 0; i < length; i++)
                    {
                        arr[i] = ValueConverter.ToSingle(response, index);
                        index += 4;
                    }

                    result = arr;
                    break;
                }
                case NativeParameterType.BoolArrayReference:
                {
                    //var len = ValueConverter.ToInt32(response, index);
                    //index += 4;
                    var arr = new bool[length];
                    for (var i = 0; i < length; i++)
                    {
                        arr[i] = ValueConverter.ToBoolean(response, index);
                        index += 4;
                    }

                    result = arr;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts the value to a collection of bytes according to this parameter.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="length">The length.</param>
        /// <param name="gameModeClient">The game mode client.</param>
        /// <returns>A collection of bytes.</returns>
        public IEnumerable<byte> GetBytes(object value, int length, IGameModeClient gameModeClient)
        {
            switch (Type)
            {
                case NativeParameterType.Int32:
                case NativeParameterType.Int32Reference:
                    if (value is int v)
                        return ValueConverter.GetBytes(v);
                    else if (value == null)
                        return ValueConverter.GetBytes(0);
                    break;
                case NativeParameterType.Single:
                case NativeParameterType.SingleReference:
                    if (value is float f)
                        return ValueConverter.GetBytes(f);
                    else if (value == null)
                        return ValueConverter.GetBytes(0.0f);
                    break;
                case NativeParameterType.Bool:
                case NativeParameterType.BoolReference:
                    if (value is bool b)
                        return ValueConverter.GetBytes(b);
                    else if (value == null)
                        return ValueConverter.GetBytes(false);
                    break;
                case NativeParameterType.String:
                    if (value is string s)
                        return ValueConverter.GetBytes(s, gameModeClient.Encoding);
                    else if (value == null)
                        return ValueConverter.GetBytes("", gameModeClient.Encoding);
                    break;
                case NativeParameterType.StringReference:
                case NativeParameterType.Int32ArrayReference:
                case NativeParameterType.SingleArrayReference:
                case NativeParameterType.BoolArrayReference:
                    if (length < 1)
                        throw new ArgumentOutOfRangeException(nameof(length));
                    return ValueConverter.GetBytes(length);
                case NativeParameterType.Int32Array:
                    if (value is int[] ai)
                    {
                        if (length < 1)
                            throw new ArgumentOutOfRangeException(nameof(length));

                        var array = new byte[length * 4 + 4];
                        ValueConverter.GetBytes(length).CopyTo(array, 0);
                        for (var i = 0; i < length; i++)
                        {
                            ValueConverter.GetBytes(ai[i]).CopyTo(array, 4 + i * 4);
                        }

                        return array;
                    }
                    break;
                case NativeParameterType.SingleArray:
                    if (value is float[] af)
                    {
                        if (length < 1)
                            throw new ArgumentOutOfRangeException(nameof(length));

                        var array = new byte[length * 4 + 4];
                        ValueConverter.GetBytes(length).CopyTo(array, 0);
                        for (var i = 0; i < length; i++)
                        {
                            ValueConverter.GetBytes(af[i]).CopyTo(array, 4 + i * 4);
                        }

                        return array;
                    }
                    break;
                case NativeParameterType.BoolArray:
                    if (value is bool[] ab)
                    {
                        if (length < 1)
                            throw new ArgumentOutOfRangeException(nameof(length));

                        var array = new byte[length * 4 + 4];
                        ValueConverter.GetBytes(length).CopyTo(array, 0);
                        for (var i = 0; i < length; i++)
                        {
                            ValueConverter.GetBytes(ab[i]).CopyTo(array, 4 + i * 4);
                        }

                        return array;
                    }
                    break;
            }

            throw new ArgumentException("Value is of invalid type", nameof(value));
        }
    }
}