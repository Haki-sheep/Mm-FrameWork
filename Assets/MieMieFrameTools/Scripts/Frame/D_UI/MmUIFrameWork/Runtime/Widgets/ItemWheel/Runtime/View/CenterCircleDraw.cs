using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ItemWheel 中心圆 Graphic 绘制脚本
/// </summary>
public class CenterCircleDraw : MaskableGraphic
{
    [SerializeField] private float centerRadius = 50f;
    [SerializeField] private int circleSegments = 32;

    /// <summary>
    /// 初始化中心圆
    /// </summary>
    public void InitCenter(float radius, int segments)
    {
        centerRadius = Mathf.Max(0.001f, radius);
        circleSegments = Mathf.Max(8, segments);
        SetVerticesDirty();
    }

    /// <summary>
    /// 填充中心圆的网格
    /// </summary>
    /// <param name="vh"></param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (centerRadius <= 0f) return;

        float singleSegment = 2f * Mathf.PI / circleSegments;
        int vertexCount = circleSegments + 1;

        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        Vector2 center = Vector2.zero;
        vert.position = center;
        vh.AddVert(vert);

        for (int i = 0; i < vertexCount; i++)
        {
            float rad = i * singleSegment;
            Vector2 circlePos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * centerRadius;
            vert.position = circlePos;
            vh.AddVert(vert);
        }

        for (int i = 0; i < circleSegments; i++)
        {
            vh.AddTriangle(0, i + 1, i + 2);
        }
    }

}
