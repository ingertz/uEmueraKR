uEmuera KR
==========

<img src="Assets/splash/icon.png" width="256"/>

Emuera(Emulator of Eramaker)의 Unity 이식판인 [uEmuera](https://github.com/xerysherry/uEmuera)를 기반으로,
**한국어 환경과 최신 Android**에 맞춰 손본 포크입니다.

era 계열 텍스트 게임을 Android 기기와 PC(Standalone)에서 실행할 수 있습니다.

원본과 달라진 점
----------------

분류별 자세한 내용은 **[docs/README.md](docs/README.md)** 에, 버전별 변경은 **[CHANGELOG.md](CHANGELOG.md)** 에 있습니다.

| 분류 | 요약 |
|---|---|
| [EE 호환](docs/ee.md) | Emuera EE의 명령·함수·확장 인자, ERD, `<div>`, CBG, `HTML_PRINT_ISLAND`, VarExt 저장 |
| [다른 파생판](docs/other-engines.md) | 메가텐 P판 Emuera 전용 기능(`VARI`/`VARS`, `HASH_XXH*`, `DICT_*`, `G_POLYGON_*`), `MATCHALL`, `CHECK_OVERFLOW` |
| [화면 표시](docs/display.md) | TextMesh Pro 렌더링, 실제 글자 폭으로 정렬, 게임 폴더의 글꼴 사용 |
| [세이브](docs/saves.md) | Shift-JIS 세이브 읽기, 세이브 압축, 저장 실패 원인 기록 |
| [앱 기능](docs/app.md) | 한국어 UI, 빠른 버튼, 되돌리기, 들여쓰기, 업데이트 확인, 화면 글자 복사, 디버그 모드 |
| [성능](docs/performance.md) | 로딩 시간 단축, 화면이 멈춰 있을 때 발열 줄이기 |

<details>
<summary>지원하는 파일 형식</summary>

- 스크립트·CSV: BOM → UTF-8 → Shift-JIS(CP932) 순서로 자동 판별합니다. UTF-8로 바꾸지 않아도 됩니다.
- 이미지: `.png`, `.jpg`, `.webp`, `.bmp`, `.gif`(첫 프레임만)
- 사운드: `.ogg`, `.mp3`, `.wav`, `.mid`/`.midi`(MIDI는 Android 전용)

</details>

설치와 사용법
-------------

### 앱 받기

**👉 [최신 버전 받으러 가기 (Releases 페이지)](https://github.com/ingertz/uEmueraKR/releases/latest)**

1. 위 링크를 누르면 최신 버전 페이지가 열립니다.
   - 링크 없이 찾아가려면: 이 저장소 첫 화면 오른쪽의 **Releases** 항목을 누릅니다.
     (휴대폰에서는 화면을 아래로 내리면 **Releases**가 보입니다)
2. 페이지 아래쪽의 **Assets**에서 받을 파일을 누르면 다운로드가 시작됩니다.
   - Assets가 접혀 있으면 **Assets**라는 글자를 눌러 펼치세요.
   - `Source code (zip)`, `Source code (tar.gz)`는 앱이 아니라 소스 코드이니 받지 않아도 됩니다.

| 파일 | 대상 | 바로 받기 |
|---|---|---|
| `uEmuera.apk` | 모든 폰 (**잘 모르겠으면 이것**) | [다운로드](https://github.com/ingertz/uEmueraKR/releases/latest/download/uEmuera.apk) |
| `uEmuera.arm64-v8a.apk` | 64비트 폰 (2019년 이후 대부분의 폰, 용량이 더 작음) | [다운로드](https://github.com/ingertz/uEmueraKR/releases/latest/download/uEmuera.arm64-v8a.apk) |
| `uEmuera.armeabi-v7a.apk` | 32비트 구형 폰 | [다운로드](https://github.com/ingertz/uEmueraKR/releases/latest/download/uEmuera.armeabi-v7a.apk) |

> 세 파일은 같은 앱입니다. 폰에 맞지 않는 파일은 "앱이 설치되지 않았습니다"라고 뜨며 설치되지 않으니,
> 그럴 때는 `uEmuera.apk`를 받으세요.

> 받은 APK를 누르면 "출처를 알 수 없는 앱" 경고가 나올 수 있습니다.
> **설정 → 이 출처 허용**을 켠 뒤 다시 설치하면 됩니다. (Play 스토어 밖에서 받은 앱이라 나오는 안내입니다)

### Android
1. 받은 APK를 설치하고, 처음 실행할 때 **파일 접근 권한(모든 파일 접근)**을 허용합니다.
2. 게임 폴더를 아래 위치에 넣습니다.
   ```
   /storage/emulated/0/emuera/<게임 폴더>/
   ```
   - `<게임 폴더>` 안에 `ERB/`와 `CSV/`가 있어야 합니다.
   - 다른 위치를 쓰고 싶으면 앱의 **옵션 → 경로 변경**에서 바꾸고, 앱을 재시작하면 됩니다.
3. 앱을 실행하면 게임 목록이 나오고, 목록에서 게임을 고르면 실행됩니다.

### PC (Standalone)
실행 파일이 있는 폴더를 기준으로 게임 폴더를 찾습니다.

게임 안 조작
------------

| 기능 | 설명 |
|---|---|
| 빠른 버튼 | 화면에 나온 선택지를 버튼으로 모아서 보여줍니다 |
| 명령 입력 | 번호나 문자열을 직접 입력합니다 |
| 확대/축소 | 확대/축소 패드로 화면 배율을 조절합니다 |
| 메뉴 | 로그 저장, 화면 글자 복사, 타이틀로, 다시 읽기, 여백(들여쓰기) 설정 |
| 게임 목록 메뉴 | 언어 선택, 경로 변경, 업데이트 확인 |

빌드하기
--------

Unity **6000.5.0f1**로 프로젝트 폴더를 열고 `File → Build Profiles`에서 빌드합니다.
Android 서명 키(`*.keystore`)는 저장소에 들어 있지 않습니다.
명령줄 릴리스 빌드, 엔진 테스트, `update.bat` 사용법은 [docs/development.md](docs/development.md)에 있습니다.

문제 제보
---------

게임을 열면 게임 폴더에 `uEmuera.log`가 만들어지고, 앱의 경고와 오류가 기록됩니다.
문제가 생기면 그 상황을 재현한 뒤 이 파일을 함께 보내 주세요. (게임을 열 때마다 새로 만들어집니다)

알려진 문제
-----------

- 앱 안에서 게임 설정(`emuera.config`)을 바꿀 수 없습니다
- 메가텐 P판 Emuera의 `SQL_*` 함수는 지원하지 않습니다
- PC판의 디버그 창은 없습니다 (게임 폴더에 `debug.on` 파일을 두면 디버그 모드로는 실행됩니다)
- CBG 버튼의 "선택됐을 때 이미지"는 표시하지 않습니다 (터치 화면에는 마우스를 올리는 동작이 없으므로)
- 모바일에서 쓸 일이 없는 PC 전용 입력은 지원하지 않습니다
  - `GETKEY`/`GETKEYTRIGGERED`, `MOUSEX`/`MOUSEY`, 툴팁
- 문자 코드 자동 판별에 내장된 레거시 인코딩은 Shift-JIS(CP932)뿐입니다
  - CP949(EUC-KR) 파일은 UTF-8로 변환해서 넣어 주세요

크레딧 / 라이선스
-----------------

- 원작: Emuera (MinorShift)
- Unity 이식: [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera)
- Emuera EE 확장 기능은 Emuera EE 프로젝트를 참고했습니다

이 저장소는 [Apache License 2.0](LICENSE)을 따릅니다.
