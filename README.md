# 概要
既定のマイク入力を受けて音声認識を行い、Voicevoxというソフトを使って音声合成を行った後、選択した再生デバイスに流し込むことができるアプリです。
マイクに喋った言葉を、Voicevoxのキャラクターが読み上げてくれます。
Yamaha Syncroomなどを再生デバイスとして選択すると、通話相手とVoicevoxの声で会話することができます。


# 動作確認環境
- windows 10 21H2
- Voicevox ver0.11.4
- VisualStudio 2022

# 使い方
1. Voicevox 0.11.4を起動する（起動した状態にする）
2. このアプリをVisual Studio 2022に読み込んで実行する
    - 開発者モードでない場合実行不可
3. アプリが立ち上がるので、お好みのキャラクターと再生デバイスを選択する
4. 音声認識中の表示中に、マイクに話しかけると音声認識が行われ、Voicevoxで合成された音が流れるようになる

# 使ったもの
- Universal Windows Platform
- WebView2
- WebSpeechAPI
- Voicevox