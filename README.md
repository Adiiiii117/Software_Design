# Software Design documentation

1. Installation instructions: Step-by-step details on setting up the project
   environment. I will follow your instructions step by step and grade
   based on the results.
   まず unity hub をダウンロードしてください。
   unity hub を使用して、unity をダウンロードしてください。
   ここで大切なのが、2022.3.55f のバージョンをダウンロードすることです。
   開発中に使用したのがこのバージョンのため、開発者の意図する形での表示を正しく行うには、このバージョンのダウンロードが必須です。

【git hub 上からクローンする場合】
続いて
https://github.com/Adiiiii117/Software_Desig
このリポジトリをあなたの git hub 上で fork してください
次に、その fork したリポジトリをローカルのターミナルでクローンしてください。
この時、VScode もダウンロードしておくとソースの確認、および Script ディレクトリにある C＃で書かれたゲームロジックの修正に便利です。
このあと、Tetris-Prototype branch にチェックアウトしてください。今回の正式なバージョンはこちらで管理されています。
Tetris-Prototype にチェックアウトされている状態を保ったまま、unity hub にてクローンしてきたディレクトリを add ボタンから追加し、バージョンは 2022.3.55f を選択してください。
最後に、画面サイズは 1920\*1080 を選択するようにしてください。
これで、unity 上でこのアプリが動くようになります。

->>追加で unity 上の作業があれば追加する

2. Running instructions: How to execute and interact with the software.
   Unity 上での再生法
   script に書くと何ができる
   画面遷移の設定方法

3. File structure overview: Brief explanation of important files and
   folders.

   このプロジェクトにおいて特に大切なのは、git hub 上で管理されている Assets, Packeges, ProjectSettings です。git hub 管理外のファイルについては、unity hub に追加した際に自動で生成されます。

4. Feature documentation: Explanation of implemented features and how to use them.
   このアプリケーションで実際に遊ぶには＜ここは必ずついか＞からダウンロードしたものから＜ここは必ず追加＞を実行して下さい。一度ダウンロードすれば、オフライン上で機能するようになっています。このゲームは、T-spin-double, T-spin-triple, REN といったテトリスにおけるレベルの高い技術を初心者が練習するためのアプリです。
   ＜ここは必ず追加＞を押すことでテトリミノの落下を停止させ、ゆっくりブロックの置き方を考えることができます。
5. Attribution: A clear list of any code or assets sourced externally,
   including links or citations. One submission per group is enough:
   make sure you include all team members name in your submission.
   Ensure all files are properly organized and included. Missing files will result in
   an inability to grade your project.

Repo for Tetris Code

Caution Was developed on Unity Ver 2022.3.55f1. Which is no longer supported / should be updated during game release

Caution Screen Size should be fixed to 1920\*1080 resolution only

