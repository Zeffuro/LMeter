using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LMeter.Act
{
    public partial class TextTagFormatter
    {
        [GeneratedRegex(@"\[(\w*)(:k)?\.?(\d+)?\]", RegexOptions.Compiled)]
        private static partial Regex GeneratedRegex();
        public static Regex TextTagRegex { get; } = GeneratedRegex();

        private readonly object _source;
        private readonly string _format;
        private readonly Dictionary<string, MemberInfo> _members;

        public TextTagFormatter(
            object source,
            string format,
            Dictionary<string, MemberInfo> members)
        {
            _source = source;
            _format = format;
            _members = members;
        }

        public string Evaluate(Match m)
        {
            if (m.Groups.Count != 4)
            {
                return m.Value;
            }

            string key = m.Groups[1].Value;
            if (!_members.TryGetValue(key, out MemberInfo? memberInfo))
            {
                return m.Value;
            }

            object? memberValue = memberInfo?.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).GetValue(_source),
                MemberTypes.Property => ((PropertyInfo)memberInfo).GetValue(_source),
                _ => null
            };

            string? value = null;
            if (memberValue is LazyFloat lazyFloat)
            {
                string format = string.IsNullOrEmpty(m.Groups[3].Value)
                    ? $"{_format}0"
                    : $"{_format}{m.Groups[3].Value}";

                bool kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                value = lazyFloat.ToString(format, kilo) ?? m.Value;
            }
            else if (memberValue is not null)
            {
                value = memberValue.ToString();
                if (!string.IsNullOrEmpty(value) &&
                    int.TryParse(m.Groups[3].Value, out int trim) &&
                    trim < value.Length)
                {
                    value = memberValue?.ToString().AsSpan(0, trim).ToString();
                }
            }

            return value ?? m.Value;
        }
    }
}