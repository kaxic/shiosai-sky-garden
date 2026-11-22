using UnityEngine;

public sealed partial class VoxelHud
{
    static readonly string[] ControlNames={"建築","格子カーソル","カメラ","設計","素材と記録"};
    static readonly string[] ControlText={
        "左クリックで面に設置。Shift＋左クリックで回収、中クリックで採色。",
        "I J K L で水平移動、U O で高さ。Space 設置、Delete 回収、C 採色。",
        "右ドラッグで旋回、ホイールで拡縮。WASD で注視点を移動、Q E で旋回。",
        "N で次の部位を選択。R 素材回転、V 設計回転、G ガイド、Enter 出品。",
        "Tab 素材、B 依頼、T 渡航。Z 戻す、Y やり直す。F8 絵葉書、F9 アルバム、F12 撮影。"
    };

    void Journal()
    {
        Rect p=Book(920,522,"ARCHITECT'S FIELD BOOK","工房の作業手帳");
        var mapTab=new Rect(p.xMax-256,p.y+13,140,47);
        Drawing(mapTab,3,true);
        var mapText=new Rect(mapTab.xMax-68,mapTab.yMax-19,68,19);
        Fill(mapText,Dark);
        Text("浮島航路図",mapText.x+1,mapText.y,mapText.width-2,19,12,Paper,align:TextAnchor.MiddleCenter);
        if(Hit(mapTab))game.Open(VoxelMode.Travel);
        float x=p.x+20,y=p.y+87,left=272,rx=x+left+18,rw=p.xMax-rx-20;
        Text(game.IslandName,x,y,left,30,20,Paper,true);
        for(int i=0;i<5;i++)
        {
            Rect row=new Rect(x,y+40+i*45,left,41);
            Row(row,game.Selection==i);
            string name=JournalName(i);
            Text(name,row.x+8,row.y+1,row.width-88,row.height-2,15,Paper);
            string flag=i==1||i==2?(game.Found(i-1)?"発見済":"未発見"):
                i==4?(game.Creative?"有効":game.CreativeAvailable?"開放済":"未開放"):"";
            Text(flag,row.xMax-80,row.y+1,70,row.height-2,13,i==4?Gold:Mint,align:TextAnchor.MiddleRight);
            if(Hit(row))game.Highlight(i);
        }
        int selected=Mathf.Clamp(game.Selection,0,4);
        string detail=selected==0?"桟橋に移動し、F で開放済み素材を補給します。":
            selected==1||selected==2?"名所に近づいて F。初回は素材を発見し、再訪すると補給できます。":
            selected==3?game.PlanHint:game.CreativeAvailable?"無制限の素材で自由に建築できます。いつでも通常の建築に戻せます。":"すべての依頼を完工すると、無制限の素材で建築できます。";
        Text(detail,x+1,y+270,left-2,77,15,Pale);
        string verb=selected==4?(game.Creative?"通常の建築へ":"自由建築にする"):"この場所へ移動";
        if(Button(new Rect(x,p.yMax-58,left,38),verb,selected!=4||game.CreativeAvailable))ActivateJournal(selected);
        Fill(new Rect(rx-9,y,1,p.height-108),Grid);
        Text("操作と製図",rx,y,rw,30,20,Paper,true);
        for(int i=0;i<ControlText.Length;i++)
        {
            float rowY=y+44+i*60;
            Fill(new Rect(rx,rowY,rw,1),Grid);
            Text(ControlNames[i],rx+1,rowY+3,101,47,14,Gold);
            Text(ControlText[i],rx+105,rowY+3,rw-109,49,15,Paper);
        }
        string record=game.ActivePlan==game.Plans.Length
            ? $"巡回の施工 {game.TourBuilt} / {game.RequiredCount} 部位　／　編集 {game.TourEdits} 手"
            : $"完工印 {game.Stamps}　／　発見 {game.Discoveries}　／　巡回 {game.RepeatCount}";
        Text(record,rx+1,p.yMax-58,rw-2,37,14,Pale);
    }

    string JournalName(int item)
    {
        switch(item)
        {
            case 0:return "補給桟橋";
            case 1:
            case 2:return game.LandmarkName(item-1);
            case 3:return "現在の設計台";
            default:return "自由建築";
        }
    }
    void ActivateJournal(int item)
    {
        switch(item)
        {
            case 0:game.FocusDock();break;
            case 1:
            case 2:game.FocusLandmark(item-1);break;
            case 3:game.FocusPlan();break;
            case 4:game.ToggleCreative();break;
        }
    }
}
