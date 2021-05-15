﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class carController : MonoBehaviour
{

    public Animator coachmanAnimator;
    public Animator mechCoachmanAnimator;
    public Animator capsuleCoachman1Animator;
    public Animator capsuleCoachman2Animator;
    /*
        coachmantype decides which coachman is activated:
        1:human-like coachman(Latifa), 2:mechCoachman(Robot Kyle), 3:CapsuleCoachman1, 4:CapsuleCoachman2
    */
    public int coachmanType = 0; 
    /*
        setRandomNum decides which decision coachman will do:
        0: let pedestian go first, 1: let itself go first
    */ 
    public int setRandomNum = 2; 

    // All gestures, head movements are achieved by animation. So I use Animator to manage them.
    private Animator activatedAnimator;
    private AnimatorStateInfo animatorInfo;
    private GameObject city;
    private Transform zebraLine;
    private GameObject coachman;
    private GameObject mechCoachman;
    private GameObject capsuleCoachman1;
    private GameObject capsuleCoachman2;
    private GameObject menu;
    //movement controls the speed of vehicle.
    private Vector3 movement = new Vector3(0.0f, 0.0f, 0.05f);
    //distance records the distance that vehicle has traversed.
    private Vector3 distance = new Vector3(0.0f, 0.0f, 0.0f);
    //private AnimationClip[] animationClip;
    private string[] coachmanTag = {"", "_Mech", "_Cap1","_Cap2"};
    //animationFlag makes sure that StartAnimation will run only once
    private bool animationFlag = false;
    private int randomNum = 0;
    private float TTD = 0;
    private List<double> dataList = new List<double>();
    //isStart is decided by the menu button:Start task
    private bool isStart = false;
    private bool isSlowDown = false;
    private bool isDecided = false;
    private int participandID = 1;
    void Start()
    {
        // located the zebraline
        city = GameObject.Find("City");
        Transform props = city.transform.Find("props");
        zebraLine = props.transform.Find("Street 8 Prefab (6)");
        // located every coachman
        coachman = GameObject.Find("CoachManAV/Coachman");
        mechCoachman = GameObject.Find("CoachManAV/MechCoachman");
        capsuleCoachman1 = GameObject.Find("CoachManAV/CapsuleCoachman1");
        capsuleCoachman2 = GameObject.Find("CoachManAV/CapsuleCoachman2");
        menu = GameObject.Find("Menu");
        //disable all unchosen coachman
        coachman.SetActive(coachmanType == 0);
        mechCoachman.SetActive(coachmanType == 1);
        capsuleCoachman1.SetActive(coachmanType == 2);
        capsuleCoachman2.SetActive(coachmanType == 3);
        if (coachmanType == 0) 
            activatedAnimator = coachmanAnimator;
        else if (coachmanType == 1) 
            activatedAnimator = mechCoachmanAnimator;
        else if (coachmanType == 2) 
            activatedAnimator = capsuleCoachman1Animator;
        else
            activatedAnimator = capsuleCoachman2Animator;
        //animationClip = activatedAnimator.runtimeAnimatorController.animationClips;
        /*
            generate the random number, the rule is:
            if setRandomNum was set in unity correctly(0 or 1), then the code will use that num, else it will be generated by Random func. 
        */
        randomNum = setRandomNum < 2 ? setRandomNum:Random.Range(0,2);
        Debug.Log("random number is:" + randomNum);
    }
    void Update()
    {        
        if(isStart)
        {  
            //moving AV
            this.transform.Translate(movement);
            distance.z += movement.z;
            //Get the square length of two object vectors
            float sqrLenght = (zebraLine.position - this.transform.position).sqrMagnitude;
            //Slow down
            if (sqrLenght < 10 * 10 && sqrLenght > 5 * 5 && !isSlowDown)
                {
                    //slow down
                    movement.z = 0.02f;
                    TTD = Time.realtimeSinceStartup;
                    Debug.Log("slow down time " + TTD);
                    isSlowDown = true;
                }
            else if (sqrLenght < 5 * 5 )
            { 
                if(OVRInput.GetDown(OVRInput.RawButton.X))
                {
                    TTD = Time.realtimeSinceStartup - TTD;
                    dataList.Add(TTD);
                    isDecided = true;
                    Debug.Log("decision time " + TTD);
                }
                if (!animationFlag)
                {   
                    animationFlag = true;
                    PlayAnimation();
                }
            }
            animatorInfo = activatedAnimator.GetCurrentAnimatorStateInfo(0);
            //normalizedTime decides if the animation ends or not. if normalizedTime > 1, it's ends
            if ((animatorInfo.normalizedTime >= 1.0f) && (animatorInfo.IsName("Negative" + coachmanTag[coachmanType])))
                TriggerSeeArounnd();
        }
    }

    // play animation according to randomNum
    public void PlayAnimation()
    {
        if ( randomNum == 0)
        {
            TriggerNegative();
            movement.z = 0.01f;
        }
            
        else
        {
            Debug.Log("stopped");
            movement = Vector3.zero;
            TriggerPositive();
        }
    }
    public void StartTask()
    {
        menu.SetActive(false);
        isStart = true;
        animationFlag = false;
        isSlowDown = false;
        isDecided = false; 
        movement.z = 0.05f;
        distance.z = -distance.z;
        this.transform.Translate(distance);
        distance.z = 0.0f;
        Debug.Log("*****StartTask");
    }

    public void Quit()
    {
        string filepath = GetAndroidExternalFilesDir();
        Debug.Log(filepath);
        WriteToCSV(filepath + "/TTD.txt");
        Debug.Log("*****Quit");
        Debug.Log(dataList);
        Application.Quit();
    }
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

    public void TriggerNegative()
    {
        Debug.Log("TriggerNegative");
        activatedAnimator.Play("Negative" + coachmanTag[coachmanType]);  
        activatedAnimator.Update(0);
        if (coachmanType == 2)
        {
            activatedAnimator.Play("Angry"); 
            activatedAnimator.Update(1);      
        }
    }

    public void TriggerPositive()
    {
        Debug.Log("*****TriggerPositive");
        activatedAnimator.Play("Positive"  + coachmanTag[coachmanType]); 
        activatedAnimator.Update(0);    
        if (coachmanType == 2)
        {
            activatedAnimator.Play("Happy"); 
            activatedAnimator.Update(1);  
        }  
    }

    public void WriteToCSV(string fileName)
    {
        if ( fileName.Length > 0)
        {
            //这个地方是打开文件 fileName是你要创建的CSV文件的路径 例如你给个窗口选择的文件 C:/test.csv
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
    }

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