using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
//using OpenCVForUnitySample;
using GoogleARCore;
using System.Linq;
using GoogleARCore.Examples.ComputerVision;
using System;
using OpenCVForUnityExample;
using System.Threading;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

public class TrackHandWithMarker : MonoBehaviour
{

    /// <summary>
    /// The threashold slider.
    /// </summary>
    public int threashold = 10000;

    /// <summary>
    /// The BLOB color hsv.
    /// </summary>
    private Scalar blobColorHsv;

    ///// <summary>
    ///// The BLOB color rgba.
    ///// </summary>
    //private Scalar blobColorRgba;

    /// <summary>
    /// The detector.
    /// </summary>
    private ColorBlobDetector detector;

    /// <summary>
    /// The spectrum mat.
    /// </summary>
    private Mat spectrumMat;

    ///// <summary>
    ///// The is color selected.
    ///// </summary>
    //private bool isColorSelected = false;

    /// <summary>
    /// The SPECTRU m_ SIZ.
    /// </summary>
    private Size SPECTRUM_SIZE;

    /// <summary>
    /// The CONTOU r_ COLO.
    /// </summary>
    private Scalar CONTOUR_COLOR;

    /// <summary>
    /// The CONTOU r_ COLO r_ WHIT.
    /// </summary>
    private Scalar CONTOUR_COLOR_WHITE;

    /// <summary>
    /// The number of fingers.
    /// </summary>
    int numberOfFingers = 0;

    /// <summary>
    /// The number of fingers text.
    /// </summary>
    //public UnityEngine.UI.Text numberOfFingersText;

    /// <summary>
    /// The web cam texture to mat helper.
    /// </summary>

    /// <summary>
    /// The stored touch point.
    /// </summary>
    //[HideInInspector]
    //public Point storedTouchPoint;

    public GameObject screen;

    Mat rgbaMat;
    //Color32[] colors;

    public GameObject ARCamera;

    Texture2D texForOpenCV;
    //Texture2D temp;

    //Marker Detection
    public Camera markerCam;
    public float markerLength = 1;
    Mat rgbMat;
    Dictionary dictionary;
    List<Mat> corners;
    Mat ids;
    DetectorParameters detectorParams;
    List<Mat> rejected;
    /// <summary>
    /// 0 = ARGameObject for right hand
    /// 1 = ARGameObject for left hand
    /// </summary>
    public GameObject[] ARGameObjects;
    Matrix4x4 invertYM;
    Matrix4x4 ARM;
    Matrix4x4 transformationM;
    Matrix4x4 invertZM;
    Mat rotMat;
    Mat rvecs;
    Mat camMatrix;
    MatOfDouble distCoeffs;
    Mat tvecs;

    byte[] m_EdgeImage;
    int m_ImageWidth;
    int m_ImageHeight;

    bool initDone;

    Mat rotatedRgbaMat; //System.Windows
    public int width;
    public int height;

    BLLDNAController BLLDNAController;
    //public bool testRot = false;

    Texture2D[] texForHands = new Texture2D[2];
    public RawImage[] screensForHands;

    List<Point> listPo = new List<Point>();

    //Use this when you mkae the opencv operations in one frame
    /// <summary>
    /// The render thread coroutine.
    /// </summary>
    //IEnumerator renderThreadCoroutine;

    // Use this for initialization
    void Start()
    {
        //renderThreadCoroutine = CallAtEndOfFrames();                                  //Use this when you mkae the opencv operations in one frame      

        if (PlayerPrefs.GetInt("Fake3D") == 0)
        {
            ARCamera.transform.localPosition = Vector3.zero;
            ARCamera.transform.parent.GetChild(1).localPosition = Vector3.zero;
        }
        //else
        //{
        //    ARCamera.transform.localPosition = new Vector3(0.0325f, 0);
        //    ARCamera.transform.parent.GetChild(1).localPosition = new Vector3(-0.0325f, 0);
        //}
        if (PlayerPrefs.GetInt("VRView") == 0)
        {
            GameObject.Find("VRView").SetActive(false);
        }
        
        BLLDNAController = GetComponent<BLLDNAController>();

        if (Application.isEditor)                     // cause Editor is Portrait
        {
            ARCamera.GetComponent<TextureReader>().ImageWidth = 480;
            height = ARCamera.GetComponent<TextureReader>().ImageHeight = 640;
        }

        width = ARCamera.GetComponent<TextureReader>().ImageWidth;
        height = ARCamera.GetComponent<TextureReader>().ImageHeight;
        //mesh = hand.GetComponent<MeshFilter>().mesh;
        //storedTouchPoint = null;

        if (Application.platform == RuntimePlatform.Android)
        {
            ARCamera.GetComponent<TextureReader>().OnImageAvailableCallback += OnImageAvailable;
        }
        else
        {
            ARCamera.GetComponent<TextureReader>().enabled = false;
        }
        StartCoroutine(setMat());
        //StartCoroutine(AfterStart());
    }

