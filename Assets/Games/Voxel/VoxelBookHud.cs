using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class VoxelHud
{
    static readonly string[] BlueprintIds={"bench","bloom_gate","vane_post","glasshouse","observatory","lighthouse"};
    readonly Dictionary<string,Texture2D> customDrawings=new Dictionary<string,Texture2D>();
    readonly Dictionary<VoxelPlan,VoxelPart[]> sortedParts=new Dictionary<VoxelPlan,VoxelPart[]>();
    readonly Dictionary<VoxelPlan,List<KeyValuePair<int,int>>> bills=new Dictionary<VoxelPlan,List<KeyValuePair<int,int>>>();

    void Palette()
    {
        Rect p=Book(913,478,"MATERIAL LIBRARY","素材見本帳");
        int count=game.Materials.Length;
        int selected=Mathf.Clamp(game.Selection,0,count-1);
        int first=selected/16*16;
        float x=p.x+20,y=p.y+87,cw=133,ch=69;
        for(int i=first;i<Mathf.Min(first+16,count);i++)
        {
            int cell=i-first;
            Rect r=new Rect(x+cell%4*(cw+5),y+cell/4*(ch+5),cw,ch);
            bool unlocked=game.Unlocked(i);
            Row(r,i==selected);
            Cube(new Rect(r.x+6,r.y+5,36,36),i,unlocked);
            Text(unlocked?(game.Creative?"∞":game.Stock(i).ToString()):"未開放",r.x+43,r.y+6,r.width-51,28,14,unlocked?Paper:Wire,align:TextAnchor.MiddleRight);
            Text(game.Materials[i].name,r.x+7,r.y+39,r.width-14,25,14,unlocked?Paper:Pale);
            if(Hit(r))game.Highlight(i);
        }
        selected=Mathf.Clamp(game.Selection,0,count-1);
        var material=game.Materials[selected];
        float rx=x+562,rw=p.xMax-rx-20;
        SwatchPlate(new Rect(rx,y,rw,104),selected);
        Text(material.name,rx+1,y+110,rw-2,35,22,Paper,true);
        Text(material.note,rx+1,y+149,rw-2,62,15,Pale);
        Text(game.Unlocked(selected)?(material.directional?"方向のある素材。R で手持ちを90度回転。":"どの方向にも取り付けられます。"):game.GateText(selected),rx+1,y+218,rw-2,59,15,game.Unlocked(selected)?Mint:Gold);
        Text(game.Creative?"自由建築　使用数は無制限":$"所持数　{game.Stock(selected)} 個",rx+1,p.yMax-100,rw-2,28,15,Paper);
        if(Button(new Rect(rx,p.yMax-57,rw,38),game.Unlocked(selected)?"この素材を持つ":"未開放",game.Unlocked(selected)))game.PickMaterial(selected);
        if(count>16)
        {
            if(Button(new Rect(x,p.yMax-57,100,35),"前の頁",first>0,false))game.Highlight(first-16);
            if(Button(new Rect(x+108,p.yMax-57,100,35),"次の頁",first+16<count,false))game.Highlight(first+16);
        }
        else Text("素材を選んで Enter　／　設置面はマウスまたは格子カーソルで指定",x,p.yMax-65,538,48,14,Pale);
    }

    void SwatchPlate(Rect r,int selected)
    {
        Fill(r,Dark);
        if(Event.current.type==EventType.Repaint)
        {
            for(float x=r.x+12;x<r.xMax;x+=20)Fill(new Rect(x,r.y,1,r.height),Blueprint);
            for(float y=r.y+12;y<r.yMax;y+=20)Fill(new Rect(r.x,y,r.width,1),Blueprint);
        }
        float size=Mathf.Min(r.height-14,r.width-22);
        Cube(new Rect(r.center.x-size/2,r.center.y-size/2,size,size),selected,game.Unlocked(selected));
    }

    void Plans()
    {
        Rect p=Book(885,503,"WORKSHOP DRAWINGS","建築の依頼");
        int standard=game.Plans.Length,selected=Mathf.Clamp(game.Selection,0,standard);
        float x=p.x+20,y=p.y+84,listW=277;
        int first=selected/7*7;
        for(int i=first;i<Mathf.Min(standard+1,first+7);i++)
        {
            Rect r=new Rect(x,y+(i-first)*47,listW,43);
            bool tour=i==standard,stamp=!tour&&game.HasStamp(i),available=game.CanSelectPlan(i);
            Row(r,i==selected);
            Text(tour?"巡回の依頼":game.Plans[i].name,r.x+9,r.y+1,r.width-77,r.height-2,16,available?Paper:Pale);
            Text(stamp?"完工":!available?"未開放":"",r.xMax-67,r.y+1,56,r.height-2,13,stamp?Mint:Wire,align:TextAnchor.MiddleRight);
            if(Hit(r))game.Highlight(i);
        }
        selected=Mathf.Clamp(game.Selection,0,standard);
        bool repeat=selected==standard;
        int blueprint=repeat?Mathf.Clamp(game.RepeatPlan,0,standard-1):selected;
        var plan=game.Plans[blueprint];
        float rx=x+listW+18,rw=p.xMax-rx-20;
        float draftWidth=Mathf.Min(198,rw*.42f);
        Drawing(new Rect(rx,y,draftWidth,150),blueprint,false);
        MaterialsBill(new Rect(rx+draftWidth+7,y,rw-draftWidth-7,150),plan);
        Text(repeat?"巡回の依頼　"+plan.name:plan.name,rx,y+159,rw,36,23,Paper,true);
        Text(plan.note,rx+1,y+202,rw-2,55,15,Pale);
        int island=game.PlanIsland(selected);
        string region=island>=0&&island<VoxelGame.IslandNames.Length?VoxelGame.IslandNames[island]:"";
        Text(region+"　／　設計 "+plan.parts.Length+" 部位",rx+1,y+268,rw-2,30,15,Gold);
        Text(PlanStatus(selected,blueprint),rx+1,y+304,rw-2,47,14,Pale);
        if(Button(new Rect(rx,p.yMax-57,rw,38),game.CanSelectPlan(selected)?"設計台へ向かう":"未開放",game.CanSelectPlan(selected)))game.SelectPlan(selected);
        if(standard+1>7)
        {
            if(Button(new Rect(x,p.yMax-54,81,32),"前の頁",first>0,false))game.Highlight(first-7);
            if(Button(new Rect(x+86,p.yMax-54,81,32),"次の頁",first+7<=standard,false))game.Highlight(first+7);
            Text($"{first/7+1} / {standard/7+1}",x+178,p.yMax-54,listW-178,32,14,Pale);
        }
        else Text($"完工印　{game.Stamps} / {standard}　　巡回　{game.RepeatCount} 件",x,p.yMax-59,listW,38,14,Mint);
    }

    string PlanStatus(int selected,int blueprint)
    {
        if(selected < game.Plans.Length)
        {
            if(game.HasStamp(selected))return "この依頼には完工印があります。";
            return selected==game.ActivePlan ? game.PlanHint : "選ぶと設計台へ移動し、必要な部位を重ねて表示します。";
        }
        if(!game.CreativeAvailable)return $"{game.Plans.Length} 件の完工後に巡回が開きます。";
        int best=game.BestEdits(blueprint);
        string record=best>0?$"最少 {best} 手":"最少手数は未記録";
        if(game.ActivePlan==selected)return $"今回 {game.TourEdits} 手 ・ 自分で施工 {game.TourBuilt} / {game.RequiredCount} 部位　　{record}";
        return $"全ての部位を自分で施工すると手数を記録します。　{record}";
    }

    void MaterialsBill(Rect r,VoxelPlan plan)
    {
        if(!bills.TryGetValue(plan,out var needed))
        {
            int[] quantities=new int[game.Materials.Length];
            foreach(var part in plan.parts)
                if(materialIndices.TryGetValue(part.materialId,out int material))quantities[material]++;
            needed=new List<KeyValuePair<int,int>>();
            for(int i=0;i<quantities.Length;i++)
                if(quantities[i]>0)needed.Add(new KeyValuePair<int,int>(i,quantities[i]));
            bills.Add(plan,needed);
        }
        Fill(r,Dark);
        Text("必要な部材",r.x+9,r.y+3,r.width-18,25,15,Gold);
        int displayed=needed.Count>8?7:needed.Count;
        int columns=displayed>4?2:1;
        float width=(r.width-16)/columns;
        for(int i=0;i<displayed;i++)
        {
            float x=r.x+8+i/4*width,y=r.y+31+i%4*27;
            var entry=needed[i];
            Cube(new Rect(x,y+3,19,19),entry.Key);
            string name=game.Materials[entry.Key].name;
            Text(name+" ×"+entry.Value,x+23,y,width-26,24,14,Paper);
        }
        if(needed.Count>displayed)
            Text($"ほか {needed.Count-displayed} 種",r.x+8+width,r.y+112,width-4,24,13,Pale);
    }

    void Travel()
    {
        Rect p=Book(914,434,"ISLAND SURVEY","浮島航路図");
        int count=VoxelGame.IslandNames.Length;
        float x=p.x+20,y=p.y+89,cw=(p.width-56)/3;
        int first=game.Selection/3*3;
        for(int i=first;i<Mathf.Min(count,first+3);i++)
        {
            Rect r=new Rect(x+(i-first)*(cw+8),y,cw,244);
            Row(r,game.Selection==i);
            Drawing(new Rect(r.x+5,r.y+5,r.width-10,122),i,true);
            Text(VoxelGame.IslandNames[i],r.x+9,r.y+131,r.width-18,33,20,Paper,true);
            Text(VoxelGame.IslandNotes[i],r.x+10,r.y+166,r.width-20,43,14,Pale);
            Text(i==game.Island?"現在地":game.CanTravel(i)?"渡航できます":game.TravelRule(i),r.x+10,r.y+211,r.width-20,27,14,game.CanTravel(i)?Mint:Gold);
            if(Hit(r))game.Highlight(i);
        }
        int selected=Mathf.Clamp(game.Selection,0,count-1);
        if(Button(new Rect(x+334,p.yMax-57,164,38),first==0?"東方の航路　›":"‹　西方の航路",true))game.Highlight(first==0?3:0);
        Text($"完工印　{game.Stamps}　／　発見した名所　{game.Discoveries} / {count*2}",x,p.yMax-58,p.width-290,36,15,Pale);
        if(Button(new Rect(p.xMax-221,p.yMax-57,201,38),selected==game.Island?"この島へ戻る":game.CanTravel(selected)?"この島へ渡る":"未開放",game.CanTravel(selected)))game.Travel(selected);
    }

    void Drawing(Rect frame,int index,bool region)
    {
        Fill(frame,Dark);
        if(region && index>=3){var land=Resources.Load<Texture2D>("Voxel/Regions/Port"+index);if(land)GUI.DrawTexture(frame,land,ScaleMode.ScaleAndCrop);return;}
        if(!region)
        {
            if(index<0 || index>=game.Plans.Length)return;
            var plan=game.Plans[index];
            if(!customDrawings.TryGetValue(plan.id,out var custom))
            {
                custom=Resources.Load<Texture2D>("Voxel/Plans/"+plan.id);
                customDrawings.Add(plan.id,custom);
            }
            if(custom){GUI.DrawTexture(frame,custom,ScaleMode.ScaleToFit);return;}
            index=Array.IndexOf(BlueprintIds,plan.id);
            if(index<0 || !game.skin.specimenPlate){PartDrawing(frame,plan);return;}
        }
        var atlas=region?game.skin.illustrations:game.skin.specimenPlate;
        if(!atlas || index<0)return;
        float aspect=atlas.width/(float)atlas.height*(region?1f:2f/3f);
        // Plans retain their margins; landscapes extend to the edge of the chart.
        float w=region?Mathf.Max(frame.width,frame.height*aspect):Mathf.Min(frame.width,frame.height*aspect);
        float h=w/aspect;
        GUI.BeginGroup(frame);
        var image=new Rect((frame.width-w)/2,(frame.height-h)/2,w,h);
        if(region)game.Art(image,index);else game.Plate(image,index,3,2);
        GUI.EndGroup();
    }

    void PartDrawing(Rect frame,VoxelPlan plan)
    {
        if(Event.current.type!=EventType.Repaint || plan.parts==null || plan.parts.Length==0)return;
        if(!sortedParts.TryGetValue(plan,out var parts))
        {
            parts=(VoxelPart[])plan.parts.Clone();
            Array.Sort(parts,(a,b)=>(a.x+a.z+a.y).CompareTo(b.x+b.z+b.y));
            sortedParts.Add(plan,parts);
        }
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity), max=-min;
        foreach(var part in parts)
        {
            Vector2 pos=PartPosition(part);
            min=Vector2.Min(min,pos-Vector2.one*.5f);max=Vector2.Max(max,pos+Vector2.one*.5f);
        }
        float scale=Mathf.Min((frame.width-14)/(max.x-min.x),(frame.height-10)/(max.y-min.y));
        Vector2 center=(min+max)*.5f;
        foreach(var part in parts)
        {
            if(!materialIndices.TryGetValue(part.materialId,out int mat))continue;
            Vector2 pos=frame.center+(PartPosition(part)-center)*scale;
            Cube(new Rect(pos.x-scale/2,pos.y-scale/2,scale,scale),mat);
        }
    }
    static Vector2 PartPosition(VoxelPart p)=>new Vector2((p.x-p.z)*.46f,(p.x+p.z)*.235f-p.y*.48f);
}
