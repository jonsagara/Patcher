using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Patcher.Extensions;

namespace Patcher
{
    /// <summary>
    /// &quot;Patch&quot; as in &quot;PATCH&quot; the HTTP verb, where you apply partial updates to an object.
    /// </summary>
    public static class SimplePatcher
    {
        /// <summary>
        /// <para>This is meant specifically for updating a destination object from a dynamic JObject received, e.g. from
        /// a Web API request. It only works on simple types (string, int, etc.), hence the name SimplePatcher.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="ignoreUnknownProperties"></param>
        public static void PatchFromJObject<T>(dynamic source, T destination, bool ignoreCase = true, bool ignoreUnknownProperties = false)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source.GetType() != typeof(JObject))
            {
                throw new ArgumentException($"This method only works with dynamic objects of type {typeof(JObject).FullName}", nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }


            // Since we're operating on a dynamic object, C# will assume we want a dynamic returned if we declare the variable
            //   as var. As such, we have to be explicit about our return type here.
            Dictionary<string, object> sourcePropertyNamesAndValues = GetPropertyNamesAndValues(source);

            var destinationProperties = GetPublicInstanceProperties(destination);
            var destinationPropertyNames = destinationProperties.Select(pi => pi.Name).ToArray();
            var propertyNameComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (!ignoreUnknownProperties)
            {
                // If there are any properties on the source object that do not exist on the destination object, throw
                //   an exception.
                var propertyNameComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

                var unknownSourcePropertyNames = sourcePropertyNamesAndValues.Keys
                    .Except(destinationPropertyNames, propertyNameComparer)
                    .ToArray();

                if (unknownSourcePropertyNames.Length > 0)
                {
                    throw new InvalidOperationException($"Source object has properties that are not present on the destination object of type {typeof(T).FullName}. Unknown source property names: {string.Join(", ", unknownSourcePropertyNames)}");
                }
            }
            
            // Enumerate the source object's properties, and set the correpsonding values on the destination object.
            foreach (var sourcePropertyNameValue in sourcePropertyNamesAndValues)
            {
                var destinationProperty = destinationProperties.SingleOrDefault(pi => pi.Name.Equals(sourcePropertyNameValue.Key, propertyNameComparison));
                if (destinationProperty == null)
                {
                    // Corresponding property not found on destination object. Go to the next property.
                    continue;
                }

                if (!destinationProperty.CanWrite)
                {
                    throw new InvalidOperationException($"Cannot write to destination property {destinationProperty.Name} of type {destination.GetType().FullName}");
                }

                if (!destinationProperty.PropertyType.IsDefaultUriBindableType())
                {
                    throw new NotSupportedException($"Destination properties implementing non-primitive, non-simple types are not supported. Property: {destinationProperty.Name}; Type: {destinationProperty.PropertyType.FullName}. See: http://www.asp.net/web-api/overview/formats-and-model-binding/parameter-binding-in-aspnet-web-api");
                }

                // Update the destination property value.
                var destinationValue = GetDestinationValue(sourcePropertyNameValue, destinationProperty);
                destinationProperty.SetValue(destination, destinationValue);
            }
        }

        private static Dictionary<string, object> GetPropertyNamesAndValues(dynamic value)
        {
            var dynamicMetaObjProvider = (IDynamicMetaObjectProvider)value;

            var dynamicMemberNames = dynamicMetaObjProvider
                .GetMetaObject(Expression.Constant(dynamicMetaObjProvider))
                .GetDynamicMemberNames()
                .ToArray();

            var propertyNamesAndValues = new Dictionary<string, object>();

            foreach (var dynamicMemberName in dynamicMemberNames)
            {
                propertyNamesAndValues.Add(dynamicMemberName, value[dynamicMemberName]);
            }

            return propertyNamesAndValues;
        }

        private static object GetDestinationValue(KeyValuePair<string, object> sourcePropertyNameValue, PropertyInfo destinationProperty)
        {
            // A note regarding integer values (see http://stackoverflow.com/a/9444519/731):
            //   Json.NET assumes all integer values are Int64 because it has no way of knowing whether it should use
            //   Int32 or Int64, and with Int64 there is less likelihood of an overflow. We have to be smart about 
            //   casting to the proper type here based on the destination type, or else we'll get a runtime exception
            //   about failing to cast an Int64 to an Int32.

            // Default, non-integer case.
            object destValue = ((JValue)sourcePropertyNameValue.Value).Value;

            if (destinationProperty.PropertyType == typeof(int) && destValue != null)
            {
                destValue = (int)(long)destValue;
            }
            else if (destinationProperty.PropertyType == typeof(short) && destValue != null)
            {
                destValue = (short)(long)destValue;
            }
            else if (destinationProperty.PropertyType == typeof(byte) && destValue != null)
            {
                destValue = (byte)(long)destValue;
            }
            else if (destinationProperty.PropertyType == typeof(long) && destValue != null)
            {
                destValue = (long)destValue;
            }
            else if (destinationProperty.PropertyType.IsGenericType && destinationProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (destinationProperty.PropertyType == typeof(int?))
                {
                    destValue = (int?)(long?)destValue;
                }
                else if (destinationProperty.PropertyType == typeof(short?))
                {
                    destValue = (short?)(long?)destValue;
                }
                else if (destinationProperty.PropertyType == typeof(byte?))
                {
                    destValue = (byte?)(long?)destValue;
                }
                else if (destinationProperty.PropertyType == typeof(long?))
                {
                    destValue = (long?)destValue;
                }
            }

            return destValue;
        }

        private static PropertyInfo[] GetPublicInstanceProperties<T>(T value)
        {
            return value
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
        }
    }
}
