using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BLLCanvasController : MonoBehaviour {
    private void Start()
    {
        if (!PlayerPrefs.HasKey("Fake3D"))
        {
            PlayerPrefs.SetInt("Fake3D", 0);
        }
        if (!PlayerPrefs.HasKey("VRView"))
        {
            PlayerPrefs.SetInt("VRView", 1);
        }
    }
    public void OnStartBtnClicked()
    {
        SceneManager.LoadScene(1);
    }
    public void OnOptionsBtnClicked()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(true);
    }
    public void OnSeeCreditsBtnClicked()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(2).gameObject.SetActive(true);
    }
    public void OnBackCreditsBtnClicked()
    {
        transform.GetChild(2).gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(true);
    }
    public void OnFake3DToggleStatusChanged(Toggle fake3DToogle)
    {
        PlayerPrefs.SetInt("Fake3D", (fake3DToogle.isOn == false) ? 0 : 1);
    }
    public void OnVRViewToggleClicked(Toggle vrViewToggle)
    {
        PlayerPrefs.SetInt("VRView", (vrViewToggle.isOn == false) ? 0 : 1);
    }
    public void OnDownloadSetupButtonClicked()
    {
        Application.OpenURL("/storage/emulated/0/Download/HtmlTest.html"); //Open Github Link
        //DownloadFile("Setup");
        //_ShowAndroidToastMessage("File downloaded to /storage/emulated/0/Download/DNAViewerSetup.pdf");
    }
    //void DownloadFile(string filename)
    //{
    //    string path = System.IO.Path.Combine(Application.persistentDataPath, filename + ".pdf");
    //    //Debug.Log("Path: " + path);

    //    TextAsset pdfTemp = Resources.Load(filename, typeof(TextAsset)) as TextAsset;
    //    //Debug.Log("pdfTem: " + pdfTemp);

    //    File.WriteAllBytes(path, pdfTemp.bytes);
    //    //Debug.Log("Bytes written");

    //    System.IO.File.Copy(Application.streamingAssetsPath + "/Setup.pdf", "/storage/emulated/0/Download/DNAViewerSetup.pdf");

    //    Debug.Log("Path: " + path);
    //    //Application.OpenURL("/storage/emulated/0/Download/HtmlTest.html"); //Open Github Link
    //    Debug.Log("Urlopened");
    //}

    public void OnBackBtnClicked(Transform canvas)
    {
        canvas.GetChild(0).gameObject.SetActive(true);
        canvas.GetChild(1).gameObject.SetActive(false);
    }

    /// <param name="message">Message string to show in the toast.</param>
    //private void _ShowAndroidToastMessage(string message)
    //{
    //    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    //    AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    //    if (unityActivity != null)
    //    {
    //        AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
    //        unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
    //        {
    //            AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
    //            toastObject.Call("show");
    //        }));
    //    }
    //}
}
