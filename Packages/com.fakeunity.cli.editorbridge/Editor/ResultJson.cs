#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace FakeUnityCLI.EditorBridge
{
    internal static class ResultJson
    {
        private const int MaxDepth = 16;
        private const int MaxCollectionItems = 4096;
        private const int MaxObjectMembers = 256;
        private const int MaxOutputCharacters = 1024 * 1024;

        internal static string Serialize(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0, new HashSet<object>(ReferenceComparer.Instance));
            if (builder.Length > MaxOutputCharacters)
                return "{\"$fuc_truncated\":\"max_output_characters\",\"original_characters\":" +
                       builder.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, object value, int depth, HashSet<object> active)
        {
            if (depth > MaxDepth) { builder.Append("{\"$fuc_truncated\":\"max_depth\"}"); return; }
            if (value == null) { builder.Append("null"); return; }
            if (value is string || value is char) { Quote(builder, Convert.ToString(value)); return; }
            if (value is bool) { builder.Append((bool)value ? "true" : "false"); return; }
            if (value is Enum) { Quote(builder, value.ToString()); return; }
            if (value is float)
            {
                var number = (float)value;
                if (Single.IsNaN(number) || Single.IsInfinity(number)) Quote(builder, Convert.ToString(number, System.Globalization.CultureInfo.InvariantCulture));
                else builder.Append(number.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (value is double)
            {
                var number = (double)value;
                if (Double.IsNaN(number) || Double.IsInfinity(number)) Quote(builder, Convert.ToString(number, System.Globalization.CultureInfo.InvariantCulture));
                else builder.Append(number.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint ||
                value is long || value is ulong || value is decimal)
            {
                builder.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); return;
            }
            if (value is DateTime || value is DateTimeOffset || value is Guid || value is TimeSpan || value is Type)
            {
                Quote(builder, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); return;
            }
            if (IsUnityObject(value.GetType()))
            {
                builder.Append("{\"name\":"); Quote(builder, ReadUnityObjectName(value));
                builder.Append(",\"type\":"); Quote(builder, value.GetType().FullName); builder.Append('}'); return;
            }
            var track = !value.GetType().IsValueType;
            if (track && !active.Add(value))
            {
                builder.Append("{\"$fuc_reference\":\"cycle\"}"); return;
            }
            try
            {
                var dictionary = value as IDictionary;
                if (dictionary != null)
                {
                    builder.Append('{'); var first = true; var count = 0;
                    foreach (DictionaryEntry item in dictionary)
                    {
                        if (count++ >= MaxCollectionItems) { AppendObjectMemberPrefix(builder, ref first, "$fuc_truncated"); Quote(builder, "max_items"); break; }
                        AppendObjectMemberPrefix(builder, ref first, Convert.ToString(item.Key));
                        Write(builder, item.Value, depth + 1, active);
                    }
                    builder.Append('}'); return;
                }
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    builder.Append('['); var first = true; var count = 0;
                    foreach (var item in enumerable)
                    {
                        if (count++ >= MaxCollectionItems)
                        {
                            if (!first) builder.Append(',');
                            builder.Append("{\"$fuc_truncated\":\"max_items\"}"); break;
                        }
                        if (!first) builder.Append(','); first = false; Write(builder, item, depth + 1, active);
                    }
                    builder.Append(']'); return;
                }
                WriteObject(builder, value, depth, active);
            }
            finally
            {
                if (track) active.Remove(value);
            }
        }

        private static void WriteObject(StringBuilder builder, object value, int depth, HashSet<object> active)
        {
            var type = value.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => !field.IsStatic).Cast<MemberInfo>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(false) != null).Cast<MemberInfo>();
            var members = fields.Concat(properties).GroupBy(member => member.Name, StringComparer.Ordinal)
                .Select(group => group.First()).OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();
            if (members.Length == 0) { Quote(builder, Convert.ToString(value)); return; }
            builder.Append('{'); var first = true; var count = 0;
            foreach (var member in members)
            {
                if (count++ >= MaxObjectMembers)
                {
                    AppendObjectMemberPrefix(builder, ref first, "$fuc_truncated"); Quote(builder, "max_members"); break;
                }
                AppendObjectMemberPrefix(builder, ref first, member.Name);
                try
                {
                    var memberValue = member is FieldInfo
                        ? ((FieldInfo)member).GetValue(value)
                        : ((PropertyInfo)member).GetValue(value, null);
                    Write(builder, memberValue, depth + 1, active);
                }
                catch (Exception exception)
                {
                    builder.Append("{\"$fuc_serialization_error\":");
                    Quote(builder, exception.GetBaseException().GetType().Name); builder.Append('}');
                }
            }
            builder.Append('}');
        }

        private static bool IsUnityObject(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
                if (String.Equals(current.FullName, "UnityEngine.Object", StringComparison.Ordinal)) return true;
            return false;
        }

        private static string ReadUnityObjectName(object value)
        {
            try
            {
                var property = value.GetType().GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
                return property == null ? null : Convert.ToString(property.GetValue(value, null));
            }
            catch { return null; }
        }

        private static void AppendObjectMemberPrefix(StringBuilder builder, ref bool first, string name)
        {
            if (!first) builder.Append(','); first = false;
            Quote(builder, name); builder.Append(':');
        }

        private static void Quote(StringBuilder builder, string value)
        {
            builder.Append('"');
            if (value != null)
                foreach (var character in value)
                    switch (character)
                    {
                        case '\\': builder.Append("\\\\"); break;
                        case '"': builder.Append("\\\""); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        default:
                            if (character < ' ') builder.Append("\\u" + ((int)character).ToString("x4"));
                            else builder.Append(character);
                            break;
                    }
            builder.Append('"');
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object left, object right) { return Object.ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }
    }
}
