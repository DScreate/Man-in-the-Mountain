﻿using UnityEngine;
using System.Collections.Generic;
using ARTScripts;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;


public class ARTMultiObjectTrackingBasedOnColor : MonoBehaviour {

    Plane TestPlane;



    /// <summary>
    /// The texture.
    /// </summary>
    Texture2D texture;

    /// <summary>
    /// max number of objects to be detected in frame
    /// </summary>
    const int MAX_NUM_OBJECTS = 50;

    /// <summary>
    /// minimum and maximum object area
    /// </summary>
    const int MIN_OBJECT_AREA = 20 * 20;

    //              /// <summary>
    //              /// max object area
    //              /// </summary>
    //              int MAX_OBJECT_AREA;

    /// <summary>
    /// The rgb mat.
    /// </summary>
    Mat rgbMat;

    /// <summary>
    /// The threshold mat.
    /// </summary>
    Mat thresholdMat;

    /// <summary>
    /// The hsv mat.
    /// </summary>
    Mat hsvMat;

    ARTColorObject blue = new ARTColorObject("blue");
    ARTColorObject yellow = new ARTColorObject("yellow");
    ARTColorObject red = new ARTColorObject("red");
    ARTColorObject green = new ARTColorObject("green");

    List<ARTColorObject> blueList = new List<ARTColorObject>();
    List<ARTColorObject> greenList = new List<ARTColorObject>();
    List<ARTColorObject> yellowList = new List<ARTColorObject>();
    List<ARTColorObject> redList = new List<ARTColorObject>();

    /// <summary>
    /// The webcam texture to mat helper.
    /// </summary>
    ARTWebcamTextureToMatHelper ARTwebCamTextureToMatHelper;

    // Use this for initialization
    void Start()
    {
        ARTwebCamTextureToMatHelper = gameObject.GetComponent<ARTWebcamTextureToMatHelper>();
        ARTwebCamTextureToMatHelper.Initialize();
    }

    /// <summary>
    /// Raises the webcam texture to mat helper initialized event.
    /// </summary>
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");

        Mat webCamTextureMat = ARTwebCamTextureToMatHelper.GetMat();

        texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

