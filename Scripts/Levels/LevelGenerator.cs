using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    enum TileType {Straight, TurnLeft, TurnRight };
    class TileOptions {
        public GameObject tile;
        public List<TileType> testedTypes = new List<TileType>();
        public TileType curType;
        public bool isTypeTested(TileType type) => testedTypes.Contains(type);
        public TileOptions(GameObject tile, TileType initialType)
        {
            this.tile = tile;
            curType = initialType;
            testedTypes.Add(initialType);
        }

        public void SwapTile(GameObject newTile, TileType tileType)
        {
            curType = tileType;
            if(tile != null)
                Destroy(tile);
            tile = newTile;
            testedTypes.Add(tileType);
        }
    }

    public int AmountOfBlocks, AmountOfCorners;
    public int BlockZDifference;
    public bool Reset;
    public int testSeed = -1;
    private int seed => (NetClient.netClient != null && NetClient.netClient.IsConnected) ? NetClient.Seed : (testSeed != -1) ? testSeed : UnityEngine.Random.Range(0,1000000);

    private List<TileOptions> Tiles = new List<TileOptions>();
    private Vector3 LastPos;
    private int Turn;
    private bool LastCorner;

    private void Start()
    {
        this.transform.position = Vector3Int.FloorToInt(this.transform.position);
        LastPos = this.transform.position;
        TileSet.Instance = Resources.Load("ScriptableObjects/TileSets/JungleSet") as TileSet;
        
        GenerateTiles(seed);
    }

    private void Update()
    {
        if (Reset)
        {
            foreach (Transform child in this.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            LastPos = this.transform.position;
            Turn = 0;
            Tiles.Clear();
            GenerateTiles(seed);
            Reset = false;
        }
    }

    private void GenerateTiles(int _seed)
    {
        GameObject newTile;
        TileType newTileType;
        UnityEngine.Random.InitState(_seed);
        var start = GameObject.Instantiate(TileSet.Instance.StraightTile, LastPos, Quaternion.identity, this.transform);
        Tiles.Add(new TileOptions(start, TileType.Straight));
        for (int i = 1; i <= AmountOfBlocks; i++)
        {
            if (UnityEngine.Random.Range(0, 101) > 50 || LastCorner)
            {
                if ((newTile = CreateTile(newTileType = TileType.Straight)) == null)
                    if ((newTile = CreateTile((UnityEngine.Random.Range(0, 101) > 50) ? newTileType = TileType.TurnRight : newTileType = TileType.TurnLeft)) == null)
                    {
                        int DestroyedTiles = i - DeleteOrReplace(i-1, false);
                        i -= DestroyedTiles;
                        continue;
                    }
            }
            else
            {
                if ((newTile = CreateTile((UnityEngine.Random.Range(0, 101) > 50)? newTileType = TileType.TurnRight: newTileType = TileType.TurnLeft)) == null)
                    if ((newTile = CreateTile(newTileType = TileType.Straight)) == null)
                    {
                        int DestroyedTiles = i - DeleteOrReplace(i-1, false);
                        i -= DestroyedTiles;
                        continue;
                    }
                
            }
            newTile.name = i.ToString() + newTileType.ToString() + " Turn:" + Turn.ToString();
            if (newTileType != TileType.Straight)
                LastCorner = true;
            else
                LastCorner = false;
            Tiles.Add(new TileOptions(newTile, newTileType));
        }
        var finish = GameObject.Instantiate(TileSet.Instance.Finish, LastPos + GetOffset(Turn), Quaternion.identity, this.transform);
        finish.transform.localEulerAngles = new Vector3(0, Turn, 0);
        Tiles.Add(new TileOptions(finish, TileType.Straight));
        for (int i = 1;i < Tiles.Count - 1;i++)
        {
            try
            {
                var TileProp = Tiles[i].tile.GetComponent<TileProperties>();
                TileProp.SetupTile(i, _seed + i);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    private int DeleteOrReplace(int iter, bool IsReverted)
    {
        Destroy(Tiles[iter].tile);
        Tiles[iter].tile = null;
        GameObject newTile;
        if (!IsReverted)
        {
            if (Tiles[iter].curType != TileType.Straight)
            {
                Turn += (Tiles[iter].curType == TileType.TurnRight) ? -90 : 90;
            }
            LastPos -= GetOffset(Turn);
        }
        foreach (TileType tileType in (TileType[])Enum.GetValues(typeof(TileType)))
        {
            if (!Tiles[iter].isTypeTested(tileType))
            {
                if ((newTile = CreateTile(tileType)) != null)
                {
                    Tiles[iter].SwapTile(newTile, tileType);
                    Debug.Log("Swapped tile with id - " + iter);
                    return iter;
                }
                else
                {
                    Tiles[iter].testedTypes.Add(tileType);
                }
            }
        }
        Tiles.RemoveAt(iter);
        Debug.LogWarning("Destroyed tile with id - " + iter);
        return DeleteOrReplace(iter - 1, false);
    }

    private GameObject CreateTile(TileType type)
    {
        GameObject newTile;
        if (type == TileType.Straight)
        {
            if (CheckAvaliablity(Turn, LastPos + GetOffset(Turn)))
                return null;
            int val = UnityEngine.Random.Range(0, 151);
            newTile = GameObject.Instantiate((val <= 50) ? TileSet.Instance.StraightTile :
                (val <= 100)?TileSet.Instance.StraightShatteredTile : TileSet.Instance.RoundTile, LastPos + GetOffset(Turn), Quaternion.identity, this.transform);
            LastPos += GetOffset(Turn);
            newTile.transform.localEulerAngles = new Vector3(0, Turn, 0);
            return newTile;
        }
        else
        {
            int newTurn = (type == TileType.TurnRight)? 90:-90;
            if (CheckAvaliablity(Turn, LastPos + GetOffset(Turn)) || CheckAvaliablity(Turn + newTurn, LastPos + GetOffset(Turn + newTurn)))
                return null;
            newTile = GameObject.Instantiate((type == TileType.TurnRight)? TileSet.Instance.RightTurn : TileSet.Instance.LeftTurn, LastPos + GetOffset(Turn), Quaternion.identity, this.transform);
            LastPos += GetOffset(Turn);
            newTile.transform.localEulerAngles = new Vector3(0, Turn, 0);
            Turn += newTurn;
            if (Mathf.Abs(Turn) == 360) Turn = 0;
            return newTile;
        }
    }

    private bool CheckAvaliablity(int expectedTurn, Vector3 excpectedPos)
    {
        bool Result = true;
        Vector3 possiblePos = excpectedPos + GetOffset(expectedTurn);
        Result = Tiles.Exists(Tile => Tile.tile != null && (Tile.tile.transform.position == excpectedPos ||
                                   Tile.tile.transform.position == excpectedPos + GetOffset(expectedTurn) ||
                                   Tile.tile.transform.position == excpectedPos + GetOffset(expectedTurn + 90) ||
                                   Tile.tile.transform.position == excpectedPos + GetOffset(expectedTurn - 90)));
        return Result;
    }

    private Vector3 GetOffset(int ForTurn)
    {
        if (Mathf.Abs(ForTurn) == 360) ForTurn = 0;
        if (Mathf.Abs(ForTurn) == 90)
        {
            if (ForTurn > 0)
                return new Vector3(BlockZDifference, 0, 0);
            else
                return new Vector3(-BlockZDifference, 0, 0);
        }
        else if (Mathf.Abs(ForTurn) == 180)
        {
            return new Vector3(0, 0, -BlockZDifference);
        }
        else if (Mathf.Abs(ForTurn) == 270)
        {
            if (ForTurn > 0)
                return new Vector3(-BlockZDifference, 0, 0);
            else
                return new Vector3(BlockZDifference, 0, 0);
        }
        else
            return new Vector3(0, 0, BlockZDifference);
    }
}
