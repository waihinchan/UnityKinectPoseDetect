using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System;
using Microsoft.Kinect.VisualGestureBuilder; 
using System.Collections.Generic; 
public class MultiSourceBodyIDManager : MonoBehaviour
{   

    // event handler
    public event BodyDataEvent OnBodyData; 
    public event GestureDataEvent OnGestureData; 
    // event handler

    // for dispose the unuse source and reader, not necessary to make a global var
    VisualGestureBuilderFrameSource disposesource;
    VisualGestureBuilderFrameReader disposereader;
    // for dispose the unuse source and reader

    // the interface for the game event
    private static MultiSourceBodyIDManager instance = null;
    public static MultiSourceBodyIDManager Instance{
        get{

            return instance;
        }
    }
    // the interface for the game event

    // kinect setting
    private KinectSensor _Sensor;
    private MultiSourceFrameReader _reader;
    CoordinateMapper coordinateMapper; // remap the value depth to color
    // kinect setting

    // array for the pixels
    private DepthSpacePoint[] depthSpacePoints;
    private byte[] _displayPixels = null; //the final pixels we show here
    private byte[] colorData = null;
	private ushort[] depthData = null;
    private byte[] bodyIndexData = null;
    private Body[] BodyData = null; //the trackinged ID inside
    // array for the pixels

    //Kinect frame(default setting)
    private BodyFrame bodyframe = null;
    private DepthFrame depthFrame = null;
    private ColorFrame colorFrame = null;
    private BodyIndexFrame bodyIndexFrame = null;
    //Kinect frame(default setting)

    // Visual gesture builder
    private VisualGestureBuilderDatabase _Database; 
    public string databasePath;  
    IList<Gesture> gesturesList = null;
    public IList<Gesture> GesturesList{
        get{return gesturesList;}
    }
    // Visual gesture builder space

    // ID management
    private Hashtable HashIdtb;
    public Hashtable GetHashIdtb(){
        return HashIdtb;
    }
    
    private Dictionary<ulong,int> PlayerArgs;
    public Dictionary<ulong,int> GetPlayerArgs(){
            return PlayerArgs;
    }
    // ID management
    
    // **********************************************default setting************************************************************

    // **********************************************custom setting************************************************************
    [Range(2, 6)]
    public uint Maxpeople = 2;



    // **********************************************custom setting************************************************************
    void awake(){
        if(Maxpeople>6){
            Maxpeople = 2;
        }
    }
    void Start()
    {   
        instance = this;
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {   
            coordinateMapper = _Sensor.CoordinateMapper;
            _reader = _Sensor.OpenMultiSourceFrameReader (FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body); 
            // we need a multi source
            if(_reader!=null){
                _reader.MultiSourceFrameArrived+=MultiSourceFrameArrivedHandler;
                //we separate this because we still need a coordinateMapper to deal with the raw data, then send it to our real custom event.
                //(if we want to combine the whole process into the custom event, we can expose the coordinateMapper and pass it into the event as a params or just get the var)

                AddEvent(); //add event to the controllers
            }

            // init array and point
            FrameDescription colorFrameDesc = _Sensor.ColorFrameSource.CreateFrameDescription (ColorImageFormat.Rgba); 
            colorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
            _displayPixels = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
            
            FrameDescription depthFrameDesc = _Sensor.DepthFrameSource.FrameDescription;
            depthData = new ushort[depthFrameDesc.LengthInPixels];
            depthSpacePoints = new DepthSpacePoint[colorFrameDesc.LengthInPixels];
            FrameDescription bodyIndexFrameDesc = _Sensor.BodyIndexFrameSource.FrameDescription;
            bodyIndexData = new byte[bodyIndexFrameDesc.BytesPerPixel * bodyIndexFrameDesc.LengthInPixels];
            // init array and point
            
            //the VGB database
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, databasePath);
            Debug.Log ("database path is "+path);
            _Database = VisualGestureBuilderDatabase.Create(path); 
            gesturesList = _Database.AvailableGestures; 
            //the VGB database




            // init our ID managements
            HashIdtb = new Hashtable();  // this manage the source and reader
            PlayerArgs = new Dictionary<ulong,int>(); // this pass to controllers
            // init our ID managements

            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }  

       
    }

    // Update is called once per frame
    void Update()
    {

        if(BodyData!=null){

            FindValidBody(BodyData);
        }
        
    }
    // *************************************************************************************************************************
    void OnApplicationQuit()
    {
        if (_reader != null)
        {
            _reader.Dispose();
            _reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
 // **************************************************these are called every frame************************************************************
    Hashtable GetBodyData(Body[] _BodyData){ // get the tracking body IDs
        if (_BodyData!=null) { // if we have bodydata(from bodyframe event)
            Hashtable Hashnewids = new Hashtable(); 
            foreach (Body body in _BodyData) {
                if(body.IsTracked && body.TrackingId!=0){ 
                    Hashnewids.Add(body.TrackingId,0); //we don't care the value
                }
            }
            return Hashnewids;
        }
        else{
            return null;
        }
    }
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
    
    void FindValidBody(Body[] _BodyData) { 
            Hashtable Hashnewids = GetBodyData(_BodyData);
            CleanUnValidBody(Hashnewids); // clean our not valid body first
            UpdateGestureResult(_BodyData);

    } 
    

 // **************************************************these are called every frame************************************************************
   
    private void GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e) {  //for each GestureReader Event
        VisualGestureBuilderFrameReference frameReference = e.FrameReference;           
        using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame()) {  
            if (frame != null) { 
                IDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults; // get the discrete gesture results which arrived with the latest frame 
                // retrun Gesture check the unity gesture scripts

                if( HashIdtb.ContainsKey(frame.TrackingId) && discreteResults != null && PlayerArgs.ContainsKey(frame.TrackingId) ){
                    
                    Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader> thisGestureGroup = HashIdtb[frame.TrackingId] as Dictionary <VisualGestureBuilderFrameSource , VisualGestureBuilderFrameReader>;
                    
                    foreach (var GestureGroup in thisGestureGroup){ // we actually only have 1 group, should i use a struct or something else?

                        var thisSource = GestureGroup.Key; 
                        foreach (Gesture gesture in thisSource.Gestures) { //all the gesture in gesturedatabase
                            if (gesture.GestureType == GestureType.Discrete) {  //check the type
                                DiscreteGestureResult result = null; 
                                discreteResults.TryGetValue(gesture, out result); 
                                if(OnGestureData!=null){ //we emit event EVERY GESTURE RESULT
                                    OnGestureData(this,new KinectGestureDataEvent(gesture.Name,result.Confidence,frame.TrackingId,PlayerArgs[frame.TrackingId]));
                                }
                            } 
                        }

                    }
                }
            }
        } 
    }
    // do custom function here
    void AddEvent(){
        if(gameObject.GetComponent<KinectBodyEventController>()!=null){
            gameObject.GetComponent<KinectBodyEventController>().AddBodyDataEvent(); //OnBodyData += custom function
        }
        if(gameObject.GetComponent<KinectGestureEventController>()!=null){
            gameObject.GetComponent<KinectGestureEventController>().AddGestureDataEvent(); //OnBodyData += custom function
        }
    }
    // do custom function here

