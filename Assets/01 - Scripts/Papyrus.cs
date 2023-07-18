using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using System;

public class Papyrus : GrabbableObject, IFlammable
{
    public enum PaintMethod
    {
        Point,
        OptimizedPoint,
        Brush
    }

    [SerializeField]
    private Renderer canvasRenderer = null;
    [SerializeField]
    private Transform spellAnchorPoint = null;
    [SerializeField]
    private Color availableColor = Color.white;
    [SerializeField]
    private Color consumedColor = Color.grey;
    [SerializeField]
    private Color burnedColor = Color.black;

    [SerializeField]
    private int resolution = 1000;
    [SerializeField]
    private PaintMethod paintMethod = PaintMethod.Point;
    [SerializeField]
    private bool allowConnectedDots = false;
    [SerializeField]
    private Texture2D brush = null;
    [SerializeField]
    private float brushDispersion = 1.0f;

    public UnityEvent<Papyrus> OnConsumed = new UnityEvent<Papyrus>();
    public UnityEvent<Papyrus> OnBurned = new UnityEvent<Papyrus>();
    public UnityEvent<Papyrus> OnDestroyed = new UnityEvent<Papyrus>();

    private Material papyrusMaterial = null;
    private Collider papyrusCollider = null;

    private Texture2D canvasTexture = null;
    private Vector2 textureSize = Vector2.zero;
    private Color32[] pixelData = null;
    private Texture2D transparentTexture = null;

    private Color32[] brushData = null;

    [ShowInInspector]
    public bool IsConsumed { get; private set; } = false;
    [ShowInInspector]
    private Color32 clearColor = new Color32(255, 255, 255, 0);
    [ShowInInspector, ReadOnly]
    private float pixelDensity = 1f;
    [ShowInInspector, ReadOnly]
    public bool isBurned { get; private set; } = false;

    public Transform GetSpellAnchorPoint() => spellAnchorPoint;

    private void Awake()
    {
        SetupBrush();
        SetupTexture(resolution);

        transparentTexture = new Texture2D(1, 1);
        transparentTexture.SetPixel(0, 0, Color.clear);
        transparentTexture.Apply();
        RemoveHelper();
    }

    [Button]
    private void SetupBrush()
    {
        brushData = brush.GetPixels32();
    }

    [Button]
    public void SetupTexture(int resolution)
    {
        papyrusMaterial = canvasRenderer.material;

        papyrusCollider = GetComponent<Collider>();
        Vector3 boxSize = papyrusCollider.bounds.size;
        float paperWidth = boxSize.x;
        float paperHeight = boxSize.z;

        Debug.Log($"PaperSize: {paperWidth} x {paperHeight}");

        int width = paperWidth >= paperHeight ? resolution : Mathf.RoundToInt(resolution * paperHeight / paperWidth);
        int height = paperWidth >= paperHeight ? Mathf.RoundToInt(resolution * paperHeight / paperWidth) : resolution;
        textureSize = new Vector2(width, height);

        pixelDensity = width / paperWidth;

        canvasTexture = new Texture2D(width, height);
        pixelData = canvasTexture.GetPixels32();
        ClearTexture();

        papyrusMaterial.SetColor("_PaperColor", availableColor);
        papyrusMaterial.SetTexture("_Canvas", canvasTexture);

        Debug.Log($"Texture created with size {canvasTexture.width} x {canvasTexture.height}");
    }

    [Button]
    public void ClearTexture()
    {
        for (int i = 0; i < pixelData.Length; i++) { pixelData[i] = clearColor; }
        ApplyChangesToTexture();
    }

    public void ApplyTemplate(Texture2D template)
    {
        var templateData = template.GetPixels32();
        for (int i = 0; i < templateData.Length; ++i)
        {
            if (templateData[i].a > 0)
            {
                pixelData[i] = templateData[i];
            }
        }

        ApplyChangesToTexture();
    }

    public void ChangePaintMethod(PaintMethod method)
    {
        paintMethod = method;
    }

    public bool ToggleConnected()
    {
        allowConnectedDots = !allowConnectedDots;
        return allowConnectedDots;
    }

