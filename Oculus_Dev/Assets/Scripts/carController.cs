﻿using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Threading;
using static OVRBoundary;

public class carController : MonoBehaviour
{
    //Animators for coachman
    public Animator coachmanAnimator;
    public Animator mechCoachmanAnimator;
    public Animator capsuleCoachman1Animator;
    public Animator capsuleCoachman2Animator;
    //Animators for hand gestures-TBA
    public Animator handAnimator;
    // All gestures, ccoachman velocitys are achieved by animation. So I use Animator to manage them.
    private Animator activatedAnimator;

    //private AnimatorStateInfo animatorInfo;

    public GameObject centerEye;
    public GameObject[] coachmen = new GameObject[4];
    // located propertities of facial expression design
    private GameObject[] lips = new GameObject[3];
    private GameObject rightEye;
    private GameObject leftEye;
    private GameObject hand;
    //menu
    private GameObject menu;
    private GameObject quitButton;
    //velocity controls the speed of vehicle, the original speed is 40km/h,or 35km/h
    private Vector3 velocity = new Vector3(0.0f, 0.0f, 10f);
    private Vector3 deceleration = new Vector3(0.0f, 0.0f, - 3.3f);
    private string[] coachmanTag = {"", "_Mech", "_Cap1","_Cap2"};
    private float[] popDis = {0.575f, 0.52f, 0.65f, 0.58f};
    //The total number of all tasks for one participant
    private const int TOTAL_TASK_NUM = 6;
    //The task that will be showed next
    private int taskNum = 0;
    private int[] randomOrder;
    private int userID;
    private int coachmanType;
    private float startTime;
    //private double slowDownTime = 0.5f;
    private Vector3 Rotation;
    private Vector3 Position;
    private float TimeInSeconds;
    private Vector3[] boundaryPoints;
    //network and DB
    private MongoClient client = new MongoClient("mongodb+srv://WeiWei:AnthroWME-db@cluster0.u0268.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");
    IMongoDatabase database;
    IMongoCollection<BsonDocument> posCollection;
    IMongoCollection<BsonDocument> rotCollection;
    IMongoCollection<BsonDocument> orderCollection;
    IMongoCollection<BsonDocument> userCollection;
    private bool animationFlag = false;
    //isStart is decided by the menu button:Start task
    private bool isStart = false;
    private bool threadFlag = true;
    private bool isSlowDown = false;
    void Start()
    {
        /*
            get all the gameObject.
        */
        menu = GameObject.Find("Menu");
        // set the quitButton unvisiable
        quitButton = GameObject.Find("Menu/Quit");
        quitButton.SetActive(false);
        leftEye = GameObject.Find("Waymo/LeftEye");
        rightEye = GameObject.Find("Waymo/RightEye");
        lips[0] = GameObject.Find("Waymo/NormalLip");
        lips[1] = GameObject.Find("Waymo/PositiveLip");
        lips[2] = GameObject.Find("Waymo/NegativeLip");
        //animationClip = activatedAnimator.runtimeAnimatorController.animationClips;

        //boundary points
        boundaryPoints = GameObject.Find("BoundaryCheck").GetComponent<GuardianScripts>().getBoundaryInfo();
        BoundaryCheck();
        //database and collections that will be used
        database = client.GetDatabase("myFirstDatabase");
        posCollection = database.GetCollection<BsonDocument>("crossinfops");
        rotCollection = database.GetCollection<BsonDocument>("crossinfors");
        orderCollection = database.GetCollection<BsonDocument>("orderinfos");
        userCollection = database.GetCollection<BsonDocument>("userinfo");
        Debug.Log("#####start getting userID from DB");
        //Get and update the userID
        GetAndUpdateUserID();
        //Generate the order for each participant and save it to database
        randomOrder = GetRandomList(TOTAL_TASK_NUM);
        SaveOrderInfoToDB();
        SetAnimator();
    }

    private void Update()
    {
        Rotation = centerEye.transform.eulerAngles;
        Position = centerEye.transform.position;
        TimeInSeconds = Time.realtimeSinceStartup - startTime;
        //if crossed, end task
        if (centerEye.transform.position.x < -324)
            EndTask();
        //if time is out, end task(the pedestrians didn't make decisions)
        else if (Time.realtimeSinceStartup - startTime > 33)
            EndTask();
    }

