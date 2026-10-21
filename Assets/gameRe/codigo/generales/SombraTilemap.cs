using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class SombraTilemap : MonoBehaviour
{
    [SerializeField] private bool sombrearInteriorMuro = false;

    private static readonly FieldInfo campoShapePath = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo campoShapePathHash = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

    void Reset()
    {
        ConfigurarComponentes();
    }

    void Awake()
    {
        CompositeCollider2D comp = ConfigurarComponentes();
        GenerarSombras(comp);
    }

    private CompositeCollider2D ConfigurarComponentes()
    {
        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.bodyType = RigidbodyType2D.Static;
        }

        if (TryGetComponent(out TilemapCollider2D tmCol))
        {
            tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        TryGetComponent(out CompositeCollider2D comp);
        if (comp != null)
        {
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            comp.GenerateGeometry();
        }

        return comp;
    }

    private void GenerarSombras(CompositeCollider2D comp)
    {
        if (comp == null) return;

        for (int i = 0; i < comp.pathCount; i++)
        {
            int numVertices = comp.GetPathPointCount(i);
            Vector2[] puntos2D = new Vector2[numVertices];
            comp.GetPath(i, puntos2D);

            Vector3[] puntos3D = new Vector3[numVertices];
            for (int j = 0; j < numVertices; j++)
            {
                puntos3D[j] = puntos2D[j];
            }

            GameObject hijo = new GameObject("SombraMuro_" + i);
            hijo.transform.SetParent(transform, false);

            ShadowCaster2D caster = hijo.AddComponent<ShadowCaster2D>();
            caster.castsShadows = true;
            caster.selfShadows = sombrearInteriorMuro;

            if (campoShapePath != null) campoShapePath.SetValue(caster, puntos3D);
            if (campoShapePathHash != null) campoShapePathHash.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));
        }
    }
}
