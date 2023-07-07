using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public class Papyrus : MonoBehaviour
{
    [SerializeField]
    private Renderer canvasRenderer = null;
    [SerializeField]
    private int resolution = 1000;

    private Material papyrusMaterial = null;
    private Collider papyrusCollider = null;

    private Texture2D canvasTexture = null;
    private Vector2 textureSize = Vector2.zero;
    private Color32[] pixelData = null;

    [ShowInInspector]
    private Color32 clearColor = Color.white;

    private void Awake()
    {
        SetupTexture();
    }

    [Button]
    private void SetupTexture()
    {
        papyrusMaterial = canvasRenderer.material;

        papyrusCollider = GetComponent<Collider>();
        Vector3 boxSize = papyrusCollider.bounds.size;
        float paperWidth = boxSize.x;
        float paperHeight = boxSize.z;

        Debug.Log($"PaperSize: {paperWidth} x {paperHeight}");

        int width = paperWidth >= paperHeight ? resolution : Mathf.RoundToInt(resolution * paperHeight / paperWidth);
        int height = paperWidth >= paperHeight ? Mathf.RoundToInt(resolution * paperHeight / paperWidth) : resolution;
        textureSize = new Vector2( width, height );

        canvasTexture = new Texture2D(width, height);
        pixelData = canvasTexture.GetPixels32();
        ClearTexture();

        papyrusMaterial.SetTexture("_Canvas", canvasTexture);

        Debug.Log($"Texture created with size {canvasTexture.width} x {canvasTexture.height}");
    }

    [Button]
    private void ClearTexture()
    {
        for (int i = 0; i < pixelData.Length; i++) { pixelData[i] = clearColor; }
        canvasTexture.SetPixels32(pixelData);
        canvasTexture.Apply();
    }

    [Button]
    public void DebugPaint(Vector3 point)
    {
        Paint(point, 2.5f, Color.black);
    }

    public void Paint(Vector3 point, float size, Color color)
    {
        Vector2 textureCenter = Transform3DPointTo2DPixel(point);
        PaintPoint(textureCenter, size, color);
    }

    private Vector2 Transform3DPointTo2DPixel(Vector3 point)
    {
        // TODO
        var bounds = papyrusCollider.bounds;
        Vector3 normal = -transform.forward;

        Vector3 center = bounds.center;
        Plane plane = new Plane(normal, center);
        Vector3 closestPoint = plane.ClosestPointOnPlane(point);
        Vector3 relativePoint = closestPoint - center;
        float relativeDistance = relativePoint.magnitude;

        Vector3 up = transform.up;
        float angle = Vector3.SignedAngle(up, relativePoint, normal);
        float rad = Mathf.Deg2Rad * angle;

        float centeredX = Mathf.Sin(rad) * relativeDistance;
        float centeredY = Mathf.Cos(rad) * relativeDistance;

        float fx = (centeredX + bounds.extents.x) / bounds.size.x;
        float fy = (centeredY + bounds.extents.z) / bounds.size.z;

        int x = Mathf.RoundToInt(Mathf.Clamp01(fx) * textureSize.x);
        int y = Mathf.RoundToInt(Mathf.Clamp01(fy) * textureSize.y);

        //Debug.Log($"Point {point}, closestPoint {closestPoint}, center {center}, relativePoint {relativePoint}, relative Distance {relativeDistance}");
        //Debug.Log($"CenteredX {centeredX}, centered y {centeredY}, bounds extents {bounds.extents}, bounds size {bounds.size}");
        //Debug.Log($"Up {up}, normal {normal}, angle {angle}, rad {rad}, fx {fx}, fy {fy} => pixel {x}, {y}");

        return new Vector2(x, y);
    }

    private void PaintPoint(Vector2 pixel, float size, Color color)
    {
        // TODO
        int width = (int)textureSize.x;
        int height = (int)textureSize.y;

        float hsize = size * 0.5f;
        int xstart = Mathf.FloorToInt(pixel.x - hsize);
        int xend = Mathf.CeilToInt(pixel.x + hsize);
        int ystart = Mathf.FloorToInt(pixel.y - hsize);
        int yend = Mathf.CeilToInt(pixel.y + hsize);

        for (int y = ystart; y < yend; ++y)
        {
            int index = y * width + xstart;
            float dy = y - pixel.y;
            float dy2 = dy * dy;

            for (int x = xstart; x < xend; ++x, ++index)
            {
                float dx = x - pixel.x;
                float d = Mathf.Sqrt(dx * dx + dy2);
                float f = Mathf.Clamp01(d / hsize);
                pixelData[index] = Color32.LerpUnclamped(color, pixelData[index], f);
            }
        }

        canvasTexture.SetPixels32(pixelData);
        canvasTexture.Apply();
    }
}