    void FixedUpdate()
    {        
        if(isStart)
        {  
            //moving AV
            this.transform.Translate(velocity * Time.deltaTime);
            //Slow down
            if (!isSlowDown&&this.transform.position.z > -16 )
            {
                velocity = velocity + deceleration * Time.deltaTime;
                 Debug.Log("##### pos: " + this.transform.position.z);
                 Debug.Log("##### velocity: " + velocity.z);
                //pop up
                if(coachmanType <= 3 && coachmanType >= 0)
                    coachmen[coachmanType].transform.Translate(new Vector3(0f, popDis[coachmanType] / 3.0f, 0f) * Time.deltaTime);
                if (!animationFlag && velocity.z <= 0)
                {
                    //play animation
                    animationFlag = true;
                    velocity.z = 0;
                    isSlowDown = true;
                    PlayAnimation();
                }
            }
            //animatorInfo = activatedAnimator.GetCurrentAnimatorStateInfo(0);
            //normalizedTime decides if the animation ends or not. if normalizedTime > 1, it's ends
            //if ((animatorInfo.normalizedTime >= 1.0f) && (animatorInfo.IsName("Negative" + coachmanTag[coachmanType])))
            //TriggerSeeArounnd();
        }
    }

    // play animation according to randomOrder
    
    public void PlayAnimation()
    {
        int pos = randomOrder[taskNum] % 23;
        if (1 < pos&&pos < 10)
        {
            //coachman design
            int isNegative = pos % 2 ;
            TriggerCoachmanAnimation(isNegative);
        }
        else if(10 <= pos&&pos < 16)
        {
            //facial expression design
            EyeTrack(pos > 12);
        }
        else if (pos >= 16)
        {
            // do the hand gesture
        }
    }
    public void StartTask()
    {
        Debug.Log(taskNum);
        if (taskNum >= TOTAL_TASK_NUM)
            {
                Debug.Log("#####Experiment is over");
                quitButton.SetActive(true);
            }
        else
        {
            menu.SetActive(false);
            isStart = true;
            animationFlag = false;
            isSlowDown = false;
            velocity = new Vector3(0.0f, 0.0f, 10f);
            startTime = Time.realtimeSinceStartup;
            this.transform.localPosition = new Vector3(-322.0f, 71.7f, -21.0f);  
            //saveDatatest();
            SendDataToDB();
        }
    }

    public void Quit()
    {
        /*Will not write data in local file any more
            string filepath = GetAndroidExternalFilesDir();
            Debug.Log(filepath);
            WriteToCSV(filepath + "/TTD.txt");
            Debug.Log("*****Quit");
            Debug.Log(dataList);
        */
        Application.Quit();
    }
    
    /*
    public void TriggerSeeArounnd()
    {
        Debug.Log("see around");
        //activatedAnimator.SetTrigger("SeeAroundTrigger");
        activatedAnimator.Play("SeeAround" + coachmanTag[coachmanType]);   //播放动画
        activatedAnimator.Update(0);
        if (coachmanType == 2)
        {
            activatedAnimator.Play("Default"); 
            activatedAnimator.Update(1);
        }
    }
    */
    //pop up the coachman at the slowdown process
    public void TriggerCoachmanAnimation(int isNegative)
    {
        if(isNegative == 0)
            activatedAnimator.Play("Positive" + coachmanTag[coachmanType]);
        else
            activatedAnimator.Play("Negative" + coachmanTag[coachmanType]);  
        activatedAnimator.Update(0);
        if (coachmanType == 2)
        {
            if (isNegative == 0)
                activatedAnimator.Play("Happy");
            else
                activatedAnimator.Play("Angry"); 
            activatedAnimator.Update(1);      
        }
    }

