using System;
using UnityEngine;

public static class PhysicRayCast
{
    #region 静态缓冲区

    // 3D射线检测缓冲区
    private static RaycastHit[] s_SingleRaycastBuffer = new RaycastHit[1];

    // 2D射线检测缓冲区
    private static RaycastHit2D[] s_SingleRaycastBuffer2D = new RaycastHit2D[1];

    #endregion

    #region 3D 物理检测

    /// <summary>
    /// 无分配版本的盒子碰撞检测
    /// </summary>
    /// <param name="center">盒子中心点</param>
    /// <param name="halfExtents">盒子半尺寸 (x=宽度/2, y=高度/2, z=深度/2)</param>
    /// <param name="direction">检测方向</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layer">检测层级</param>
    /// <param name="queryTriggerInteraction">触发器交互模式</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的对象数量</returns>
    public static int BoxCastNonAlloc(
        Vector3 center,
        Vector3 halfExtents,
        Vector3 direction,
        RaycastHit[] hits,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        int hitCount = Physics.BoxCastNonAlloc(
            center, halfExtents, direction, hits, Quaternion.identity, distance, layer, queryTriggerInteraction);

        if (debugDraw)
        {
            // 绘制检测方向线
            Debug.DrawRay(center, direction * distance, hitCount > 0 ? Color.green : Color.red);

            // 绘制盒子轮廓
            DrawBoxWireframe(center, halfExtents, hitCount > 0 ? Color.green : Color.red);
        }

        return hitCount;
    }

    /// <summary>
    /// 无分配版本的球形碰撞检测
    /// </summary>
    /// <param name="origin">球形起点</param>
    /// <param name="radius">球形半径</param>
    /// <param name="direction">检测方向</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layer">检测层级</param>
    /// <param name="queryTriggerInteraction">触发器交互模式</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的对象数量</returns>
    public static int SphereCastNonAlloc(
        Vector3 origin,
        float radius,
        Vector3 direction,
        RaycastHit[] hits,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin, radius, direction, hits, distance, layer, queryTriggerInteraction);

        if (debugDraw)
        {
            // 绘制检测方向线
            Debug.DrawRay(origin, direction * distance, hitCount > 0 ? Color.green : Color.red);

            // 绘制球形轮廓
            DrawSphereWireframe(origin, radius, hitCount > 0 ? Color.green : Color.red);
        }

