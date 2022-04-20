using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Http;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Speech2Voicevox
{

    public class VoicevoxSpeaker
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    internal class VoicevoxApi
    {

        //---------------------------------------------------
        // Json シリアライズ用のクラス定義
        //---------------------------------------------------
        [DataContract]
        private class Style
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
        }

        [DataContract]
        private class Speaker
        {
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string speaker_uuid { get; set; }
            [DataMember]
            public List<Style> styles { get; set; }
        }

        //-------------------------------------
        // エンドポイント
        //-------------------------------------
        private static string baseUrl = "http://localhost:50021/";


        //-------------------------------------
        // 使える音声をリストアップ化して返却する非同期関数
        //-------------------------------------
        public static async Task<List<VoicevoxSpeaker>> getSpeakers()
        {
            var url = baseUrl + "speakers";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<Speaker>));
                List<Speaker> speakers = serializer.ReadObject(await response.Content.ReadAsStreamAsync()) as List<Speaker>;
                List<VoicevoxSpeaker> result = new List<VoicevoxSpeaker>();
                foreach(Speaker speaker in speakers)
                {
                    foreach(Style style in speaker.styles)
                    {
                        result.Add(new VoicevoxSpeaker()
                        {
                            id = int.Parse(style.id),
                            name = speaker.name + "(" + style.name +")",
                        });
                    }
                }
                result.Sort((a, b) => a.name.CompareTo(b.name));
                return result;
            }
        }

        // ------------------------------------------
        // wavファイルを生成し選択したaudioから流す非同期関数
        // ------------------------------------------
        private static async void makeNplayWavAudio(int speaker, string body, DeviceInformation device)
        {
            var url = baseUrl + "synthesis";
            url += "?speaker=" + Uri.EscapeDataString(speaker.ToString());
            url += "&enable_interrogative_upspeak=true";

            var content = new StringContent(body, Encoding.UTF8, @"application/json");
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(url, content);
                var mediaPlayer = new MediaPlayer();
                mediaPlayer.Source = MediaSource.CreateFromStream((await response.Content.ReadAsStreamAsync()).AsRandomAccessStream(), "audio/wav");
                if (device != null)
                {
                    mediaPlayer.AudioDevice = device;
                }
                mediaPlayer.Play();
            }
        }

        //-----------------------------------------------------
        // 選択したデバイスから、選択したキャラクターのボイスで、textを読み上げてくれる非同期関数
        //-----------------------------------------------------
        public static async void playAudio(string text, int speaker, DeviceInformation device)
        {
            var url = baseUrl + "audio_query";
            url += "?text=" + Uri.EscapeDataString(text);
            url += "&speaker=" + Uri.EscapeDataString(speaker.ToString());

            var content = new FormUrlEncodedContent(new Dictionary<String, String>(){});
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(url, content);
                var result = await response.Content.ReadAsStringAsync();
                makeNplayWavAudio(speaker, result, device);
            }
        }
    }
}
