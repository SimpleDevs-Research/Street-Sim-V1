using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanoramaCamera : MonoBehaviour
{

    private Camera targetCamera;
    public RenderTexture cubeMapLeft;
    public RenderTexture equirectRT;
    public Material postprocessMaterial;

    private void Awake() {
        targetCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            Capture();
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture des) {
        RenderTexture temporaryTexture = RenderTexture.GetTemporary(src.width, src.height);
		Graphics.Blit(src, temporaryTexture, postprocessMaterial, 0);
		Graphics.Blit(temporaryTexture, des, postprocessMaterial, 1);
		RenderTexture.ReleaseTemporary(temporaryTexture);
    }

    public void Capture() {
        targetCamera.RenderToCubemap(cubeMapLeft);
        cubeMapLeft.ConvertToEquirect(equirectRT);
        Save(equirectRT);
    }

    public void Save(RenderTexture rt) {
        Texture2D tex = new Texture2D(rt.width, rt.height);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0,0,rt.width,rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        string path = Application.dataPath + "/Panorama.png";
        System.IO.File.WriteAllBytes(path,bytes);
        Debug.Log("Saving image to " + path);
    }
}
