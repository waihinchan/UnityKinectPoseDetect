# ReadMe

Keys：

1. multi Id manage(dispose, base on ID to track)
2. FSM
3. Remap Depth data to Color

core: multisourceidmanager

Key1:

if we need the body frame and gesture frame at the same time , we need a [MultiSourceFrameReader](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn791337(v=ieb.10))  

```csharp
private MultiSourceFrameReader _reader;
private KinectSensor _Sensor;
_reader = _Sensor.OpenMultiSourceFrameReader (FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);
```

(clean + assign + event)  manage the id (in case of body in/out)

```csharp
void FindValidBody(Body[] _BodyData) { 
            Hashtable Hashnewids = GetBodyData(_BodyData);
            CleanUnValidBody(Hashnewids); // clean our not valid body first
            UpdateGestureResult(_BodyData);

    }

// BodyData from the above _reader "frame arrived" event(check the source code to see the detail)
// every time we will get a Id list from the new _BodyData, we compare with the tracked id, if the tracked id not exist in the current Id list, we remove it.
VisualGestureBuilderFrameSource disposesource;
VisualGestureBuilderFrameReader disposereader;
void CleanUnValidBody(Hashtable _Hashnewids){
        if(_Hashnewids!=null){
            ArrayList key = new ArrayList(HashIdtb.Keys);
            foreach (ulong id in key) 
            {   
                if(!_Hashnewids.ContainsKey(id)){ //use the pass IDlist to check if the new IDlist still has that value
                    Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader> tempgroup = HashIdtb[id] as Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader>;
                    //this is our bodyid coorespond to the gesture result
                    foreach(VisualGestureBuilderFrameSource single_group in tempgroup.Keys){ // we only have 1 group in this dictionary
                        disposesource = single_group;
                        disposereader = tempgroup[single_group];
                    }
                    tempgroup.Clear();
                    if(disposesource!=null){
                        disposesource.Dispose();
                        disposesource = null;
                    }
                    if(disposereader!=null){
                        disposereader.IsPaused = true;
                        // disposereader.FrameArrived-=GestureFrameArrived;
       

                        disposereader.Dispose();
                        disposereader=null;
                    }
                    HashIdtb.Remove(id); //Remove the Id from our trackedIDtable(the "HashIdtb")
                    PlayerArgs.Remove(id);
                    Debug.Log("Remove a " + id);
                }
            }
        }
    }
// in this case, we need to Dispose the unuse ID as there are brunch of event and gameobject binded to this.

// update data and refill the new ID
// we update the exist id. and init the new id
// the GestureFrameArrived event is kind of like the MultiSourceFrameArrived event, when ever a gesture detect frame arrived, we send some custom event
// (to make everything not mess, we send all the gesture in our database to the real gesture event receiver, and let it to judge if this is the gesture we want).
// we also have different gesture name and confidence here, check the event folder to see the gesture event.
void UpdateGestureResult(Body[] _BodyData){
        if(_BodyData!=null){ 
            int bodyindex = 0; //the bodyindex for those event controllers
            foreach (Body body in _BodyData) {
                // init a new ID with source and reader
                if (body.IsTracked && HashIdtb.Count<Maxpeople && !HashIdtb.ContainsKey(body.TrackingId) && body.TrackingId!=0){
                    
                        
                        VisualGestureBuilderFrameSource _Source = VisualGestureBuilderFrameSource.Create(_Sensor, 0); // create a VGB from sensor
                        // add the Gestures in database
                        for (int g = 0; g < gesturesList.Count; g++) { 
                            Gesture gesture = gesturesList[g];
                            
                            _Source.AddGesture(gesture); 
                        } 
                        // add the Gestures in database

                        _Source.TrackingId = body.TrackingId; // assign a ID
                        VisualGestureBuilderFrameReader  _Reader = _Source.OpenReader(); 

                        if (_Reader != null) { 
                            _Reader.IsPaused = false; 
                            _Reader.FrameArrived += GestureFrameArrived;
                        }

                        Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader> _Group = new Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader>();
                        _Group.Add(_Source,_Reader);
                        HashIdtb.Add(_Source.TrackingId,_Group);
                        if(!PlayerArgs.ContainsKey(body.TrackingId)){
                            PlayerArgs.Add(_Source.TrackingId,bodyindex);
                        }
                        Debug.Log("Add a " + body.TrackingId); 
                }
                // init a new ID with source and reader

                // only update our body index to our playerargs(bcos once the reader init, we don't need to update the data mannually)
                if (body.IsTracked && PlayerArgs.ContainsKey(body.TrackingId) && body.TrackingId!=0){
                    PlayerArgs[body.TrackingId] = bodyindex;
                }

                    bodyindex+=1; //move the index to next
            }
        }
    }

// update data and refill the new ID

```

