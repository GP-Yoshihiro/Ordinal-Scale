# Claudeへの次作業依頼：iPhone ARの権限表示と配置判定

## 目的

2026-10-05のiPhone 15 Pro実機検証で、iOSのカメラ権限を許可し、実際のカメラ映像と`Session: SessionTracking`・`Tracking: Tracking`を確認した。しかしアプリは`Camera: Denied`と表示する。`PlacementGate`はこの値で敵の配置を止めるため、S2の実機受入へ進めない。承認済みの[iPhone AR設計](../requirements/iPhone_AR_先行検証_設計案.md)のS1は合格、S2は未実施。

## 再現と証拠

1. 検証ブランチ`codex/iphone-ar-prep`の[`f3b63c1`](https://github.com/GP-Yoshihiro/Ordinal-Scale/commit/f3b63c1)と、Claude作業ブランチ宛ての[ドラフトPR #2](https://github.com/GP-Yoshihiro/Ordinal-Scale/pull/2)を参照する。iOS用`UNITY_XR_ARKIT_LOADER_ENABLED`がない旧ビルドは`UnityARKit`のネイティブプラグインをXcodeプロジェクトへ含めず、`Session: None`となった。設定追加後はプラグインが組み込まれ、iOSのカメラ許可ダイアログとARKitセッションが動作した。
2. ユーザーはカメラ許可ダイアログで「許可」を選び、実映像・`SessionTracking`・`Tracking`を観察した。それでも`Camera: Denied`が残った。設定アプリのカメラ項目が許可後に現れたかは確認待ち。
3. `OrdinalScale/Assets/_Project/Scripts/Platform/ARFoundation/ARFoundationSpatialProvider.cs`の`Start()`は`Application.RequestUserAuthorization(WebCam)`後の`Application.HasUserAuthorization(WebCam)`を一度だけ`_cameraPermission`に保存し、`Status`はその値を読み続ける。`ARCameraManager.permissionGranted`は参照していない。インストール済みAR Foundation 6.5.1の同APIはカメラサブシステムの権限状態を返し、ARKit側の実装はネイティブ権限判定を使う。誤表示の直接原因は保存値とARKit稼働状態の不一致だが、`HasUserAuthorization`がなぜ偽になったかは未確定。
4. ローカルの端末ログ：旧ビルド`~/dev/Ordinal-Scale/OrdinalScale/Logs/IOSCameraPermissionConsole.log`、更新版`IOSCameraPermissionAfterARKit.log`。個人識別子・カメラ映像は共有しない。S1の実績は[実機記録票](../verification/iPhone_AR_S1_実機記録票.md)。

## 完了条件

- アプリ内のカメラ権限表示と配置可否が、iOSで許可した現在の状態と一致する。許可後に実映像とAR追跡が動作しているのに`Denied`と表示・配置抑止しない。
- 初回の許可待ち、明示的な拒否、許可後の再起動でも理由表示と配置判定が矛盾しない。拒否時は検出していない面への配置を許さない。
- Unity Editorの関連テストとiOS実機で確認し、結果・再現手順・使用コミットを報告する。S2の床面選択と敵1体の配置はこの修正後に別途受入判定する。
- `IOSARSetup.Validate()`がiOS用ARKit定義の欠落を検出するようにする。ビルド後は生成Xcodeプロジェクトに`UnityARKit.m`と`libUnityARKit.a`が含まれることも確認する。今回の旧ビルドでは設定検証NG 0件でも両ファイルが欠落していた。

実装方式とテスト構成はClaudeが判断する。Unity／Xcodeの生成物、署名情報、端末ログ、個人情報はGitHubへコミットしない。