    public void SendDataToDB()
    {
        threadFlag = true;
        Thread thread = new Thread(new ThreadStart(this.timer));
        thread.Start();
    }
    public async void SaveCrossInfoToDB()
    {
        Debug.Log("#####save cross data to DB");
        var posData = new BsonDocument { { "userID", userID }, { "x", Position.x }, { "z", Position.z }, { "timeInSeconds", TimeInSeconds } };
        var rotData = new BsonDocument { { "userID", userID }, { "rotationX", Rotation.x }, { "rotationY", Rotation.y }, { "rotationZ", Rotation.z }, { "timeInSeconds", TimeInSeconds } };
        await posCollection.InsertOneAsync(posData);
        await rotCollection.InsertOneAsync(rotData);
        Debug.Log("#####saving cross data to DB is successful");
    }

    public async void SaveOrderInfoToDB()
    {
        Debug.Log("#####save order data to DB");
        string order = "";
        foreach (int value in randomOrder)
        {
            order = order + value.ToString() + ", ";
        }
        order = order.Substring(0,order.Length-2);
        
        var orderData = new BsonDocument { { "userID", userID }, { "order", order }};
        await orderCollection.InsertOneAsync(orderData);
        Debug.Log("#####saving order data to DB is successful");
    }
    public async void GetAndUpdateUserID()
    {
        var userInfo = userCollection.FindAsync(new BsonDocument());
        var userInfoAwaited = await userInfo;

        Debug.Log("#####received info from DB");
        var userIDJson = userInfoAwaited.ToList()[0].ToString();
        int startPos = userIDJson.IndexOf("),", 0) + 2;
        int beginPos = userIDJson.IndexOf(": ", startPos);
        string userId = "";
        for (int j = beginPos + 2; j < userIDJson.Length - 1; j++)
            userId = userId + userIDJson[j];
        userID = int.Parse(userId);
        var filter = Builders<BsonDocument>.Filter.Eq("userID", userID);
        var update = Builders<BsonDocument>.Update.Set("userID", userID + 1);
        Debug.Log("#####update user DB");
        await userCollection.UpdateOneAsync(filter, update);
    }
    public void timer()
    {
        //Thread.CurrentThread.IsBackground = true;
        while (threadFlag)
        {
            Thread.CurrentThread.Join(10000);
            SaveCrossInfoToDB();
        }
    }

    public void BoundaryCheck()
    {
        //get the boundary info
        float x1 = 0f, y1 = 0f, z1 = 0f,x2 = 0f, y2 = 0f, z2 = 0f;
        float maxDis = 0f;
        for (int i = 0; i < boundaryPoints.Length; i++)
        {
            if (boundaryPoints[i].x > x1)
                x1 = boundaryPoints[i].x;
            if (boundaryPoints[i].x < x2)
                x2 = boundaryPoints[i].x;
            if (boundaryPoints[i].y > y1)
                y1 = boundaryPoints[i].y;
            if (boundaryPoints[i].y < y2)
                y2 = boundaryPoints[i].y;
            if (boundaryPoints[i].z > z1)
                z1 = boundaryPoints[i].z;
            if (boundaryPoints[i].z < z2)
                z2 = boundaryPoints[i].z;
            float temp = (float)(Math.Pow(boundaryPoints[i].x, 2) + Math.Pow(boundaryPoints[i].z, 2));
            if (maxDis < temp)
                maxDis = temp;
        }
        Debug.Log("boundary max x is " + x1 + " min x is " + x2);
        Debug.Log("boundary max y is " + y1 + " min y is " + y2);
        Debug.Log("boundary max z is " + z1 + " min z is " + z2);
        Debug.Log("boundary max dis" + maxDis);
         if (maxDis < (4.5 * 4.5))
            {
                //show hint
                Quit();
            }
        // Debug.Log(dim.x);
        // Debug.Log(dim.y);
        // Debug.Log(dim.z);
    }

    //TBA
    private void EyeTrack(bool isTrack)
    {
        if(isTrack)
        {
            // do the track
        }
    }

    public int[] GetRandomList(int maxnum)
    {
        System.Random ran = new System.Random();
        int[] ans = new int[maxnum];
        for (int i = 1; i < maxnum+1; i++)
            ans[i-1] = i;
        while(maxnum>=1)
        {
            int temp = ran.Next(0, maxnum);
            // ans[temp] ^= ans[maxnum - 1];
            // ans[maxnum - 1] ^= ans[temp];
            // ans[temp] ^= ans[maxnum - 1];
            int te = ans[temp];
            ans[temp] = ans[maxnum - 1];
            ans[maxnum - 1] = te;
            Debug.Log(te);
            maxnum--;
        }
        return ans;
    }

