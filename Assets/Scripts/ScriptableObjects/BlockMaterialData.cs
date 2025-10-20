using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockMaterial", menuName = "Block/Material Data")]
public class BlockMaterialData : ScriptableObject
{
    [Header("Block Appearance")]
    public int colorID;        
    public Material material;     
}
