using System;
using UnityEngine;

public sealed class VoxelPostcard : StudioPrint
{
    public static bool Framing { get; private set; }
    static readonly Color Card = new Color32(242, 231, 197, 255);
    static readonly Color Ink = new Color32(37, 62, 67, 255);
    protected override string Folder => "Postcards";
    protected override string Prefix => "SkyGarden";
    protected override string Caption => "絵はがき";
    protected override string CheckMarker => "VOXEL_POSTCARD_OK";
    protected override bool Eligible => true;
    protected override void SetFrame(bool value) { Framing = value; }

    public static void DrawBorder(VoxelGame g)
    {
        float w = g.W, h = g.H, edge = 11, footer = 48;
        g.Box(new Rect(0, 0, w, edge), Card);
        g.Box(new Rect(0, 0, edge, h), Card);
        g.Box(new Rect(w - edge, 0, edge, h), Card);
        g.Box(new Rect(0, h - footer, w, footer), Card);
        g.Label(new Rect(24, h - footer + 4, w * .55f, 39), g.IslandName, 24, Ink, true);
        g.Label(new Rect(w * .55f, h - footer + 4, w * .45f - 24, 39),
            "空庭ブロック工房　 " + DateTime.Now.ToString("yyyy.MM.dd"), 15, Ink, false, TextAnchor.MiddleRight);
    }
}
