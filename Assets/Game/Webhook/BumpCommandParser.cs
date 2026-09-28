using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Webhook
{
    /// <summary>
    /// Turns a /bump request's query string and body into a <see cref="BumpCommand"/>. Hand-written
    /// and allocation-light because the payload is three optional strings; no UnityEngine calls.
    /// The body is a flat JSON object of strings when it starts with '{', otherwise
    /// application/x-www-form-urlencoded. Body values override query values.
    /// </summary>
    internal static class BumpCommandParser
    {
        public const int MaxTagLength = 24;

        public const string InvalidPolarity = "invalid_polarity";
        public const string BadBody = "bad_body";

        /// <summary>Returns false with an error code (InvalidPolarity / BadBody) when the request is malformed.</summary>
        public static bool TryParse(string query, string body, out BumpCommand command, out string error)
        {
            command = default;
            error = null;

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ParseForm(query, fields);

            string trimmedBody = body == null ? string.Empty : body.TrimStart('\uFEFF').Trim();
            if (trimmedBody.Length > 0)
            {
                if (trimmedBody[0] == '{')
                {
                    if (!TryParseFlatJson(trimmedBody, fields))
                    {
                        error = BadBody;
                        return false;
                    }
                }
                else
                {
                    ParseForm(trimmedBody, fields);
                }
            }

            BumpPolarity? polarity = null;
            string rawPolarity = Clean(Get(fields, "polarity"));
            if (rawPolarity != null)
            {
                if (string.Equals(rawPolarity, "positive", StringComparison.OrdinalIgnoreCase))
                {
                    polarity = BumpPolarity.Positive;
                }
                else if (string.Equals(rawPolarity, "negative", StringComparison.OrdinalIgnoreCase))
                {
                    polarity = BumpPolarity.Negative;
                }
                else
                {
                    error = InvalidPolarity;
                    return false;
                }
            }

            string typeId = Clean(Get(fields, "type"));
            string tag = Clean(Get(fields, "tag"));
            if (tag != null && tag.Length > MaxTagLength)
            {
                int cut = MaxTagLength;
                if (char.IsHighSurrogate(tag[cut - 1]))
                {
                    cut--; // never split a surrogate pair.
                }

                tag = tag.Substring(0, cut).TrimEnd();
                if (tag.Length == 0)
                {
                    tag = null;
                }
            }

            command = new BumpCommand(polarity, typeId, tag);
            return true;
        }

        private static string Get(Dictionary<string, string> fields, string key)
        {
            return fields.TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>Strips control characters and surrounding whitespace; an empty result counts as missing (null).</summary>
        private static string Clean(string value)
        {
            if (value == null)
            {
                return null;
            }

            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (!IsInvisible(c))
                {
                    sb.Append(c);
                }
            }

            string cleaned = sb.ToString().Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }

        /// <summary>Control characters, zero-width/format characters (BOM, joiners, bidi marks) and the line/paragraph separators.</summary>
        private static bool IsInvisible(char c)
        {
            if (char.IsControl(c))
            {
                return true;
            }

            UnicodeCategory category = char.GetUnicodeCategory(c);
            return category == UnicodeCategory.Format
                || category == UnicodeCategory.LineSeparator
                || category == UnicodeCategory.ParagraphSeparator;
        }

        /// <summary>Stores a value only when something is left after cleaning, so an empty `field=` never clears an earlier value.</summary>
        private static void Store(Dictionary<string, string> into, string key, string value)
        {
            string cleaned = Clean(value);
            if (cleaned != null)
            {
                into[key] = cleaned;
            }
        }

        private static void ParseForm(string text, Dictionary<string, string> into)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (string pair in text.Split('&'))
            {
                if (pair.Length == 0)
                {
                    continue;
                }

                int eq = pair.IndexOf('=');
                string key = Decode(eq < 0 ? pair : pair.Substring(0, eq));
                string value = eq < 0 ? string.Empty : Decode(pair.Substring(eq + 1));
                Store(into, key, value);
            }
        }

        private static string Decode(string encoded)
        {
            try
            {
                return Uri.UnescapeDataString(encoded.Replace('+', ' '));
            }
            catch (UriFormatException)
            {
                return encoded;
            }
        }

        /// <summary>
        /// Reads one flat JSON object. String values are stored; other scalar values are skipped for
        /// unknown keys and rejected for the three known ones; nested objects/arrays and any
        /// syntax error reject the whole body.
        /// </summary>
        private static bool TryParseFlatJson(string json, Dictionary<string, string> into)
        {
            int i = 0;
            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] != '{')
            {
                return false;
            }

            i++;
            SkipWhitespace(json, ref i);
            if (i < json.Length && json[i] == '}')
            {
                return SkipTrailing(json, i + 1);
            }

            while (true)
            {
                SkipWhitespace(json, ref i);
                if (!TryReadString(json, ref i, out string key))
                {
                    return false;
                }

                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] != ':')
                {
                    return false;
                }

                i++;
                SkipWhitespace(json, ref i);
                if (i >= json.Length)
                {
                    return false;
                }

                bool known = IsKnownKey(key);
                if (json[i] == '"')
                {
                    if (!TryReadString(json, ref i, out string value))
                    {
                        return false;
                    }

                    Store(into, key, value);
                }
                else if (json[i] == '{' || json[i] == '[' || known || !TrySkipScalar(json, ref i))
                {
                    return false;
                }

                SkipWhitespace(json, ref i);
                if (i >= json.Length)
                {
                    return false;
                }

                if (json[i] == ',')
                {
                    i++;
                    continue;
                }

                if (json[i] == '}')
                {
                    return SkipTrailing(json, i + 1);
                }

                return false;
            }
        }

        private static bool IsKnownKey(string key)
        {
            return string.Equals(key, "polarity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "tag", StringComparison.OrdinalIgnoreCase);
        }

        private static bool SkipTrailing(string json, int index)
        {
            SkipWhitespace(json, ref index);
            return index == json.Length;
        }

        private static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
            {
                i++;
            }
        }

        private static bool TrySkipScalar(string json, ref int i)
        {
            int start = i;
            while (i < json.Length && json[i] != ',' && json[i] != '}' && !char.IsWhiteSpace(json[i]))
            {
                i++;
            }

            string token = json.Substring(start, i - start);
            return token == "true" || token == "false" || token == "null"
                || double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static bool TryReadString(string json, ref int i, out string value)
        {
            value = null;
            if (i >= json.Length || json[i] != '"')
            {
                return false;
            }

            i++;
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i++];
                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (c < ' ')
                {
                    return false; // raw control characters are illegal inside JSON strings.
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (i >= json.Length)
                {
                    return false;
                }

                char escape = json[i++];
                switch (escape)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > json.Length
                            || !int.TryParse(json.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                        {
                            return false;
                        }

                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:
                        return false;
                }
            }

            return false; // unterminated string.
        }
    }
}
