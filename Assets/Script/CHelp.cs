using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CHelp : MonoBehaviour
{
    public static bool hasHit()
    {
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonUp(0))
        {
            return true;
        }

        return false;
    }
    //1.0~100.0
    public static bool hasPercent(float per)
    {
        float tar = per * 100.0f;
        float ran_tar = Random.Range(1.0f, 10000.0f);

        if (ran_tar <= tar)
            return true;
        return false;
    }
    public static Vector2 v3tov2(Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }
}
