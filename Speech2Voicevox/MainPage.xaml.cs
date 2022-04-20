using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.Media.Devices;
using Windows.Devices.Enumeration;
using System.Collections.Generic;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Speech2Voicevox
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private const int MAX_LOG_ROW = 50;
        private const int MAX_CHARA = 20;
        private DeviceInformation selectedDevice = (DeviceInformation)null;
        private int selectedChara = 0;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_loaded;
        }

        private void MainPage_loaded(object sender, RoutedEventArgs args)
        {
            audioSelectorBoxInitialized();
            charaSelectorBoxInitializedAsync();
            addLogToTextBox("[SystemMessage] Main Page Loaded");
            wv2.CoreWebView2Initialized += Wv2_Initialized;
            wv2.EnsureCoreWebView2Async();
        }


        // ---------------------------------------------------------
        // 再生デバイス選択ボックスのコントロールまわり
        // ---------------------------------------------------------
        private async void audioSelectorBoxInitialized()
        {
            string audioSelector = MediaDevice.GetAudioRenderSelector();
            var outputDevices = await DeviceInformation.FindAllAsync(audioSelector);
            foreach (var device in outputDevices)
            {
                var deviceItem = new ComboBoxItem();
                deviceItem.Content = device.Name;
                deviceItem.Tag = device;
                audioSelectorBox.Items.Add(deviceItem);
            }
        }

        private void audioSelectorBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceInformation selectedDevice = (DeviceInformation)((ComboBoxItem) audioSelectorBox.SelectedItem).Tag;
            if (selectedDevice != null)
            {
                this.selectedDevice = selectedDevice;
            }
        }


        // ---------------------------------------------------------
        // キャラクター選択ボックスのコントロールまわり
        // ---------------------------------------------------------
        private async void charaSelectorBoxInitializedAsync()
        {
            charaSelectorBox.DisplayMemberPath = "name";
            List<VoicevoxSpeaker> speakers = await VoicevoxApi.getSpeakers();
            foreach(VoicevoxSpeaker speaker in speakers)
            {
                charaSelectorBox.Items.Add(speaker);
            }
        }

        private void charaSelectorBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            VoicevoxSpeaker selected = (VoicevoxSpeaker)charaSelectorBox.SelectedItem;
            if(selected.id > -1)
            {
                this.selectedChara = selected.id;
            }
        }


        // ----------------------------------------------------------------------------------
        // WebView2 のコントロールまわり
        // ----------------------------------------------------------------------------------
        private void Wv2_Initialized(object sender, CoreWebView2InitializedEventArgs args)
        {
            addLogToTextBox("[SystemMessage] Web View Initialized");
            string currentDir = Environment.CurrentDirectory;
            Uri uri = new Uri($"{currentDir}/index.html");
            wv2.CoreWebView2.Navigate(uri.AbsoluteUri);
            wv2.CoreWebView2.WebMessageReceived += Wv2_WevMessageReceived;
            wv2.CoreWebView2.PermissionRequested += Wv2_PermissionRequested;
        }

        private void Wv2_WevMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            string text = args.TryGetWebMessageAsString();
            addLogToTextBox("[SpeechRecognition] " + text);
            VoicevoxApi.playAudio(text, selectedChara, selectedDevice);
        }

        private void Wv2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs args)
        {
            if( args.PermissionKind == CoreWebView2PermissionKind.Microphone)
            {
                args.State = CoreWebView2PermissionState.Allow;
            }
        }



        //---------------------------------------------------------------------------
        // TextBoxのコントロールまわり
        //---------------------------------------------------------------------------
        private void addLogToTextBox(String log)
        {
            DateTime now = DateTime.Now;
            string beforeText = textBlock.Text;
            string afterText = now.ToString("yyyy/MM/dd HH:mm:ss") + "  :  " + log + "\r\n" + beforeText;

            int row = 0;
            int cutPos = 0;
            for(cutPos=0; cutPos<afterText.Length; cutPos++)
            {
                if (row >= MAX_LOG_ROW) break;
                if (afterText[cutPos] == '\n') row++ ;
            }
            afterText = afterText.Substring(0, cutPos);

            textBlock.Text = afterText;
        }
    }
}