Key2:

fsm is kind of quite simple and can be reuse many time:

for example. if we want some certain gestures keep validing until some times to trigger some funciton. we can make a fsm like this:

```csharp
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

```

And make "NotValiding" , "Validing", "Completed".  Each State has some certain to enter the next state or enter the next state.

Key3:

We need a [CoordinateMapper](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn758445(v=ieb.10)) to deal with the raw data.  also see the [reference](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn758461(v=ieb.10)?redirectedfrom=MSDN) : the value not belong to the body is 255
the whole process is , there is also a Index data to figure out which pixels is belong to which "body" (0-5 is body, 255 is background)
then according to this "ID", we find the index in that remap color data(we use CoordinateMapper to deal(allign) the depth pixels to the color pixels and get the "remap color data"), so that we finally find out where is the body and where is the background.

```csharp
//this case we only care the body pixels
private void DealwithRawData(object sender,KinectBodyDataEvent e){ //be called every bodyframe received from kinect
        
      if(_Texture == null){
          _Texture = new Texture2D (1920, 1080, TextureFormat.RGBA32, false); //we hard coding here colorFrameDesc.Width, colorFrameDesc.Height
      }
      if(_displayPixels==null){
          _displayPixels = new byte[8294400];//we hard coding here: colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels
      }

			DepthSpacePoint[] depthSpacePoints = e.depthSpacePoints;
      byte[] bodyIndexData = e.bodyIndexData;
      byte[] colorData = e.colorData;
      Array.Clear(_displayPixels, 0, _displayPixels.Length);
      for (int colorIndex = 0; colorIndex < depthSpacePoints.Length; ++colorIndex)
      {
          DepthSpacePoint depthSpacePoint = depthSpacePoints[colorIndex]; 
          
          if (!float.IsNegativeInfinity(depthSpacePoint.X) && !float.IsNegativeInfinity(depthSpacePoint.Y)) 
          {
              int depthX = (int)(depthSpacePoint.X + 0.5f); 
              int depthY = (int)(depthSpacePoint.Y + 0.5f);

              if ((depthX >= 0) && (depthX < 512) && (depthY >= 0) && (depthY < 424)) 
              {
                  int depthIndex = (depthY * 512) + depthX; 
                  byte player = bodyIndexData[depthIndex]; //find the depth index in bodyindex's value

                  // Identify whether the point belongs to a player
                  if (player != 255) //if value not = 255(means a background)
                  {   
                      if(MultiSourceBodyIDManager.Instance.GetPlayerArgs().ContainsValue(player)){
                          int sourceIndex = colorIndex * 4; //our source index * 3 (we have RGBA value in color frame)
                          _displayPixels[sourceIndex] = colorData[sourceIndex++];  //r  
                          _displayPixels[sourceIndex] = colorData[sourceIndex++];    //g
                          _displayPixels[sourceIndex] = colorData[sourceIndex++];   //b 
                          _displayPixels[sourceIndex] = 255;
                      }
                  }
              }
          }
        }
        _Texture.LoadRawTextureData(_displayPixels);
        _Texture.Apply();
```