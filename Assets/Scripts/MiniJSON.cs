// File: MiniJSON.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class MiniJSON
{
    public static string Serialize(object obj)
    {
        return Json.Serialize(obj);
    }

    public static class Json
    {
        public static string Serialize(object obj)
        {
            StringBuilder builder = new StringBuilder();
            SerializeValue(obj, builder);
            return builder.ToString();
        }

        static void SerializeValue(object value, StringBuilder builder)
        {
            if (value == null)
            {
                builder.Append("null");
            }
            else if (value is string str)
            {
                builder.Append($"\"{str.Replace("\"", "\\\"")}\"");
            }
            else if (value is bool b)
            {
                builder.Append(b ? "true" : "false");
            }
            else if (value is IDictionary dict)
            {
                SerializeObject(dict, builder);
            }
            else if (value is IList list)
            {
                SerializeArray(list, builder);
            }
            else
            {
                builder.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        static void SerializeObject(IDictionary obj, StringBuilder builder)
        {
            bool first = true;
            builder.Append("{");
            foreach (object e in obj.Keys)
            {
                if (!first) builder.Append(",");
                SerializeValue(e, builder);
                builder.Append(":");
                SerializeValue(obj[e], builder);
                first = false;
            }
            builder.Append("}");
        }

        static void SerializeArray(IList list, StringBuilder builder)
        {
            builder.Append("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) builder.Append(",");
                SerializeValue(item, builder);
                first = false;
            }
            builder.Append("]");
        }
    }
}