// *************************************************************************************************************************
    void MultiSourceFrameArrivedHandler(object sender,MultiSourceFrameArrivedEventArgs e){ //when ever a frame arrived
        MultiSourceFrame frame  = e.FrameReference.AcquireFrame();
        bool updated = RefreshFrame(frame);
        if(updated){
            SendDatatoController();
        }
    }
// *************************************************************************************************************************
    bool RefreshFrame(MultiSourceFrame _frame){ //updateframe to our array
        if(_frame==null){
            return false;
        }
        try{
            bodyframe = _frame.BodyFrameReference.AcquireFrame();
            depthFrame = _frame.DepthFrameReference.AcquireFrame();
            colorFrame = _frame.ColorFrameReference.AcquireFrame();
            bodyIndexFrame = _frame.BodyIndexFrameReference.AcquireFrame();
            // If any frame has expired by the time we process this event, return.
            // The "finally" statement will Dispose any that are not null.
            if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null || bodyframe == null))
            {   
                return false;
            }
            else{
                if(BodyData==null){BodyData = new Body[_Sensor.BodyFrameSource.BodyCount];}
                // update all the array and points
                bodyframe.GetAndRefreshBodyData(BodyData);
                colorFrame.CopyConvertedFrameDataToArray (colorData, ColorImageFormat.Rgba);
                depthFrame.CopyFrameDataToArray (depthData);
                bodyIndexFrame.CopyFrameDataToArray (bodyIndexData);
                // update all the array and points
                _frame = null;
                return true;
            }
        }
        finally
        {
            if(bodyframe !=null){
                bodyframe.Dispose();
            }
            if (depthFrame != null)
            {
                depthFrame.Dispose();
            }

            if (colorFrame != null)
            {
                colorFrame.Dispose();
            }

            if (bodyIndexFrame != null)
            {
                bodyIndexFrame.Dispose();
            }
        }
    }
// ******************************************************Body frame event************************************************************
    void SendDatatoController(){ 
        //emit a event to bodydataeventcontroller
        if(OnBodyData!=null){
            coordinateMapper.MapColorFrameToDepthSpace (depthData, depthSpacePoints); 
            OnBodyData(this,new KinectBodyDataEvent(depthSpacePoints,bodyIndexData,colorData));	
        }
        //emit a event to bodydataeventcontroller

    }
// ******************************************************Body frame event************************************************************
}


        