using System.Collections.Generic;
using System.Reflection;
using log4net;
using Newtonsoft.Json.Linq;

namespace ChartIQ.Finsemble
{
    /// <summary>
    /// Helper methods for interacting with JObjects. Includes extension methods to convert a JObject to a dictionary, 
    /// and a JArray to a list, as well as functions to convert lists to a JArray and an dictionary to a JObject.
    /// </summary>
    internal static class JObjectExtensions
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Converts a JObject to a recursive dictionary of primitives.
        /// </summary>
        /// <param name="object">The JObject to convert.</param>
        /// <returns>Dictionary of properties.</returns>
        /// <see cref="https://stackoverflow.com/a/14903514"/>
        public static IDictionary<string, object> ToDictionary(this JObject @object)
        {
            // Dictionary of the original object. Values will be J-types.
            var input = @object.ToObject<Dictionary<string, object>>();

            var result = new Dictionary<string, object>();
            foreach (var key in input.Keys)
            {
                var entry = input[key];
                if (entry == null)
                {
                    result[key] = entry;
                    continue;
                }
                if (entry.GetType() != typeof(JToken) && entry.GetType() != typeof(JObject) && entry.GetType() != typeof(JArray) && entry.GetType() != typeof(JValue))
                {
                    Logger.Debug($"List entry is not a JToken. Type: {entry.GetType()}");
                    result[key] = entry;
                    continue;
                }

                var value = entry as JToken;
                result[key] = ConvertJObjectToObject(value);
            }

            return result;
        }

        /// <summary>
        /// Converts a JArray to a list of either primitives or IDictionary&lt;string, object&gt;.
        /// </summary>
        /// <param name="@object">The JArray to convert.</param>
        /// <returns>IList&lt;object&gt; of objects that are either primitives or a dictionary of objects.</returns>
        public static IList<object> ToList(this JArray @object)
        {
            // Convert the JArray to a list. Items in the array will still be a J-type.
            var input = @object.ToObject<List<object>>();

            var result = new List<object>(input.Count);
            for (int i = 0; i < input.Count; ++i)
            {
                var entry = input[i];

                if ((entry != null) &&
                    (!entry.GetType().IsPrimitive && (entry.GetType() != typeof(string)) && (entry.GetType() != typeof(JToken))))
                {
                    Logger.Debug($"List entry is not a primitive or JToken. Type: {entry.GetType()}");
                    continue;
                }

                var type = entry.GetType();
                if (type.IsPrimitive || type == typeof(string))
                {
                    result.Add(entry);
                }
                else
                {
                    var value = entry as JToken;
                    result.Add(ConvertJObjectToObject(value));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a JObject from a dictionary of primitive properties.
        /// </summary>
        /// <param name="dictionary">The dictionary of primitive properties.</param>
        public static JObject ToJObject(this IDictionary<string, object> dictionary)
        {
            var obj = new JObject();

            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];

                obj[key] = ConvertObjectToJToken(value);
            }

            return obj;
        }

        /// <summary>
        /// Converts an array of objects into JArray
        /// </summary>
        /// <param name="list">The list to convert into a JArray.</param>
        /// <returns>JArray containing JObjects, JArrays and JValues</returns>
        public static JArray ToJArray(this IList<object> list)
        {
            var array = new JArray();
            foreach (var value in list)
            {
                array.Add(ConvertObjectToJToken(value));
            }

            return array;
        }

        /// <summary>
        /// Converts an object to a JToken (e.g. JArray, JObject, JValue).
        /// </summary>
        /// <param name="obj">C# representation of object</param>
        /// <returns>JSON representation of object</returns>
        public static JToken ConvertObjectToJToken(this object value)
        {
            JToken result;

            if (value == null)
            {
                result = null;
            }
            else
            {
                var type = value.GetType();
                if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                {
                    // Convert dictionary to JObject
                    var dictionary = value as IDictionary<string, object>;
                    result = dictionary.ToJObject();
                }
                else if (typeof(IList<object>).IsAssignableFrom(type))
                {
                    // Convert list to JArray
                    var subArray = value as IList<object>;
                    result = subArray.ToJArray();
                }
                else if (type == typeof(JObject))
                {
                    result = value as JObject;
                }
                else if (type == typeof(JArray))
                {
                    result = value as JArray;
                }
                else
                {
                    // Convert primitive to JValue
                    result = new JValue(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a JToken (e.g. JArray, JObject, JValue) to an object.
        /// </summary>
        /// <param name="jObj">JSON representation of object</param>
        /// <returns>C# representation of object</returns>
        public static object ConvertJObjectToObject(JToken value)
        {
            object result;
            if (value == null)
            {
                result = null;
            }
            else
            {
                if (value.GetType() == typeof(JObject))
                {
                    // Convert JObject to dictionary
                    var obj = value as JObject;
                    result = obj.ToDictionary();
                }
                else if (value.GetType() == typeof(JArray))
                {
                    // Convert JArray to list
                    var obj = value as JArray;
                    result = obj.ToList();
                }
                else if (value.GetType() == typeof(JValue))
                {
                    // Get the value of the JValue.
                    var obj = value as JValue;
                    result = obj.Value;
                }
                else
                {
                    Logger.Debug($"Unexpected type found: {value.GetType()}");
                    result = null;
                }
            }

            return result;
        }
    }
}
