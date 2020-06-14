using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialReferences : MonoBehaviour
{
    public static Dictionary<string, Material> toOpaqueMaterials;
    public static Dictionary<string, Material> toTransparentMaterials;

    [SerializeField] Material[] opaqueMaterials;
    [SerializeField] Material[] transparentMaterials;

    private void Awake()
    {
        toOpaqueMaterials = new Dictionary<string, Material>();
        toTransparentMaterials = new Dictionary<string, Material>();

        for(int i = 0; i < opaqueMaterials.Length; i++)
        {
            toOpaqueMaterials.Add(transparentMaterials[i].name + " (Instance)", opaqueMaterials[i]);
            toTransparentMaterials.Add(opaqueMaterials[i].name + " (Instance)", transparentMaterials[i]);
        }
    }
}