        return hitCount;
    }

    /// <summary>
    /// 无分配版本的单条射线检测
    /// </summary>
    /// <param name="origin">射线起点</param>
    /// <param name="direction">射线方向</param>
    /// <param name="hit">命中结果</param>
    /// <param name="distance">射线长度</param>
    /// <param name="layer">检测层级</param>
    /// <param name="queryTriggerInteraction">触发器交互模式</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>是否命中</returns>
    public static bool SingleRaycast(
        Vector3 origin,
        Vector3 direction,
        out RaycastHit hit,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        bool hitFound = Physics.Raycast(origin, direction, out hit, distance, layer, queryTriggerInteraction);

        if (debugDraw)
        {
            Color color = hitFound ? Color.green : Color.red;
            Debug.DrawLine(origin, origin + direction * distance, color);
            // if (hitFound)
            // {
            //     // 绘制命中点和法线
            //     Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.blue);
            // }
        }

        return hitFound;
    }

    /// <summary>
    /// 无分配版本的多射线检测
    /// </summary>
    /// <param name="origins">射线起点数组</param>
    /// <param name="directions">射线方向数组</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">射线长度</param>
    /// <param name="layer">检测层级</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>(hitFound:是否有命中, hitCount:命中数量)</returns>
    public static (bool hitFound, int hitCount) MultiRaycastNoAlloc(
        Vector3[] origins,
        Vector3[] directions,
        RaycastHit[] hits,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        Debug.Assert(hits.Length >= origins.Length && hits.Length >= directions.Length, "hits缓冲区长度必须 >= 射线数量");

        bool hitFound = false;
        int hitCount = 0;

        for (int i = 0; i < origins.Length; i++)
        {
            // 使用静态缓冲区进行无分配检测
            int currentHitCount = Physics.RaycastNonAlloc(origins[i], directions[i], s_SingleRaycastBuffer, distance, layer, queryTriggerInteraction);
            bool currentHit = currentHitCount > 0;

            if (currentHit)
            {
                hits[i] = s_SingleRaycastBuffer[0];  // 将结果复制到输出缓冲区
                hitFound = true;
                hitCount++;
            }

            // 调试绘制
            if (debugDraw)
            {
                Color color = currentHit ? Color.green : Color.red;
                Debug.DrawLine(origins[i], origins[i] + directions[i] * distance, color);
                // if (currentHit)
                // {
                //     // 绘制命中点和法线
                //     Debug.DrawLine(hits[i].point, hits[i].point + hits[i].normal * 0.5f, Color.blue);
                // }
            }
        }

        return (hitFound, hitCount);
    }

    /// <summary>
    /// 便捷方法：三条射线检测（左、中、右）
    /// </summary>
    public static (bool hitFound, int hitCount) ThreeRaycastNoAlloc(
        Vector3 origin,
        float spacing,
        Transform transform,
        RaycastHit[] hits,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        Vector3[] origins = new Vector3[3];
        origins[0] = origin;                           // 中心
        origins[1] = origin - transform.right * spacing; // 左
        origins[2] = origin + transform.right * spacing; // 右

        Vector3[] directions = new Vector3[3];
        directions[0] = Vector3.down;  // 中心
        directions[1] = Vector3.down;  // 左
        directions[2] = Vector3.down;  // 右

        return MultiRaycastNoAlloc(origins, directions, hits, distance, layer, queryTriggerInteraction, debugDraw);
    }

    /// <summary>
    /// 便捷方法：五条射线检测（左、左中、中、右中、右）
    /// </summary>
    public static (bool hitFound, int hitCount) FiveRaycastNoAlloc(
        Vector3 origin,
        float spacing,
        Transform transform,
        RaycastHit[] hits,
        float distance,
        LayerMask layer,
        QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore,
        bool debugDraw = false)
    {
        Vector3[] origins = new Vector3[5];
        origins[0] = origin - transform.right * spacing;       // 左
        origins[1] = origin - transform.right * spacing * 0.5f; // 左中
        origins[2] = origin;                                   // 中
        origins[3] = origin + transform.right * spacing * 0.5f; // 右中
        origins[4] = origin + transform.right * spacing;        // 右

        Vector3[] directions = new Vector3[5];
        for (int i = 0; i < 5; i++)
        {
            directions[i] = Vector3.down;
        }

        return MultiRaycastNoAlloc(origins, directions, hits, distance, layer, queryTriggerInteraction, debugDraw);
    }

    #endregion

    #region 3D 调试绘制辅助方法

    /// <summary>
    /// 绘制盒子线框
    /// </summary>
    private static void DrawBoxWireframe(Vector3 center, Vector3 halfExtents, Color color)
    {
        Vector3[] corners = new Vector3[8];

        // 计算8个顶点
        corners[0] = center + new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[1] = center + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[2] = center + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[3] = center + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[4] = center + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[5] = center + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[6] = center + new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);
        corners[7] = center + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);

        // 绘制12条边
        int[] edges = {
            0,1, 1,2, 2,3, 3,0,  // 底面
            4,5, 5,6, 6,7, 7,4,  // 顶面
            0,4, 1,5, 2,6, 3,7   // 垂直边
        };

        for (int i = 0; i < edges.Length; i += 2)
        {
            Debug.DrawLine(corners[edges[i]], corners[edges[i + 1]], color);
        }
    }

    /// <summary>
    /// 绘制球形线框
    /// </summary>
    private static void DrawSphereWireframe(Vector3 center, float radius, Color color)
    {
        int segments = 16;
        float angleStep = 360f / segments;

        // 绘制XY平面的圆
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            Debug.DrawLine(p1, p2, color);
        }

        // 绘制XZ平面的圆
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color);
        }

        // 绘制YZ平面的圆
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color);
        }
    }


    #endregion

    #region 2D 物理检测

    /// <summary>
    /// 无分配版本的盒子碰撞检测 (2D)
    /// </summary>
    /// <param name="center">盒子中心点</param>
    /// <param name="size">盒子尺寸</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="direction">检测方向</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layer">检测层级</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的对象数量</returns>
    public static int BoxCastNonAlloc2D(
        Vector2 center,
        Vector2 size,
        float angle,
        Vector2 direction,
        RaycastHit2D[] hits,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        // 使用非过时的 BoxCast 方法，返回 ContactFilter2D 的结果
        var contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(layer);
        contactFilter.useTriggers = false;

        int hitCount = Physics2D.BoxCast(center, size, angle, direction, contactFilter, hits, distance);

        if (debugDraw)
        {
            // 绘制检测方向线
            Debug.DrawRay(center, direction * distance, hitCount > 0 ? Color.green : Color.red);

            // 绘制盒子轮廓
            DrawBoxWireframe2D(center, size, angle, hitCount > 0 ? Color.green : Color.red);
        }

        return hitCount;
    }

    /// <summary>
    /// 无分配版本的圆形碰撞检测 (2D)
    /// </summary>
    /// <param name="origin">圆形起点</param>
    /// <param name="radius">圆形半径</param>
    /// <param name="direction">检测方向</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layer">检测层级</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的对象数量</returns>
    public static int CircleCastNonAlloc2D(
        Vector2 origin,
        float radius,
        Vector2 direction,
        RaycastHit2D[] hits,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        // 使用非过时的 CircleCast 方法
        var contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(layer);
        contactFilter.useTriggers = false;

        int hitCount = Physics2D.CircleCast(origin, radius, direction, contactFilter, hits, distance);

        if (debugDraw)
        {
            // 绘制检测方向线
            Debug.DrawRay(origin, direction * distance, hitCount > 0 ? Color.green : Color.red);

            // 绘制圆形轮廓
            DrawCircleWireframe2D(origin, radius, hitCount > 0 ? Color.green : Color.red);
        }

        return hitCount;
    }

    /// <summary>
    /// 无分配版本的单条射线检测 (2D)
    /// </summary>
    /// <param name="origin">射线起点</param>
    /// <param name="direction">射线方向</param>
    /// <param name="hit">命中结果</param>
    /// <param name="distance">射线长度</param>
    /// <param name="layer">检测层级</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>是否命中</returns>
    public static bool SingleRaycast2D(
        Vector2 origin,
        Vector2 direction,
        out RaycastHit2D hit,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        hit = Physics2D.Raycast(origin, direction, distance, layer);
        bool hitFound = hit.collider != null;

        if (debugDraw)
        {
            Color color = hitFound ? Color.green : Color.red;
            Debug.DrawLine(origin, origin + direction * distance, color);
            // if (hitFound)
            // {
            //     // 绘制命中点和法线
            //     Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.blue);
            // }
        }

        return hitFound;
    }

    /// <summary>
    /// 无分配版本的多射线检测 (2D)
    /// </summary>
    /// <param name="origins">射线起点数组</param>
    /// <param name="directions">射线方向数组</param>
    /// <param name="hits">接收结果的缓冲区</param>
    /// <param name="distance">射线长度</param>
    /// <param name="layer">检测层级</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>(hitFound:是否有命中, hitCount:命中数量)</returns>
    public static (bool hitFound, int hitCount) MultiRaycastNoAlloc2D(
        Vector2[] origins,
        Vector2[] directions,
        RaycastHit2D[] hits,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        Debug.Assert(hits.Length >= origins.Length && hits.Length >= directions.Length, "hits缓冲区长度必须 >= 射线数量");

        bool hitFound = false;
        int hitCount = 0;

        for (int i = 0; i < origins.Length; i++)
        {
            hits[i] = Physics2D.Raycast(origins[i], directions[i], distance, layer);
            bool currentHit = hits[i].collider != null;

            if (currentHit)
            {
                hitFound = true;
                hitCount++;
            }

            // 调试绘制
            if (debugDraw)
            {
                Color color = currentHit ? Color.green : Color.red;
                Debug.DrawLine(origins[i], origins[i] + directions[i] * distance, color);
                // if (currentHit)
                // {
                //     // 绘制命中点和法线
                //     Debug.DrawLine(hits[i].point, hits[i].point + hits[i].normal * 0.5f, Color.blue);
                // }
            }
        }

        return (hitFound, hitCount);
    }

    /// <summary>
    /// 便捷方法：三条射线检测 (2D)（左、中、右）
    /// </summary>
    public static (bool hitFound, int hitCount) ThreeRaycastNoAlloc2D(
        Vector2 origin,
        float spacing,
        Transform transform,
        RaycastHit2D[] hits,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        Vector2[] origins = new Vector2[3];
        origins[0] = origin;                           // 中心
        origins[1] = origin - (Vector2)transform.right * spacing; // 左
        origins[2] = origin + (Vector2)transform.right * spacing; // 右

        Vector2[] directions = new Vector2[3];
        directions[0] = Vector2.down;  // 中心
        directions[1] = Vector2.down;  // 左
        directions[2] = Vector2.down;  // 右

        return MultiRaycastNoAlloc2D(origins, directions, hits, distance, layer, debugDraw);
    }

    /// <summary>
    /// 便捷方法：五条射线检测 (2D)（左、左中、中、右中、右）
    /// </summary>
    public static (bool hitFound, int hitCount) FiveRaycastNoAlloc2D(
        Vector2 origin,
        float spacing,
        Transform transform,
        RaycastHit2D[] hits,
        float distance,
        LayerMask layer,
        bool debugDraw = false)
    {
        Vector2[] origins = new Vector2[5];
        origins[0] = origin - (Vector2)transform.right * spacing;       // 左
        origins[1] = origin - (Vector2)transform.right * spacing * 0.5f; // 左中
        origins[2] = origin;                                           // 中
        origins[3] = origin + (Vector2)transform.right * spacing * 0.5f; // 右中
        origins[4] = origin + (Vector2)transform.right * spacing;        // 右

        Vector2[] directions = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            directions[i] = Vector2.down;
        }

        return MultiRaycastNoAlloc2D(origins, directions, hits, distance, layer, debugDraw);
    }


    /// <summary>
    /// 2D 盒装重叠检测（带可选调试绘制）
    /// </summary>
    /// <param name="point">中心点</param>
    /// <param name="size">胶囊尺寸（宽，高）</param>
    /// <param name="direction">胶囊方向</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="layerMask">图层掩码</param>
    /// <param name="minDepth">最小深度</param>
    /// <param name="maxDepth">最大深度</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的 Collider2D（第一个）或者 null</returns>
    /// <returns></returns>
    public static Collider2D OverlapBox2D(Vector2 point, Vector2 size, float angle, LayerMask layerMask,
                                            float minDepth = -Mathf.Infinity, float maxDepth = Mathf.Infinity,
                                            bool debugDraw = false)
    {
        Collider2D col = Physics2D.OverlapBox(
             point,
             size,
             angle,
             layerMask,
             minDepth,
             maxDepth
         );


        if (debugDraw)
        {
            Color color = col != null ? Color.green : Color.red;
            DrawBoxWireframe2D(point, size, angle, color);
        }

        return col;
    }

    /// <summary>
    /// 2D 胶囊重叠检测（带可选调试绘制）
    /// </summary>
    /// <param name="point">中心点</param>
    /// <param name="size">胶囊尺寸（宽，高）</param>
    /// <param name="direction">胶囊方向</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="layerMask">图层掩码</param>
    /// <param name="minDepth">最小深度</param>
    /// <param name="maxDepth">最大深度</param>
    /// <param name="debugDraw">是否绘制调试线</param>
    /// <returns>命中的 Collider2D（第一个）或者 null</returns>
    public static Collider2D OverlapCapsule2D(Vector2 point, Vector2 size, CapsuleDirection2D direction, float angle, LayerMask layerMask,
        float minDepth = -Mathf.Infinity, float maxDepth = Mathf.Infinity, bool debugDraw = false)
    {
        Collider2D col = Physics2D.OverlapCapsule(point, size, direction, angle, layerMask, minDepth, maxDepth);

        if (debugDraw)
        {
            Color color = col != null ? Color.green : Color.red;
            DrawCapsuleWireframe2D(point, size, direction, angle, color);
        }

        return col;
    }

    #endregion

    #region 2D 调试绘制辅助方法

    /// <summary>
    /// 绘制盒子线框 (2D)
    /// </summary>
    private static void DrawBoxWireframe2D(Vector2 center, Vector2 size, float angle, Color color)
    {
        Vector3 center3D = center;
        // DrawBoxWireframe expects halfExtents; convert full size to half extents
        Vector3 halfExtents3D = new Vector3(size.x * 0.5f, size.y * 0.5f, 0.01f);
        DrawBoxWireframe(center3D, halfExtents3D, color);
    }

    /// <summary>
    /// 绘制圆形线框 (2D)
    /// </summary>
    private static void DrawCircleWireframe2D(Vector2 center, float radius, Color color)
    {
        int segments = 16;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(p1, p2, color);
        }
    }

    /// <summary>
    /// 绘制胶囊线框 (2D)；用矩形加两端圆来近似表示
    /// </summary>
    private static void DrawCapsuleWireframe2D(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle, Color color)
    {
        // 计算胶囊半径和中间矩形长度
        float radius;
        float length;
        if (direction == CapsuleDirection2D.Vertical)
        {
            radius = size.x * 0.5f;
            length = Mathf.Max(0f, size.y - 2f * radius);
        }
        else
        {
            radius = size.y * 0.5f;
            length = Mathf.Max(0f, size.x - 2f * radius);
        }

        // 旋转矩阵
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        // 中间矩形尺寸
        Vector2 rectSize = direction == CapsuleDirection2D.Vertical
            ? new Vector2(size.x - 2f * 0f, length)
            : new Vector2(length, size.y - 2f * 0f);

        // 计算两个半圆心在本地空间（未旋转）
        Vector3 offsetLocal = direction == CapsuleDirection2D.Vertical ? new Vector3(0, length * 0.5f, 0) : new Vector3(length * 0.5f, 0, 0);

        Vector3 topCenter3 = rot * offsetLocal + (Vector3)center;
        Vector3 bottomCenter3 = rot * -offsetLocal + (Vector3)center;

        // 绘制中间矩形（使用3D盒线框函数）
        Vector3 rectCenter3 = (topCenter3 + bottomCenter3) * 0.5f;
        Vector3 rectHalfExtents3 = direction == CapsuleDirection2D.Vertical
            ? new Vector3(rectSize.x * 0.5f, rectSize.y * 0.5f, 0.01f)
            : new Vector3(rectSize.x * 0.5f, rectSize.y * 0.5f, 0.01f);

        DrawBoxWireframe(rectCenter3, rectHalfExtents3, color);

        // 绘制两端圆形（在旋转后的位置绘制圆）
        DrawCircleWireframe2D((Vector2)topCenter3, radius, color);
        DrawCircleWireframe2D((Vector2)bottomCenter3, radius, color);
    }

    #endregion
}
