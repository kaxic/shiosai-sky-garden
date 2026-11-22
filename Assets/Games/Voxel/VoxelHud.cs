using System.Collections.Generic;
using UnityEngine;

public sealed partial class VoxelHud
{
    static readonly Color Ink = new Color32(27, 49, 61, 255);
    static readonly Color Blueprint = new Color32(34, 64, 79, 255);
    static readonly Color Raised = new Color32(49, 82, 94, 255);
    static readonly Color Grid = new Color32(69, 99, 108, 255);
    static readonly Color Wire = new Color32(109, 151, 151, 255);
    static readonly Color Pale = new Color32(175, 207, 201, 255);
    static readonly Color Paper = new Color32(250, 241, 212, 255);
    static readonly Color Gold = new Color32(240, 187, 103, 255);
    static readonly Color Mint = new Color32(153, 220, 183, 255);
    static readonly Color Dark = new Color32(19, 39, 48, 255);
    static readonly string[] Bearings = { "北", "東", "南", "西" };
    VoxelGame game;
    VoxelMaterial[] swatchSource;
    Color[] swatches;
    readonly Dictionary<string,int> materialIndices = new Dictionary<string,int>();
    Texture2D cube;

    public void Draw(VoxelGame g)
    {
        if(VoxelPostcard.Framing)return;
        game = g;
        if (swatchSource != g.Materials)
        {
            swatchSource = g.Materials;
            swatches = new Color[swatchSource.Length];
            materialIndices.Clear();
            bills.Clear();
            for (int i = 0; i < swatches.Length; i++)
            {
                materialIndices[swatchSource[i].id] = i;
                string hex = swatchSource[i].color;
                if (!string.IsNullOrEmpty(hex) && hex[0] != '#') hex = "#" + hex;
                if (!ColorUtility.TryParseHtmlString(hex, out swatches[i])) swatches[i] = Pale;
            }
        }
        switch (g.Mode)
        {
            case VoxelMode.Build: Build(); break;
            case VoxelMode.Palette: Palette(); break;
            case VoxelMode.Plans: Plans(); break;
            case VoxelMode.Travel: Travel(); break;
            case VoxelMode.Journal: Journal(); break;
        }
    }

