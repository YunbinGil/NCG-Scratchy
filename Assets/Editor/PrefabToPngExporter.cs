using System.IO;
using UnityEditor;
using UnityEngine;

public static class PrefabToPngExporter
{
    private const int Resolution = 1024;
    private const float Padding = 1.35f;
    private const string OutputFolder = "Assets/ExportedPngs";

    [MenuItem("Tools/Export Selected 2D or UI Prefab To PNG")]
    private static void ExportSelectedToPng()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("먼저 Project 창에서 프리팹이나 Hierarchy에서 오브젝트를 선택하세요.");
            return;
        }

        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        bool isAsset = EditorUtility.IsPersistent(selected);
        GameObject target = isAsset ? (GameObject)PrefabUtility.InstantiatePrefab(selected) : selected;

        GameObject camObj = new GameObject("TempPngCaptureCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.cameraType = CameraType.Preview;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;

        GameObject tempCanvasObj = null;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        RectTransform targetRect = target.GetComponent<RectTransform>();

        if (targetRect != null && canvas == null)
        {
            tempCanvasObj = new GameObject("TempPngCaptureCanvas");
            canvas = tempCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            target.transform.SetParent(canvas.transform, false);
            targetRect.anchoredPosition = Vector2.zero;
        }
        else if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
        }

        Canvas.ForceUpdateCanvases();

        Bounds bounds = CalculateBounds(target);
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        cam.transform.rotation = Quaternion.identity;

        float halfHeight = Mathf.Max(bounds.extents.y, 0.1f);
        float halfWidth = Mathf.Max(bounds.extents.x, 0.1f);
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth) * Padding;

        RenderTexture rt = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        RenderTexture previousRt = RenderTexture.active;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
        tex.Apply();

        RenderTexture.active = previousRt;
        cam.targetTexture = null;

        byte[] pngBytes = tex.EncodeToPNG();
        string safeName = selected.name.Replace(" ", "_");
        string fullPath = Path.Combine(OutputFolder, $"{safeName}.png");
        File.WriteAllBytes(fullPath, pngBytes);

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObj);

        if (tempCanvasObj != null)
        {
            Object.DestroyImmediate(tempCanvasObj);
        }
        else if (isAsset && target != null)
        {
            Object.DestroyImmediate(target);
        }

        AssetDatabase.Refresh();
        Debug.Log($"PNG 저장 완료: {fullPath}");
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        Bounds combined = new Bounds();
        bool hasBounds = false;

        RectTransform[] rects = go.GetComponentsInChildren<RectTransform>(true);
        Vector3[] corners = new Vector3[4];
        foreach (RectTransform rt in rects)
        {
            if (!rt.gameObject.activeSelf) continue;
            rt.GetWorldCorners(corners);
            foreach (Vector3 c in corners)
            {
                if (!hasBounds)
                {
                    combined = new Bounds(c, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(c);
                }
            }
        }

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (!r.gameObject.activeSelf) continue;
            if (!hasBounds)
            {
                combined = r.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds || combined.size.sqrMagnitude < 0.0001f)
        {
            return new Bounds(go.transform.position, Vector3.one);
        }

        return combined;
    }
}