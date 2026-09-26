using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

namespace uEmuera
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource bgmSource;
        private AudioSource seSource;
        private readonly System.Collections.Generic.Queue<System.Action> executeOnMainThread = new System.Collections.Generic.Queue<System.Action>();

        private AndroidMidiPlayer midiPlayer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (gameObject.GetComponent<AudioListener>() == null)
                {
                    gameObject.AddComponent<AudioListener>();
                }

                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;

                seSource = gameObject.AddComponent<AudioSource>();
                seSource.loop = false;
                seSource.playOnAwake = false;

                midiPlayer = new AndroidMidiPlayer();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (midiPlayer != null)
            {
                midiPlayer.Release();
            }
        }

        private void Update()
        {
            lock (executeOnMainThread)
            {
                while (executeOnMainThread.Count > 0)
                {
                    executeOnMainThread.Dequeue().Invoke();
                }
            }
        }

        public void PlayBGM(string filename)
        {
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() => StartCoroutine(LoadAndPlayClip(filename, bgmSource, false)));
            }
        }

        public void StopBGM()
        {
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() =>
                {
                    if (bgmSource.isPlaying)
                        bgmSource.Stop();
                    if (midiPlayer != null && midiPlayer.IsPlaying())
                        midiPlayer.Stop();
                });
            }
        }

        public void SetBGMVolume(int volume)
        {
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() =>
                {
                    float vol = Mathf.Clamp01(volume / 100f);
                    bgmSource.volume = vol;
                    if (midiPlayer != null)
                        midiPlayer.SetVolume(vol);
                });
            }
        }

        public void PlaySound(string filename)
        {
            PlaySound(filename, 1);
        }

        /// <summary>
        /// PLAYSOUND 名前, 回数(EE)。回数だけ続けて鳴らす(1未満は1)
        /// </summary>
        public void PlaySound(string filename, int repeat)
        {
            if (repeat < 1)
                repeat = 1;
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() =>
                {
                    var co = StartCoroutine(LoadAndPlayClip(filename, seSource, true, repeat));
                    soundCoroutines.Add(co);
                });
            }
        }
        /// <summary>繰り返し再生中の効果音。STOPSOUNDで止める</summary>
        private readonly System.Collections.Generic.List<Coroutine> soundCoroutines = new System.Collections.Generic.List<Coroutine>();

        public void StopSound()
        {
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() =>
                {
                    foreach (var co in soundCoroutines)
                        if (co != null)
                            StopCoroutine(co);
                    soundCoroutines.Clear();
                    if (seSource.isPlaying)
                        seSource.Stop();
                    if (midiPlayer != null && midiPlayer.IsPlaying())
                        midiPlayer.Stop();
                });
            }
        }

        public void SetSoundVolume(int volume)
        {
            lock (executeOnMainThread)
            {
                executeOnMainThread.Enqueue(() =>
                {
                    float vol = Mathf.Clamp01(volume / 100f);
                    seSource.volume = vol;
                    if (midiPlayer != null)
                        midiPlayer.SetVolume(vol);
                });
            }
        }

        private IEnumerator LoadAndPlayClip(string filename, AudioSource source, bool playOneShot, int repeat = 1)
        {
            string fullPath = Path.GetFullPath(MinorShift.Emuera.Program.ExeDir + "sound/" + filename);
            if (!File.Exists(fullPath))
            {
                // try to find with extensions
                string[] exts = { ".ogg", ".mp3", ".wav", ".mid", ".midi" };
                bool found = false;
                foreach (var e in exts)
                {
                    string p = fullPath + e;
                    if (File.Exists(p))
                    {
                        fullPath = p;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogWarning("Audio file not found: " + fullPath);
                    yield break;
                }
            }

            string fileExt = Path.GetExtension(fullPath).ToLower();
            if (fileExt == ".mid" || fileExt == ".midi")
            {
                if (midiPlayer != null)
                {
                    midiPlayer.Play(fullPath, !playOneShot);
                }
                yield break;
            }

            // Detect AudioType from extension
            AudioType audioType = AudioType.UNKNOWN;
            if (fileExt == ".mp3") audioType = AudioType.MPEG;
            else if (fileExt == ".ogg") audioType = AudioType.OGGVORBIS;
            else if (fileExt == ".wav") audioType = AudioType.WAV;

            // UnityWebRequest needs proper escaping
            string escapedPath = System.Uri.EscapeUriString(fullPath.Replace("\\", "/"))
                                   .Replace("+", "%2B")
                                   .Replace("#", "%23");
            string uri = "file:///" + escapedPath;

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error loading audio: " + www.error + " -> " + uri);
                }
                else
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        if (playOneShot)
                        {
                            //本家は同じ音を回数分続けて鳴らす
                            for (int i = 0; i < repeat; ++i)
                            {
                                source.PlayOneShot(clip);
                                if (i + 1 < repeat)
                                    yield return new WaitForSeconds(clip.length);
                            }
                        }
                        else
                        {
                            source.clip = clip;
                            source.Play();
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to decode audio clip from: " + uri);
                    }
                }
            }
        }
    }
}
