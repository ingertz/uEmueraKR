# 개발

[← 목차](README.md)

<details>
<summary><b>빌드</b></summary>

1. Unity **6000.5.0f1**로 프로젝트 폴더를 엽니다.
2. `File → Build Profiles`에서 Android 또는 Windows를 고르고 빌드합니다.
3. Android 서명 키(`*.keystore`)는 저장소에 들어 있지 않습니다. 직접 만들거나 가지고 있는 키를 지정하세요.

커밋할 대상은 `Assets/`, `Packages/`, `ProjectSettings/`, `Tools/`, 문서입니다.
`Library/`, `Temp/`, `*.csproj`, `*.sln`처럼 Unity나 IDE가 만드는 파일은 `.gitignore`로 제외되어 있습니다.

</details>

<details>
<summary><b>릴리스용 APK를 명령줄로 만들기</b></summary>

Unity 에디터를 닫은 상태에서:

```
"C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe" -batchmode -projectPath . -buildTarget Android -executeMethod ReleaseBuild.Android -logFile build.log
```

- `Build/Release/`에 `uEmuera.apk`, `uEmuera.arm64-v8a.apk`, `uEmuera.armeabi-v7a.apk`가 만들어집니다.
- 서명 비밀번호는 환경 변수 `UEMUERA_KEYSTORE_PASS`(키 비밀번호가 다르면 `UEMUERA_KEYALIAS_PASS`)에서 읽습니다.
- 버전은 `ProjectSettings/ProjectSettings.asset`의 `bundleVersion`과 `AndroidBundleVersionCode`입니다.
  앱의 업데이트 확인이 버전을 비교하므로, 수정본을 낼 때마다 올립니다.
- 스크립트: [Assets/Editor/ReleaseBuild.cs](../Assets/Editor/ReleaseBuild.cs)

</details>

<details>
<summary><b>엔진 테스트</b></summary>

Unity를 열지 않고 엔진 코드를 컴파일해서 확인합니다. 릴리스 빌드 전에 돌립니다.

```
powershell -ExecutionPolicy Bypass -File Tools/EngineTests/run.ps1
```

- 확인하는 것: 바이너리·텍스트 세이브 읽기와 쓰기(압축, MAP·XML·DataTable, 예전 형식, Shift-JIS), 읽은 뒤 파일이 닫히는지,
  `HASH_XXH32`/`HASH_XXH3` 값, `UseNewRandom` 난수, 업데이트 확인의 버전 비교
- Unity가 만드는 `Assembly-CSharp.csproj`와 .NET SDK 10 이상이 필요합니다.
- 화면 그리기처럼 Unity가 있어야 하는 부분은 여기서 확인할 수 없습니다.
- 코드: [Tools/EngineTests](../Tools/EngineTests)

</details>

<details>
<summary><b>최신 코드 받기 (Windows)</b></summary>

프로젝트 폴더가 이 저장소의 clone이라면, `update.bat`을 더블클릭하면 `master`의 최신 코드를 받아옵니다.
Unity를 켜 둔 채로 실행해도 되고, 받아온 뒤 자동으로 다시 컴파일됩니다.

</details>
