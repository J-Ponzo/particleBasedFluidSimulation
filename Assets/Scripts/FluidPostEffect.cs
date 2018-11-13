using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidPostEffect : MonoBehaviour
{
    [SerializeField]
    private Material postEffect_mat;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, postEffect_mat);
        
    }
}
