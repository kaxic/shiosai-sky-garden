using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class VoxelMaterial
{
    public string id,name,color,note;
    public int island,site=-1,initial,supply=8;
    public bool directional;
}
[Serializable] public sealed class VoxelPart { public int x,y,z,turns; public string materialId; }
[Serializable] public sealed class VoxelPlan
{
    public string id,name,note;
    public int island,anchorX,anchorZ;
    public VoxelPart[] parts;
}
[Serializable] public sealed class VoxelCatalog
{
    public VoxelMaterial[] materials; public VoxelPlan[] plans;
    [NonSerialized] Dictionary<string,int> materialIds,planIds;
    public static VoxelCatalog Load()
    {
        var source=Resources.Load<TextAsset>("Voxel/Workshop");
        if(!source)throw new InvalidOperationException("Voxel/Workshop.json が見つかりません。");
        var c=JsonUtility.FromJson<VoxelCatalog>(source.text);c.Validate();return c;
    }
    public int Material(string id)=>id!=null&&materialIds.TryGetValue(id,out int i)?i:-1;
    public int Plan(string id)=>id!=null&&planIds.TryGetValue(id,out int i)?i:-1;
    public void Validate()
    {
        if(materials==null||materials.Length==0||plans==null||plans.Length==0)throw new InvalidOperationException("素材と設計を最低1件ずつ登録してください。");
        materialIds=new Dictionary<string,int>(StringComparer.Ordinal);planIds=new Dictionary<string,int>(StringComparer.Ordinal);
        for(int i=0;i<materials.Length;i++)
        {
            var m=materials[i];
            if(m==null||string.IsNullOrWhiteSpace(m.id)||string.IsNullOrWhiteSpace(m.name)||m.island<0||m.island>=VoxelGame.IslandNames.Length||m.site< -1||m.site>1||
                m.initial<0||m.supply<1||!ColorUtility.TryParseHtmlString(m.color,out _))
                throw new InvalidOperationException($"素材 {i+1} のID、色、採取場所、数量を確認してください。");
            if(materialIds.ContainsKey(m.id))throw new InvalidOperationException("素材ID重複: "+m.id);materialIds.Add(m.id,i);
        }
        for(int i=0;i<plans.Length;i++)
        {
            var p=plans[i];
            if(p==null||string.IsNullOrWhiteSpace(p.id)||string.IsNullOrWhiteSpace(p.name)||p.island<0||p.island>=VoxelGame.IslandNames.Length||p.parts==null||p.parts.Length==0)
                throw new InvalidOperationException($"設計 {i+1} のID・島・部位を確認してください。");
            if(planIds.ContainsKey(p.id))throw new InvalidOperationException("設計ID重複: "+p.id);planIds.Add(p.id,i);
            var cells=new HashSet<Vector3Int>();
            foreach(var part in p.parts)
                if(part==null||Material(part.materialId)<0||part.y<0||part.y>8||part.turns<0||part.turns>3||!cells.Add(new Vector3Int(part.x,part.y,part.z)))
                    throw new InvalidOperationException(p.name+"の部位に未登録素材・重複座標・不正な高さまたは向きがあります。");
        }
    }
}
