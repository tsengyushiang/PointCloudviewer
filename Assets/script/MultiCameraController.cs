using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading;

[System.Serializable]
public class CameraParamList {
    public List<CameraParam> camera = new List<CameraParam>();
}

[System.Serializable]
public class CameraParam {
    public int id;
    public string[] img;
    public Matrix4x4 world2screenMat;
    public Vector3 pos;
    public Quaternion quat;

}

public class MultiCameraController : MonoBehaviour {
    public float initAngle = -90.0f;
    public float radius = 3.5f;

    public int ceilingCameraCount = 5;
    public float ceilingradius = 3.5f;

    public bool isPlaying;
    public Camera mainCamera;

    private Camera[] cameras;
    private RenderTexture[] renderTextures;
    private int activeCameraID = 0;

    private bool isRecord = false;
    private const int recordLen = 1;
    private int curRecordFrame = 0;
    private bool isSaving = false;

    //8k 7680*4320
    //4k 3840*2260
    private int resWidth = 1024;
    private int resHeight = 1024;

    private List<List<byte[]>> camData;


    void Awake() {

        cameras = GetComponentsInChildren<Camera>();
        renderTextures = new RenderTexture[cameras.Length];

        int normalCameraCount = cameras.Length-ceilingCameraCount;

        int col = 3;
        float heightStep = 0.5f;
        float angleStep = 360 / (normalCameraCount/col);
        float currentAngle = initAngle;

        camData = new List<List<byte[]>>();

        for (int i = 0; i < normalCameraCount; ++i) {
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 newPosition = new Vector3(radius * Mathf.Cos(rad), i%col*heightStep, radius * Mathf.Sin(rad)) + this.transform.position;
            cameras[i].transform.position = newPosition;
            cameras[i].transform.rotation = Quaternion.LookRotation(this.transform.position - newPosition, Vector3.up);

            if(i%col==0){
                currentAngle += angleStep;
            }

            renderTextures[i] = new RenderTexture(resWidth, resHeight, 24, RenderTextureFormat.ARGB32);
            renderTextures[i].depth = 0;
            renderTextures[i].Create();

            cameras[i].targetTexture = renderTextures[i];
            camData.Add(new List<byte[]>());
        }

        angleStep = 360 / (ceilingCameraCount);
        currentAngle = initAngle;
        
        for (int i = normalCameraCount; i < cameras.Length; ++i) {    
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 newPosition = new Vector3(ceilingradius * Mathf.Cos(rad), (col-1)*heightStep, ceilingradius * Mathf.Sin(rad)) + this.transform.position;
            cameras[i].transform.position = newPosition;
            cameras[i].transform.rotation = Quaternion.LookRotation(this.transform.position - newPosition, Vector3.up);
            currentAngle += angleStep;            

            renderTextures[i] = new RenderTexture(resWidth, resHeight, 24, RenderTextureFormat.ARGB32);
            renderTextures[i].depth = 0;
            renderTextures[i].Create();

            cameras[i].targetTexture = renderTextures[i];
            camData.Add(new List<byte[]>());
        }

        StartCoroutine(startRecord());
    }
    IEnumerator startRecord(){
        yield return new WaitForSeconds(5);
        isRecord = true;   
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            activeCameraID = (activeCameraID + 1) % cameras.Length;
            SetCameraEnable(activeCameraID);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            activeCameraID = (activeCameraID - 1 + cameras.Length) % cameras.Length;
            SetCameraEnable(activeCameraID);
        } else if (Input.GetKeyDown(KeyCode.P)) {
            isPlaying = !isPlaying;
        } else if (Input.GetKeyDown(KeyCode.S) && !isRecord) {            
            curRecordFrame = 0;
        }

        //handle record
        if (isRecord) {
            //pause screen
            isPlaying = false;
            //check is record finish
            if (curRecordFrame >= recordLen) {
                isRecord = false;
                //save screen shot
                new Thread(SaveScreenShot).Start();
                //save json
                SaveJson(recordLen);
                Debug.Break();
                return;
            }
            cacheScreenShot();
            //check record finish
            curRecordFrame++;
            //continue screen
            isPlaying = true;
        }
    }

    void SaveJson(int len) {
        CameraParamList list = new CameraParamList();

        for (int i = 0; i < cameras.Length; ++i) {
            CameraParam cam = new CameraParam();
            cam.world2screenMat = cameras[i].projectionMatrix * cameras[i].worldToCameraMatrix;
            cam.id = i;
            cam.pos = cameras[i].transform.position;
            cam.quat = cameras[i].transform.rotation;
            cam.img = new string[len];
            for (int frame = 0; frame < len; frame++) {
                cam.img[frame] = "/" + i.ToString() + "_" + frame + ".png";
            }
            list.camera.Add(cam);
        }

        string camInfo = JsonUtility.ToJson(list);
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/camera.json", camInfo);
    }

    void SetCameraEnable(int cameraID) {
        activeCameraID = Math.Min(Math.Max(cameraID, 0), cameras.Length - 1);
        //for(int i = 0; i < cameras.Length; ++i)
        //{
        //    cameras[i].enabled = false;
        //}
        //mainCamera.transform.position = cameras[activeCameraID].transform.position;
        //mainCamera.transform.rotation = cameras[activeCameraID].transform.rotation;
    }

    void cacheScreenShot() {
        for (int i = 0; i < cameras.Length; ++i) {
            camData[i].Add(saveRenderTextureToByte(cameras[i].targetTexture));
        }
    }

    void SaveScreenShot() {
        for (int camIndex = 0; camIndex < camData.Count; camIndex++) {
            for (int dataIndex = 0; dataIndex < camData[camIndex].Count; dataIndex++) {
                string imgPath = saveByteToFile(camData[camIndex][dataIndex], "/" + camIndex.ToString() + "_" + dataIndex + ".png");
                Debug.Log(imgPath);
            }
        }
    }

    private string SaveRenderTextureToFile(RenderTexture rt, string fileName) {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();

        string path = Application.streamingAssetsPath + fileName;
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }

    private byte[] saveRenderTextureToByte(RenderTexture rt) {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();
        return bytes;
    }

    private string saveByteToFile(byte[] data, string fileName) {
        string path = Application.streamingAssetsPath + fileName;
        System.IO.File.WriteAllBytes(path, data);
        return path;
    }
}
