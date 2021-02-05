using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Microsoft.Kinect.VisualGestureBuilder; 
using Windows.Kinect;

public class KinectGestureDataEvent : EventArgs
{
	public string name;
	public ulong id;
	public float confidence; 
	public int indexinbodyframe;
    public KinectGestureDataEvent(string _name, float _confidence,ulong _id,int _indexinbodyframe) 
	{ 	
		id = _id;
		name = _name; 
		confidence = _confidence;
		indexinbodyframe = _indexinbodyframe;
	} 

}
public delegate void GestureDataEvent(object sender,KinectGestureDataEvent e); //委托定义
