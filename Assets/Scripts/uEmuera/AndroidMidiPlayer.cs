using UnityEngine;

namespace uEmuera
{
    public class AndroidMidiPlayer
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject mediaPlayer;
#endif

        public AndroidMidiPlayer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                mediaPlayer = new AndroidJavaObject("android.media.MediaPlayer");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to initialize Android MediaPlayer: " + e.Message);
            }
#else
            Debug.LogWarning("AndroidMidiPlayer is only supported on Android devices.");
#endif
        }

        public void Play(string path, bool loop)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mediaPlayer == null) return;
            
            try 
            {
                mediaPlayer.Call("reset");
                mediaPlayer.Call("setDataSource", path);
                mediaPlayer.Call("prepare");
                mediaPlayer.Call("setLooping", loop);
                mediaPlayer.Call("start");
                Debug.Log("AndroidMidiPlayer playing: " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("AndroidMidiPlayer Play Error: " + e.Message);
            }
#else
            Debug.LogWarning($"[STUB] AndroidMidiPlayer Play called for path: {path}");
#endif
        }

        public void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mediaPlayer == null) return;
            try
            {
                if (mediaPlayer.Call<bool>("isPlaying"))
                {
                    mediaPlayer.Call("stop");
                    Debug.Log("AndroidMidiPlayer stopped.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("AndroidMidiPlayer Stop Error: " + e.Message);
            }
#else
            Debug.LogWarning("[STUB] AndroidMidiPlayer Stop called");
#endif
        }

        public void SetVolume(float volume)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mediaPlayer == null) return;
            try
            {
                mediaPlayer.Call("setVolume", volume, volume);
            }
            catch (System.Exception e)
            {
                Debug.LogError("AndroidMidiPlayer SetVolume Error: " + e.Message);
            }
#else
            Debug.LogWarning($"[STUB] AndroidMidiPlayer SetVolume called: {volume}");
#endif
        }

        public void Release()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mediaPlayer == null) return;
            try
            {
                mediaPlayer.Call("release");
                mediaPlayer = null;
            }
            catch {}
#endif
        }
        
        public bool IsPlaying()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mediaPlayer == null) return false;
            try
            {
                return mediaPlayer.Call<bool>("isPlaying");
            }
            catch { return false; }
#else
            return false;
#endif
        }
    }
}
