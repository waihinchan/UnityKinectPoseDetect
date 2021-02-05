using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Kinect.VisualGestureBuilder; 
using Windows.Kinect;
public class KinectBodyDataEvent : EventArgs{ //this event for handle when received the bodyindexrawdata
    public DepthSpacePoint[] depthSpacePoints;
    public byte[] bodyIndexData;
    public byte[] colorData;
	public KinectBodyDataEvent(DepthSpacePoint[] _depthSpacePoints, byte[] _bodyIndexData,byte[] _colorData) 
	{ 	
        depthSpacePoints = _depthSpacePoints;
        bodyIndexData = _bodyIndexData;
        colorData = _colorData;
	} 
}
public delegate void BodyDataEvent(object sender,KinectBodyDataEvent e); //委托定义

// public class KinectGestureEvent : EventArgs {

// 	public string name;
// 	public ulong id;
// 	public float confidence; 
// 	public KinectGestureEvent(string _name, float _confidence,ulong _id) 
// 	{ 	
// 		id = _id;
// 		name = _name; 
// 		confidence = _confidence;
// 	} 
// }

// public delegate void GestureEvent(object sender,KinectGestureEvent e); //委托定义

