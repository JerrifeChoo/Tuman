using UnityEngine;

namespace TT.Extensions
{
    public static class TransformExtensions
    {
        public static T Find<T>(this GameObject gameObject, string name) where T : Component
        {
            return gameObject.transform.Find<T>(name);
        }

        public static T Find<T>(this Transform transform, string name) where T : Component
        {
            if (transform.name == name)
            {
                var component = transform.GetComponent<T>();
                if (component != null)
                    return component;
            }
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var result = child.Find<T>(name);
                if(result != null)
                    return result;
            }
            return null;
        }

        public static void DestroyAllChild(this Transform transform)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static void DestroyImmediateAllChild(this Transform transform)
        {
            while (transform.childCount > 0)
            {
                Object.DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
    }
}
