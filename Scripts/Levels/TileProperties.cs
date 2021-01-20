using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileProperties : MonoBehaviour
{
    public enum PropType { TileSwap, Object};
    [SerializeField] private int AllowedWeight = 100;
    [SerializeField] private Transform CurTile = null;
    [SerializeField] private List<PropSpot> props = new List<PropSpot>();

    public void SetupTile(int TileID, int seed, bool Empty = false)
    {
        if (Empty) return;
        GetComponentInChildren<CheckPoint>().CheckPointID = TileID;
        int WeightLeft = AllowedWeight;
        List<PropSpot> avaliableProps = new List<PropSpot>(props);
        //Preserving old seed
        UnityEngine.Random.State oldstate = UnityEngine.Random.state;
        Random.InitState(seed);

        while (avaliableProps.Count != 0)
        {
            int id = Random.Range(0, (WeightLeft == AllowedWeight) ? avaliableProps.Count : avaliableProps.Count + 1);
            if (id == avaliableProps.Count) break;
            var Prop = avaliableProps[id];
            if(Prop.propType == PropType.TileSwap)
            {
                CurTile.gameObject.SetActive(false);
                Prop.connectedTile.gameObject.SetActive(true);
                CurTile = Prop.connectedTile.transform;
            }
            else
            {
                int Selected = Random.Range(0, Prop.propPositions.Count);
                for (int i = 0; i < Prop.propPositions.Count; i++)
                {
                    var obst = GameObject.Instantiate(TileSet.Instance.Obstacles[Prop.Code], Prop.propPositions[i], false);
                    if(Prop.Code == 4)
                    {
                        try
                        {
                            obst.GetComponent<Gate>().SetState(Selected == i);
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                    else if (Prop.Code == 5)
                    {
                        try
                        {
                            obst.GetComponent<Blocker>().Delay = Random.Range(1, 6);
                            seed++;
                            Random.InitState(seed);
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                }
            }
            WeightLeft -= Prop.Weight;
            props.Remove(Prop);
            avaliableProps.Clear();
            avaliableProps = GetListWithWeights(WeightLeft);
        }
        //Seed is reverted
        UnityEngine.Random.state = oldstate;
    }

    private List<PropSpot> GetListWithWeights(int WeightBelow)
    {
        List<PropSpot> tmp = new List<PropSpot>();
        for (int i = 0; i < props.Count; i++)
        {
            if (props[i].Weight <= WeightBelow)
                tmp.Add(props[i]);
        }
        return tmp;
    }
}

[System.Serializable]
public class PropSpot
{
    public TileProperties.PropType propType;
    [DrawIf("propType", TileProperties.PropType.Object)]
    public int Code;
    public int Weight;
    [DrawIf("propType", TileProperties.PropType.TileSwap)]
    public Transform connectedTile;
    [DrawIf("propType", TileProperties.PropType.Object)]
    public List<Transform> propPositions = new List<Transform>();
}
