using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Player
{
    public string Name { get; set; }

    public Player()
    {
    }

    public Player(string name)
    {
        this.Name = name;
    }
    public override string ToString()
    {
        return Name;
    }
}