    void Build()
    {
        int id = game.SelectedMaterial;
        float bottom = game.H - 12;
        var site = new Rect(15, 15, 232, 74);
        Panel(site);
        Text("空 庭 ブ ロ ッ ク 工 房", site.x+12, site.y+5, 206, 23, 13, Pale);
        Text(game.IslandName, site.x+11, site.y+28, 170, 37, 22, Paper, true);
        Key(new Rect(site.xMax-43,site.y+32,29,22),"T");
        if (Hit(site)) game.Open(VoxelMode.Travel);
        Consume(site);

        var sheet = new Rect(game.W-260, 15, 245, 93);
        Panel(sheet);
        Text(game.Creative ? "自由建築" : "施工中の設計",sheet.x+11,sheet.y+4,165,21,13,Pale);
        Key(new Rect(sheet.xMax-38,sheet.y+6,27,20),"B");
        Text(game.PlanName,sheet.x+10,sheet.y+25,sheet.width-20,29,18,Paper,true);
        Progress(new Rect(sheet.x+11,sheet.y+67,sheet.width-87,6),game.MatchCount,game.RequiredCount);
        Text(game.MatchCount+" / "+game.RequiredCount,sheet.xMax-77,sheet.y+53,65,31,14,Mint,align:TextAnchor.MiddleRight);
        if (Hit(sheet)) game.Open(VoxelMode.Plans);
        Consume(sheet);
        Rect advance=new Rect(sheet.x,sheet.yMax+5,sheet.width,30);
        bool completed=game.ActivePlan>=0&&game.ActivePlan<game.Plans.Length&&game.HasStamp(game.ActivePlan);
        string next=game.CanSubmit?"Enter　完成して出品":completed?"B　次の依頼を選ぶ":"N　次の部位に合わせる";
        if(Button(advance,next,true,game.CanSubmit))
        {
            if(game.CanSubmit)game.Submit();
            else if(completed)game.Open(VoxelMode.Plans);
            else game.NextPart();
        }
        Consume(advance);

        var bar = new Rect((game.W-546)/2,bottom-56,546,56);
        Panel(bar);
        int start = Mathf.Max(0,id) / 8 * 8;
        for (int i=start; i<Mathf.Min(start+8, game.Materials.Length); i++)
        {
            int cell = i-start;
            var r = new Rect(bar.x+4+cell*67,bar.y+4,66,48);
            bool selected=i==id;
            Fill(r,selected?Raised:Dark);
            if (selected) Fill(new Rect(r.x,r.yMax-3,r.width,3),Gold);
            Cube(new Rect(r.x+9,r.y+4,35,35),i,game.Unlocked(i));
            if(i<9)Text((i+1).ToString(),r.x+3,r.y,15,18,11,Pale);
            Text(game.Creative?"∞":game.Stock(i).ToString(),r.x+31,r.y+18,29,21,13,game.Unlocked(i)?Paper:Wire,align:TextAnchor.MiddleRight);
            if (game.Unlocked(i) && Hit(r)) game.PickMaterial(i);
        }
        Consume(bar);
        var palette = new Rect(bar.xMax+7,bar.y,88,56);
        Panel(palette);
        Text("Tab",palette.x+4,palette.y+4,palette.width-8,19,13,Gold,align:TextAnchor.MiddleCenter);
        Text("全素材",palette.x+3,palette.y+22,palette.width-6,25,15,Paper,align:TextAnchor.MiddleCenter);
        if(Hit(palette))game.Open(VoxelMode.Palette);
        Consume(palette);
        string label = game.Materials[id].name;
        if(game.Materials[id].directional) label += "　"+Facing()+"　R 回転";
        float ribbonSize = Mathf.Max(151,label.Length*15+28);
        var selectedRibbon = new Rect(bar.x, bar.y-31, ribbonSize, 26);
        Fill(selectedRibbon,Dark);
        Text(label, selectedRibbon.x+7,selectedRibbon.y,selectedRibbon.width-14,selectedRibbon.height,14,Gold);
        if(game.Materials[id].directional && Hit(selectedRibbon))game.Rotate();
        Consume(selectedRibbon);
        string context=game.Prompt;
        if(!string.IsNullOrEmpty(context))
        {
            float hx=selectedRibbon.xMax+5,available=palette.xMax-hx;
            var hint=new Rect(hx,selectedRibbon.y,available,selectedRibbon.height);
            Fill(hint,Dark);
            Text(context,hint.x+7,hint.y,hint.width-14,hint.height,13,Pale);
            Consume(hint);
        }

        string help = game.PointerMode ? "左 設置　Shift＋左 回収" : "Space 設置　Delete 回収";
        var controls = new Rect(15,bar.y+1,bar.x-26,54);
        Fill(controls,Dark);
        Text(help,controls.x+6,controls.y+1,controls.width-12,25,14,Paper);
        float third=(controls.width-6)/3;
        var helpButton=new Rect(controls.x+2,controls.y+26,third,25);
        var undoButton=new Rect(helpButton.xMax+1,helpButton.y,third,25);
        var redoButton=new Rect(undoButton.xMax+1,helpButton.y,third,25);
        if(Button(helpButton,"H 手帳",true,false))game.Open(VoxelMode.Journal);
        if(Button(undoButton,"Z 戻す",game.UndoCount>0,false))game.Undo();
        if(Button(redoButton,"Y 再度",game.RedoCount>0,false))game.Redo();
        Consume(controls);
        var guide = new Rect(palette.xMax+7,bar.y,game.W-palette.xMax-22,56);
        Panel(guide);
        Text("G　設計ガイド",guide.x+6,guide.y+2,guide.width-12,24,13,Pale,align:TextAnchor.MiddleCenter);
        Text(game.Guide?"表示中":"非表示",guide.x+6,guide.y+25,guide.width-12,25,15,game.Guide?Mint:Paper,align:TextAnchor.MiddleCenter);
        if(Hit(guide))game.ToggleGuide();
        Consume(guide);
    }