    //Set texForOpenCV
    private void OnImageAvailable(TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
    {
        if (texForOpenCV == null || m_EdgeImage == null || m_ImageWidth != width || m_ImageHeight != height)
        {
            texForOpenCV = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            m_EdgeImage = new byte[width * height * 4];
            m_ImageWidth = width;
            m_ImageHeight = height;

            //Mat: 1.: Zeilen(Höhe); 2.: Spalten(Weite)
            rgbaMat = new Mat(height, width, CvType.CV_8UC4);
            Debug.Log("rgbaMat width: " + rgbaMat.width());
            //temp = new Texture2D(width, height);
        }

        System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, m_EdgeImage, 0, bufferSize);

        // Update the rendering texture with the sampled image.
        texForOpenCV.LoadRawTextureData(m_EdgeImage);
        texForOpenCV.Apply();
    }
    //private int _ChooseCameraConfiguration(List<CameraConfig> supportedConfigurations)
    //{
    //    Vector2 ImageSize = supportedConfigurations[0].ImageSize;
    //    ImageSize = supportedConfigurations[supportedConfigurations.Count - 1].ImageSize;

    //    return supportedConfigurations.Count - 1;
    //}

    //Windows: Set texForOpenCV
    //Android: Skip
    IEnumerator setMat()
    {
        Debug.Log("Try to get ARCore Texture");
        yield return new WaitForSeconds(1f);
        if (Application.platform == RuntimePlatform.Android)
        {
            OnWebCamTextureToMatHelperInited();
        }
        else
        {
            Frame.CameraImage.AcquireCameraImageBytes();
            if (Frame.CameraImage.Texture != null)
            {
                //if (colors == null || colors.Length != Screen.width * Screen.height)
                //    colors = new Color32[width * height];
                //temp = new Texture2D(height, width);
                rgbaMat = new Mat(height, width, CvType.CV_8UC4);
                texForOpenCV = new Texture2D(Frame.CameraImage.Texture.width, Frame.CameraImage.Texture.height, TextureFormat.RGBA32, false);
                texForOpenCV = (Texture2D)Frame.CameraImage.Texture;
                //TextureTools.scale(texForOpenCV, width, height); // =TextureReader Image Size
                //screen.GetComponent<RawImage>().texture = temp;
                //gameObject.GetComponent<Renderer>().material.mainTexture = temp;
                rotatedRgbaMat = new Mat(height, width, CvType.CV_8UC4);
                if (texForOpenCV == null || texForOpenCV.width == 0)
                {
                    StartCoroutine(setMat());
                }
                else
                {
                    OnWebCamTextureToMatHelperInited();
                }
            }
            else
            {
                StartCoroutine(setMat());
            }
        }
    }

    /// <summary>
    /// Raises the web cam texture to mat helper inited event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInited()
    {
        //rotatedRgbaMat = new Mat(Frame.CameraImage.Texture.height, Frame.CameraImage.Texture.width, CvType.CV_8UC4);
        Debug.Log("OnWebCamTextureToMatHelperInited");

        //Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();
        rgbaMat = GetMat();

        //gameObject.GetComponent<Renderer>().material.mainTexture = temp;

        //gameObject.transform.localScale = new Vector3(rgbaMat.cols(), rgbaMat.rows(), 1);

        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        float height = rgbaMat.height();
        float width = rgbaMat.width();

        gameObject.transform.localScale = new Vector3(width, height, 1);

        float imageSizeScale = 1.0f;
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            imageSizeScale = (float)Screen.height / (float)Screen.width;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        detector = new ColorBlobDetector();
        spectrumMat = new Mat();
        //blobColorRgba = new Scalar (255);
        blobColorHsv = new Scalar(255);
        SPECTRUM_SIZE = new Size(200, 64);
        CONTOUR_COLOR = new Scalar(255, 0, 0, 255);
        CONTOUR_COLOR_WHITE = new Scalar(255, 255, 255, 255);

        //Marker Detection
        detectorParams = DetectorParameters.create();
        dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_50);
        corners = new List<Mat>();
        ids = new Mat();
        rejected = new List<Mat>();
        rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        rvecs = new Mat();
        rotMat = new Mat(3, 3, CvType.CV_64FC1);
        transformationM = new Matrix4x4();
        invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
        Debug.Log("invertYM " + invertYM.ToString());
        invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        Debug.Log("invertZM " + invertZM.ToString());
        distCoeffs = new MatOfDouble(0, 0, 0, 0);
        Debug.Log("distCoeffs " + distCoeffs.dump());
        tvecs = new Mat();

