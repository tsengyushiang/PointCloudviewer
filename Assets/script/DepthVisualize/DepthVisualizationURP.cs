using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class DepthVisualizationURP : MonoBehaviour {
	public Camera _camera;
	public Material _mat;
    private RenderTexture _renderTexture;
    // Use this for initialization
    void Start () {
		if (_camera == null)
			_camera = GetComponent<Camera> ();
		if (_camera != null) {
			_camera.depthTextureMode = DepthTextureMode.Depth;
            RenderPipelineManager.endCameraRendering += EndCameraRender;
        }
	}
    
    //not work on URP / HDRP
	void OnRenderImage(RenderTexture src, RenderTexture dest) {
        Debug.Log("render");
		Graphics.Blit(src, dest, _mat);
	}

    void EndCameraRender(ScriptableRenderContext context, Camera cam) {
        if(cam == _camera) {
            //shader render
            Debug.Log("render");
            Graphics.Blit(_renderTexture, _camera.targetTexture, _mat);
        }
    }

    void Awake() {
        if (_camera != null) {
            //_renderTexture = RenderTexture.GetTemporary(1024, 1024);
        }
    }

    void Update() {
        if (_camera != null) {
            Debug.Log("update");
            //catch original texture for shader input
            _renderTexture = _camera.targetTexture;
            //bind material texture
            _mat.mainTexture = _renderTexture;
            return;
            RenderTexture.ReleaseTemporary(_renderTexture);
            _renderTexture = RenderTexture.GetTemporary(1024, 1024);
            
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = null;
        }
    }
}