    string Facing() => Bearings[(game.Turns%4+4)%4]+"向き";
    void Progress(Rect r,int count,int needed)
    {
        Fill(r,Grid);
        if(needed>0) Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(count/(float)needed),r.height),count>=needed?Mint:Gold);
    }
    Rect Book(float w,float h,string overline,string title)
    {
        Fill(new Rect(0,0,game.W,game.H),new Color(.05f,.10f,.13f,.62f));
        var p=new Rect((game.W-w)/2,(game.H-h)/2,w,h);
        Panel(p);
        Fill(new Rect(p.x+13,p.y+15,3,41),Gold);
        Text(overline,p.x+25,p.y+9,p.width-164,21,12,Pale);
        Text(title,p.x+23,p.y+29,p.width-176,34,24,Paper,true);
        if(Button(new Rect(p.xMax-105,p.y+20,85,33),"戻る",true,false))game.Back();
        Fill(new Rect(p.x+18,p.y+70,w-36,1),Grid);
        return p;
    }
    void Panel(Rect r)
    {
        Fill(new Rect(r.x+2,r.y+3,r.width,r.height),new Color(.02f,.05f,.07f,.26f));
        Fill(r,Blueprint);
        Fill(new Rect(r.x,r.y,r.width,1),Wire);
        Fill(new Rect(r.x,r.y,1,r.height),Grid);
    }
    void Row(Rect r,bool selected)
    {
        Fill(r,selected?Raised:Hover(r)?new Color32(44,76,88,255):Dark);
        if(selected) Fill(new Rect(r.x,r.y,3,r.height),Gold);
    }
    void Key(Rect r,string value)
    {
        Fill(r,Raised);
        Text(value,r.x,r.y,r.width,r.height,13,Gold,align:TextAnchor.MiddleCenter);
    }
    bool Button(Rect r,string value,bool enabled,bool primary=true)
    {
        Fill(r,enabled?(primary?(Hover(r)?Paper:Gold):(Hover(r)?Raised:Dark)):Raised);
        Text(value,r.x+4,r.y,r.width-8,r.height,15,enabled&&primary?Dark:enabled?Paper:Wire,align:TextAnchor.MiddleCenter);
        return enabled && Hit(r);
    }
    static bool Hover(Rect r)=>r.Contains(Event.current.mousePosition);
    static bool Hit(Rect r)=>GUI.Button(r,GUIContent.none,GUIStyle.none);
    static void Consume(Rect r)
    {
        Event e=Event.current;
        if(r.Contains(e.mousePosition) && (e.isMouse || e.type==EventType.ScrollWheel))e.Use();
    }
    void Text(string text,float x,float y,float w,float h,int size,Color color,bool serif=false,TextAnchor align=TextAnchor.MiddleLeft)
        =>game.Label(new Rect(x,y,w,h),text,size,color,serif,align);
    void Fill(Rect rect,Color color)=>game.Box(rect,color);
    void Cube(Rect r,int material,bool enabled=true)
    {
        if(Event.current.type!=EventType.Repaint)return;
        if(!cube)cube=MakeCube();
        Color old=GUI.color;
        GUI.color=enabled?swatches[material]:Wire;
        GUI.DrawTexture(r,cube,ScaleMode.ScaleToFit);
        GUI.color=old;
    }
    static Texture2D MakeCube()
    {
        const int side=48;
        var texture=new Texture2D(side,side,TextureFormat.RGBA32,false){name="Architect material swatch",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color32[side*side];
        for(int y=0;y<side;y++)for(int x=0;x<side;x++)
        {
            float dx=Mathf.Abs(x-23.5f);
            float fromTop=47-y;
            float outerTop=2+dx*.52f,join=25-dx*.51f,bottom=46-dx*.52f;
            if(dx>21 || fromTop<outerTop || fromTop>bottom)continue;
            byte v=fromTop<join?(byte)250:(x<24?(byte)158:(byte)203);
            if(Mathf.Abs(fromTop-join)<.8f)v=224;
            pixels[y*side+x]=new Color32(v,v,v,255);
        }
        texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
    }
}
