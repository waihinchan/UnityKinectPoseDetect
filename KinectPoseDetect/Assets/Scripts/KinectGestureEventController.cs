using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Kinect.VisualGestureBuilder; 
using Windows.Kinect;
using System;

public class KinectGestureEventController : MonoBehaviour
{
    // these will assign to KinectBodyArgsManager object
    public uint bufferlength = 10;
    public float EachCountDown = 1.0f;
    public float totalcountDown = 3.0f;
    // these will assign to KinectBodyArgsManager object
    
    private Dictionary<ulong,GameObject> KinectBodyArgsManagers = null; 
    public Dictionary<ulong,GameObject> GetKinectBodyArgsManagers(){
        return KinectBodyArgsManagers;
    }
    MultiSourceBodyIDManager multisourcebodyidmanager;
    void awake(){
        KinectBodyArgsManagers = new Dictionary<ulong,GameObject>();
    }
    void Start()
    {
     
    }

    // Update is called once per frame
    void Update()
    {
        ManageKinectBodyArgsManager(); // clean the not valid ID every frames;
    }
    private void ManageKinectBodyArgsManager(){
        if(KinectBodyArgsManagers==null||KinectBodyArgsManagers.Count==0){
            return;
        }
        multisourcebodyidmanager = MultiSourceBodyIDManager.Instance;
        Dictionary<ulong,int> PlayerArgs = multisourcebodyidmanager.GetPlayerArgs();
        ArrayList key = new ArrayList(KinectBodyArgsManagers.Keys);
        foreach (ulong trackedId in key)
        {   
            if(!PlayerArgs.ContainsKey(trackedId)){

                Destroy(KinectBodyArgsManagers[trackedId]);
                KinectBodyArgsManagers.Remove(trackedId);
            }
        }
    }
    public void DestoryAllBody(){
        if(KinectBodyArgsManagers==null||KinectBodyArgsManagers.Count==0){
            return;
        }
        ArrayList key = new ArrayList(KinectBodyArgsManagers.Keys);
        foreach (ulong trackedId in key)
        {   
            Destroy(KinectBodyArgsManagers[trackedId]);
            KinectBodyArgsManagers.Remove(trackedId);
        }
    }
    public void AddGestureDataEvent(){//the bridge to Gesturesource
        multisourcebodyidmanager = MultiSourceBodyIDManager.Instance;
        multisourcebodyidmanager.OnGestureData += DealWithGestureData;
    }

    
    private void DealWithGestureData(object sender,KinectGestureDataEvent e){ 
        // we throw the data to each bodyargsmanager and let them to caculate their condtion and status when the frame arrived
        // Debug.Log("Received event from reader");

        KinectBodyArgsManager thisKinectBodyArgsManager;
        GameObject thisbody;
        
        // ID management
        if(KinectBodyArgsManagers==null){
            KinectBodyArgsManagers = new Dictionary<ulong,GameObject>();
        }
        if(!KinectBodyArgsManagers.ContainsKey(e.id)){ //add a new id

            
            thisbody = new GameObject();
            thisKinectBodyArgsManager = thisbody.AddComponent(typeof(KinectBodyArgsManager)) as KinectBodyArgsManager;
            thisKinectBodyArgsManager.initPrams(e.indexinbodyframe,EachCountDown,totalcountDown,bufferlength);
            
            KinectBodyArgsManagers.Add(e.id,thisbody);
        }
        else{ 
            thisbody = KinectBodyArgsManagers[e.id];
            thisKinectBodyArgsManager = thisbody.GetComponent<KinectBodyArgsManager>(); //assign the exist id
            thisKinectBodyArgsManager.myIndexinBodyFrame = e.indexinbodyframe; // update the bodyindex in case of the sequence in bodyframe changed
        }
        
        // ID management
        //gesture name and data management
        if(RoomEventManager.currentchallenge==e.name){ //only emit when ever the gesture is what we looking for

            thisKinectBodyArgsManager.UpdateGestureBuffers(e.name,e.confidence);
            //gesture name and data management

            //emit their validgesture event
            //emit their validgesture event
        }
    }
    


    
}




