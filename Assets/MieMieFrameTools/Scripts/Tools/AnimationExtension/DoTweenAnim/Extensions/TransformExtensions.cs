#if UNITY_EDITOR || !UNITY_CW
using UnityEngine;
using UnityEngine.UI;

namespace CWTools.Extensions
{
    public static class TransformExtensions
    {
        public static void SetPositionX(this Transform self, float x)
        {
            Vector3 pos = self.position;
            pos.x = x;
            self.position = pos;
        }

        public static void SetPositionY(this Transform self, float y)
        {
            Vector3 pos = self.position;
            pos.y = y;
            self.position = pos;
        }

        public static void SetPositionZ(this Transform self, float z)
        {
            Vector3 pos = self.position;
            pos.z = z;
            self.position = pos;
        }

        public static void SetLocalPositionX(this Transform self, float x)
        {
            Vector3 pos = self.localPosition;
            pos.x = x;
            self.localPosition = pos;
        }

        public static void SetLocalPositionY(this Transform self, float y)
        {
            Vector3 pos = self.localPosition;
            pos.y = y;
            self.localPosition = pos;
        }

        public static void SetLocalPositionZ(this Transform self, float z)
        {
            Vector3 pos = self.localPosition;
            pos.z = z;
            self.localPosition = pos;
        }

        public static void SetLocalScaleX(this Transform self, float x)
        {
            Vector3 scale = self.localScale;
            scale.x = x;
            self.localScale = scale;
        }

        public static void SetLocalScaleY(this Transform self, float y)
        {
            Vector3 scale = self.localScale;
            scale.y = y;
            self.localScale = scale;
        }

        public static void SetLocalScaleZ(this Transform self, float z)
        {
            Vector3 scale = self.localScale;
            scale.z = z;
            self.localScale = scale;
        }
    }

    public static class RectTransformExtensions
    {
        public static void SetAnchoredPositionX(this RectTransform self, float x)
        {
            Vector2 pos = self.anchoredPosition;
            pos.x = x;
            self.anchoredPosition = pos;
        }

        public static void SetAnchoredPositionY(this RectTransform self, float y)
        {
            Vector2 pos = self.anchoredPosition;
            pos.y = y;
            self.anchoredPosition = pos;
        }

        public static void SetAnchoredPosition3DZ(this RectTransform self, float z)
        {
            Vector3 pos = self.anchoredPosition3D;
            pos.z = z;
            self.anchoredPosition3D = pos;
        }
    }

    public static class GraphicExtensions
    {
        public static void SetColorAlpha(this Graphic self, float alpha)
        {
            Color color = self.color;
            color.a = alpha;
            self.color = color;
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3 ChangeX(this Vector3 self, float x)
        {
            self.x = x;
            return self;
        }

        public static Vector3 ChangeY(this Vector3 self, float y)
        {
            self.y = y;
            return self;
        }

        public static Vector3 ChangeZ(this Vector3 self, float z)
        {
            self.z = z;
            return self;
        }
    }

    public static class ColorExtensions
    {
        public static Color WithAlpha(this Color self, float alpha)
        {
            self.a = alpha;
            return self;
        }
    }

    public static class LayoutElementExtensions
    {
        public static Vector2 GetFlexibleSize(this LayoutElement self)
        {
            return new Vector2(self.flexibleWidth, self.flexibleHeight);
        }

        public static Vector2 GetMinSize(this LayoutElement self)
        {
            return new Vector2(self.minWidth, self.minHeight);
        }

        public static Vector2 GetPreferredSize(this LayoutElement self)
        {
            return new Vector2(self.preferredWidth, self.preferredHeight);
        }

        public static void SetFlexibleSize(this LayoutElement self, Vector2 size)
        {
            self.flexibleWidth = size.x;
            self.flexibleHeight = size.y;
        }

        public static void SetMinSize(this LayoutElement self, Vector2 size)
        {
            self.minWidth = size.x;
            self.minHeight = size.y;
        }

        public static void SetPreferredSize(this LayoutElement self, Vector2 size)
        {
            self.preferredWidth = size.x;
            self.preferredHeight = size.y;
        }
    }
}
#endif
