using System.Collections.Generic;
using UnityEngine;


public abstract class BaseCsvTable<T> : ScriptableObject
{
    public List<T> dataList = new();
    public abstract void LoadFromString(string csvText);
    public abstract string ToCSV();
}
