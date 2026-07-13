using System;
using System.Reflection;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Helper for reading private serialized fields of game components.
    /// Used only where the game exposes no public API (e.g. ContinueScreen).
    /// </summary>
    public static class ReflectionUtil
    {
        /// <summary>
        /// Find an instance field, walking base types: GetField on a derived type
        /// does not return private fields declared on a base class.
        /// </summary>
        private static FieldInfo FindField(object obj, string fieldName)
        {
            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }

        /// <summary>
        /// Read a private int field. Returns the fallback on any failure (never throws).
        /// </summary>
        public static int GetIntField(object obj, string fieldName, int fallback)
        {
            if (obj == null) return fallback;
            try
            {
                FieldInfo field = FindField(obj, fieldName);
                if (field != null && field.FieldType == typeof(int))
                    return (int)field.GetValue(obj);
            }
            catch
            {
                // fall through to fallback
            }
            return fallback;
        }

        /// <summary>
        /// Read a private bool field. Returns the fallback on any failure (never throws).
        /// </summary>
        public static bool GetBoolField(object obj, string fieldName, bool fallback)
        {
            if (obj == null) return fallback;
            try
            {
                FieldInfo field = FindField(obj, fieldName);
                if (field != null && field.FieldType == typeof(bool))
                    return (bool)field.GetValue(obj);
            }
            catch
            {
                // fall through to fallback
            }
            return fallback;
        }

        /// <summary>
        /// Invoke a private/protected parameterless instance method.
        /// Returns true if the call was made without throwing.
        /// </summary>
        public static bool InvokeMethod(object obj, string methodName)
        {
            if (obj == null) return false;
            try
            {
                MethodInfo method = obj.GetType().GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                if (method == null) return false;
                method.Invoke(obj, null);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "ReflectionUtil",
                    $"Failed to invoke {methodName} on {obj.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Write a private instance field. Returns true on success (never throws).
        /// </summary>
        public static bool SetField(object obj, string fieldName, object value)
        {
            if (obj == null) return false;
            try
            {
                FieldInfo field = FindField(obj, fieldName);
                if (field == null) return false;
                field.SetValue(obj, value);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "ReflectionUtil",
                    $"Failed to set field {fieldName} on {obj.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read a private instance field. Returns null on any failure (never throws).
        /// </summary>
        public static T GetField<T>(object obj, string fieldName) where T : class
        {
            if (obj == null) return null;
            try
            {
                FieldInfo field = FindField(obj, fieldName);
                return field?.GetValue(obj) as T;
            }
            catch (Exception ex)
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "ReflectionUtil",
                    $"Failed to read field {fieldName} from {obj.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}
