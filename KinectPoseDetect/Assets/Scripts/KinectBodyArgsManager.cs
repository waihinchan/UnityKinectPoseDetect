using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using myState;

    public class KinectBodyArgsManager:MonoBehaviour
    {   

        //some status management
        public GestureState myState{get; set;}
        public bool Counting = false;
        public float threshold {get;private set;}
        public ValidGestureState current_state;
        public string current_Gesture;
        public bool GestureComplete = false;
        public Hashtable myGestureBuffers = null;
        public int myIndexinBodyFrame;
        public float waitTime;
        public float TotalCountDown{get;set;}
        private float totalcountdown;
        private uint bufferlength;
        
        public void initPrams(int _myIndexinBodyFrame,float _waitTime,float _TotalCountDown,uint _bufferlength){
            myGestureBuffers = new Hashtable();
            myIndexinBodyFrame = _myIndexinBodyFrame;
            TotalCountDown = _TotalCountDown;
            waitTime =  _waitTime;
            bufferlength = _bufferlength;
            threshold = 0.3f;
            initstate();

        }
        public void initstate(){
            StopAllCoroutines();

            myState = GestureState.NotValiding;
            current_state = new NotValiding(this);
            StartCoroutine(current_state.StartState());

        }
        void awake(){
        }
        public void displaystate(){

            if(myState!=null){
                
                switch (myState)
                {
                    case GestureState.NotValiding:
                        KinectBodyEventController.modifyBodyColor(255,myIndexinBodyFrame);
                        break;
                    case GestureState.Validing:
                        KinectBodyEventController.modifyBodyColor(20,myIndexinBodyFrame);
                        break;
                    case GestureState.Completed:
                        KinectBodyEventController.modifyBodyColor(0,myIndexinBodyFrame);
                        break;
                    
                }
            }
        }
        void update(){
           
        }

        public void UpdateGestureBuffers(string _gesture_name, float _confidence){ 
            //refill buffer every time(we can write a condition here if we want to save the pass record ..)
            
            current_Gesture = _gesture_name;
            if(myGestureBuffers==null){
                myGestureBuffers = new Hashtable();
            }
            
            Queue<float> thisGestureBuffer; // new or assign
            if(!myGestureBuffers.ContainsKey(_gesture_name)){ //check if this gesture in my buffer
                thisGestureBuffer = new Queue<float>();
                thisGestureBuffer.Enqueue(_confidence); //the first result
                myGestureBuffers.Add(_gesture_name,thisGestureBuffer);
            }
            else{
                thisGestureBuffer = myGestureBuffers[_gesture_name] as Queue<float>;
                if(thisGestureBuffer.Count>=bufferlength){
                    thisGestureBuffer.Dequeue();
                    thisGestureBuffer.Enqueue(_confidence);
                }
                else{
                    thisGestureBuffer.Enqueue(_confidence);
                }
            }

        }
        public bool ourcondition(float current_result){
            if(current_result>threshold){
                return true;
            }
            else{
                return false;
            }
        }

        byte OutPutValue(float _Result){
             
            _Result*=255;
            if(_Result>255){
                _Result = 255;
            }
            if(_Result<0){
                _Result = 0;
            }
            return Convert.ToByte(_Result);
        }
        

        public float CaculateGestureResult(Hashtable _myGestureBuffers,string _GestureName){ //caculate the gesture buffer
            float totalresult = 0; 
            if(!_myGestureBuffers.ContainsKey(_GestureName)){
                return totalresult; // we need a NAN here maybe? (if the condition we wirte allow negative)
            }
            Queue<float> thisgesturebuffer = _myGestureBuffers[_GestureName] as Queue<float>;
            
            // write our algorithm here
            foreach (float gestureresult in thisgesturebuffer){
                totalresult+=gestureresult;
            }
            totalresult = totalresult/thisgesturebuffer.Count;
            return totalresult;
        
        }
        // not sure if this would working
        void OnDestroy(){
            StopAllCoroutines();
        }
        
















    }













