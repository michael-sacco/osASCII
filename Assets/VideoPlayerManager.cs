using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerManager : MonoBehaviour
{
    
    private VideoPlayer videoPlayer;
    private int activeClip = 0;

    [SerializeField] List<VideoClip> clips;
    [SerializeField] bool loopFromEnd = false;

    // Start is called before the first frame update
    void Start()
    {
        activeClip = 0;
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.clip = clips[activeClip];
        videoPlayer.Play();
        videoPlayer.loopPointReached += PlayNext;
    }

    void PlayNext(VideoPlayer videoPlayer)
    {
        activeClip++;
        if (activeClip >= clips.Count)
        {
            if (loopFromEnd)
                activeClip = 0;
            else
                return;
        }

        videoPlayer.clip = clips[activeClip];
        
        videoPlayer.Play();
    }
}