    [Button]
    public void DebugPaint(Vector3 point)
    {
        Paint(point, 2.5f, Color.black, false);
    }

    private Vector2 prevTexturePoint = Vector2.one * Mathf.Infinity;
    private float prevPointSize = 0f;

    public void Paint(Vector3 point, float size, Color32 color, bool connected)
    {
        Vector2 texturePoint = Transform3DPointTo2DPixel(point);
        size *= pixelDensity;

        if (texturePoint == prevTexturePoint
            && size == prevPointSize)
        {
            return;
        }

        connected &= allowConnectedDots;
        //var stopWatch = System.Diagnostics.Stopwatch.StartNew();

        // Paint with circles and rectangles
        if (paintMethod == PaintMethod.Point)
        {
            PaintPoint(texturePoint, size, color);
            if (connected)
            {
                PaintLineBetweenPoints(prevTexturePoint, texturePoint, color, size);
            }
        }
        else if (paintMethod == PaintMethod.OptimizedPoint)
        {
            PaintPoint_Optimized(texturePoint, size, color);
            if (connected)
            {
                PaintLineBetweenPoints(prevTexturePoint, texturePoint, color, size);
            }
        }
        else
        {
            // Paint with brushes
            if (connected)
            {
                DrawBrushBetweenPoints(prevTexturePoint, texturePoint, size, color);
            }
            else
            {
                DrawBrush(texturePoint, size, color);
            }
        }

        prevTexturePoint = texturePoint;
        prevPointSize = size;

        ApplyChangesToTexture();

        //stopWatch.Stop();
        //long dt = stopWatch.ElapsedMilliseconds;

        //Debug.Log($"dt {dt}");

        //totalTime += dt;
        //totalTimes++;
        //averageTime = totalTime / (float)totalTimes;
    }

    private Vector2 Transform3DPointTo2DPixel(Vector3 point)
    {
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

    private void ApplyChangesToTexture()
    {
        canvasTexture.SetPixels32(pixelData);
        canvasTexture.Apply();
    }

    private void PaintPoint(Vector2 pointCenter, float size, Color32 color)
    {
        int width = (int)textureSize.x;
        int height = (int)textureSize.y;

        float hsize = (size - 1f) * 0.5f;
        float hsizeMax = hsize + 1f;

        int xstart = Mathf.Clamp(Mathf.FloorToInt(pointCenter.x - hsizeMax), 0, width - 1);
        int xend = Mathf.Clamp(Mathf.CeilToInt(pointCenter.x + hsizeMax), 0, width - 1);
        int ystart = Mathf.Clamp(Mathf.FloorToInt(pointCenter.y - hsizeMax), 0, height - 1);
        int yend = Mathf.Clamp(Mathf.CeilToInt(pointCenter.y + hsizeMax), 0, height - 1);

        for (int y = ystart; y <= yend; ++y)
        {
            int index = y * width + xstart;
            float dy = Mathf.Abs(y - pointCenter.y) + 0.5f;
            float dy2 = dy * dy;

            for (int x = xstart; x <= xend; ++x, ++index)
            {
                float dx = Mathf.Abs(x - pointCenter.x) + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy2);

                if (d <= hsize)
                {
                    pixelData[index] = color;
                }
                else if (d <= hsizeMax)
                {
                    float f = hsizeMax - d;
                    PaintTranslucidPixel(index, color, f);
                }
            }
        }
    }

