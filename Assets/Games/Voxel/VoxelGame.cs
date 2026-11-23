using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VoxelPostcard))]
public sealed class VoxelGame : StudioGame
{
    public static readonly string[] IslandNames={"芽吹きの庭島","琥珀の風車島","群青の星晶島","桜霞の社島","水車の雲滝島","宵炉の黒曜島"};
    public static readonly string[] IslandNotes={"花の種と木漏れ日を運ぶ、庭師たちの浮島。","砂岩の段丘と渡り風が育てたガラス職人の島。","群青の岩盤に星を映す結晶の島。","桜の花と朱塗りの社が雲に浮かぶ。","段丘を走る水路と職人の水車。","星を測る鍛冶師の黒い火山島。"};
    static readonly string[,] landmarks={{"木漏れ日の灯標","花染めの苗床"},{"銅の風読み台","渡り風の羽根塚"},{"星晶の露頭","星冠の祭壇"},{"組子の社","桜の苗床"},{"青銅の水門","雲滝の水車"},{"火花の鋳型","火口観測台"}};
    static readonly string[] directions={"北","東","南","西"};
    VoxelWorkshop work;
    VoxelCatalog catalog;
    VoxelView view;
    VoxelHud hud;
    int worldEpoch;
    Vector3 oldMouse;
    VoxelRecord S=>work.State;
    public VoxelMode Mode {get;private set;}
    public int Selection {get;private set;}
    public VoxelMaterial[] Materials=>catalog.materials;
    public VoxelPlan[] Plans=>catalog.plans;
    public int Island=>work.Island;
    public string IslandName=>IslandNames[Island];
    public int SelectedMaterial=>Mathf.Max(0,catalog.Material(S.selectedId));
    public string SelectedId=>S.selectedId;
    public int Turns=>S.turns;
    public bool Guide=>S.guide;
    public int PlanTurn=>S.planTurn;
    public int ActivePlan=>work.ActivePlan;
    public int RepeatPlan=>work.RepeatPlan;
    public int Stamps=>work.Stamps;
    public int RepeatCount=>S.repeats;
    public int TourEdits=>S.tourEdits;
    public int TourBuilt=>work.TourBuilt;
    public int BestEdits(int planIndex)=>work.BestEdits(planIndex);
    public int Discoveries {get {int count=0;for(int i=0;i<IslandNames.Length*2;i++)if((S.discoveries&(1<<i))!=0)count++;return count;}}
    public bool Creative=>S.creative;
    public bool CreativeAvailable=>work.CreativeAvailable;
    public string LastLog=>S.log;
    public int UndoCount=>work.HistoryCount(false);
    public int RedoCount=>work.HistoryCount(true);
    public int MatchCount=>work.Matched;
    public int RequiredCount=>work.Plan.parts.Length;
    public bool CanSubmit=>work.CanSubmit;
    public string PlanName=>(S.repeating?"巡回・":"")+work.Plan.name;
    public bool PointerMode {get;private set;}
    public bool HasTarget {get;private set;}
    public bool CanPlace=>HasTarget&&work.CanPlace(TargetCell,SelectedId);
    public Vector3Int TargetCell {get;private set;}
    public Vector3Int TargetBlock {get;private set;}
    public Vector3Int FaceNormal {get;private set;}
    public Vector3 Pivot=>S.pivot;
    public float Yaw=>S.yaw;
    public float Pitch=>S.pitch;
    public float Zoom=>S.zoom;
    public int WorldVersion=>worldEpoch+work.Version;
    public List<VoxelBlock> Blocks=>S.blocks;
    public List<VoxelGuideCell> GuideCells=>work.GuideCells;
    public Vector3Int Anchor=>work.Anchor;
    public Vector3Int Dock=>work.Dock;
    public Vector3Int Landmark(int i)=>work.Landmark(i);
    public string LandmarkName(int i)=>landmarks[Island,Mathf.Clamp(i,0,1)];
    public int GroundTop(int x,int z)=>VoxelWorkshop.GroundTop(Island,x,z);
    public int GroundBottom(int x,int z)=>VoxelWorkshop.GroundBottom(Island,x,z);
    public bool Found(int i)=>work.Found(Island,i);
    public bool HasDiscovery(int island,int site)=>work.Found(island,site);
    public int Stock(int i)=>work.Stock(i);
    public bool Unlocked(int i)=>work.Unlocked(i);
    public bool CanTravel(int i)=>work.CanTravel(i);
    public bool HasStamp(int i)=>work.HasStamp(i);
    public bool CanSelectPlan(int i)=>work.CanSelect(i);
    public int PlanIsland(int i)=>work.PlanIsland(i);
    public string TravelRule(int i)=>i<=0?"いつでも渡航できます":$"完工印 {VoxelWorkshop.Gate(i)} 個で渡航";
    public string GateText(int material)
    {
        if(material<0||material>=Materials.Length)return "";
        var m=Materials[material];return IslandNames[m.island]+(m.site<0?"の桟橋で補給":"の"+landmarks[m.island,m.site]+"で F");
    }
    public string Prompt
    {
        get
        {
            int n=work.Nearby();
            if(n==-1)return "補給桟橋　F で資材を補給";
            if(n>=0)return LandmarkName(n)+(Found(n)?"　F で資材を補給":"　F で調査");
            if(CanSubmit)return "設計が完成　Enter で出品";
            return Stamps==0?"N 次の部位　Space 設置　Enter 出品":"N 次の部位　H 名所と補給";
        }
    }
    public string PlanHint
    {
        get
        {
            if(Island!=work.Plan.island)return IslandNames[work.Plan.island]+"に設計台があります。";
            if(CanSubmit)return "全部位がそろいました。Enter で出品できます。";
            if(!work.Next(out var part))return "完工印を獲得済み。B で別の依頼を選べます。";
            int i=catalog.Material(part.materialId);
            if(!Unlocked(i))return "次は"+Materials[i].name+"。"+GateText(i);
            if(work.At(part.cell)!=null)return "N で異なる部位を選び、Delete で回収。";
            if(!Creative&&Stock(i)==0)return Materials[i].name+"の在庫切れ。H → 補給桟橋 → F";
            return "次は"+Materials[i].name+(Materials[i].directional?"（"+directions[part.turns]+"向き）":"")+"。N → Space で設置。";
        }
    }
    protected override void BeginGame()
    {
        catalog=VoxelCatalog.Load();work=new VoxelWorkshop(catalog,Load<VoxelRecord>());
        view=gameObject.AddComponent<VoxelView>();hud=new VoxelHud();oldMouse=Input.mousePosition;
        KeyboardTarget();view.Render(this);
    }
    protected override void LateGame(){if(work!=null){SetIntenseMusic(Island==2 || Island==5);view.Render(this);}}
    protected override void SaveCheckpoint(){if(work!=null)Save(S);}
    protected override bool HandleEscape(){if(Mode==VoxelMode.Build)return false;Back();return true;}
    void Say(string text,int sound=-1){S.log=text;Notice(text);if(sound>=0)Tone(sound);}
    public void Open(VoxelMode mode)
    {
        Mode=mode;Selection=mode==VoxelMode.Palette?SelectedMaterial:mode==VoxelMode.Plans?ActivePlan:mode==VoxelMode.Travel?Island:0;
    }
    public void Back(){Mode=VoxelMode.Build;oldMouse=Input.mousePosition;}
    public void Highlight(int i){Selection=Mathf.Clamp(i,0,MenuCount()-1);}
    int MenuCount()=>Mode==VoxelMode.Palette?Materials.Length:Mode==VoxelMode.Plans?Plans.Length+1:Mode==VoxelMode.Travel?IslandNames.Length:5;
    public void PickMaterial(int i)
    {
        if(!Unlocked(i)){Say(GateText(i));return;}S.selectedId=Materials[i].id;Back();SaveCheckpoint();
    }
    public void SelectPlan(int i)
    {
        if(!work.Select(i)){Say(i==Plans.Length?"すべての依頼を完工すると巡回に参加できます。":TravelRule(PlanIsland(i)));return;}
        Back();KeyboardTarget();Say(PlanName+"　N で部位を選択",1);SaveCheckpoint();
    }
    public void Travel(int i)
    {
        if(!work.Travel(i)){Say(TravelRule(i));return;}
        Back();KeyboardTarget();Say(IslandName+" に到着　H で名所へ",1);SaveCheckpoint();
    }
    public void ToggleGuide(){S.guide=!S.guide;SaveCheckpoint();}
    public void ToggleCreative()
    {
        if(!CreativeAvailable){Say("すべての設計を完工すると自由建築が開きます。");return;}
        S.creative=!S.creative;Back();Say(Creative?"自由建築　素材を消費せずに設置できます。":"通常の建築に戻りました。");SaveCheckpoint();
    }
    public void FocusPlan()
    {
        if(Island!=work.Plan.island&&!work.Travel(work.Plan.island)){Say(TravelRule(work.Plan.island));return;}
        Focus(Anchor);Say("設計台　"+PlanName);
    }
    public void FocusDock(){Focus(Dock);Say("補給桟橋　F で資材を補給");}
    public void FocusLandmark(int site)
    {
        if(site<0||site>1)return;Focus(Landmark(site));Say(LandmarkName(site)+"　F で調査・補給");
    }
    void Focus(Vector3Int cell){work.Focus(cell);Back();KeyboardTarget();SaveCheckpoint();}
    public void Collect()
    {
        int site=work.Nearby();bool first=site>=0&&!Found(site);
        if(!work.Collect()){Say("H の作業手帳から、補給桟橋か名所へ移動できます。");return;}
        Say(first?LandmarkName(site)+"を発見！ 新しい素材を入手しました。":"開放済みの素材を補給しました。",first?4:1);SaveCheckpoint();
    }
    public void Submit()
    {
        if(!work.Submit()){Say(PlanHint);return;}
        Say(CreativeAvailable?"完工印！ H で自由建築、B で巡回依頼を選べます。":"完工印を獲得！ B で次の依頼を選べます。",4);SaveCheckpoint();
    }
    public void Rotate(){S.turns=VoxelWorkshop.Turn(Turns+1);Say("手持ちの向き　"+directions[Turns]);SaveCheckpoint();}
    public void RotatePlan(){work.RotatePlan();Say("設計を90度回転　"+directions[PlanTurn]);SaveCheckpoint();}
    public void NextPart()
    {
        if(Island!=work.Plan.island)FocusPlan();
        if(!work.Next(out var next)){Say(PlanHint);return;}
        S.cursor=next.cell;
        if((Pivot-VoxelWorkshop.Center(next.cell)).sqrMagnitude>30f)S.pivot=VoxelWorkshop.Center(Anchor);
        KeyboardTarget();int i=catalog.Material(next.materialId);
        if(!Unlocked(i)){Say(GateText(i));return;}
        S.selectedId=next.materialId;S.turns=next.turns;
        Say(work.At(next.cell)!=null?"ここは異なる部位です。Delete で回収して Space で設置。":Stock(i)==0&&!Creative?"在庫切れ　H → 補給桟橋 → F":Materials[i].name+"　Space で設置");
    }
    public void Undo(){if(work.History(false)){Say("この島の編集を1回戻しました。",2);SaveCheckpoint();}else Say("この島で戻せる編集はありません。");}
    public void Redo(){if(work.History(true)){Say("この島の編集を1回やり直しました。",1);SaveCheckpoint();}else Say("この島でやり直せる編集はありません。");}
    public void ResetCamera(){S.yaw=22;S.pitch=40;S.zoom=19;work.Focus(Island==work.Plan.island?Anchor:Dock);KeyboardTarget();SaveCheckpoint();}
    void KeyboardTarget()
    {
        PointerMode=false;HasTarget=true;TargetCell=S.cursor;TargetBlock=S.cursor;FaceNormal=Vector3Int.up;
    }
    void PointerTarget()
    {
        Camera cam=Camera.main;if(cam==null){HasTarget=false;return;}
        PointerTarget(cam.ScreenPointToRay(Input.mousePosition));
    }
    void PointerTarget(Ray ray)
    {
        PointerMode=true;
        if(work.Raycast(ray,out var hit,out var face))
        {HasTarget=true;TargetBlock=hit;FaceNormal=face;TargetCell=hit+face;}
        else HasTarget=false;
    }
    void BuildTarget()
    {
        if(!HasTarget){Say("島か建物の面を指してください。");return;}
        if(!work.Place(TargetCell,SelectedId,Turns))
        {
            Say(!Creative&&Stock(SelectedMaterial)==0?"在庫切れ　H → 補給桟橋 → F":"地面やブロックに接する、空いたマスに置けます。");return;
        }
        S.log=Materials[SelectedMaterial].name+"を設置。"+(CanSubmit?" Enter で出品。":"");
        if(CanSubmit)Notice("設計がそろいました。Enter で出品できます。");
        Tone(0);SaveCheckpoint();if(PointerMode)PointerTarget();
    }
    void RemoveTarget()
    {
        if(HasTarget&&work.Remove(TargetBlock)){S.log="部材を回収しました。";Tone(2);SaveCheckpoint();if(PointerMode)PointerTarget();}
        else Say("自分が置いたブロックを選んでください。");
    }
    void Sample()
    {
        var b=HasTarget?work.At(TargetBlock):null;
        if(b==null){Say("建物の部材から素材と向きを写し取れます。");return;}
        int i=catalog.Material(b.materialId);
        if(!Unlocked(i)){Say(i>=0?GateText(i):"この素材は現在の見本帳にありません。");return;}
        S.selectedId=b.materialId;S.turns=b.turns;Say(Materials[i].name+"を採色しました。",1);SaveCheckpoint();
    }
    static bool Press(KeyCode k)=>Input.GetKeyDown(k);
    static bool EnterPressed()=>Press(KeyCode.Return)||Press(KeyCode.KeypadEnter);
    protected override void TickGame()
    {
        var mouse=Input.mousePosition;Vector3 delta=mouse-oldMouse;oldMouse=mouse;
        if(Press(KeyCode.Tab)){if(Mode==VoxelMode.Palette)Back();else Open(VoxelMode.Palette);return;}
        if(Press(KeyCode.B)){if(Mode==VoxelMode.Plans)Back();else Open(VoxelMode.Plans);return;}
        if(Press(KeyCode.T)){if(Mode==VoxelMode.Travel)Back();else Open(VoxelMode.Travel);return;}
        if(Press(KeyCode.H)){if(Mode==VoxelMode.Journal)Back();else Open(VoxelMode.Journal);return;}
        if(Mode!=VoxelMode.Build){MenuInput();return;}
        float dt=Mathf.Min(Time.unscaledDeltaTime,.08f);
        int orbit=(Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0);
        S.yaw=Mathf.Repeat(Yaw+orbit*82*dt,360f);
        if(Input.GetMouseButton(1)){S.yaw+=delta.x*.29f;S.pitch=Mathf.Clamp(Pitch-delta.y*.24f,22,76);}
        if(Press(KeyCode.Minus))S.zoom=Mathf.Min(32,Zoom+2);
        if(Press(KeyCode.Equals)||Press(KeyCode.KeypadPlus))S.zoom=Mathf.Max(7,Zoom-2);
        var pan=new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),0,(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
        var p=Pivot+Quaternion.Euler(0,Yaw,0)*pan.normalized*dt*7.5f;
        S.pivot=new Vector3(Mathf.Clamp(p.x,-10,10),p.y,Mathf.Clamp(p.z,-8,8));
        if(PointerMode||delta.sqrMagnitude>3f&&!Input.GetMouseButton(1))PointerTarget();
        Vector3Int move=new Vector3Int((Press(KeyCode.L)?1:0)-(Press(KeyCode.J)?1:0),(Press(KeyCode.U)?1:0)-(Press(KeyCode.O)?1:0),(Press(KeyCode.I)?1:0)-(Press(KeyCode.K)?1:0));
        if(move!=Vector3Int.zero)
        {
            if(PointerMode&&HasTarget)S.cursor=TargetCell;
            var c=S.cursor+move;S.cursor=new Vector3Int(Mathf.Clamp(c.x,-10,10),Mathf.Clamp(c.y,-1,8),Mathf.Clamp(c.z,-8,8));KeyboardTarget();
            if((Pivot-VoxelWorkshop.Center(S.cursor)).sqrMagnitude>42)S.pivot=VoxelWorkshop.Center(S.cursor);
        }
        for(int i=0;i<Mathf.Min(9,Materials.Length);i++)if(Press(KeyCode.Alpha1+i))PickMaterial(i);
        if(Press(KeyCode.N))NextPart();else if(Press(KeyCode.Space))BuildTarget();
        else if(Press(KeyCode.Delete)||Press(KeyCode.Backspace))RemoveTarget();else if(Press(KeyCode.C))Sample();
        else if(Press(KeyCode.R))Rotate();else if(Press(KeyCode.V))RotatePlan();else if(Press(KeyCode.G))ToggleGuide();
        else if(Press(KeyCode.Z))Undo();else if(Press(KeyCode.Y))Redo();else if(Press(KeyCode.F))Collect();
        else if(EnterPressed())Submit();else if(Press(KeyCode.Home))ResetCamera();
    }
    void MenuInput()
    {
        int width=Mode==VoxelMode.Palette?4:1;
        int step=(Press(KeyCode.RightArrow)?1:0)-(Press(KeyCode.LeftArrow)?1:0)+(Press(KeyCode.DownArrow)?width:0)-(Press(KeyCode.UpArrow)?width:0);
        if(step!=0)Highlight(Selection+step);
        if(!EnterPressed())return;
        switch(Mode)
        {
            case VoxelMode.Palette:PickMaterial(Selection);break;
            case VoxelMode.Plans:SelectPlan(Selection);break;
            case VoxelMode.Travel:Travel(Selection);break;
            case VoxelMode.Journal:
                if(Selection==0)FocusDock();else if(Selection<3)FocusLandmark(Selection-1);else if(Selection==3)FocusPlan();else ToggleCreative();
                break;
        }
    }
    protected override void DrawGame()
    {
        if(VoxelPostcard.Framing){VoxelPostcard.DrawBorder(this);return;}
        hud.Draw(this);if(Mode!=VoxelMode.Build)return;
        Event e=Event.current;
        if(e.type==EventType.ScrollWheel){S.zoom=Mathf.Clamp(Zoom+e.delta.y*.65f,7,32);e.Use();}
        if(e.type!=EventType.MouseDown || e.button==1)return;
        Camera cam=Camera.main;
        if(cam==null)return;
        PointerTarget(cam.ViewportPointToRay(new Vector3(e.mousePosition.x/W,1f-e.mousePosition.y/H,0)));
        if(e.button==0){if(e.shift)RemoveTarget();else BuildTarget();e.Use();}
        else if(e.button==2){Sample();e.Use();}
    }
    public override void VerifyGame()
    {
        var checkedWorld=VoxelVerification.Run(catalog);
        if(Reviewing)
        {
            Save(checkedWorld.State);var restored=new VoxelWorkshop(catalog,Load<VoxelRecord>());
            if(restored.Stamps!=catalog.plans.Length||restored.State.blocks.Count!=checkedWorld.State.blocks.Count)
                throw new System.InvalidOperationException("工房の保存・再開チェックに失敗しました。");
        }
    }
    public override int ShowcaseViews=>3;
    public override void ShowcaseView(int index)
    {
        if(index==0){work=VoxelVerification.Populated(catalog);worldEpoch+=100000;work.Travel(0);work.Select(catalog.Plan("bloom_gate"));}
        else if(index==1){work.Travel(2);work.Select(catalog.Plan("lighthouse"));}
        work.State.pivot=index==1?new Vector3(-.1f,1.8f,1.8f):new Vector3(.4f,1.35f,1.5f);
        work.State.yaw=index==1?-22:21;work.State.pitch=index==1?30:27;work.State.zoom=index==1?18:17;work.State.guide=false;
        Back();PointerMode=false;HasTarget=false;DismissNotice();
        if(index==2){Open(VoxelMode.Plans);Highlight(5);}view.Render(this);
        if(index==0)GetComponent<VoxelPostcard>().Take();
    }
}
