using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Block/BlockMaterialLibrary")]
public class BlockMaterialLibrary : ScriptableObject
{
    public List<BlockMaterialData> materials;
}
