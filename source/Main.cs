// - - VWantedMusic - -
// Created by ItsClonkAndre
// Modified Version 1.3 - Updated to use Mission Complete Audio

using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace VWantedMusic
{
    public class Main : Script
    {

        #region Variables and Enums
        private bool tempBool;
        private bool isHandleCurrentlyFadingOut;
        private bool loop;
        private bool fadeOut;
        private bool fadeIn;
        private bool wasWantedBefore; // Để theo dõi trạng thái wanted trước đó

        private int initalVolume;
        private int musicHandle;
        private int rndSeed;
        private int fadingSpeed;
        private int startAt;

        private Random rnd;

        private string[] musicFiles;
        private readonly string DataDir = Game.InstallFolder + @"\scripts\VWantedMusic";

        // Danh sách các ID âm thanh hoàn thành nhiệm vụ hợp lệ
        private readonly int[] validMissionCompleteAudioIds = { 6, 7, 10, 11, 15, 18, 24, 25, 27, 28, 33, 34, 35, 42, 43, 50,51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 71 };

        private enum AudioPlayMode
        {
            Play,
            Pause,
            Stop,
            None
        }
        #endregion

        #region Methods
        private int CreateFile(string file, bool createWithZeroDecibels, bool dontDestroyOnStreamEnd = false, bool loopStream = false)
        {
            if (!string.IsNullOrWhiteSpace(file))
            {
                if (createWithZeroDecibels)
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        int handle;
                        if (loopStream)
                        {
                            handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_MUSIC_LOOP);
                        }
                        else
                        {
                            handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                        }
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                    else
                    {
                        int handle = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                        SetStreamVolume(handle, 0f);
                        return handle;
                    }
                }
                else
                {
                    if (dontDestroyOnStreamEnd)
                    {
                        return Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_PRESCAN);
                    }
                    else
                    {
                        return Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_STREAM_AUTOFREE);
                    }
                }
            }
            else
            {
                return 0;
            }
        }

        public bool SetStreamVolume(int stream, float volume)
        {
            if (stream != 0)
            {
                return Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, volume / 100.0F);
            }
            else
            {
                return false;
            }
        }

        private AudioPlayMode GetStreamPlayMode(int stream)
        {
            if (stream != 0)
            {
                switch (Bass.BASS_ChannelIsActive(stream))
                {
                    case BASSActive.BASS_ACTIVE_PLAYING:
                        return AudioPlayMode.Play;
                    case BASSActive.BASS_ACTIVE_PAUSED:
                        return AudioPlayMode.Pause;
                    case BASSActive.BASS_ACTIVE_STOPPED:
                        return AudioPlayMode.Stop;
                    default:
                        return AudioPlayMode.None;
                }
            }
            else
            {
                return AudioPlayMode.None;
            }
        }

        private async void FadeStreamOut(int stream, AudioPlayMode after, int fadingSpeed = 1000)
        {
            if (!isHandleCurrentlyFadingOut)
            {
                isHandleCurrentlyFadingOut = true;

                float handleVolume = 0f;
                Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, 0f, fadingSpeed);

                while (Bass.BASS_ChannelIsActive(stream) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref handleVolume);

                    if (handleVolume <= 0f)
                    {
                        switch (after)
                        {
                            case AudioPlayMode.Stop:
                                Bass.BASS_ChannelStop(stream);
                                isHandleCurrentlyFadingOut = false;
                                musicHandle = 0;
                                break;
                            case AudioPlayMode.Pause:
                                Bass.BASS_ChannelPause(stream);
                                isHandleCurrentlyFadingOut = false;
                                musicHandle = 0;
                                break;
                        }
                        break;
                    }

                    await Task.Delay(5);
                }
            }
        }

        private void FadeStreamIn(int stream, float fadeToVolumeLevel, int fadingSpeed)
        {
            Bass.BASS_ChannelPlay(stream, false);
            Bass.BASS_ChannelSlideAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, fadeToVolumeLevel / 100.0f, fadingSpeed);
        }

        private void PlayRandomSoundtrack()
        {
            try
            {
                // Lọc ra những file nhạc (loại trừ end.wav nếu có)
                var availableMusicFiles = musicFiles.Where(file => !file.EndsWith("end.wav", StringComparison.OrdinalIgnoreCase)).ToArray();

                if (availableMusicFiles.Length == 0)
                {
                    //Game.Console.Print("VWantedMusic: No music files found.");
                    return;
                }

                string selectedFile = availableMusicFiles[rnd.Next(0, availableMusicFiles.Length)];

                // Tạo stream không loop và với BASS_STREAM_PRESCAN để detect khi kết thúc
                musicHandle = CreateFile(selectedFile, false, true, false);

                if (musicHandle != 0)
                {
                    SetStreamVolume(musicHandle, initalVolume);
                    Bass.BASS_ChannelPlay(musicHandle, false);
                    //Game.Console.Print("VWantedMusic: Playing " + Path.GetFileName(selectedFile));
                }
                else
                {
                    //Game.Console.Print("VWantedMusic could not play file. musicHandle was zero.");
                }
            }
            catch (Exception ex)
            {
               // Game.Console.Print("VWantedMusic error in PlayRandomSoundtrack method. Details: " + ex.ToString());
            }
        }

        private void PlayMissionCompleteAudio()
        {
            try
            {
                // Stop nhạc hiện tại ngay lập tức
                if (musicHandle != 0)
                {
                    Bass.BASS_ChannelStop(musicHandle);
                    musicHandle = 0;
                }

                // Chọn ngẫu nhiên một ID âm thanh hoàn thành nhiệm vụ
                int randomAudioId = validMissionCompleteAudioIds[rnd.Next(validMissionCompleteAudioIds.Length)];

                // Phát âm thanh hoàn thành nhiệm vụ bằng GTA Native function
                Function.Call("TRIGGER_MISSION_COMPLETE_AUDIO", randomAudioId);

                //Game.Console.Print($"VWantedMusic: Playing mission complete audio ID: {randomAudioId}");
            }
            catch (Exception ex)
            {
                //Game.Console.Print("VWantedMusic error in PlayMissionCompleteAudio method. Details: " + ex.ToString());
            }
        }

        private void StopSoundtrack(bool instant = false)
        {
            if (musicHandle != 0)
            {
                if (GetStreamPlayMode(musicHandle) == AudioPlayMode.Play)
                {
                    if (instant)
                    {
                        Bass.BASS_ChannelStop(musicHandle);
                        musicHandle = 0;
                    }
                    else
                    {
                        if (fadeOut)
                        {
                            FadeStreamOut(musicHandle, AudioPlayMode.Stop, fadingSpeed);
                        }
                        else
                        {
                            Bass.BASS_ChannelStop(musicHandle);
                            musicHandle = 0;
                        }
                    }
                }
                else
                {
                    Bass.BASS_ChannelStop(musicHandle);
                    musicHandle = 0;
                }
            }
        }
        #endregion

        public Main()
        {
            try
            {
                // Setup Bass.dll
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

                // Get and set settings
                rndSeed = Settings.GetValueInteger("RndSeed", "General", DateTime.Now.Millisecond);
                loop = Settings.GetValueBool("Loop", "Music", false);
                fadeOut = Settings.GetValueBool("FadeOut", "Music", true);
                fadeIn = Settings.GetValueBool("FadeIn", "Music", true);
                fadingSpeed = Settings.GetValueInteger("FadingSpeed", "Music", 3000);
                startAt = Settings.GetValueInteger("StartAt", "Music", 3);
                if (startAt < 1 | startAt > 6)
                {
                    startAt = 3;
                }
                initalVolume = Settings.GetValueInteger("Volume", "Music", 20);

                // Set new random
                rnd = new Random(rndSeed);

                // Initialize wanted state tracking
                wasWantedBefore = false;

                this.Interval = 100;
                this.Tick += VWantedMusic_Tick;
                this.ConsoleCommand += VWantedMusic_ConsoleCommand;
            }
            catch (Exception ex)
            {
                //Game.Console.Print("VWantedMusic error: " + ex.ToString() + " - Please let the developer know about this problem.");
            }
        }

        private void VWantedMusic_ConsoleCommand(object sender, ConsoleEventArgs e)
        {
            switch (e.Command.ToLower())
            {
                case "vwmusic:reloadsettings":
                    try
                    {
                        //Game.Console.Print("VWantedMusic: Reloading settings...");
                        loop = Settings.GetValueBool("Loop", "Music", false);
                        fadeOut = Settings.GetValueBool("FadeOut", "Music", true);
                        fadeIn = Settings.GetValueBool("FadeIn", "Music", true);
                        fadingSpeed = Settings.GetValueInteger("FadingSpeed", "Music", 3000);
                        startAt = Settings.GetValueInteger("StartAt", "Music", 3);
                        if (startAt < 1 | startAt > 6)
                        {
                            startAt = 3;
                        }
                        initalVolume = Settings.GetValueInteger("Volume", "Music", 20);
                        //Game.Console.Print("VWantedMusic: Ready.");
                    }
                    catch (Exception ex)
                    {
                        //Game.Console.Print("VWantedMusic error while reloading settings: " + ex.Message);
                    }
                    break;
            }
        }

        private void VWantedMusic_Tick(object sender, EventArgs e)
        {
            if (Directory.Exists(DataDir))
            {
                musicFiles = Directory.EnumerateFiles(DataDir).Where(file => Path.GetExtension(file) == ".mp3" || Path.GetExtension(file) == ".wav").ToArray();
                if (musicFiles.Length != 0)
                {

                    // Kiểm tra nếu người chơi có wanted level từ startAt trở lên
                    if (Game.LocalPlayer.WantedLevel >= startAt)
                    {
                        wasWantedBefore = true; // Đánh dấu là đã có wanted level cao

                        if (!isHandleCurrentlyFadingOut)
                        {
                            if (!tempBool)
                            {
                                PlayRandomSoundtrack();
                                tempBool = true;
                            }
                            // Kiểm tra nếu bài hát đã kết thúc, phát bài mới
                            else if (musicHandle != 0 && GetStreamPlayMode(musicHandle) == AudioPlayMode.Stop)
                            {
                                PlayRandomSoundtrack();
                            }
                        }
                    }
                    // Kiểm tra nếu wanted level về 0 và trước đó đã có wanted level cao
                    else if (Game.LocalPlayer.WantedLevel == 0 && wasWantedBefore)
                    {
                        if (tempBool)
                        {
                            PlayMissionCompleteAudio(); // Phát âm thanh hoàn thành nhiệm vụ thay vì end.wav
                            tempBool = false;
                            wasWantedBefore = false; // Reset trạng thái
                        }
                    }
                    // Nếu wanted level về 0 nhưng chưa từng có wanted level cao
                    else if (Game.LocalPlayer.WantedLevel == 0)
                    {
                        wasWantedBefore = false; // Đảm bảo reset trạng thái

                        if (tempBool)
                        {
                            StopSoundtrack();
                            tempBool = false;
                        }

                        if (!isHandleCurrentlyFadingOut)
                        {
                            if (GetStreamPlayMode(musicHandle) == AudioPlayMode.Play)
                            {
                                if (fadeOut)
                                {
                                    FadeStreamOut(musicHandle, AudioPlayMode.Stop, fadingSpeed);
                                }
                                else
                                {
                                    Bass.BASS_ChannelStop(musicHandle);
                                    musicHandle = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}