    public void SetAnimator()
    {
        int pos = randomOrder[taskNum] % 23;
        rightEye.SetActive(pos >= 10 && pos <= 15);
        leftEye.SetActive(pos >= 10 && pos <= 15);
        lips[0].SetActive(pos == 10 || pos == 13);
        lips[1].SetActive(pos == 11 || pos == 14);
        lips[2].SetActive(pos == 12 || pos == 15);

        //TBA hand.SetActive(pos >= 16);
        //disable all unchosen coachman
        coachmanType = pos / 2 - 1;
        for(int i = 0; i < 4; i++)
        {
            coachmen[i].SetActive(coachmanType == i);
        }
        if (coachmanType == 0)
            activatedAnimator = coachmanAnimator;
        else if (coachmanType == 1)
            activatedAnimator = mechCoachmanAnimator;
        else if (coachmanType == 2)
            activatedAnimator = capsuleCoachman1Animator;
        else if (coachmanType == 3)
            activatedAnimator = capsuleCoachman2Animator;
        else //TBA: hand gesture
            ;
    }

    //when one task ends
    public void EndTask()
    {
        startTime = Time.realtimeSinceStartup;
        threadFlag = false;
        isStart = false;
        //save the waiting time&crossing time to DB

        //relocate the pedestrian
        GameObject.Find("RigCameraRig").GetComponent<OVRCameraMove>().ResetPos();
        //show the menu
        menu.SetActive(true);
        taskNum++;
        //TBA: relocate the coachman
    }

    //This method will not be called
    /*    public void WriteToCSV(string fileName)
        {
            if ( fileName.Length > 0)
            {
                //FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    //write data
                    string dataHeard = string.Empty;
                    dataHeard = "Participant ID,TTD,IsRight";
                    sw.WriteLine(dataHeard);
                    foreach(double time in dataList)
                    {
                        string dataStr = participandID.ToString() + ",";
                        dataStr += time.ToString();
                        sw.WriteLine(dataStr);
                    }
                }

            }
        }*/

    /*Used to access the file system in oculus. But since we are going to do the experiment online,
     * this method will be not called
    */
    private static string GetAndroidExternalFilesDir()
    {
     using (AndroidJavaClass unityPlayer = 
            new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
     {
          using (AndroidJavaObject context = 
                 unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
          {
               // Get all available external file directories (emulated and sdCards)
               AndroidJavaObject[] externalFilesDirectories = 
                                   context.Call<AndroidJavaObject[]>
                                   ("getExternalFilesDirs", (object)null);

               AndroidJavaObject emulated = null;
               AndroidJavaObject sdCard = null;

               for (int i = 0; i < externalFilesDirectories.Length; i++)
               {
                    AndroidJavaObject directory = externalFilesDirectories[i];
                    using (AndroidJavaClass environment = 
                           new AndroidJavaClass("android.os.Environment"))
                    {
                        // Check which one is the emulated and which the sdCard.
                        bool isRemovable = environment.CallStatic<bool>
                                          ("isExternalStorageRemovable", directory);
                        bool isEmulated = environment.CallStatic<bool>
                                          ("isExternalStorageEmulated", directory);
                        if (isEmulated)
                            emulated = directory;
                        else if (isRemovable && isEmulated == false)
                            sdCard = directory;
                    }
               }
               // Return the sdCard if available
               if (sdCard != null)
                    return sdCard.Call<string>("getAbsolutePath");
               else
                    return emulated.Call<string>("getAbsolutePath");
            }
      }
    }
    // void GetAnimatorInfo()
    // {
    //     string name = activatedAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;//获取当前播放动画的名称
    //     Debug.Log("当前播放的动画名为：" + name);
    //     float length = activatedAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.length;//获取当前动画的时间长度
    //     Debug.Log("播放动画的长度：" + length);
    // }
}
