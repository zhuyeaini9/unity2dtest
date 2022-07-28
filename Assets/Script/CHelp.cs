using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CHelp : MonoBehaviour
{
    public static float distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow((x2 - x1), 2) + Mathf.Pow((y2 - y1), 2));
    }
}