        int max_d = (int)Mathf.Max(width, height);
        //int max_d = (int)Mathf.Max(2, 1);
        double fx = max_d;
        double cx = width / 2.0f;
        double fy = max_d;
        double cy = height / 2.0f;
        camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);
        Debug.Log("camMatrix " + camMatrix.dump());

        Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
        //Size imageSize = new Size(2, 1);
        double[] fovx = new double[1];
        double[] fovy = new double[1];
        double[] focalLength = new double[1];
        Point principalPoint = new Point(0, 0);
        double[] aspectratio = new double[1];
        Calib3d.calibrationMatrixValues(camMatrix, imageSize, 0, 0, fovx, fovy, focalLength, principalPoint, aspectratio);

        double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
        double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

        //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
        if (widthScale < heightScale)
        {
            markerCam.GetComponent<Camera>().fieldOfView = (float)(fovx[0] * fovXScale);
        }
        else
        {
            markerCam.GetComponent<Camera>().fieldOfView = (float)(fovy[0] * fovYScale);
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            screen.transform.localRotation = Quaternion.Euler(0, 180, 180);
        }
        else
        {
            screen.transform.localRotation = Quaternion.Euler(0, 180, 90);
        }
        texForHands[0] = new Texture2D(rgbaMat.width(), rgbaMat.height(), TextureFormat.RGBA32, false);
        texForHands[1] = new Texture2D(rgbaMat.width(), rgbaMat.height(), TextureFormat.RGBA32, false);

        screen.GetComponent<RawImage>().texture = texForOpenCV;
        initDone = true;
        //StartCoroutine(renderThreadCoroutine);                                                                                                        //Use this when you mkae the opencv operations in one frame
    }

    /// <summary>
    /// Raises the web cam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (spectrumMat != null)
            spectrumMat.Dispose();

        if (rgbMat != null)
            rgbMat.Dispose();
        if (ids != null)
            ids.Dispose();
        foreach (var item in corners)
        {
            item.Dispose();
        }
        corners.Clear();
        foreach (var item in rejected)
        {
            item.Dispose();
        }
        rejected.Clear();
        if (rvecs != null)
            rvecs.Dispose();
        if (tvecs != null)
            tvecs.Dispose();
        if (rotMat != null)
            rotMat.Dispose();

    }

    /// <summary>
    /// Raises the web cam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }
    public float xScale = 24427.48092f;
    public float yScale = 21818.18182f;
    public float zScale = 10;

    // Update is called once per frame
    void Update()
    {
        if (!initDone)
        {
            return;
        }
        if(Time.frameCount % 3 == 0)
        {
            //float t = Time.realtimeSinceStartup;
            rgbaMat.setTo(new Scalar(0, 0, 0, 0));
            rgbMat.setTo(new Scalar(0, 0, 0, 0));
            rgbaMat = GetMat();
            Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
            //Debug.Log("Get Mat Time: " + (Time.realtimeSinceStartup - t));
        }
        else if (Time.frameCount % 3 == 1)
        {
            //float t = Time.realtimeSinceStartup;

            //Marker Detection
            Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejected);        //rejected: Marker mit ungüliger Codierung(-> andere Quadrate)
            Aruco.estimatePoseSingleMarkers(corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);
            //Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(255, 0, 0));
            //Aruco.drawAxis(rgbMat, camMatrix, distCoeffs, rvecs, tvecs, markerLength * 0.5f);
            for (int i = 0; i < ids.rows(); i++)
            {
                int id = (int)ids.get(i, 0)[0];
                if (id > 1)
                {
                    continue;
                }
                //int p = (ids.total() == 1) ? 0 : id;
                ARGameObjects[id].SetActive(true);
                //print("Marker detected: " + ids.get(i, 0)[0]);

                Calib3d.Rodrigues(rvecs.row(i), rotMat);
                //Debug.Log("transform: " + rotMat.size());
                transformationM.SetRow(0, new Vector4((float)rotMat.get(0, 0)[0], (float)rotMat.get(0, 1)[0], (float)rotMat.get(0, 2)[0], (float)tvecs.get(i, 0)[0]));
                transformationM.SetRow(1, new Vector4((float)rotMat.get(1, 0)[0], (float)rotMat.get(1, 1)[0], (float)rotMat.get(1, 2)[0], (float)tvecs.get(i, 0)[1]));
                transformationM.SetRow(2, new Vector4((float)rotMat.get(2, 0)[0], (float)rotMat.get(2, 1)[0], (float)rotMat.get(2, 2)[0], (float)tvecs.get(i, 0)[2]));
                transformationM.SetRow(3, new Vector4(0, 0, 0, 1));
                //ARGameObject.transform.position = new Vector3((float)tvecs.get(0, 0)[0], (float)tvecs.get(0, 0)[1], (float)tvecs.get(0, 0)[2]);
                ARM = markerCam.transform.localToWorldMatrix * invertYM * transformationM * invertZM;
                ARUtils.SetTransformFromMatrix(ARGameObjects[id].transform, ref ARM, xScale, yScale, zScale);//, screen.GetComponentInParent<RectTransform>().localScale.x /width, screen.GetComponentInParent<RectTransform>().localScale.y /height, 0.1f);
                screensForHands[id].transform.localPosition = new Vector3(0, 0, -1 + ARGameObjects[id].transform.localPosition.z);
                screensForHands[id].transform.localScale = new Vector3(ARGameObjects[id].transform.localPosition.z, ARGameObjects[id].transform.localPosition.z, 1);
                //
                Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
                //Debug: Remove for better Perfomance
                //Debug.Log("rgbaMat: " + rgbaMat.width() + ", " + rgbaMat.height() + "; temp: " + temp.width + ", " + temp.height);

                //Debug.Log("Time for Marker Detection: " + (Time.realtimeSinceStartup - t));
            }
        }
        else if (Time.frameCount % 3 == 2)
        {
            //float t = Time.realtimeSinceStartup;
            for (int i = 0; i< ids.total(); i++) {
                int id = (int)ids.get(i, 0)[0];
                //Debug.Log("ID: " + id);
                if(id > 1)
                {
                    continue;
                }
                Point[] rectAroundHand = new Point[4];
                int p = (ids.total() == 1) ? 0 : id;
                Point upperLeftCorner = new Point(corners[p].col(0).get(0, 0)[0], corners[p].col(0).get(0, 0)[1]);
                Point upperRightCorner = new Point(corners[p].col(1).get(0, 0)[0], corners[p].col(1).get(0, 0)[1]);
                Point bottomRightCorner = new Point(corners[p].col(2).get(0, 0)[0], corners[p].col(2).get(0, 0)[1]);
                Point bottomLeftCorner = new Point(corners[p].col(3).get(0, 0)[0], corners[p].col(3).get(0, 0)[1]);
                Point center = bottomLeftCorner + ((upperRightCorner - bottomLeftCorner) / 2);

                float yoffset = (int)Vector2.Distance(new Vector2((float)bottomRightCorner.x, (float)bottomRightCorner.y), new Vector2((float)bottomLeftCorner.x, (float)bottomLeftCorner.y)) / 5;
                Vector2 yoffsetDir = Vector2.Perpendicular(new Vector2((float)bottomRightCorner.x, (float)bottomRightCorner.y) - new Vector2((float)bottomLeftCorner.x, (float)bottomLeftCorner.y));

                rectAroundHand[0] = upperLeftCorner + ((yoffset / 5) * (upperLeftCorner - center));
                rectAroundHand[1] = upperRightCorner + ((yoffset / 5) * (upperRightCorner - center));
                rectAroundHand[2] = bottomRightCorner + ((yoffset / 5) * (bottomRightCorner - center));
                rectAroundHand[3] = bottomLeftCorner + ((yoffset / 5) * (bottomLeftCorner - center));
                Mat mask = Mat.zeros(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8U);
                List<MatOfPoint> rotatedRect = new List<MatOfPoint>();
                rotatedRect.Add(new MatOfPoint(rectAroundHand));
                Imgproc.drawContours(mask, rotatedRect, -1, new Scalar(255), Core.FILLED);

                Mat crop = Mat.zeros(100, 100, CvType.CV_8UC4);
                rgbaMat.copyTo(crop, mask);
                mask.release();

                Point c = new Point();
                c = bottomRightCorner + ((bottomLeftCorner - bottomRightCorner) / 2);
                //Debug.Log(yoffset / 5);
                c += (0.3 + yoffset / 120) * (new Point(yoffsetDir.x, yoffsetDir.y));
                //Debug.Log("C: " + c);
                Scalar handColor;
                try
                {
                    handColor = onTouch(rgbaMat, c);
                    if (handColor == null)
                    {
                        //Debug.Log("HandColor = null; ID: " + id);
                        screensForHands[id].color = new Color(0, 0, 0, 0);
                        return;
                    }
                }
                catch (CvException e)
                {
                    Debug.Log("CVExeception: " + e);
                    screensForHands[id].color = new Color(0, 0, 0, 0);
                    return;
                }
                Imgproc.circle(rgbaMat, c, 2, new Scalar(255, 0, 255, 255), -1);

                //Debug
                Point[] rectAroundMarker = new Point[4];
                rectAroundMarker[0] = upperLeftCorner + ((yoffset / 50) * (upperLeftCorner - center));
                rectAroundMarker[1] = upperRightCorner + ((yoffset / 50) * (upperRightCorner - center));
                rectAroundMarker[2] = bottomRightCorner + ((yoffset / 50) * (bottomRightCorner - center));
                rectAroundMarker[3] = bottomLeftCorner + ((yoffset / 50) * (bottomLeftCorner - center));
                List<MatOfPoint> marker = new List<MatOfPoint>();
                marker.Add(new MatOfPoint(rectAroundMarker));
                Imgproc.drawContours(crop, marker, -1, handColor, Core.FILLED);
                //
                Mat handMat = handPoseEstimationProcess(crop);
                crop.release();

                Vector2 h_finger =Vector2.zero;
                if (handMat != null)
                {
                    Mat textMat = Mat.zeros(handMat.rows(), handMat.cols(), CvType.CV_8UC4);
                    double xPos = (bottomLeftCorner + ((upperLeftCorner - bottomLeftCorner) / 2)).x;
                    Imgproc.putText(textMat, id == 0 ? "Rechte Hand" : "Linke Hand", new Point(xPos, center.y - 2*yoffset), Core.FONT_HERSHEY_SIMPLEX, yoffset / 40, new Scalar(255, 255, 255, 255), 2);
                    Imgproc.putText(textMat, "Time: " + DateTime.Now.Hour + ":" + DateTime.Now.Minute, new Point(xPos, center.y -yoffset), Core.FONT_HERSHEY_SIMPLEX, yoffset / 40, new Scalar(255, 255, 255, 255), 2);
                    Imgproc.putText(textMat, "Battery: " + (SystemInfo.batteryLevel != -1 ? (SystemInfo.batteryLevel * 100) + "%"  : SystemInfo.batteryStatus.ToString()), new Point(xPos, center.y), Core.FONT_HERSHEY_SIMPLEX, yoffset / 40, new Scalar(255, 255, 255, 255), 2);
                    Imgproc.putText(textMat, "Fingers: " + numberOfFingers, new Point(xPos, center.y + yoffset), Core.FONT_HERSHEY_SIMPLEX, yoffset / 40, new Scalar(255, 255, 255, 255), 2);
                    ////Debug.Log("Angle: " + -AngleInDeg(bottomLeftCorner, bottomRightCorner));
                    textMat = RotateMat(textMat, -AngleInDeg(bottomLeftCorner, bottomRightCorner), center);
                    handMat += textMat;
                    textMat.release();
                    //Debug.Log("Mat Width: " + handMat.width() + " Tex Width: " + texForHands[id].width);
                    //Debug.Log("Mat Height: " + handMat.height());
                    OpenCVForUnity.Utils.matToTexture2D(handMat, texForHands[id]);
                    handMat.release();
                    texForHands[id].Apply();
                    screensForHands[id].texture = texForHands[id];
                    screensForHands[id].color = Color.white;

                    foreach(Point point in listPo)
                    {
                        if(h_finger == Vector2.zero)
                        {
                            h_finger = new Vector2((float)point.x, (float)point.y);
                            continue;
                        }
                        if (Vector2.Distance(new Vector2((float)bottomLeftCorner.x, (float)bottomLeftCorner.y), new Vector2((float)point.x, (float)point.y)) > Vector2.Distance(new Vector2((float)bottomLeftCorner.x, (float)bottomLeftCorner.y), h_finger))
                        {
                            Vector2 s = GetIntersectionPointCoordinates(new Vector2((float)bottomLeftCorner.x, (float)bottomLeftCorner.y), new Vector2((float)bottomRightCorner.x, (float)bottomRightCorner.y), new Vector2((float)point.x, (float)point.y), new Vector2((float)point.x, (float)point.y) + Vector2.down);
                            if (bottomRightCorner.x > bottomLeftCorner.x) //Rechts
                            {
                                if (point.y < s.y)
                                {
                                    //Imgproc.circle(rgbaMat, new Point((double)s.x, (double)s.y), 6, new Scalar(0, 255, 255, 255), -1);
                                    h_finger = new Vector2((float)point.x, (float)point.y);
                                }
                            }
                            else if (bottomLeftCorner.x > bottomRightCorner.x) //Links
                            {
                                if (point.y > s.y)
                                {
                                    h_finger = new Vector2((float)point.x, (float)point.y);
                                }
                            }
                            else if (bottomRightCorner.y > bottomLeftCorner.y) // Unten
                            {
                                if (point.x > s.x)
                                {
                                    h_finger = new Vector2((float)point.x, (float)point.y);
                                }
                            }
                            else // Oben
                            {
                                if (point.x < s.x)
                                {
                                    h_finger = new Vector2((float)point.x, (float)point.y);
                                }
                            }
                        }
                    }
                    if (h_finger == new Vector2((float)listPo[0].x, (float)listPo[0].y))
                    {
                        h_finger = Vector2.zero;
                    }
                    Imgproc.circle(rgbaMat, new Point((double)h_finger.x, (double)h_finger.y), 6, new Scalar(0, 0, 255, 255), -1);
                    //h_finger = ARGameObjects[id].transform.localPosition.z * new Vector2((float)h_finger.x / 640, (float)h_finger.y / 480);
                    //Debug.Log(h_finger);
                }
                else
                {
                    screensForHands[id].color = new Color(0, 0, 0, 0);
                }
                BLLDNAController.SetData(numberOfFingers, id, h_finger);

                //Texture2D testt = new Texture2D(rgbaMat.width(), rgbaMat.height(), TextureFormat.RGBA32, false);
                //OpenCVForUnity.Utils.matToTexture2D(rgbaMat, testt/*, GetBufferColors()*/);
                //testt.Apply();
                //GameObject.Find("RawImage").GetComponent<RawImage>().texture = testt;
            }
            //Debug.Log("Time for Hand Detection: " + (Time.realtimeSinceStartup - t));

            if (ids.total() == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    screensForHands[i].color = new Color(0, 0, 0, 0);
                    ARGameObjects[i].SetActive(false);
                }
            }
            else if (ids.total() == 1)
            {
                //Debug.Log("Marker: " + ids.get(0, 0)[0]);
                if (ids.get(0, 0)[0] == 0)
                {
                    screensForHands[1].color = new Color(0, 0, 0, 0);
                    ARGameObjects[1].SetActive(false);
                }
                else if (ids.get(0, 0)[0] == 1)
                {
                    screensForHands[0].color = new Color(0, 0, 0, 0);
                    ARGameObjects[0].SetActive(false);
                }
            }
        }
    }
    Mat RotateMat(Mat src, float angle, Point center)
    {
        //int len = src.cols();
        //Point pt = new Point(src.cols() / 2, src.rows() / 2);
        Mat r = Imgproc.getRotationMatrix2D(center, angle, 1.0);
        Mat dst = new Mat();
        Imgproc.warpAffine(src, dst, r, new Size(src.cols(), src.rows()));
        return dst;
    }
    //float angleBetween2Points(Point v1, Point v2)
    //{
    //    float len1 = Mathf.Sqrt((float)(v1.x * v1.x + v1.y * v1.y));
    //    float len2 = Mathf.Sqrt((float)(v2.x * v2.x + v2.y * v2.y));

    //    float dot = (float)(v1.x * v2.x + v1.y * v2.y);

    //    float a = dot / (len1 * len2);

    //    if (a >= 1.0)
    //        return 0.0f;
    //    else if (a <= -1.0)
    //        return Mathf.PI;
    //    else
    //        return Mathf.Acos(a); // 0..PI
    //}
    public static float AngleInRad(Point vec1, Point vec2)
    {
        return Mathf.Atan2((float)(vec2.y - vec1.y), (float)(vec2.x - vec1.x));
    }

    //This returns the angle in degrees
    public static float AngleInDeg(Point vec1, Point vec2)
    {
        return AngleInRad(vec1, vec2) * 180 / Mathf.PI;
    }
    /// <summary>
    /// Hands the pose estimation process.
    /// </summary>
    public Mat handPoseEstimationProcess(Mat rgbaMat)
    {
        //Imgproc.blur(mRgba, mRgba, new Size(5,5));
        Mat matForHandPostEstimat = new Mat();
        Imgproc.GaussianBlur(rgbaMat, matForHandPostEstimat, new OpenCVForUnity.Size(3, 3), 1, 1);
        //Imgproc.medianBlur(mRgba, mRgba, 3);

        //if (!isColorSelected)
        //{
        //    return null;
        //}
        List<MatOfPoint> contours = detector.GetContours();
        detector.Process(matForHandPostEstimat);

        if (contours.Count <= 0)
        {
            return null;
        }

        RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

        double boundWidth = rect.size.width;
        double boundHeight = rect.size.height;
        int boundPos = 0;
        for (int i = 1; i < contours.Count; i++)
        {
            rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
            if (rect.size.width * rect.size.height > boundWidth * boundHeight)
            {
                boundWidth = rect.size.width;
                boundHeight = rect.size.height;
                boundPos = i;
            }
        }

        OpenCVForUnity.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contours[boundPos].toArray()));
        //Imgproc.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), CONTOUR_COLOR_WHITE, 2, 8, 0);

        //                      Debug.Log (
        //                      " Row start [" + 
        //                              (int)boundRect.tl ().y + "] row end [" +
        //                              (int)boundRect.br ().y + "] Col start [" +
        //                              (int)boundRect.tl ().x + "] Col end [" +
        //                              (int)boundRect.br ().x + "]");


        double a = boundRect.br().y - boundRect.tl().y;
        a = a * 0.7;
        a = boundRect.tl().y + a;

        //                      Debug.Log (
        //                      " A [" + a + "] br y - tl y = [" + (boundRect.br ().y - boundRect.tl ().y) + "]");

        //Core.rectangle( mRgba, boundRect.tl(), boundRect.br(), CONTOUR_COLOR, 2, 8, 0 );
        //Imgproc.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), CONTOUR_COLOR, 2, 8, 0);

        //Mat handMat = new Mat(rgbaMat.size(), CvType.CV_8U);
        //for (int i = 0; i < rgbaMat.rows(); i++) {
        //    for (int j = 0; j < rgbaMat.cols(); j++) {
        //        handMat.put(i, j, rgbaMat.get(i, j));
        //    }
        //}

        //Utils.matToTexture2D(handMat, t);
        //t.Apply();
        //GameObject.Find("ScreenForHand").GetComponent<RawImage>().texture = t;


        //OpenCVForUnity.Rect srcRect = new OpenCVForUnity.Rect(new Point(0, 1), new Size(rgbaMat.cols(), 1)); //select the 2nd row
        //OpenCVForUnity.Rect dstRect = new OpenCVForUnity.Rect(new Point(3, 5), srcRect.size() ); //destination in (3,5), size same as srcRect

        //dstRect = dstRect.intersect(new OpenCVForUnity.Rect(new Point(0, 0), result.size())); //intersection to avoid out of range
        //srcRect = new OpenCVForUnity.Rect(srcRect.tl(), dstRect.size()); //adjust source size same as safe destination
        //rgbaMat.submat(srcRect).copyTo(result.submat(dstRect)); //copy from (0,1) to (3,5) max allowed cols

        //GameObject.Find("ScreenForHand").transform.localScale = new Vector3((float)t.width / 640, (float)t.height / 480);
        //GameObject.Find("ScreenForHand").transform.localPosition = new Vector3(-25/*left edge = 0 in mat, but in unity = -12.5f*/ + (boundRect.x + (float)boundRect.width/2) / 12.5f, 25f - ((float)boundRect.y + ((float)boundRect.height/2)) / 12.5f, 0);

        MatOfPoint2f pointMat = new MatOfPoint2f();
        Imgproc.approxPolyDP(new MatOfPoint2f(contours[boundPos].toArray()), pointMat, 3, true);
        //Imgproc.drawContours(rgbaMat, contours, -1, new Scalar(0, 0, 255, 255), 3);
        contours[boundPos] = new MatOfPoint(pointMat.toArray());

        MatOfInt hull = new MatOfInt();
        MatOfInt4 convexDefect = new MatOfInt4();
        Imgproc.convexHull(new MatOfPoint(contours[boundPos].toArray()), hull);

        if (hull.toArray().Length < 5)
            return null;

        Imgproc.convexityDefects(new MatOfPoint(contours[boundPos].toArray()), hull, convexDefect);

        List<MatOfPoint> hullPoints = new List<MatOfPoint>();
        listPo.Clear();
        for (int j = 0; j < hull.toList().Count; j++)
        {
            listPo.Add(contours[boundPos].toList()[hull.toList()[j]]);
        }

        MatOfPoint e = new MatOfPoint();
        e.fromList(listPo);
        hullPoints.Add(e);

        List<MatOfPoint> defectPoints = new List<MatOfPoint>();
        List<Point> listPoDefect = new List<Point>();

        //Debug.Log("Hand Detected");
        if (convexDefect.size() != new Size(0, 1))                          //new
        {
            //Debug.Log("Mat Size: " + convexDefect.size());
            for (int j = 0; j < convexDefect.toList().Count; j = j + 4)
            {
                Point farPoint = contours[boundPos].toList()[convexDefect.toList()[j + 2]];
                int depth = convexDefect.toList()[j + 3];
                if (depth > threashold && farPoint.y < a)
                {
                    listPoDefect.Add(contours[boundPos].toList()[convexDefect.toList()[j + 2]]);
                }
                //                              Debug.Log ("defects [" + j + "] " + convexDefect.toList () [j + 3]);
            }
        }

        MatOfPoint e2 = new MatOfPoint();
        e2.fromList(listPo);
        defectPoints.Add(e2);

        //testListPo = listPo;
        //testListPo.ToArray();

        //Imgproc.drawContours(rgbaMat, hullPoints, -1, CONTOUR_COLOR, 3);
        //                      int defectsTotal = (int)convexDefect.total();
        //                      Debug.Log ("Defect total " + defectsTotal);

        this.numberOfFingers = listPoDefect.Count;
        if (this.numberOfFingers > 5)
            this.numberOfFingers = 5;

        //                      Core.putText (mRgba, "" + numberOfFingers, new Point (mRgba.cols () / 2, mRgba.rows () / 2), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 255, 255, 255), 6, Core.LINE_AA, false);
        //numberOfFingersText.text = numberOfFingers.ToString();


        //foreach (Point p in listPoDefect)
        //{
        //    Imgproc.circle(rgbaMat, p, 6, new Scalar(255, 0, 255, 255), -1);
        //}

        //My Code
        List<MatOfPoint> con = new List<MatOfPoint>();
        MatOfPoint myE = new MatOfPoint();
        myE.fromList(contours[boundPos].toList());
        con.Add(myE);
        Mat mask = Mat.zeros(matForHandPostEstimat.rows(), matForHandPostEstimat.cols(), CvType.CV_8U);
        Imgproc.drawContours(mask, con, -1, new Scalar(255), Core.FILLED);
        Imgproc.drawContours(rgbaMat, con, -1, new Scalar(0, 255, 0, 255), 3);
        Mat crop = new Mat(100, 100, CvType.CV_8UC4);                                                       // mit rgbamat width und height cropped es sich das ganze rgbamat auf Android, im Editor funktioniert es
                                                                                                            //crop.copyTo(rgbaMat, mask);
                                                                                                            //Core.normalize(mask.clone(), mask, 0.0, 255.0, Core.NORM_MINMAX, CvType.CV_8UC1);
        rgbaMat.copyTo(crop, mask);

        return crop;
    }

    /// <summary>
    /// Ons the touch.
    /// </summary>
    /// <param name="touchPoint">Touch point.</param>
    public Scalar onTouch(Mat rgbaMat, Point touchPoint)
    {
        int x = (int)touchPoint.x;
        int y = (int)touchPoint.y;

        //                      Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");
        //Debug.Log("X: " + x + "Y: " + y + " Cols: " + cols + " Rows: " + rows);
        //Debug.Log("Input.mousePosition.y: " + Input.mousePosition.y);
        if ((x < 0) || (y < 0) || (x > width) || (y > height))
            return null;
        OpenCVForUnity.Rect touchedRect = new OpenCVForUnity.Rect();

        touchedRect.x = (x > 5) ? x - 5 : 0;
        touchedRect.y = (y > 5) ? y - 5 : 0;

        touchedRect.width = (x + 5 < height) ? x + 5 - touchedRect.x : width - touchedRect.x;
        touchedRect.height = (y + 5 < height) ? y + 5 - touchedRect.y : height - touchedRect.y;

        Mat touchedRegionRgba = rgbaMat.submat(touchedRect);

        Mat touchedRegionHsv = new Mat();
        Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

        // Calculate average color of touched region
        blobColorHsv = Core.sumElems(touchedRegionHsv);
        int pointCount = touchedRect.width * touchedRect.height;
        for (int i = 0; i < blobColorHsv.val.Length; i++)
            blobColorHsv.val[i] /= pointCount;

        //blobColorRgba = converScalarHsv2Rgba (blobColorHsv);

        //                      Debug.Log ("Touched rgba color: (" + mBlobColorRgba.val [0] + ", " + mBlobColorRgba.val [1] +
        //                              ", " + mBlobColorRgba.val [2] + ", " + mBlobColorRgba.val [3] + ")");

        detector.SetHsvColor(blobColorHsv);

        Imgproc.resize(detector.GetSpectrum(), spectrumMat, SPECTRUM_SIZE);

        //isColorSelected = true;

        touchedRegionRgba.release();
        touchedRegionHsv.release();

        return converScalarHsv2Rgba(blobColorHsv);
    }

    /// <summary>
    /// Convers the scalar hsv2 rgba.
    /// </summary>
    /// <returns>The scalar hsv2 rgba.</returns>
    /// <param name="hsvColor">Hsv color.</param>
    private Scalar converScalarHsv2Rgba(Scalar hsvColor)
    {
        Mat pointMatRgba = new Mat();
        Mat pointMatHsv = new Mat(1, 1, CvType.CV_8UC3, hsvColor);
        Imgproc.cvtColor(pointMatHsv, pointMatRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);

        return new Scalar(pointMatRgba.get(0, 0));
    }

    /// <summary>
    /// Converts the screen point.
    /// </summary>
    /// <returns>The screen point.</returns>
    /// <param name="screenPoint">Screen point.</param>
    /// <param name="quad">Quad.</param>
    /// <param name="cam">Cam.</param>
    static Point convertScreenPoint(Point screenPoint, GameObject quad, Camera cam)
    {
        Vector2 tl;
        Vector2 tr;
        Vector2 br;
        Vector2 bl;


        tl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
        tr = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
        br = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
        bl = cam.WorldToScreenPoint(new Vector3(quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));


        Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2);


        srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
        dstRectMat.put(0, 0, 0.0, 0.0, quad.transform.localScale.x, 0.0, quad.transform.localScale.x, quad.transform.localScale.y, 0.0, quad.transform.localScale.y);


        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat);

        //                      Debug.Log ("srcRectMat " + srcRectMat.dump ());
        //                      Debug.Log ("dstRectMat " + dstRectMat.dump ());
        //                      Debug.Log ("perspectiveTransform " + perspectiveTransform.dump ());

        MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint);
        MatOfPoint2f dstPointMat = new MatOfPoint2f();

        Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

        //                      Debug.Log ("srcPointMat " + srcPointMat.dump ());
        //                      Debug.Log ("dstPointMat " + dstPointMat.dump ());

        return dstPointMat.toArray()[0];
    }
    public Mat GetMat()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            //Debug.Log("texture2DToMat width: " + texForOpenCV.width);
            //Debug.Log("rgbaMat width: " + rgbaMat.width());
            OpenCVForUnity.Utils.texture2DToMat(texForOpenCV, rgbaMat);
            //Core.transpose(rotatedRgbaMat, rgbaMat);
            Core.flip(rgbaMat, rgbaMat, 0);
            //Debug.Log(rgbaMat.size());
            return rgbaMat;
        }
        else
        {
            Texture2D testex = TextureTools.scaled(texForOpenCV, width, height);
            //Utils.matToTexture2D(rgbaMat, texForOpenCV/*, GetBufferColors()*/);
            OpenCVForUnity.Utils.texture2DToMat(testex, rotatedRgbaMat);
            Core.transpose(rotatedRgbaMat, rgbaMat);
            Core.flip(rgbaMat, rgbaMat, -1);
            //Debug.Log(rgbaMat);
            return rgbaMat;
        }
    }
    //Color32[] GetBufferColors()
    //{
    //    return colors;
    //}

    //void analyzeHand(List<Point> handPoints)
    //{
    //    Vector3[] vertices = new Vector3[handPoints.Count];
    //    int[] triangles = new int[handPoints.Count * 3];
    //    int i = 0;
    //    int j = 0;
    //    foreach (Point point in handPoints)
    //    {
    //        vertices[i] = new Vector3((float)point.x / Screen.width, -(float)point.y / Screen.height, 0);
    //        //vertices[vertices.Count() / 2 + i] = new Vector3((float)point.x / Screen.width, -(float)point.y / Screen.height, 0.5f);
    //        //triangles[j] = 0;
    //        //triangles[j + 1] = i;
    //        //triangles[j + 2] = handPoints.Count() -1;
    //        if (i == 0)
    //        {
    //            triangles[j] = 0;
    //        }
    //        else
    //        {
    //            triangles[j] = i - 1;
    //        }
    //        triangles[j + 1] = i;
    //        if (i == vertices.Count() - 1)
    //        {
    //            triangles[j + 2] = i;
    //        }
    //        else
    //        {
    //            triangles[j + 2] = i + 1;
    //        }
    //        j = j + 3;
    //        i++;
    //    }
    //    foreach (int t in triangles)
    //    {
    //        Debug.Log(t);
    //    }
    //    mesh.Clear();
    //    mesh.vertices = vertices;
    //    mesh.triangles = triangles;
    //    mesh.RecalculateNormals();

    //    //List<Vector2> sortfingers = new List<Vector2>();
    //    //foreach (Point point in handPoints)
    //    //{
    //    //    sortfingers.Add(new Vector2((float)point.x, (float)point.y));
    //    //}
    //    //IEnumerable<Vector2> sorted = sortfingers.OrderBy(v => v.x);
    //    //Debug.Log("count: " + sorted.ToList().Count());
    //    //Drawing.DrawLine(new Vector2(sorted.ToList()[0].x, sorted.ToList()[0].y), new Vector2(sorted.ToList()[sorted.ToList().Count -1].x, sorted.ToList()[sorted.ToList().Count - 1].y), Color.blue, 10);
    //    //for (int i = 0; i < 6; sorted.Count())
    //    //{
    //    //    hand.transform.GetChild(i).position = sorted.ToList()[i];
    //    //}
    //    //sortedHandPoints = sorted;
    //}

    //Aus https://blog.dakwamine.fr/?p=1943
    /// <summary>
    /// Gets the coordinates of the intersection point of two lines.
    /// </summary>
    /// <param name="A1">A point on the first line.</param>
    /// <param name="A2">Another point on the first line.</param>
    /// <param name="B1">A point on the second line.</param>
    /// <param name="B2">Another point on the second line.</param>
    /// <param name="found">Is set to false of there are no solution. true otherwise.</param>
    /// <returns>The intersection point coordinates.</returns>
    public Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
    {
        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);
 
        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;
 
        return new Vector2(
            B1.x + (B2.x - B1.x) * mu,
            B1.y + (B2.y - B1.y) * mu
        );
    }

    //bool debug;
    //public void ChangeDebugStatus()
    //{
    //    Debug.Log("texforOPenCV width: " + texForOpenCV.width);
    //    debug = !debug;
    //    if (debug)
    //    {
    //        Debug.Log("Debug Mode On");
    //        screen.transform.localRotation = new Quaternion(0, 0, 0, 0);
    //        screen.GetComponent<RawImage>().texture = temp;
    //    }
    //    else
    //    {
    //        Debug.Log("Debug Mode off");
    //        if (Application.platform == RuntimePlatform.Android)
    //        {
    //            screen.transform.localRotation = Quaternion.Euler(0, 180, 180); //cause TextureReader gives a rotated texForOpenCV
    //        }
    //        else
    //        {
    //            screen.transform.localRotation = Quaternion.Euler(0, 180, 90);
    //        }
    //        screen.GetComponent<RawImage>().texture = texForOpenCV;
    //    }
    //}
}