    private void PaintPoint_Optimized(Vector2 pointCenter, float size, Color32 color)
    {
        int width = (int)textureSize.x;
        int height = (int)textureSize.y;

        float hsize = (size - 1f) * 0.5f;
        float hsizeMax = hsize + 1f;

        int ystart = Mathf.Clamp(Mathf.FloorToInt(pointCenter.y - hsizeMax), 0, height - 1);
        int yend = Mathf.Clamp(Mathf.CeilToInt(pointCenter.y + hsizeMax), 0, height - 1);

        float h2 = hsize * hsize;
        float hMax2 = hsizeMax * hsizeMax;

        for (int y = ystart; y <= yend; ++y)
        {
            float dy = Mathf.Abs(y - pointCenter.y) + 0.5f;
            float dy2 = dy * dy;

            if (dy2 <= hMax2)
            {
                float fx1 = Mathf.Sqrt(hMax2 - dy2);
                int xstart1 = Mathf.Clamp(Mathf.FloorToInt(pointCenter.x - fx1), 0, width - 1);
                int xend1 = Mathf.Clamp(Mathf.CeilToInt(pointCenter.x + fx1), 0, width - 1);

                int index = y * width + xstart1;

                if (dy2 <= h2)
                {
                    float fx2 = Mathf.Sqrt(h2 - dy2);
                    int xstart2 = Mathf.Clamp(Mathf.FloorToInt(pointCenter.x - fx2), 0, width - 1);
                    int xend2 = Mathf.Clamp(Mathf.CeilToInt(pointCenter.x + fx2), 0, width - 1);

                    for (int x = xstart1; x <= xstart2; ++x, ++index)
                    {
                        float dx = Mathf.Abs(x - pointCenter.x) + 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy2);

                        float f = hsizeMax - d;
                        PaintTranslucidPixel(index, color, f);
                    }

                    for (int x = xstart2 + 1; x < xend2; ++x, ++index)
                    {
                        PaintPixel(index, color);
                    }

                    for (int x = xend2; x <= xend1; ++x, ++index)
                    {
                        float dx = Mathf.Abs(x - pointCenter.x) + 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy2);

                        float f = hsizeMax - d;
                        PaintTranslucidPixel(index, color, f);
                    }
                }
                else
                {
                    for (int x = xstart1; x <= xend1; ++x, ++index)
                    {
                        float dx = Mathf.Abs(x - pointCenter.x) + 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy2);

                        float f = hsizeMax - d;
                        PaintTranslucidPixel(index, color, f);
                    }
                }
            }
        }
    }

    private void PaintLineBetweenPoints(Vector2 prevPoint, Vector2 nextPoint, Color32 color, float size)
    {
        if (prevPoint == nextPoint)
        {
            return;
        }

        Vector2 first;
        Vector2 second;
        float hsize = size * 0.5f;

        if (prevPoint.x < nextPoint.x
            || (prevPoint.x == nextPoint.x
                && prevPoint.y < nextPoint.y))
        {
            first = prevPoint;
            second = nextPoint;
        }
        else
        {
            first = nextPoint;
            second = prevPoint;
        }

        int width = canvasTexture.width;
        int height = canvasTexture.height;

        if (first.x == second.x)
        {
            //Debug.Log($"Draw Vertical Line");

            int starty = Mathf.Clamp(Mathf.FloorToInt(first.y), 0, height - 1);
            int endy = Mathf.Clamp(Mathf.RoundToInt(second.y), 0, height - 1);
            int startx = Mathf.Clamp(Mathf.RoundToInt(first.x - hsize), 0, width - 1);
            int endx = Mathf.Clamp(Mathf.RoundToInt(first.x + hsize), 0, width - 1);

            for (int y = starty; y <= endy; ++y)
            {
                int index = y * width + startx;
                for (int x = startx; x <= endx; ++x, ++index)
                {
                    //pixelData[index] = color;
                    PaintPixel(index, color);
                }
            }
        }
        else if (first.y == second.y)
        {
            //Debug.Log($"Draw Horizontal Line");

            int starty = Mathf.Clamp(Mathf.FloorToInt(first.y - hsize), 0, height - 1);
            int endy = Mathf.Clamp(Mathf.RoundToInt(first.y + hsize), 0, height - 1);
            int startx = Mathf.Clamp(Mathf.RoundToInt(first.x), 0, width - 1);
            int endx = Mathf.Clamp(Mathf.RoundToInt(second.x), 0, width - 1);

            for (int y = starty; y <= endy; ++y)
            {
                int index = y * width + startx;
                for (int x = startx; x <= endx; ++x, ++index)
                {
                    //pixelData[index] = color;
                    PaintPixel(index, color);
                }
            }
        }
        else
        {
            //Debug.Log($"Draw Diagonal Line");

            Vector2 dir = (second - first).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            float slope = (second.y - first.y) / (second.x - first.x);

            Vector2 firstA = first + normal * hsize;
            Vector2 firstB = first - normal * hsize;
            Vector2 secondA = second + normal * hsize;
            Vector2 secondB = second - normal * hsize;

            if (slope > 0f)
            {
                float slopeNormal = (firstB.y - firstA.y) / (firstB.x - firstA.x);

                float firstABOffset = firstA.y - firstA.x * slopeNormal;
                float secondABOffset = secondA.y - secondA.x * slopeNormal;
                float firstSecondAOffset = firstA.y - firstA.x * slope;
                float firstSecondBOffset = firstB.y - firstB.x * slope;

                int y1 = Mathf.Clamp(Mathf.FloorToInt(firstB.y), 0, height - 1);
                int y2 = Mathf.Clamp(Mathf.FloorToInt(firstA.y), 0, height - 1);
                int y3 = Mathf.Clamp(Mathf.CeilToInt(secondB.y), 0, height - 1);
                int y4 = Mathf.Clamp(Mathf.CeilToInt(secondA.y), 0, height - 1);
                //int startx = Mathf.Clamp(Mathf.RoundToInt(firstB.x), 0, width - 1);
                //int endx = Mathf.Clamp(Mathf.RoundToInt(secondA.x), 0, width - 1);

                int firstEndy = Mathf.Min(y2, y3);
                for (int y = y1; y < firstEndy; y++)
                {
                    float x1 = (y - firstABOffset) / slopeNormal;
                    float x2 = (y - firstSecondBOffset) / slope;
                    int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                    int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                    int index = y * width + startx;
                    for (int x = startx; x < endx; x++, ++index)
                    {
                        pixelData[index] = color;
                    }
                }

                int secondEndy = Mathf.Max(y2, y3);
                if (y2 < y3)
                {
                    for (int y = firstEndy; y < secondEndy; y++)
                    {
                        float x1 = (y - firstSecondAOffset) / slope;
                        float x2 = (y - firstSecondBOffset) / slope;
                        int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                        int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                        int index = y * width + startx;
                        for (int x = startx; x < endx; x++, ++index)
                        {
                            pixelData[index] = color;
                        }
                    }
                }
                else
                {
                    for (int y = firstEndy; y < secondEndy; y++)
                    {
                        float x1 = (y - firstABOffset) / slopeNormal;
                        float x2 = (y - secondABOffset) / slopeNormal;
                        int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                        int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                        int index = y * width + startx;
                        for (int x = startx; x < endx; x++, ++index)
                        {
                            pixelData[index] = color;
                        }
                    }
                }

                for (int y = secondEndy; y <= y4; y++)
                {
                    float x1 = (y - firstSecondAOffset) / slope;
                    float x2 = (y - secondABOffset) / slopeNormal;
                    int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                    int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                    int index = y * width + startx;
                    for (int x = startx; x < endx; x++, ++index)
                    {
                        pixelData[index] = color;
                    }
                }
            }
            else
            {
                float slopeNormal = (firstA.y - firstB.y) / (firstA.x - firstB.x);

                float firstABOffset = firstA.y - firstA.x * slopeNormal;
                float secondABOffset = secondA.y - secondA.x * slopeNormal;
                float firstSecondAOffset = firstA.y - firstA.x * slope;
                float firstSecondBOffset = firstB.y - firstB.x * slope;

                int y1 = Mathf.Clamp(Mathf.FloorToInt(secondB.y), 0, height - 1);
                int y2 = Mathf.Clamp(Mathf.FloorToInt(secondA.y), 0, height - 1);
                int y3 = Mathf.Clamp(Mathf.CeilToInt(firstB.y), 0, height - 1);
                int y4 = Mathf.Clamp(Mathf.CeilToInt(firstA.y), 0, height - 1);
                //int startx = Mathf.Clamp(Mathf.RoundToInt(firstB.x), 0, width - 1);
                //int endx = Mathf.Clamp(Mathf.RoundToInt(secondA.x), 0, width - 1);

                int firstEndy = Mathf.Min(y2, y3);
                for (int y = y1; y < firstEndy; y++)
                {
                    float x1 = (y - firstSecondBOffset) / slope;
                    float x2 = (y - secondABOffset) / slopeNormal;
                    int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                    int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                    int index = y * width + startx;
                    for (int x = startx; x < endx; x++, ++index)
                    {
                        pixelData[index] = color;
                    }
                }

                int secondEndy = Mathf.Max(y2, y3);
                if (y2 < y3)
                {
                    for (int y = firstEndy; y < secondEndy; y++)
                    {
                        float x1 = (y - firstSecondBOffset) / slope;
                        float x2 = (y - firstSecondAOffset) / slope;
                        int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                        int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                        int index = y * width + startx;
                        for (int x = startx; x < endx; x++, ++index)
                        {
                            pixelData[index] = color;
                        }
                    }
                }
                else
                {
                    for (int y = firstEndy; y < secondEndy; y++)
                    {
                        float x1 = (y - firstABOffset) / slopeNormal;
                        float x2 = (y - secondABOffset) / slopeNormal;
                        int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                        int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                        int index = y * width + startx;
                        for (int x = startx; x < endx; x++, ++index)
                        {
                            pixelData[index] = color;
                        }
                    }
                }

                for (int y = secondEndy; y <= y4; y++)
                {
                    float x1 = (y - firstABOffset) / slopeNormal;
                    float x2 = (y - firstSecondAOffset) / slope;
                    int startx = Mathf.Clamp(Mathf.RoundToInt(x1), 0, width - 1);
                    int endx = Mathf.Clamp(Mathf.RoundToInt(x2), 0, width - 1);

                    int index = y * width + startx;
                    for (int x = startx; x < endx; x++, ++index)
                    {
                        pixelData[index] = color;
                    }
                }
            }
        }
    }

    private void DrawBrush(Vector2 brushCenter, float size, Color32 color)
    {
        int brushWidth = brush.width;
        int brushHeight = brush.height;
        int width = canvasTexture.width;
        int height = canvasTexture.height;

        float hsize = size * 0.5f;

        int ystart = Mathf.Clamp(Mathf.FloorToInt(brushCenter.y - hsize), 0, height - 1);
        int yend = Mathf.Clamp(Mathf.CeilToInt(brushCenter.y + hsize), 0, height - 1);
        int xstart = Mathf.Clamp(Mathf.FloorToInt(brushCenter.x - hsize), 0, width - 1);
        int xend = Mathf.Clamp(Mathf.CeilToInt(brushCenter.x + hsize), 0, width - 1);

        for (int y = ystart; y <= yend; y++)
        {
            float dy = y - brushCenter.y;
            float fy = (dy + 0.5f) / hsize;
            int by = Mathf.Clamp(Mathf.RoundToInt(fy * brushHeight), 0, brushHeight - 1);

            int index = y * width + xstart;
            int bindex = by * brushWidth;
            for (int x = xstart; x <= xend; x++, ++index)
            {
                float dx = x - brushCenter.x;
                float fx = (dx + 0.5f) / hsize;
                int bx = Mathf.Clamp(Mathf.RoundToInt(fx * brushWidth), 0, brushWidth - 1);

                Color32 brushColor = color;
                brushColor.a = brushData[bindex + bx].a;
                if (brushColor.a > 0)
                {
                    if (brushColor.a == byte.MaxValue)
                    {
                        pixelData[index] = brushColor;
                    }
                    else
                    {
                        PaintTranslucidPixel(index, brushColor, brushColor.a / (float)byte.MaxValue);
                    }
                }
            }
        }
    }

    private void DrawBrushBetweenPoints(Vector2 prevPoint, Vector2 nextPoint, float size, Color32 color)
    {
        if (prevPoint == nextPoint)
        {
            return;
        }

        Vector2 firstPoint;
        Vector2 secondPoint;

        if (prevPoint.x < nextPoint.x
            || (prevPoint.x == nextPoint.x && prevPoint.y < nextPoint.y))
        {
            firstPoint = prevPoint;
            secondPoint = nextPoint;
        }
        else
        {
            firstPoint = nextPoint;
            secondPoint = prevPoint;
        }

        int width = canvasTexture.width;
        int height = canvasTexture.height;
        int dx = Mathf.FloorToInt(secondPoint.x - firstPoint.x);
        int dy = Mathf.FloorToInt(secondPoint.y - firstPoint.y);

        if (dx == 0)
        {
            int ystart = Mathf.Clamp(Mathf.FloorToInt(firstPoint.y) + 1, 0, height - 1);
            int yend = Mathf.Clamp(Mathf.FloorToInt(secondPoint.y), 0, height - 1);

            int x = Mathf.FloorToInt(firstPoint.x);
            for (float y = ystart; y <= yend; y += brushDispersion)
            {
                DrawBrush(new Vector2(x, y), size, color);
            }
        }
        else if (dy == 0)
        {
            int xstart = Mathf.Clamp(Mathf.FloorToInt(firstPoint.x) + 1, 0, width - 1);
            int xend = Mathf.Clamp(Mathf.FloorToInt(secondPoint.x), 0, width - 1);

            int y = Mathf.FloorToInt(firstPoint.y);
            for (float x = xstart; x <= xend; x += brushDispersion)
            {
                DrawBrush(new Vector2(x, y), size, color);
            }
        }
        else
        {
            float slope = (secondPoint.y - firstPoint.y) / (secondPoint.x - firstPoint.x);

            if (dx >= dy)
            {
                int xstart = Mathf.Clamp(Mathf.FloorToInt(firstPoint.x) + 1, 0, width - 1);
                int xend = Mathf.Clamp(Mathf.FloorToInt(secondPoint.x), 0, width - 1);

                float fy = firstPoint.y;
                for (float x = xstart; x <= xend; x += brushDispersion)
                {
                    fy += slope;
                    int y = Mathf.Clamp(Mathf.RoundToInt(fy), 0, height - 1);

                    DrawBrush(new Vector2(x, y), size, color);
                }
            }
            else
            {
                int ystart = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(firstPoint.y, secondPoint.y)) + 1, 0, height - 1);
                int yend = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(secondPoint.y, secondPoint.y)), 0, height - 1);
                float xstart = firstPoint.y < secondPoint.y ? firstPoint.x : secondPoint.x;
                float yoffset = ystart - slope * xstart;

                float xinc = 1f / (firstPoint.y < secondPoint.y ? slope : -slope);
                float fx = (ystart - yoffset) / slope;
                for (float y = ystart; y <= yend; y += brushDispersion)
                {
                    int x = Mathf.Clamp(Mathf.RoundToInt(fx), 0, width - 1);
                    fx += xinc;

                    DrawBrush(new Vector2(x, y), size, color);
                }
            }
        }
    }

    private void PaintPixel(int index, Color32 color)
    {
        pixelData[index] = color;
    }

    private void PaintTranslucidPixel(int index, Color32 color, float alpha)
    {
        Color32 currentColor = pixelData[index];
        Color32 nextColor = Color32.Lerp(currentColor, color, alpha);
        nextColor.a = (byte)(Mathf.Clamp01(currentColor.a + alpha) * 255f);
        pixelData[index] = nextColor;
    }

    [Button]
    public void RemoveHelper()
    {
        papyrusMaterial.SetTexture("_Helper", transparentTexture);
    }

    [Button]
    public void SetHelper(Texture2D helper)
    {
        if (helper == null)
        {
            RemoveHelper();
            return;
        }

        papyrusMaterial.SetTexture("_Helper", helper);
    }

    public Color32[] GetDrawingData() => pixelData;

    [Button]
    public void Consume()
    {
        IsConsumed = true;
        papyrusMaterial.SetColor("_PaperColor", consumedColor);
        RemoveHelper();

        OnConsumed?.Invoke(this);
    }

    [Button]
    public void DestroyPapyrus()
    {
        OnDestroyed?.Invoke(this);
    }

    public void Ignite(float fireIntensity)
    {
        if (isBurned)
        {
            return;
        }

        isBurned = true;
        Consume();

        OnBurned?.Invoke(this);
        papyrusMaterial.SetColor("_PaperColor", burnedColor);
        ClearTexture();
    }
}
