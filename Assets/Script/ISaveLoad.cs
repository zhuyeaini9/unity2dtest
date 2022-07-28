using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveLoad
{
    void loadData(GameData data);
    void saveData(GameData data);
}
