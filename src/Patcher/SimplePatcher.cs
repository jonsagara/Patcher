using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Patcher
{
    public static class SimplePatcher
    {
        /// <summary>
        /// This is meant specifically for updating a destination object from a dynamic JObject receive, e.g. from
        /// a Web API request.
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
                throw new ArgumentException($"This method only works with dynamic objects of type JObject", nameof(source));
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

                // Update the destination property value.
                destinationProperty.SetValue(destination, ((JValue)sourcePropertyNameValue.Value).Value);
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

        private static PropertyInfo[] GetPublicInstanceProperties<T>(T value)
        {
            return value
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToArray();
        }
    }
}
