using OpenCVForUnity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

public class BLLDNAController : MonoBehaviour {

    public TextMeshProUGUI instructions;

    public Transform[] hands;

    List<List<float>> handZPos = new List<List<float>>();

    List<List<int>> fingers = new List<List<int>>();

    List<List<float>> xPosForSwipe = new List<List<float>>();
    List<List<float>> yPosForSwipe = new List<List<float>>();

    private int handID;

    public GameObject dna_Model;
    private GameObject dna;
    private GameObject dnaScaleSliderHandle;

    public GameObject InfoCanvas;
    private GameObject infoCan;

    Vector2 h_finger;

    public Transform[] screenForHands = new Transform[2];

    // Use this for initialization
    void Start () {
        for (int i = 0; i < 2; i++)
        {
            handZPos.Add(new List<float>());
            fingers.Add(new List<int>());
            xPosForSwipe.Add(new List<float>());
            yPosForSwipe.Add(new List<float>());

            screenForHands[i] = GetComponent<TrackHandWithMarker>().screensForHands[i].transform;
        }

        StartCoroutine(StartInstrucions());
	}
    IEnumerator StartInstrucions()
    {
        yield return new WaitForSeconds(4);
        instructions.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        instructions.text = "<mark=#000000A0>Greife die DNA mit der rechten Hand, um sie zu bewegen.</mark>";//Grab with your right Hand the DNA to move it</mark>";
        yield return new WaitForSeconds(3);
        instructions.text = "<mark=#000000A0>Wische nach rechts über einer Plane, um das Info-Canvas zu platzieren.</mark>";//Swipe right over a plane to see a Info-Canvas</mark>";
        yield return new WaitForSeconds(3);
        instructions.text = "<mark=#000000A0>Tippe mit dem Zeigefinger auf Elemente innerhalb des Info-Canvas, um mehr Informationen zu erhalten.</mark>";//Press with your forefinger on elements in the Info-Canvas to see more information</mark>";
        yield return new WaitForSeconds(3);
        instructions.text = "<mark=#000000A0>Wische nach links über ein Info-Canvas, um es zu zerstören.</mark>";//Swipe left over the info-canvas to destroy it</mark>";
        yield return new WaitForSeconds(3);
        instructions.gameObject.SetActive(false);
    }
    private void Update()
    {
        Vector3 CameraCenter = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane));
        RaycastHit hit;
        //Debug.DrawRay(CameraCenter, Camera.main.transform.forward, Color.red);
        if (Physics.Raycast(CameraCenter, Camera.main.transform.forward, out hit, 100))
        {
            if (hit.transform.tag == "Target")
            {
                hit.transform.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                foreach (GameObject infos in GameObject.FindGameObjectsWithTag("Target"))
                {
                    infos.transform.GetChild(0).gameObject.SetActive(false);
                }
            }
        }
        else
        {
            foreach(GameObject infos in GameObject.FindGameObjectsWithTag("Target"))
            {
                infos.transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        //Fingerspitze.transform.forward = screenForHands[handID].transform.forward;
        //Fingerspitze.transform.position = screenForHands[handID].position + screenForHands[handID].TransformDirection(screenForHands[handID].localPosition.z * new Vector3(0.5f - (float)h_finger.x / 640, -0.5f + (float)h_finger.y / 480, 0));
    }
    
    IEnumerator DeleteVars(int handID)
    {
        yield return new WaitForSeconds(2);
        yield return new WaitForEndOfFrame();
        if (xPosForSwipe[handID].Count > 0)
        {
            xPosForSwipe[handID].RemoveAt(0);
        }
        if (fingers[handID].Count > 0)
        {
            fingers[handID].RemoveAt(0);
            //Debug.Log("Finger in List: " + fingers[handID].Count);
        }
        if (handZPos[handID].Count > 0)
        {
            handZPos[handID].RemoveAt(0);
        }
    }

    public void SetData(int new_fingers, int handID, Vector2 h_finger)
    {
        this.h_finger = h_finger;
        this.handID = handID;
        this.handZPos[handID].Add(hands[handID].localPosition.z);
        fingers[handID].Add(new_fingers);
        //if(fingers[handID].Count > 8)
        //{
        //    fingers[handID].RemoveAt(0);
        //}
        //hands[handID].gameObject.SetActive(true);
        StartCoroutine(DeleteVars(handID));
        Run();
    }
    public void Run()
    {
        //Hand
        RaycastHit hit;
        if (Physics.Raycast(hands[handID].transform.position, hands[handID].transform.forward, out hit, 0.5f))
        {
            Debug.DrawRay(hands[handID].transform.position, hands[handID].transform.forward, Color.blue);
            if (infoCan == null)
            {
                if (hit.transform.tag == "Plane")
                {
                    xPosForSwipe[handID].Add(hands[handID].localPosition.x);
                    if (xPosForSwipe[handID].Count > 6)      // *3 für Frame Bsp: maxData=10, maxData * 3 = 30 -> Betrachte letze Sekunde bei fsp von 30
                    {
                        //xPosForSwipe[handID].RemoveAt(0);
                        //if (infoCan == null)
                        //{
                        //Debug.Log("Test SwipeRight");
                        if (IfSwipedRight(0.08f))
                        {
                            infoCan = Instantiate(InfoCanvas, hit.point, Quaternion.Euler(new Vector3(-90, Camera.main.transform.rotation.eulerAngles.y, Camera.main.transform.rotation.eulerAngles.z)));

                            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
                            {
                                plane.GetComponent<MeshRenderer>().enabled = false;
                                plane.GetComponent<MeshCollider>().enabled = false;
                            }
                        }
                        //}
                    }
                }
            }
            else
            { 
                //Fingerspitze
                if(h_finger!= Vector2.zero) {
                    Debug.DrawRay(screenForHands[handID].position + screenForHands[handID].TransformDirection(screenForHands[handID].localPosition.z * new Vector3(0.5f - (float)h_finger.x / 640, -0.5f + (float)h_finger.y / 480, 0)), hands[handID].transform.forward, Color.yellow);
                    if (Physics.Raycast(screenForHands[handID].position + screenForHands[handID].TransformDirection(screenForHands[handID].localPosition.z * new Vector3(0.5f - (float)h_finger.x / 640, -0.5f + (float)h_finger.y / 480, 0)), hands[handID].transform.forward, out hit, 0.5f))
                    {
                        //Debug.Log("xPosForSwipe.Count: " + xPosForSwipe.Count);
                        //Debug.Log("hit: " + hit.transform.name);
                        //xPosForSwipe[handID].Add(hands[handID].transform.TransformPoint(Vector3.zero).x + (float)h_finger.x / 640);
                        //xPosForSwipe[handID].Add(hands[handID].localPosition.x);
                        //if (xPosForSwipe[handID].Count > 8)      // *3 für Frame Bsp: maxData=10, maxData * 3 = 30 -> Betrachte letze Sekunde bei fsp von 30
                        //{
                        //    //xPosForSwipe[handID].RemoveAt(0);

                        //    if (IfSwipedLeft(0.2f))
                        //    {
                        //        Destroy(infoCan);
                        //    }
                        //}

                        if (hit.transform.tag == "InfoCan")
                        {
                            xPosForSwipe[handID].Add(hands[handID].localPosition.x);

                            infoCan.transform.GetChild(1).gameObject.SetActive(true);
                            infoCan.transform.GetChild(1).position = hit.point;

                            if (xPosForSwipe[handID].Count > 8)      // *3 für Frame Bsp: maxData=10, maxData * 3 = 30 -> Betrachte letze Sekunde bei fsp von 30
                            {
                                //xPosForSwipe[handID].RemoveAt(0);

                                if (IfSwipedLeft(0.215f))
                                {
                                    Destroy(infoCan);

                                    foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
                                    {
                                        plane.GetComponent<MeshRenderer>().enabled = true;
                                        plane.GetComponent<MeshCollider>().enabled = true;
                                    }
                                }
                            }
                            if (handZPos[handID].Count > 6)
                            {
                                if (hit.transform.name == "BaseInfo")
                                {
                                    if (IfButtonPressed(0.15f))
                                    {
                                        Debug.Log("Button pressed");
                                        hit.transform.GetChild(0).gameObject.SetActive(true);
                                        hit.transform.GetComponent<UnityEngine.UI.Text>().enabled = false;
                                        hit.transform.GetComponent<BoxCollider>().enabled = false;
                                        hit.transform.parent.GetChild(0).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(1).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(2).gameObject.SetActive(false);
                                    }
                                }
                                else if (hit.transform.name == "HelixInfo")
                                {
                                    if (IfButtonPressed(0.15f))
                                    {
                                        Debug.Log("Button pressed");
                                        hit.transform.GetChild(0).gameObject.SetActive(true);
                                        hit.transform.GetComponent<UnityEngine.UI.Text>().enabled = false;
                                        hit.transform.GetComponent<BoxCollider>().enabled = false;
                                        hit.transform.parent.GetChild(0).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(3).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(2).gameObject.SetActive(false);
                                    }
                                }
                                else if (hit.transform.name == "DNAInfo")
                                {
                                    if (IfButtonPressed(0.15f))
                                    {
                                        //Debug.Log("Button pressed");
                                        hit.transform.GetChild(0).gameObject.SetActive(true);
                                        hit.transform.GetComponent<UnityEngine.UI.Text>().enabled = false;
                                        hit.transform.GetComponent<BoxCollider>().enabled = false;
                                        hit.transform.parent.GetChild(0).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(3).gameObject.SetActive(false);
                                        hit.transform.parent.GetChild(1).gameObject.SetActive(false);
                                    }
                                }
                                else if (hit.transform.name == "BackBtn")
                                {
                                    if (IfButtonPressed(0.15f))
                                    {
                                        if (hit.transform.parent.GetChild(0).gameObject.activeSelf == true)
                                        {
                                            Destroy(infoCan);
                                            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
                                            {
                                                plane.GetComponent<MeshRenderer>().enabled = true;
                                                plane.GetComponent<MeshCollider>().enabled = true;
                                            }
                                        }
                                        else
                                        {
                                            hit.transform.parent.GetChild(3).gameObject.SetActive(true);
                                            hit.transform.parent.GetChild(3).GetComponent<UnityEngine.UI.Text>().enabled = true;
                                            hit.transform.parent.GetChild(3).GetComponent<BoxCollider>().enabled = true;
                                            hit.transform.parent.GetChild(3).GetChild(0).gameObject.SetActive(false);

                                            hit.transform.parent.GetChild(1).gameObject.SetActive(true);
                                            hit.transform.parent.GetChild(1).GetComponent<UnityEngine.UI.Text>().enabled = true;
                                            hit.transform.parent.GetChild(1).GetComponent<BoxCollider>().enabled = true;
                                            hit.transform.parent.GetChild(1).GetChild(0).gameObject.SetActive(false);

                                            hit.transform.parent.GetChild(2).gameObject.SetActive(true);
                                            hit.transform.parent.GetChild(2).GetComponent<UnityEngine.UI.Text>().enabled = true;
                                            hit.transform.parent.GetChild(2).GetComponent<BoxCollider>().enabled = true;
                                            hit.transform.parent.GetChild(2).GetChild(0).gameObject.SetActive(false);

                                            hit.transform.parent.GetChild(0).gameObject.SetActive(true);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            infoCan.transform.GetChild(1).gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        infoCan.transform.GetChild(1).gameObject.SetActive(false);
                    }
                }
                else
                {
                    infoCan.transform.GetChild(1).gameObject.SetActive(false);
                }
            }
        }

        if (dna == null)
        {
            if (handID == 0)
            {
                if (IfHandOpened())
                {
                    dna = Instantiate(dna_Model, hands[handID].position, Quaternion.Euler(0, hands[handID].rotation.eulerAngles.y - 90, 0));
                    dna.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    dnaScaleSliderHandle = dna.transform.GetChild(3).GetChild(0).GetChild(2).GetChild(0).gameObject;

                    Destroy(hands[handID].GetChild(0).gameObject);
                }
            }
        }
        else
        {
            if (handID == 0)
            {
                //Verschiebe DNA
                if ((dna.transform.position - hands[handID].position).sqrMagnitude < 0.1f)
                {
                    //Debug.Log("Fingers: " + fingers[handID][fingers.Count - 1] + "; " + (fingers[handID][fingers.Count - 1] > 1));
                    if (fingers[handID].Last() > 1)
                    {
                        dna.transform.parent = null;
                    }
                    else if (IfHandClosed())
                    {
                        dna.transform.parent = hands[handID];
                        dna.transform.localPosition = new Vector3(0, -0.1f, -0.1f);
                    }
                }
            }
            else
            {
                //Debug.Log("Distance: " + ((dnaScaleSliderHandle.transform.position - hands[handID].position).sqrMagnitude));
                if ((dnaScaleSliderHandle.transform.position - hands[handID].position).sqrMagnitude < 0.1f)
                {
                    //float handXPos = Mathf.InverseLerp(dnaScaleSlider.transform.parent.InverseTransformPoint(dnaScaleSlider.transform.parent.GetChild(1).position).x, dnaScaleSlider.transform.parent.InverseTransformPoint(dnaScaleSlider.transform.parent.GetChild(2).position).x, dnaScaleSlider.transform.parent.InverseTransformPoint(hands[handID].position).x);
                    //Debug.Log("startPOs: " + dnaScaleSlider.transform.parent.InverseTransformPoint(dnaScaleSlider.transform.parent.GetChild(1).position).x);
                    //Debug.Log("handPos: " + dnaScaleSlider.transform.parent.InverseTransformPoint(hands[handID].position).x);
                    //Debug.Log("handXPos: " + handXPos);
                    //if (handXPos > 0 && handXPos < 1) {
                    //    dna.GetComponentInChildren<Slider>().value = handXPos;
                    //    dna.transform.localScale *= handXPos;
                    //}

                    //https://answers.unity.com/questions/934977/how-far-to-the-local-leftright-is-another-object.html
                    // (B - A)
                    Vector3 distBetween = (hands[handID].transform.position - dnaScaleSliderHandle.transform.parent.GetChild(1).position);
                    // A's local X-Axis
                    Vector3 xAxis = dnaScaleSliderHandle.transform.right;
                    // Vector projection on A's Z-Axis (assuming the Y-Axis isn't a factor
                    Vector3 xAxisDist = Vector3.Project(distBetween, xAxis);
                    //Debug.Log("xAxisdist: " + xAxisDist);

                    Vector3 distBetweenStartandEnd = dnaScaleSliderHandle.transform.parent.GetChild(2).position - dnaScaleSliderHandle.transform.parent.GetChild(1).position;
                    Vector3 xAxisDistStartandEnd = Vector3.Project(distBetweenStartandEnd, xAxis);
                    //float d = dnaScaleSlider.transform.parent.GetChild(2).position.x- dnaScaleSlider.transform.parent.GetChild(1).position.x;
                    //Debug.Log("1: " + dnaScaleSlider.transform.parent.GetChild(2).position.x + "- 2: " + dnaScaleSlider.transform.parent.GetChild(1).position.x + " = " + (dnaScaleSlider.transform.parent.GetChild(2).position.x - dnaScaleSlider.transform.parent.GetChild(1).position.x));
                    //Debug.Log("D: " + xAxisDistStartandEnd);
                    float sliderValue = xAxisDist.z / xAxisDistStartandEnd.z;
                    //Debug.Log("sliderValue: " + sliderValue);
                    if (sliderValue >= 0 && sliderValue <= 1)
                    {
                        dna.GetComponentInChildren<Slider>().value = sliderValue;
                        dna.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f) * (sliderValue +0.5f);
                    }
                }
            }
        }
    }


    private bool IfHandOpened()
    {
        bool openHand = false;
        bool fist = false;
        int i = 0;
        while (i < fingers[handID].Count - 1)
        {
            if (i < (fingers[handID].Count - 1) / 2)
            {
                if (fingers[handID][i] == 0)
                {
                    fist = true;
                }
            }
            else
            {
                if (fingers[handID][i] > 1)
                {
                    openHand = true;
                }
            }
            i++;
        }

        if (fist == true && openHand == true)
        {
            return true;
        }
        return false;
    }

    private bool IfHandClosed()
    {
        bool openHand = false;
        bool fist = false;
        int i = 0;
        while (i < fingers[handID].Count - 1)
        {
            if (i < (fingers[handID].Count - 1) / 2)
            {
                if (fingers[handID][i] > 1)
                {
                    openHand = true;
                }
            }
            else
            {
                if (fingers[handID][i] == 0)
                {
                    fist = true;
                }
            }
            i++;
        }

        //Debug.Log("Fist: " + fist);
        //Debug.Log("OpenHand: " + openHand);
        if (fist == true && openHand == true)
        {
            //Debug.Log("Hand was closed");
            return true;
        }
        return false;
    }

    private bool IfSwipedLeft(float distance)
    {
        float lastPos = xPosForSwipe[handID][0];
        int i = 1;
        while (i < xPosForSwipe[handID].Count - 1)
        {
            if (xPosForSwipe[handID][i] <= lastPos)
            {
                if (xPosForSwipe[handID][0] - xPosForSwipe[handID][1] > distance)
                {
                    Debug.Log("Left Swipe");
                    xPosForSwipe[handID].Clear();
                    return true;
                }
            }
            else
            {
                return false;
            }
            i++;
        }
        return false;
    }
    private bool IfSwipedRight(float distance)
    {
        //Debug.Log("ifswipedRight");
        float lastPos = xPosForSwipe[handID][0];
        int i = 1;
        while (i < xPosForSwipe[handID].Count - 1)
        {
            if (xPosForSwipe[handID][i] >= lastPos)
            {
                //Debug.Log("distance: " + (xPosForSwipe[handID][i] - xPosForSwipe[handID][0]));
                if (xPosForSwipe[handID][i] - xPosForSwipe[handID][0] > distance)
                {
                    xPosForSwipe[handID].Clear();
                    return true;
                }
            }
            else
            {
                return false;
            }
            i++;
        }
        return false;
    }

    private bool IfButtonPressed(float d)
    {
        float highestD = handZPos[handID][4];
        float lowestD = handZPos[handID][0];
        int i = 1;
        while (i < handZPos[handID].Count - 1)
        {
            if (i < (handZPos.Count - 1) / 2)
            {
                if (handZPos[handID][i] < lowestD)
                {
                    lowestD = handZPos[handID][i];
                }
            }
            else
            {
                if (handZPos[handID][i] > highestD)
                {
                    highestD = handZPos[handID][i];
                }
            }
            i++;
        }
        //Debug.Log("d: " + (highestD - lowestD));
        if ((highestD - lowestD) > d)
        {
            //Debug.Log("Button pressed");
            handZPos[handID].Clear();
            return true;
        }
        return false;
    }
}
