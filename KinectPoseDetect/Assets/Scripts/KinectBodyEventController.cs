using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Kinect.VisualGestureBuilder; 
using Windows.Kinect;
public class KinectBodyEventController : MonoBehaviour
{   
    MultiSourceBodyIDManager multisourcebodyidmanager;
    
    [HideInInspector]
    private static byte[] bodycolor = null;
    private byte[] _displayPixels = null; //the final pixels we show here
    private Texture2D _Texture = null; 
    public Texture2D GetDisplayTexture()
    {
        return _Texture;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public static void modifyBodyColor(byte _color, int index){
        bodycolor[index] = _color;
    }
    public void AddBodyDataEvent(){ //bridge to source manager
        multisourcebodyidmanager = MultiSourceBodyIDManager.Instance;
        multisourcebodyidmanager.OnBodyData += DealwithRawData;
    }

    // this event only control 1 person.
    private void DealwithRawData(object sender,KinectBodyDataEvent e){ //be called every bodyframe received from kinect
        
        if(_Texture == null){
            _Texture = new Texture2D (1920, 1080, TextureFormat.RGBA32, false); //we hard coding here colorFrameDesc.Width, colorFrameDesc.Height
        }
        if(_displayPixels==null){
            _displayPixels = new byte[8294400];//we hard coding here: colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels
        }
        if(bodycolor==null){
            bodycolor = new byte[6]{255,255,255,255,255,255};
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
                            int sourceIndex = colorIndex * 4; //our source index * 3 (we have BGRA value in color frame)
                            byte status = bodycolor[player];
                            if(status == 0){  //gesture complete

                                _displayPixels[sourceIndex] = colorData[sourceIndex++];  //r  
                                _displayPixels[sourceIndex] = colorData[sourceIndex++];    //g
                                _displayPixels[sourceIndex] = colorData[sourceIndex++];   //b
                            }

                            else if(status == 255){ //default
                                _displayPixels[sourceIndex] = 10;  //r 
                                sourceIndex++ ;
                                _displayPixels[sourceIndex] = 10;  //g 
                                sourceIndex++ ;
                                _displayPixels[sourceIndex] = 10;  //b 
                                sourceIndex++ ;
                            }

                            else{ //on validing
                                int randomInt = (int)UnityEngine.Random.Range(0, 255);

                                _displayPixels[sourceIndex] = colorData[sourceIndex++];  //r  
                                _displayPixels[sourceIndex] = Convert.ToByte(randomInt);;    //g
                                sourceIndex++;
                                _displayPixels[sourceIndex] = colorData[sourceIndex++];   //b
                            }

                            
                            _displayPixels[sourceIndex] = 255;
                        }
               
                    }
                }
            }
        }
        _Texture.LoadRawTextureData(_displayPixels);
        _Texture.Apply();

	}
}