        gameObject.GetComponent<Renderer>().material.mainTexture = texture;

        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }


        rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
        thresholdMat = new Mat();
        hsvMat = new Mat();

        //                                      MAX_OBJECT_AREA = (int)(webCamTexture.height * webCamTexture.width / 1.5);
    }

    /// <summary>
    /// Raises the webcam texture to mat helper disposed event.
    /// </summary>
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (rgbMat != null)
            rgbMat.Dispose();
        if (thresholdMat != null)
            thresholdMat.Dispose();
        if (hsvMat != null)
            hsvMat.Dispose();
    }

    /// <summary>
    /// Raises the webcam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(ARTWebcamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }
    
    
    // Update is called once per frame
    void Update()
    {
        if (ARTwebCamTextureToMatHelper.IsPlaying() && ARTwebCamTextureToMatHelper.DidUpdateThisFrame())
        {
            //alpha is never used, potentially we can change it so only rgb mat is being generated to save performance
            Mat rgbaMat = ARTwebCamTextureToMatHelper.GetMat();
            var blueMat = new Mat();
            var yellowMat = new Mat();
            var redMat = new Mat();
            var greenMat = new Mat();

            Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

            //first find blue objects
            Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

            /*Core.inRange(hsvMat, blue.getHSVmin(), blue.getHSVmax(), blueMat);
            Core.inRange(hsvMat, yellow.getHSVmin(), yellow.getHSVmax(), yellowMat);
            Core.inRange(hsvMat, red.getHSVmin(), red.getHSVmax(), redMat);
            Core.inRange(hsvMat, green.getHSVmin(), green.getHSVmax(), greenMat);
            Utils.matToTexture2D(rgbMat, texture, ARTwebCamTextureToMatHelper.GetBufferColors());*/

            Core.inRange(hsvMat, blue.getHSVmin(), blue.getHSVmax(), thresholdMat);
            morphOps(thresholdMat);
            trackFilteredObject(blue, thresholdMat, rgbMat);
            //then yellows
            
            Core.inRange(hsvMat, yellow.getHSVmin(), yellow.getHSVmax(), thresholdMat);            

          //  Imgproc.threshold(hsvMat, thresholdMat, 0.0, 0.0, 0); //Can we use this after Core.inRange to fill in white areas with some grey?

             morphOps(thresholdMat);
             trackFilteredObject(yellow, thresholdMat, rgbMat);

            //then reds
            
            Core.inRange(hsvMat, red.getHSVmin(), red.getHSVmax(), thresholdMat);
             morphOps(thresholdMat);
             trackFilteredObject(red, thresholdMat, rgbMat);

            //then greens
            
            Core.inRange(hsvMat, green.getHSVmin(), green.getHSVmax(), thresholdMat);
             morphOps(thresholdMat);
              trackFilteredObject(green, thresholdMat, rgbMat);


            //TODO: Remove SO
            Imgproc.putText(rgbMat, "W:" + rgbMat.width() + " H:" + rgbMat.height() + " SO:" + Screen.orientation, new Point(5, rgbMat.rows() - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            
            Utils.matToTexture2D(rgbMat, texture, ARTwebCamTextureToMatHelper.GetBufferColors());            
        }
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        ARTwebCamTextureToMatHelper.Dispose();
    }

    /// <summary>
    /// Raises the back button click event.
    /// </summary>
    public void OnBackButtonClick()
    {
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
        SceneManager.LoadScene("OpenCVForUnityExample");
#else
            Application.LoadLevel ("OpenCVForUnityExample");
#endif
    }

    /// <summary>
    /// Raises the play button click event.
    /// </summary>
    public void OnPlayButtonClick()
    {
        ARTwebCamTextureToMatHelper.Play();
    }

    /// <summary>
    /// Raises the pause button click event.
    /// </summary>
    public void OnPauseButtonClick()
    {
        ARTwebCamTextureToMatHelper.Pause();
    }

    /// <summary>
    /// Raises the stop button click event.
    /// </summary>
    public void OnStopButtonClick()
    {
        ARTwebCamTextureToMatHelper.Stop();
    }

    /// <summary>
    /// Raises the change camera button click event.
    /// </summary>
    public void OnChangeCameraButtonClick()
    {
        ARTwebCamTextureToMatHelper.Initialize(null, ARTwebCamTextureToMatHelper.requestedWidth, ARTwebCamTextureToMatHelper.requestedHeight);
    }

    //TODO: Remove this method, being used to draw colors on image.
    /// <summary>
    /// Draws the object.
    /// </summary>
    /// <param name="theColorObjects">The color objects.</param>
    /// <param name="frame">Frame.</param>
    /// <param name="temp">Temp.</param>
    /// <param name="contours">Contours.</param>
    /// <param name="hierarchy">Hierarchy.</param>
    private void drawObject(List<ARTColorObject> theColorObjects, Mat frame, Mat temp, List<MatOfPoint> contours, Mat hierarchy)
    {
        for (int i = 0; i < theColorObjects.Count; i++)
        {
            Imgproc.drawContours(frame, contours, i, theColorObjects[i].getColor(), -1, 8, hierarchy, int.MaxValue, new Point());
            //Imgproc.circle(frame, new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos()), 5, theColorObjects[i].getColor());
            //Imgproc.putText(frame, theColorObjects[i].getXPos() + " , " + theColorObjects[i].getYPos(), new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos() + 20), 1, 1, theColorObjects[i].getColor(), 2);
            //Imgproc.putText(frame, theColorObjects[i].getType(), new Point(theColorObjects[i].getXPos(), theColorObjects[i].getYPos() - 20), 1, 2, theColorObjects[i].getColor(), 2);
        }
    }

    /// <summary>
    /// Morphs the ops.
    /// </summary>
    /// <param name="thresh">Thresh.</param>
    private void morphOps(Mat thresh)
    {
        //create structuring element that will be used to "dilate" and "erode" image.
        //the element chosen here is a 3px by 3px rectangle
        Mat erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
        //dilate with larger element so make sure object is nicely visible
        Mat dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(8, 8));

        Imgproc.erode(thresh, thresh, erodeElement);
        Imgproc.erode(thresh, thresh, erodeElement);

        Imgproc.dilate(thresh, thresh, dilateElement);
        Imgproc.dilate(thresh, thresh, dilateElement);
    }
    /// <summary>
    /// Tracks the filtered object.
    /// </summary>
    /// <param name="theColorObject">The color object.</param>
    /// <param name="threshold">Threshold.</param>
    /// <param name="HSV">HS.</param>
    /// <param name="cameraFeed">Camera feed.</param>
    //private void trackFilteredObject(ARTColorObject theColorObject, Mat threshold, Mat HSV, Mat cameraFeed)
    private void trackFilteredObject(ARTColorObject theColorObject, Mat threshold, Mat cameraFeed)
    {

        List<ARTColorObject> colorList = new List<ARTColorObject>();
        Mat temp = new Mat();
        threshold.copyTo(temp);
        //these two vectors needed for output of findContours
        List<MatOfPoint> contours = new List<MatOfPoint>();
        Mat hierarchy = new Mat();
        //find contours of filtered image using openCV findContours function
        Imgproc.findContours(temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);

        //use moments method to find our filtered object
        bool colorObjectFound = false;
        if (hierarchy.rows() > 0)
        {
            int numObjects = hierarchy.rows();

            //                      Debug.Log("hierarchy " + hierarchy.ToString());

            //if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
            if (numObjects < MAX_NUM_OBJECTS)
            {
                for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0])
                {

                    Moments moment = Imgproc.moments(contours[index]);
                    double area = moment.get_m00();

                    //if the area is less than 20 px by 20px then it is probably just noise
                    //if the area is the same as the 3/2 of the image size, probably just a bad filter
                    //we only want the object with the largest area so we safe a reference area each
                    //iteration and compare it to the area in the next iteration.
                    if (area > MIN_OBJECT_AREA)
                    {

                        ARTColorObject colorObject = new ARTColorObject();

                        colorObject.setXPos((int)(moment.get_m10() / area));
                        colorObject.setYPos((int)(moment.get_m01() / area));
                        colorObject.setType(theColorObject.getType());
                        colorObject.setColor(theColorObject.getColor());

                        colorList.Add(colorObject);

                        colorObjectFound = true;

                    }
                    else
                    {
                        colorObjectFound = false;
                    }
                }
                //let user know you found an object

                //TODO: delete if statement, not drawing here
                if (colorObjectFound == true)
                {
                    //draw object location on screen
                    drawObject(colorList, cameraFeed, temp, contours, hierarchy);
                }

            }
            else
            {
                Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
        }
    }

    public List<ARTColorObject> getBlueList()
    {
        return blueList;
    }

    public List<ARTColorObject> getYellowList()
    {
        return yellowList;
    }

    public List<ARTColorObject> getGreenList()
    {
        return greenList;
    }

    public List<ARTColorObject> getRedList()
    {
        return redList;
    }
}

