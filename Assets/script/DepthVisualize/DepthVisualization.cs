using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DepthVisualization : MonoBehaviour {
	public Camera _camera;
	public Material _mat;
    public RenderTexture _renderTexture;
    // Use this for initialization
    void Start () {
		if (_camera == null)
			_camera = GetComponent<Camera> ();
		if (_camera != null)
			_camera.depthTextureMode = DepthTextureMode.Depth;
	}

	void OnRenderImage(RenderTexture src, RenderTexture dest) {
		Graphics.Blit(src, dest, _mat);
	}

    void Awake() {
        if (_camera != null) {
            //_renderTexture = RenderTexture.GetTemporary(1024, 1024);
        }
    }

    void Update() {
        if (_camera != null) {
            return;
            RenderTexture.ReleaseTemporary(_renderTexture);
            _renderTexture = RenderTexture.GetTemporary(1024, 1024);
            
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = null;
        }
    }
}
