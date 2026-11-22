using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class VoxelBlueprintWindow : EditorWindow
{
    const string CatalogPath = "Assets/Games/Voxel/Resources/Voxel/Workshop.json";
    const int Side = 7;
    static readonly string[] Islands = { "芽吹きの庭島", "琥珀の風車島", "群青の星晶島" };
    static readonly string[] Directions = { "北 ↑", "東 →", "南 ↓", "西 ←" };
    readonly Stack<VoxelPart[]> undo = new Stack<VoxelPart[]>();
    VoxelCatalog catalog;
    string baseline, error;
    string[] materialNames;
    int selected, material, turn, layer, originX = -3, originZ = -3;
    Vector2 scroll;

    [MenuItem("潮彩作品集/空庭の設計図を描く")]
    static void Open()
    {
        var window = GetWindow<VoxelBlueprintWindow>("空庭の設計図");
        window.minSize = new Vector2(430, 490);
    }

    void OnEnable() { Reload(); }
    void Reload()
    {
        try
        {
            baseline = File.ReadAllText(CatalogPath);
            catalog = JsonUtility.FromJson<VoxelCatalog>(baseline);
            catalog.Validate();
            materialNames = Array.ConvertAll(catalog.materials, row => row.name);
            selected = Mathf.Clamp(selected, 0, catalog.plans.Length - 1);
            undo.Clear(); error = null;
        }
        catch (Exception ex) { catalog = null; error = ex.Message; }
    }

    void OnGUI()
    {
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        if (catalog == null) { if (GUILayout.Button("設計図を再読込")) Reload(); return; }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUILayout.HorizontalScope())
        {
            int next = EditorGUILayout.Popup(selected, Array.ConvertAll(catalog.plans, row => row.name));
            if (next != selected) { selected = next; layer = 0; undo.Clear(); }
            if (GUILayout.Button("複製して新しい依頼を作る", GUILayout.Width(166))) Duplicate();
        }
        var plan = catalog.plans[selected];
        plan.id = EditorGUILayout.TextField("設計ID", plan.id);
        plan.name = EditorGUILayout.TextField("設計の名前", plan.name);
        plan.note = EditorGUILayout.TextField("施工の説明", plan.note);
        plan.island = EditorGUILayout.Popup("施工する島", plan.island, Islands);
        var anchor = EditorGUILayout.Vector2IntField("設計の原点（島の X / Z）", new Vector2Int(plan.anchorX, plan.anchorZ));
        plan.anchorX = anchor.x; plan.anchorZ = anchor.y;
        EditorGUILayout.Space();
        material = EditorGUILayout.Popup("描く素材", material, materialNames);
        using (new EditorGUI.DisabledScope(!catalog.materials[material].directional))
            turn = EditorGUILayout.Popup("素材の向き", turn, Directions);
        layer = EditorGUILayout.IntSlider("高さの層（0 が地上）", layer, 0, 8);
        var origin = EditorGUILayout.Vector2IntField("表示範囲の左下（相対 X / Z）", new Vector2Int(originX, originZ));
        originX = origin.x; originZ = origin.y;
        EditorGUILayout.LabelField("上が北。左クリックで配置または塗り替え、右クリックで消去。", EditorStyles.miniLabel);
        DrawGrid(plan);
        using (new EditorGUI.DisabledScope(undo.Count == 0))
            if (GUILayout.Button("直前のマス操作を戻す")) plan.parts = undo.Pop();
        EditorGUILayout.LabelField($"部材 {plan.parts.Length} 個", EditorStyles.miniLabel);
        EditorGUILayout.EndScrollView();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("検証して保存", GUILayout.Height(33))) Save();
            if (GUILayout.Button("再読込", GUILayout.Width(80), GUILayout.Height(33)) &&
                EditorUtility.DisplayDialog("保存済みの設計を開く", "現在の記入内容を保存済みの値に戻します。", "再読込", "戻る")) Reload();
        }
    }

    void Duplicate()
    {
        var copy = JsonUtility.FromJson<VoxelPlan>(JsonUtility.ToJson(catalog.plans[selected]));
        string stem = copy.id + "_";
        int suffix = catalog.plans.Length;
        do { copy.id = stem + suffix++; } while (Array.Exists(catalog.plans, p => p.id == copy.id));
        copy.name += "（新規）";
        selected = catalog.plans.Length; Array.Resize(ref catalog.plans, selected + 1);
        catalog.plans[selected] = copy; undo.Clear();
    }

    void DrawGrid(VoxelPlan plan)
    {
        float cell = Mathf.Min(44, (position.width - 35) / Side);
        var grid = GUILayoutUtility.GetRect(cell * Side, cell * Side, GUILayout.ExpandWidth(false));
        var onLight = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        var onDark = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        onLight.normal.textColor = Color.black; onDark.normal.textColor = Color.white;
        if (Event.current.type == EventType.Repaint && position.width - grid.xMax > 190)
            DrawPreview(new Rect(grid.xMax + 8, grid.y, position.width - grid.xMax - 27, grid.height), plan);
        for (int iz = 0; iz < Side; iz++)
        for (int ix = 0; ix < Side; ix++)
        {
            int x = originX + ix, z = originZ + Side - 1 - iz;
            var r = new Rect(grid.x + ix * cell + 1, grid.y + iz * cell + 1, cell - 2, cell - 2);
            var part = Array.Find(plan.parts, p => p.x == x && p.y == layer && p.z == z);
            int id = part == null ? -1 : catalog.Material(part.materialId);
            Color color = new Color(.18f,.23f,.26f);
            string mark = x == 0 && z == 0 ? "原点" : "";
            if (id >= 0)
            {
                var mat = catalog.materials[id];
                ColorUtility.TryParseHtmlString(mat.color, out color);
                mark = mat.directional ? Directions[part.turns].Substring(2) : mat.name.Substring(0,1);
            }
            else if (layer > 0 && Array.Exists(plan.parts, p => p.x == x && p.y == layer - 1 && p.z == z))
                mark = "下";
            EditorGUI.DrawRect(r, color);
            GUI.Label(r, new GUIContent(mark, $"X {x} / Y {layer} / Z {z}"), color.grayscale > .55f ? onLight : onDark);
            var ev = Event.current;
            if (ev.type != EventType.MouseDown || !r.Contains(ev.mousePosition) || ev.button > 1) continue;
            Paint(plan, x, layer, z, ev.button == 1);
            ev.Use(); Repaint();
        }
    }

    void DrawPreview(Rect viewport, VoxelPlan plan)
    {
        EditorGUI.DrawRect(viewport, new Color(.11f,.18f,.22f));
        if (plan.parts.Length == 0) return;
        var parts = (VoxelPart[])plan.parts.Clone();
        Array.Sort(parts, (a,b) => (a.x+a.z+2*a.y).CompareTo(b.x+b.z+2*b.y));
        Vector2 Project(float x, float y, float z) => new Vector2((x-z)*.85f, (x+z)*.43f-y);
        Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
        foreach (var p in parts)
        for (int corner = 0; corner < 8; corner++)
        {
            var v = Project(p.x+(corner&1), p.y+((corner>>1)&1), p.z+((corner>>2)&1));
            min = Vector2.Min(min,v); max = Vector2.Max(max,v);
        }
        float scale = Mathf.Min((viewport.width-22)/(max.x-min.x), (viewport.height-22)/(max.y-min.y));
        Vector2 center = viewport.center - (min + max) * (.5f * scale);
        Vector3 P(float x,float y,float z) => center + Project(x,y,z)*scale;
        Handles.BeginGUI();
        Color original = Handles.color;
        try
        {
            foreach (var p in parts)
            {
                int index = catalog.Material(p.materialId);
                if (index < 0) continue;
                ColorUtility.TryParseHtmlString(catalog.materials[index].color, out var color);
                int x=p.x, y=p.y, z=p.z;
                var top=P(x+1,y+1,z+1); var east=P(x+1,y+1,z); var west=P(x,y+1,z+1);
                Handles.color = Color.Lerp(color,Color.black,.33f);
                Handles.DrawAAConvexPolygon(west, top, P(x+1,y,z+1), P(x,y,z+1));
                Handles.color = Color.Lerp(color,Color.black,.16f);
                Handles.DrawAAConvexPolygon(top, east, P(x+1,y,z), P(x+1,y,z+1));
                Handles.color = Color.Lerp(color,Color.white,.19f);
                Handles.DrawAAConvexPolygon(P(x,y+1,z), east, top, west);
            }
        }
        finally { Handles.color = original; Handles.EndGUI(); }
    }

    void Paint(VoxelPlan plan, int x, int y, int z, bool erase)
    {
        var parts = new List<VoxelPart>(plan.parts);
        int old = parts.FindIndex(p => p.x == x && p.y == y && p.z == z);
        var mat = catalog.materials[material];
        int orientation = mat.directional ? turn : 0;
        if (erase && old < 0) return;
        if (!erase && old >= 0 && parts[old].materialId == mat.id && parts[old].turns == orientation) return;
        undo.Push(plan.parts);
        if (old >= 0) parts.RemoveAt(old);
        if (!erase) parts.Add(new VoxelPart { x=x, y=y, z=z, materialId=mat.id, turns=orientation });
        plan.parts = parts.ToArray();
    }

    void Save()
    {
        try
        {
            catalog.Validate();
            if (File.ReadAllText(CatalogPath) != baseline)
                throw new IOException("JSONが別の場所で更新されました。再読込してから編集してください。");
            string next = JsonUtility.ToJson(catalog, true) + Environment.NewLine;
            string tmp = Path.GetFullPath(CatalogPath) + ".writing";
            File.WriteAllText(tmp, next, new UTF8Encoding(false)); File.Replace(tmp, CatalogPath, null);
            baseline = next; error = null; AssetDatabase.ImportAsset(CatalogPath);
            ShowNotification(new GUIContent("設計図を保存しました"));
        }
        catch (Exception ex) { error = ex.Message; }
    }
}
