﻿using OpenGauss.NET.BackendMessages;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using OpenGauss.NET.Internal.TypeHandling;

namespace OpenGauss.NET
{
    static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowInvalidCastException_NotSupportedType(OpenGaussTypeHandler handler, OpenGaussParameter? parameter, Type type)
        {
            var parameterName = parameter is null
                ? null
                : parameter.TrimmedName == string.Empty
                    ? $"${parameter.Collection!.IndexOf(parameter) + 1}"
                    : parameter.TrimmedName;

            throw new InvalidCastException(parameterName is null
                ? $"Cannot write a value of CLR type '{type}' as database type '{handler.PgDisplayName}'."
                : $"Cannot write a value of CLR type '{type}' as database type '{handler.PgDisplayName}' for parameter '{parameterName}'.");
        }

        [DoesNotReturn]
        internal static void ThrowInvalidCastException_NoValue(FieldDescription field) =>
            throw new InvalidCastException($"Column '{field.Name}' is null.");

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_NoPropertyGetter(Type type, MemberInfo property) =>
            throw new InvalidOperationException($"Composite type '{type}' cannot be written because the '{property}' property has no getter.");

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_NoPropertySetter(Type type, MemberInfo property) =>
            throw new InvalidOperationException($"Composite type '{type}' cannot be read because the '{property}' property has no setter.");

        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_BinaryImportParametersMismatch(int columnCount, int valueCount) =>
            throw new InvalidOperationException($"The binary import operation was started with {columnCount} column(s), but {valueCount} value(s) were provided.");
    }
}
