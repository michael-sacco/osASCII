using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace OccaSoftware
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoPlayerManager : MonoBehaviour
    {
        private VideoPlayer videoPlayer;
        private int activeClip = 0;


        /* Dropbox video URL links need to be reformatted from the Copy Link format as follows
         * from: https://www.dropbox.com/s/pv1i9mq3w5bv3fb/pexels-mikhail-nilov-8301918.mp4?dl=0
         * to:   https://dl.dropbox.com/s/pv1i9mq3w5bv3fb/pexels-mikhail-nilov-8301918.mp4?dl=1
         */
        [SerializeField] List<string> videoURLList;
        [SerializeField] bool loopFromEnd = false;


        void Start()
        {
            activeClip = 0;
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoURLList[activeClip];
            videoPlayer.Play();
            videoPlayer.loopPointReached += PlayNext;
        }

        void PlayNext(VideoPlayer videoPlayer)
        {
            activeClip++;
            if (activeClip >= videoURLList.Count)
            {
                if (loopFromEnd)
                    activeClip = 0;
                else
                    return;
            }

            videoPlayer.url = videoURLList[activeClip];
            videoPlayer.Play();
        }

    }

}
