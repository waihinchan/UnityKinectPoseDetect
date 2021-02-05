using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ChangeImage : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Texture2D> challengeImage;
    void Start()
    {
        
    }

    // Update is called once per frame
    public void setrawimage(int index){
        gameObject.GetComponent<RawImage>().texture = challengeImage[index];
    }
    void Update()
    {
        
    }
}
