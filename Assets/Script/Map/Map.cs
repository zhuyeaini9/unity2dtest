using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour, ISaveLoad
{
    public string mGuid;
    [ContextMenu("GEN GUID")]
    void genGUID()
    {
        mGuid = System.Guid.NewGuid().ToString();
    }
    public void loadData(GameData data)
    {
        
    }

    public void saveData(GameData data)
    {
        CMapData mapData = new CMapData();
        mapData.name = "test";
        data.mMapDatas[mGuid] = mapData; 
    }
}

public class CMapData
{
    public string name;
}
