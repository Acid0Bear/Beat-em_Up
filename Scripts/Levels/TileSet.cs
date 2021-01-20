using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TileSet : ScriptableObject
{
    public List<GameObject> Obstacles;

    public GameObject StraightTile, StraightShatteredTile, RightTurn, LeftTurn, RoundTile, Finish;

    public static TileSet Instance;
}
