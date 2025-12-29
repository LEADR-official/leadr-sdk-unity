/*
 * MiniJSON - A minimal JSON parser and serializer for C#
 *
 * Based on the original MiniJSON by Calvin Rien
 * https://gist.github.com/darktable/1411710
 *
 * This is public domain software. Do with it as you please.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Leadr.Internal
{
    public static class Json
    {
        public static object Deserialize(string json)
        {
            if (json == null)
                return null;

            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            StringReader json;

            Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();

                json.Read(); // Skip opening brace

                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            string name = ParseString();
                            if (name == null)
                                return null;

                            if (NextToken != TOKEN.COLON)
                                return null;

                            json.Read(); // Skip colon

                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            List<object> ParseArray()
            {
                var array = new List<object>();

                json.Read(); // Skip opening bracket

                var parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            object value = ParseByToken(nextToken);
                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseObject();
                    case TOKEN.SQUARED_OPEN:
                        return ParseArray();
                    case TOKEN.TRUE:
                        return true;
                    case TOKEN.FALSE:
                        return false;
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            string ParseString()
            {
                var s = new StringBuilder();
                char c;

                json.Read(); // Skip opening quote

                bool parsing = true;
                while (parsing)
                {
                    if (json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    s.Append(c);
                                    break;
                                case 'b':
                                    s.Append('\b');
                                    break;
                                case 'f':
                                    s.Append('\f');
                                    break;
                                case 'n':
                                    s.Append('\n');
                                    break;
                                case 'r':
                                    s.Append('\r');
                                    break;
                                case 't':
                                    s.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new char[4];
                                    for (int i = 0; i < 4; i++)
                                        hex[i] = NextChar;

                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            object ParseNumber()
            {
                string number = NextWord;

                if (number.Contains(".") || number.Contains("e") || number.Contains("E"))
                {
                    if (double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                        return result;
                }
                else
                {
                    if (long.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out long result))
                        return result;
                }

                return 0;
            }

            void EatWhitespace()
            {
                while (char.IsWhiteSpace(PeekChar))
                {
                    json.Read();

                    if (json.Peek() == -1)
                        break;
                }
            }

            char PeekChar => Convert.ToChar(json.Peek());

            char NextChar => Convert.ToChar(json.Read());

            string NextWord
            {
                get
                {
                    var word = new StringBuilder();

                    while (!IsWordBreak(PeekChar))
                    {
                        word.Append(NextChar);

                        if (json.Peek() == -1)
                            break;
                    }

                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();

                    if (json.Peek() == -1)
                        return TOKEN.NONE;

                    switch (PeekChar)
                    {
                        case '{':
                            return TOKEN.CURLY_OPEN;
                        case '}':
                            json.Read();
                            return TOKEN.CURLY_CLOSE;
                        case '[':
                            return TOKEN.SQUARED_OPEN;
                        case ']':
                            json.Read();
                            return TOKEN.SQUARED_CLOSE;
                        case ',':
                            json.Read();
                            return TOKEN.COMMA;
                        case '"':
                            return TOKEN.STRING;
                        case ':':
                            return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "false":
                            return TOKEN.FALSE;
                        case "true":
                            return TOKEN.TRUE;
                        case "null":
                            return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            static bool IsWordBreak(char c)
            {
                return char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }
        }

        sealed class Serializer
        {
            StringBuilder builder;

            Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance.builder.ToString();
            }

            void SerializeValue(object value)
            {
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value is string str)
                {
                    SerializeString(str);
                }
                else if (value is bool b)
                {
                    builder.Append(b ? "true" : "false");
                }
                else if (value is IList list)
                {
                    SerializeArray(list);
                }
                else if (value is IDictionary dict)
                {
                    SerializeObject(dict);
                }
                else if (value is char c)
                {
                    SerializeString(new string(c, 1));
                }
                else
                {
                    SerializeOther(value);
                }
            }

            void SerializeObject(IDictionary obj)
            {
                bool first = true;
                builder.Append('{');

                foreach (object key in obj.Keys)
                {
                    if (!first)
                        builder.Append(',');

                    SerializeString(key.ToString());
                    builder.Append(':');
                    SerializeValue(obj[key]);

                    first = false;
                }

                builder.Append('}');
            }

            void SerializeArray(IList array)
            {
                builder.Append('[');
                bool first = true;

                foreach (object obj in array)
                {
                    if (!first)
                        builder.Append(',');

                    SerializeValue(obj);
                    first = false;
                }

                builder.Append(']');
            }

            void SerializeString(string str)
            {
                builder.Append('\"');

                foreach (char c in str)
                {
                    switch (c)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            int codepoint = Convert.ToInt32(c);
                            if (codepoint >= 32 && codepoint <= 126)
                            {
                                builder.Append(c);
                            }
                            else
                            {
                                builder.Append("\\u");
                                builder.Append(codepoint.ToString("x4"));
                            }
                            break;
                    }
                }

                builder.Append('\"');
            }

            void SerializeOther(object value)
            {
                if (value is float f)
                {
                    builder.Append(f.ToString("R", CultureInfo.InvariantCulture));
                }
                else if (value is int
                    || value is uint
                    || value is long
                    || value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is ulong)
                {
                    builder.Append(value);
                }
                else if (value is double
                    || value is decimal)
                {
                    builder.Append(Convert.ToDouble(value).ToString("R", CultureInfo.InvariantCulture));
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }
        }
    }

    public static class JsonExtensions
    {
        public static string GetString(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
                return value.ToString();
            return null;
        }

        public static int GetInt(this Dictionary<string, object> dict, string key, int defaultValue = 0)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is long l)
                    return (int)l;
                if (value is int i)
                    return i;
                if (value is double d)
                    return (int)d;
            }
            return defaultValue;
        }

        public static long GetLong(this Dictionary<string, object> dict, string key, long defaultValue = 0)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is long l)
                    return l;
                if (value is int i)
                    return i;
                if (value is double d)
                    return (long)d;
            }
            return defaultValue;
        }

        public static double GetDouble(this Dictionary<string, object> dict, string key, double defaultValue = 0)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (value is double d)
                    return d;
                if (value is long l)
                    return l;
                if (value is int i)
                    return i;
            }
            return defaultValue;
        }

        public static bool GetBool(this Dictionary<string, object> dict, string key, bool defaultValue = false)
        {
            if (dict.TryGetValue(key, out var value) && value is bool b)
                return b;
            return defaultValue;
        }

        public static List<object> GetList(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value is List<object> list)
                return list;
            return null;
        }

        public static List<string> GetStringList(this Dictionary<string, object> dict, string key)
        {
            var list = dict.GetList(key);
            if (list == null)
                return new List<string>();

            var result = new List<string>();
            foreach (var item in list)
            {
                if (item != null)
                    result.Add(item.ToString());
            }
            return result;
        }

        public static Dictionary<string, object> GetDict(this Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value is Dictionary<string, object> d)
                return d;
            return null;
        }

        public static DateTime? GetDateTime(this Dictionary<string, object> dict, string key)
        {
            var str = dict.GetString(key);
            if (string.IsNullOrEmpty(str))
                return null;

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
                return result;

            return null;
        }

        public static DateTime GetDateTimeRequired(this Dictionary<string, object> dict, string key)
        {
            var result = dict.GetDateTime(key);
            if (result.HasValue)
                return result.Value;
            return DateTime.MinValue;
        }
    }
}
