using System.Collections.Generic;
using UnityEngine;

public interface IFileHandler
{
    public SPUM_Prefabs Save(SPUM_Prefabs prefabs, SPUM_Manager manager);
    public SPUM_Prefabs Edit(SPUM_Prefabs prefabs, SPUM_Manager manager);
    public SPUM_Prefabs[] Load();
    public void Delete(SPUM_Prefabs prefabs);
    public SPUM_Prefabs SaveConvertPrefabs(SPUM_Prefabs prefabs, SPUM_Manager manager);
    public (int, List<PreviewMatchingElement>) ValidateSpumFile(SPUM_Prefabs PrefabObject, SPUM_Manager manager);
}

