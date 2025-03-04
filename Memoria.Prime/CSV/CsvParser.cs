﻿using System;
using System.Globalization;
using System.Linq;

namespace Memoria.Prime.CSV
{
    public static class CsvParser
    {
        public static Boolean Boolean(String raw)
        {
            switch (raw[0])
            {
                case '1':
                    return true;
                case '0':
                    return false;
                default:
                    throw new NotSupportedException(raw);
            }
        }

        private const NumberStyles NumberStyle = NumberStyles.Integer | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        private const NumberStyles FloatStyle = NumberStyles.Float | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

        public static Byte ByteOrMinusOne(String raw)
        {
            if (raw.Contains("-1"))
                return System.Byte.MaxValue;

            return Byte(raw);
        }

        public static Byte Byte(String raw)
        {
            return System.Byte.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static SByte SByte(String raw)
        {
            return System.SByte.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static Int16 Int16(String raw)
        {
            return System.Int16.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static Int32 Int32(String raw)
        {
            return System.Int32.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static UInt16 UInt16(String raw)
        {
            return System.UInt16.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static UInt32 UInt32(String raw)
        {
            return System.UInt32.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static UInt64 UInt64(String raw)
        {
            return System.UInt64.Parse(raw, NumberStyle, CultureInfo.InvariantCulture);
        }

        public static Single Single(String raw)
        {
            return System.Single.Parse(raw, FloatStyle, CultureInfo.InvariantCulture);
        }

        public static T EnumValue<T>(String raw) where T : struct
        {
            Int32 bracketIndex = raw.IndexOf('(');
            UInt64 value;
            if (bracketIndex >= 0)
            {
                Int32 nextBracket = raw.IndexOf(')', bracketIndex + 2);
                if (nextBracket < 0)
                    throw new CsvParseException($"Cannot find closing bracket: {raw}");

                String subString = raw.Substring(bracketIndex + 1, nextBracket - bracketIndex - 1);
                value = System.UInt64.Parse(subString, NumberStyle, CultureInfo.InvariantCulture);
            }
            else
            {
                if (!System.UInt64.TryParse(raw, NumberStyle, CultureInfo.InvariantCulture, out value))
                    throw new CsvParseException($"Cannot cast {raw} to {TypeCache<T>.Type.FullName}");
            }

            return Caster<UInt64, T>.Cast(value);
        }

        public static Byte[] ByteArray(String raw)
        {
            if (System.String.IsNullOrEmpty(raw))
                return new Byte[0];

            return raw.Split(',').Select(Byte).ToArray();
        }

        public static String String(String raw)
        {
            if (raw == "<null>")
                return null;
            if (raw == "\"\"")
                return System.String.Empty;

            return raw;
        }
    }
}