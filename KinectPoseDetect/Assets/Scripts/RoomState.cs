using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace myState
{   

    // bodystate
    public enum GestureState
    {
        NotValiding,
        Validing,
        Completed
    }
    public abstract class ValidGestureState{
        protected KinectBodyArgsManager kinectbodyargsmanager;
        public ValidGestureState(KinectBodyArgsManager _kinectbodyargsmanager){
            kinectbodyargsmanager = _kinectbodyargsmanager;
        }
        public virtual IEnumerator StartState(){
            yield break;
        }
        public virtual IEnumerator UpdateState(){
            yield break;
        }
        public virtual IEnumerator EndState(){
            yield break;
        }
    }
    public class NotValiding:ValidGestureState{ //waiting for gesture
        public NotValiding(KinectBodyArgsManager _kinectbodyargsmanager):base(_kinectbodyargsmanager){}

        public override IEnumerator StartState(){
            kinectbodyargsmanager.StartCoroutine(this.UpdateState());
            yield break;
        }

        public override IEnumerator UpdateState(){
            while(kinectbodyargsmanager.myState==GestureState.NotValiding){ //default status, listen on buffer
                if(kinectbodyargsmanager.current_Gesture==null){
                    yield return null;
                }
                else{
                    float current_result = kinectbodyargsmanager.CaculateGestureResult(kinectbodyargsmanager.myGestureBuffers,kinectbodyargsmanager.current_Gesture);
                    if(kinectbodyargsmanager.ourcondition(current_result)){ 
                        kinectbodyargsmanager.myState = GestureState.Validing;

                        // init a new loop
                        kinectbodyargsmanager.current_state = new OnValiding(kinectbodyargsmanager);
                        kinectbodyargsmanager.StartCoroutine(kinectbodyargsmanager.current_state.StartState());
                        // init a new loop

                        yield break;//once the condition match,jump out this
                    }
                    else{
                        yield return null; //otherwise keep validing
                    }
                }
            }
            yield break;
        }


    }
    public class OnValiding:ValidGestureState{
        public OnValiding(KinectBodyArgsManager _kinectbodyargsmanager):base(_kinectbodyargsmanager){}
        
        public override IEnumerator StartState(){
            Debug.Log($"Condition match,keeping the current gesture {kinectbodyargsmanager.current_Gesture}");
            kinectbodyargsmanager.StartCoroutine(this.UpdateState());
            yield break;
        }

        public override IEnumerator UpdateState(){
            float CountDown = kinectbodyargsmanager.TotalCountDown; //reset the timer(3s)
            kinectbodyargsmanager.StartCoroutine(this.KeepValiding()); //keep Validing while On Validing(Start another loop)
            while(kinectbodyargsmanager.myState == GestureState.Validing){//only charge for countdown

                yield return new WaitForSeconds(1.0f);
                CountDown-=1;
                Debug.Log($"Keep Gesture Remain {CountDown} Seconds!");
                if(CountDown==0){ //till end
                    kinectbodyargsmanager.myState = GestureState.Completed; //when change this, the keepvaliding should gone
                    kinectbodyargsmanager.current_state = new Completed(kinectbodyargsmanager);
                    kinectbodyargsmanager.StartCoroutine(kinectbodyargsmanager.current_state.StartState());
                    yield break; //jump out the loop
                }
                yield return null;
            }
            yield break;
        }
        public IEnumerator KeepValiding(){ //only this loop can change the onvaliding status
            while(kinectbodyargsmanager.myState == GestureState.Validing){
                float current_result = kinectbodyargsmanager.CaculateGestureResult(kinectbodyargsmanager.myGestureBuffers,kinectbodyargsmanager.current_Gesture);
                    if(!kinectbodyargsmanager.ourcondition(current_result)){ 
                        kinectbodyargsmanager.myState = GestureState.NotValiding;
                        kinectbodyargsmanager.initstate(); //goback to initstate,restart listening the buffer
                        yield break;
                    }
                yield return null;
            }
            yield break;
        }
    }
    public class Completed:ValidGestureState{
        public Completed(KinectBodyArgsManager _kinectbodyargsmanager):base(_kinectbodyargsmanager){}
        public override IEnumerator StartState(){ 
            //when Gesture Complete, we need to listen the event from the roomevent manager(wait for other guy or)
            Debug.Log("waiting for next challenge or end game");
            kinectbodyargsmanager.StartCoroutine(this.UpdateState());
            yield break;
        }
        public override IEnumerator UpdateState(){
            while(kinectbodyargsmanager.myState == GestureState.Completed){ //waiting for next command
                yield return null;
            }
            if(kinectbodyargsmanager.myState==GestureState.NotValiding){ //if the gameend, room state won't set this to NotValiding
                kinectbodyargsmanager.initstate(); //means restart the new challenge
                yield break;
            }
            yield break; //if gameend, will end this loop
        }
    }
    // bodystate
    //*******************************************************************************************************************************************
    // room state
    public abstract class RoomState 
    {
        // Start is called before the first frame update
        // protected readonly BattleSystem _system;
        protected RoomEventManager roomeventmanager;

        public RoomState(RoomEventManager _roomeventmanager){
            roomeventmanager = _roomeventmanager;
        }
        public virtual IEnumerator StartState(){
            yield break;
        }
        public virtual IEnumerator UpdateState(){
            yield break;
        }
        public virtual IEnumerator EndState(){
            yield break;
        }
        public void close(){ //if we could write a dispose here?
            
        }
        public void CheckBaseCondition(){ //when ever the condition not match, reset all value     
            if(MultiSourceBodyIDManager.Instance.GetPlayerArgs().Count<=0){ 
                roomeventmanager.StopAllCoroutines(); 
                roomeventmanager.current_state = new WaitingForPeople(roomeventmanager);
                roomeventmanager.StartCoroutine(roomeventmanager.current_state.StartState());
                return;
            }
        }
    }

    public class WaitingForPeople:RoomState{
        bool waiting = false;
        public WaitingForPeople(RoomEventManager _roomeventmanager):base(_roomeventmanager){

        }
        public override IEnumerator StartState(){
            // _roomeventmanager.current_state = this;
            waiting = true;
            roomeventmanager.RoomStatus.text = "waitting for people";
            
            // yield return null;
            roomeventmanager.StartCoroutine(this.UpdateState());//start to waiting for people
            yield break;
            
        }
        public override IEnumerator UpdateState(){      
            while(waiting){
                
                if(MultiSourceBodyIDManager.Instance!=null){
                    if(MultiSourceBodyIDManager.Instance.GetPlayerArgs().Count>0){ //once we detect people
                        waiting = false;
                        roomeventmanager.current_state = new ReadyingRoom(roomeventmanager);
                        
                        roomeventmanager.StartCoroutine(roomeventmanager.current_state.StartState());
                    }
                }
                yield return null;
            }
            yield break;
        }

    }
    public class ReadyingRoom:RoomState{
        bool gamestart = false;
        uint countdown = 10;
        public ReadyingRoom(RoomEventManager _roomeventmanager):base(_roomeventmanager){

        }
        public override IEnumerator StartState(){
            // _roomeventmanager.current_state = this;
            CheckBaseCondition(); //when ever the game start, we need to check condition every time
            
            roomeventmanager.RoomStatus.text = "ReadingRoom";
            roomeventmanager.StartCoroutine(this.UpdateState());//start to waiting for people
            yield break;
            // UpdateState();
        }
        public override IEnumerator UpdateState(){            
            while(!gamestart){
                CheckBaseCondition();
                 yield return new WaitForSeconds(1);
                 countdown -= 1;
                 roomeventmanager.RoomStatus.text = $"Find {MultiSourceBodyIDManager.Instance.GetPlayerArgs().Count} peoples";
                 roomeventmanager.CountDownTips.text = $"game start after {countdown} seconds";
                 
                 if(countdown==0){
                    gamestart = true;
                    roomeventmanager.RoomStatus.text = "game start!";
                    roomeventmanager.current_state = new OnPlaying(roomeventmanager);
                    roomeventmanager.StartCoroutine(roomeventmanager.current_state.StartState());
                 }
                 yield return null;
            }
            yield break;
        }   

    }
    public class OnPlaying:RoomState{
        // private 
        // IEnumerator current_challenge;
        uint IenumeratorIndex = 0;
        bool gameend = false;
        bool challenging = false;
        uint challenge_countdown = 10;
        bool current_challenge_succeed = false;
        public OnPlaying(RoomEventManager _roomeventmanager):base(_roomeventmanager){
            // current_challenge = roomeventmanager.Challenges.GetEnumerator();
        }
        public override IEnumerator StartState(){
            // _roomeventmanager.current_state = this;
            CheckBaseCondition(); //when ever the game start, we need to check condition every time
            
            // current_challenge.Reset();
            
            roomeventmanager.StartCoroutine(this.UpdateState());//start to waiting for people
            yield break;
            // UpdateState();
        }
        private void ResetStatue(){
            if(!gameend){
                current_challenge_succeed = false;
                challenge_countdown = 10;
                Dictionary<ulong,GameObject> trackingbody = roomeventmanager.KinectEventSourceManager.GetComponent<KinectGestureEventController>().GetKinectBodyArgsManagers();
                foreach (GameObject body in trackingbody.Values){ 
                    KinectBodyArgsManager bodyarg = body.GetComponent<KinectBodyArgsManager>();
                    bodyarg.myState = GestureState.NotValiding; //iter all body and set them to notvaliding(waiting for next challenge)
                } 
            }
        }
        public override IEnumerator UpdateState(){            
            while(!gameend){ 
                CheckBaseCondition(); //move this mother fucker in the future
                if(!challenging){ //means the current challenge end

                    if(IenumeratorIndex==RoomEventManager.Challenges.Length){ //means the finished all challenged
                        gameend = true;
                        //write some victory event here.
                        
                        roomeventmanager.RoomStatus.text = "gameEnd";
                        //need to clean the pixels and other stuff here(a fast way to do this is to switch to other scene and switch it back)
                        // roomeventmanager.KinectEventSourceManager.GetComponent<KinectGestureEventController>().DestoryAllBody();
                        
                        roomeventmanager.StartCoroutine(this.EndState());
                        yield break;
                    }
                    else{ //generate a new one
                        // roomeventmanager.GestureName.text = "next challenge start after 3 secondscurrent_Gesture";
                        roomeventmanager.challengeimage.GetComponent<ChangeImage>().setrawimage((int)IenumeratorIndex);
                        yield return new WaitForSeconds(3.0f);
                        // move the pointer to the next first so that the kinect start to emit the next gesture event
                        RoomEventManager.currentchallenge = RoomEventManager.Challenges[IenumeratorIndex];
                        IenumeratorIndex++;
                        
                        ResetStatue();
                        roomeventmanager.RoomStatus.text = "playing";
                        challenging =true; 
                        roomeventmanager.StartCoroutine(this.OnChallenging()); //start this new challenging
                        yield return null;
                    }
                    yield return null;
                }
                yield return null;
            }
            yield break;
        }  
        public override IEnumerator EndState(){    //restart game after seconds
            yield return new WaitForSeconds(3.0f);
            roomeventmanager.initstate();   
        }
        public IEnumerator KeepValiding(){
            Debug.Log($"{IenumeratorIndex} challenge start!");
            roomeventmanager.GestureName.text = $"challing on {RoomEventManager.currentchallenge}";
            while(challenging){ //when challinging
                CheckBaseCondition(); //move this mother fucker
                uint CountSucceed = 0; //cuclate the condition if match
                Dictionary<ulong,GameObject> trackingbody = roomeventmanager.KinectEventSourceManager.GetComponent<KinectGestureEventController>().GetKinectBodyArgsManagers();
                foreach (GameObject body in trackingbody.Values) //iter all
                {   
                    //emit different event
                    KinectBodyArgsManager bodyarg = body.GetComponent<KinectBodyArgsManager>();
                    bodyarg.displaystate();
                    switch (bodyarg.myState)
                    {
                        
                        case GestureState.NotValiding:
                            // roomeventmanager.CurrentGestureState.text = "NotValiding";
                            
                            break;

                        case GestureState.Validing:
                            // roomeventmanager.CurrentGestureState.text = $"this guy is Validing Gesture On {RoomEventManager.currentchallenge}";
                            break;

                        case GestureState.Completed:
                            // roomeventmanager.CurrentGestureState.text = $"this guy is completed{RoomEventManager.currentchallenge}";
                            CountSucceed+=1; //count

                            if(CountSucceed>=roomeventmanager.Maxpeople){ //once the count match to the condition
                                challenging = false; 
                                current_challenge_succeed = true;
                                roomeventmanager.GestureName.text = $"Current Challenge{RoomEventManager.currentchallenge} succeed!";
                                yield break; //end this loop
                            }
                            
                            break; //this break is for 1 person
                    }
                    //emit different event

                }
                yield return null;

            }
            yield break;
        }
        public IEnumerator OnChallenging(){
            roomeventmanager.StartCoroutine(this.KeepValiding()); //start listening the Gesture complete status(another loop)
            while(challenging){
                roomeventmanager.CountDownTips.text = $"{IenumeratorIndex} {RoomEventManager.currentchallenge} challenge remain {challenge_countdown}"; //refresh the countdown gui           
                yield return new WaitForSeconds(1);
                challenge_countdown-=1;
                if(challenge_countdown==0){ //once the countdown end

                    if(!current_challenge_succeed){
                        roomeventmanager.GestureName.text = $"Current Challenge{RoomEventManager.currentchallenge} failed!"; 
                    }
                    else{
                        roomeventmanager.GestureName.text = $"Current Challenge{RoomEventManager.currentchallenge} succeed!";
                    }

                    challenging = false;
                    
                    yield break;
                }
                // count down condition
                yield return null;
            }
            yield break;

        }
    }
    // room state

}
