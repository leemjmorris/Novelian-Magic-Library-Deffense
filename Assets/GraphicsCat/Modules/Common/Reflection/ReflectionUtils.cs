using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;

namespace GraphicsCat
{
    public static class ReflectionUtils
    {
        #region Method Invocation (supports private methods)

        /// <summary>
        /// Invoke instance method (supports private methods)
        /// </summary>
        public static object InvokeMethod<T>(T instance, string methodName, object[] parameters = null)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var methodInfo = GetMethodInfo<T>(methodName, parameters);
            return methodInfo.Invoke(instance, parameters ?? Array.Empty<object>());
        }

        /// <summary>
        /// Invoke static method (supports private methods)
        /// </summary>
        public static object InvokeStaticMethod<T>(string methodName, object[] parameters = null)
        {
            var methodInfo = GetMethodInfo<T>(methodName, parameters, isStatic: true);
            return methodInfo.Invoke(null, parameters ?? Array.Empty<object>());
        }

        /// <summary>
        /// Invoke generic instance method (supports private methods)
        /// </summary>
        public static object InvokeGenericMethod<T>(T instance, string methodName, Type[] genericTypes, object[] parameters = null)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var methodInfo = GetMethodInfo<T>(methodName, parameters).MakeGenericMethod(genericTypes);
            return methodInfo.Invoke(instance, parameters ?? Array.Empty<object>());
        }

        private static MethodInfo GetMethodInfo<T>(string methodName, object[] parameters, bool isStatic = false)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;

            var type = typeof(T);

            if (parameters == null || parameters.Length == 0)
            {
                var methodInfo = type.GetMethod(methodName, bindingFlags);
                if (methodInfo == null)
                    throw new MissingMethodException($"Method '{methodName}' not found in type '{type.FullName}'");
                return methodInfo;
            }

            var parameterTypes = parameters.Select(p => p?.GetType()).ToArray();
            var method = type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);

            if (method == null)
                throw new MissingMethodException($"Method '{methodName}' not found in type '{type.FullName}' with specified parameters");

            return method;
        }

        #endregion

        #region Property Access (supports private properties)

        /// <summary>
        /// Get instance property value (supports private properties)
        /// </summary>
        public static object GetPropertyValue<T>(T instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var propertyInfo = GetPropertyInfo<T>(propertyName);
            return propertyInfo.GetValue(instance);
        }

        /// <summary>
        /// Set instance property value (supports private properties)
        /// </summary>
        public static void SetPropertyValue<T>(T instance, string propertyName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var propertyInfo = GetPropertyInfo<T>(propertyName);
            propertyInfo.SetValue(instance, value);
        }

        /// <summary>
        /// Get static property value (supports private properties)
        /// </summary>
        public static object GetStaticPropertyValue<T>(string propertyName)
        {
            var propertyInfo = GetPropertyInfo<T>(propertyName, isStatic: true);
            return propertyInfo.GetValue(null);
        }

        /// <summary>
        /// Set static property value (supports private properties)
        /// </summary>
        public static void SetStaticPropertyValue<T>(string propertyName, object value)
        {
            var propertyInfo = GetPropertyInfo<T>(propertyName, isStatic: true);
            propertyInfo.SetValue(null, value);
        }

        private static PropertyInfo GetPropertyInfo<T>(string propertyName, bool isStatic = false)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;

            var type = typeof(T);
            var propertyInfo = type.GetProperty(propertyName, bindingFlags);

            if (propertyInfo == null)
                throw new MissingMemberException($"Property '{propertyName}' not found in type '{type.FullName}'");

            return propertyInfo;
        }

        #endregion

        #region Field Access (supports private fields)

        /// <summary>
        /// Get instance field value (supports private fields)
        /// </summary>
        public static object GetFieldValue<T>(T instance, string fieldName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var fieldInfo = GetFieldInfo<T>(fieldName);
            return fieldInfo.GetValue(instance);
        }

        /// <summary>
        /// Set instance field value (supports private fields)
        /// </summary>
        public static void SetFieldValue<T>(T instance, string fieldName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var fieldInfo = GetFieldInfo<T>(fieldName);
            fieldInfo.SetValue(instance, value);
        }

        /// <summary>
        /// Get static field value (supports private fields)
        /// </summary>
        public static object GetStaticFieldValue<T>(string fieldName)
        {
            var fieldInfo = GetFieldInfo<T>(fieldName, isStatic: true);
            return fieldInfo.GetValue(null);
        }

        /// <summary>
        /// Set static field value (supports private fields)
        /// </summary>
        public static void SetStaticFieldValue<T>(string fieldName, object value)
        {
            var fieldInfo = GetFieldInfo<T>(fieldName, isStatic: true);
            fieldInfo.SetValue(null, value);
        }

        private static FieldInfo GetFieldInfo<T>(string fieldName, bool isStatic = false)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
            bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;

            var type = typeof(T);
            var fieldInfo = type.GetField(fieldName, bindingFlags);

            if (fieldInfo == null)
                throw new MissingMemberException($"Field '{fieldName}' not found in type '{type.FullName}'");

            return fieldInfo;
        }

        #endregion

        #region Instance Creation (supports private constructors)

        /// <summary>
        /// Create instance (supports private constructors)
        /// </summary>
        public static T CreateInstance<T>(object[] parameters = null)
        {
            return (T)CreateInstance(typeof(T), parameters);
        }

        private static object CreateInstance(Type type, object[] parameters)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (parameters == null || parameters.Length == 0)
            {
                var ctor = type.GetConstructor(bindingFlags, null, Type.EmptyTypes, null);
                if (ctor == null)
                    throw new MissingMethodException($"Parameterless constructor not found in type '{type.FullName}'");

                return ctor.Invoke(Array.Empty<object>());
            }

            var parameterTypes = parameters.Select(p => p?.GetType()).ToArray();
            var constructor = type.GetConstructor(bindingFlags, null, parameterTypes, null);

            if (constructor == null)
                throw new MissingMethodException($"Constructor not found in type '{type.FullName}' with specified parameters");

            return constructor.Invoke(parameters);
        }

        #endregion

        #region Extension Methods (based on expressions)

        /// <summary>
        /// Invoke instance method through expression
        /// </summary>
        public static object InvokeMethod<T>(T instance, Expression<Action<T>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var methodInfo = methodCall.Method;
                var parameters = methodCall.Arguments.Select(GetExpressionValue).ToArray();
                return methodInfo.Invoke(instance, parameters);
            }
            throw new ArgumentException("Expression is not a method call", nameof(expression));
        }

        /// <summary>
        /// Get property value through expression
        /// </summary>
        public static TProp GetPropertyValue<T, TProp>(T instance, Expression<Func<T, TProp>> expression)
        {
            if (expression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                return (TProp)propertyInfo.GetValue(instance);
            }
            throw new ArgumentException("Expression is not a property access", nameof(expression));
        }

        /// <summary>
        /// Get field value through expression
        /// </summary>
        public static TField GetFieldValue<T, TField>(T instance, Expression<Func<T, TField>> expression)
        {
            if (expression.Body is MemberExpression memberExpression &&
                memberExpression.Member is FieldInfo fieldInfo)
            {
                return (TField)fieldInfo.GetValue(instance);
            }
            throw new ArgumentException("Expression is not a field access", nameof(expression));
        }

        private static object GetExpressionValue(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
                return constantExpression.Value;

            var lambda = Expression.Lambda(expression);
            return lambda.Compile().DynamicInvoke();
        }

        #endregion
    }
}