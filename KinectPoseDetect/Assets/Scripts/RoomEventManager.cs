using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using myState;
public class RoomEventManager : MonoBehaviour
{   
    public GameObject KinectEventSourceManager; //get the player onrunning
    public static string[] Challenges = null; //need get from the database
    public static string currentchallenge = "empty";
    public uint Maxpeople{get;private set;}
    public GameObject challengeimage;
    //gui
    public Text CountDownTips;
    public Text RoomStatus;
    public Text GestureName;
    public Text CurrentGestureState;
    public RoomState current_state;
    //gui
    
    
    public void initstate(){
        CountDownTips.text = null;
        GestureName.text = null;
        CurrentGestureState.text = null;
        StopAllCoroutines();
        current_state = new WaitingForPeople(this);
        StartCoroutine(current_state.StartState());
    }
    void Awake(){
    }
    void Start()
    {   
        Maxpeople = MultiSourceBodyIDManager.Instance.Maxpeople;
        initstate();
    }
    void Update()
    {   
        if(Challenges==null){
            if(MultiSourceBodyIDManager.Instance.GesturesList!=null){
                RoomEventManager.Challenges = new string[MultiSourceBodyIDManager.Instance.GesturesList.Count];
                for (int g = 0; g < MultiSourceBodyIDManager.Instance.GesturesList.Count; g++) { 
                    Microsoft.Kinect.VisualGestureBuilder.Gesture gesture = MultiSourceBodyIDManager.Instance.GesturesList[g];
                    RoomEventManager.Challenges[g] = gesture.Name; 
                } 
            }
        }

    }


  

}




