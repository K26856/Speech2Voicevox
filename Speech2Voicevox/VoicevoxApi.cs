using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.Collections.Generic;
using System;
using Windows.Data.Json;
using System.Diagnostics;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Speech2Voicevox
{

    public class VoicevoxSpeaker
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    internal class VoicevoxApi
    {
        //-------------------------------------
        // エンドポイント
        //-------------------------------------
        private string baseUrl = "http://localhost:50021/";

        public int speaker { get; set; } = 0;
        public DeviceInformation device { get; set; } = null;


        //-------------------------------------
        // 使える音声をリストアップ化して返却する非同期関数
        //-------------------------------------
        public async Task<List<VoicevoxSpeaker>> getSpeakers()
        {
            var client = new HttpClient();
            var requestUri = new Uri(baseUrl + "speakers");

            List<VoicevoxSpeaker> result = new List<VoicevoxSpeaker>();
            try
            {
                var response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
                string res_message = await response.Content.ReadAsStringAsync();
                JsonObject jo = JsonObject.Parse("{\"a\":" + res_message + "}");
                foreach(var speaker in jo.GetNamedArray("a").GetArray())
                {
                    var name = speaker.GetObject().GetNamedString("name");
                    foreach(var style in speaker.GetObject().GetNamedArray("styles"))
                    {
                        var id = style.GetObject().GetNamedNumber("id");
                        var style_name = style.GetObject().GetNamedString("name");
                        result.Add(new VoicevoxSpeaker()
                        {
                            id = (int)id,
                            name = name + "(" + style_name + ")"
                        });
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "error: " + ex.Message + ", " + ex.StackTrace.ToString(),
                    CloseButtonText = "Ok"
                };
                _ = await dialog.ShowAsync();
                return result;
            }
        }

        //-----------------------------------------------------
        // 選択したデバイスから、選択したキャラクターのボイスで、textを読み上げてくれる非同期関数
        // 読み上げに必要な情報を取得し，その情報を使ってwavを作成する関数に渡す
        //-----------------------------------------------------
        public async Task<bool> playAudio(string text)
        {

            var uri = baseUrl + "audio_query";
            uri += "?text=" + Uri.EscapeDataString(text);
            uri += "&speaker=" + Uri.EscapeDataString(speaker.ToString());

            var requestUri = new Uri(uri);
            var client = new HttpClient();
            var content = new HttpStringContent("", Windows.Storage.Streams.UnicodeEncoding.Utf8, "");
            try {
                var response = await client.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return await makeNplayWavAudio(result);
            }catch(Exception ex)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "error: " + ex.Message + ", " + ex.StackTrace.ToString(),
                    CloseButtonText = "Ok"
                };
                _ = await dialog.ShowAsync();
                return false;
            }
        }

        // ------------------------------------------
        // wavファイルを生成し選択したaudioから流す非同期関数
        // 受け取った情報をもとにwavを作成し，作成完了時にwav再生を実行する
        // ------------------------------------------
        private async Task<bool> makeNplayWavAudio(string body)
        {
            var uri = baseUrl + "synthesis";
            uri += "?speaker=" + Uri.EscapeDataString(speaker.ToString());
            uri += "&enable_interrogative_upspeak=true";

            var requestUri = new Uri(uri);
            var content = new HttpStringContent(body, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            var tmpFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("temp.wav", CreationCollisionOption.GenerateUniqueName);
            try
            {
                var downloader = new BackgroundDownloader();
                downloader.Method = "POST";
                var dlOperation = await downloader.CreateDownloadAsync(requestUri, tmpFile, await content.ReadAsInputStreamAsync());
                dlOperation.StartAsync().Completed = makeWavCompleted;
                return true;
            }
            catch (Exception ex)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "error: " + ex.Message + ", " + ex.StackTrace.ToString(),
                    CloseButtonText = "Ok"
                };
                _ = await dialog.ShowAsync();
                _ = tmpFile.DeleteAsync();
                return false;
            }
        }

        private void makeWavCompleted(IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> asyncInfo, AsyncStatus asyncStatus)
        {
            Debug.WriteLine("play wav");
            var tmpFile = asyncInfo.GetResults().ResultFile;
            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(tmpFile);
            if (device != null)
            {
                mediaPlayer.AudioDevice = device;
            }
            mediaPlayer.MediaEnded += (MediaPlayer sender, object args) =>
            {
                _ = tmpFile.DeleteAsync();
            };
            mediaPlayer.Play();
        }
    }
}
