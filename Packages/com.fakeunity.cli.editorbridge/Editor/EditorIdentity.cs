#if UNITY_EDITOR
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeUnityCLI.EditorBridge
{
    internal static class EditorIdentity
    {
        /// <summary>
        /// 输入对象和字段名 返回可选身份 保持显式 JSON null 的缺省语义
        /// </summary>
        internal static JToken OptionalToken(JObject Target, string Name)
        {
            var Token = Target[Name];
            return Token == null || Token.Type == JTokenType.Null ? null : Token;
        }

        /// <summary>
        /// 输入对象 返回当前 Unity 版本的无截断身份标识
        /// </summary>
        internal static object InstanceId(Object Target)
        {
#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(Target.GetEntityId());
#else
            return Target.GetInstanceID();
#endif
        }

        /// <summary>
        /// 输入场景 返回当前 Unity 版本的无截断场景标识
        /// </summary>
        internal static object SceneId(Scene Target)
        {
#if UNITY_6000_4_OR_NEWER
            return Target.handle.GetRawData();
#else
            return Target.handle;
#endif
        }

        /// <summary>
        /// 输入对象标识 返回对应 Editor 对象
        /// </summary>
        internal static Object Resolve(JToken Token)
        {
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(EntityId.FromULong(Token.Value<ulong>()));
#else
            return EditorUtility.InstanceIDToObject(Token.Value<int>());
#endif
        }

        /// <summary>
        /// 输入原始身份与查询标识 返回身份是否一致
        /// </summary>
        internal static bool Matches(object Identity, JToken Token) =>
            System.Convert.ToString(Identity, CultureInfo.InvariantCulture) == Token.ToString();

        /// <summary>
        /// 输入对象引用属性 返回是否存在非空身份
        /// </summary>
        internal static bool HasReference(SerializedProperty Property)
        {
#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(Property.objectReferenceEntityIdValue) != 0;
#else
            return Property.objectReferenceInstanceIDValue != 0;
#endif
        }
    }
}
#endif
