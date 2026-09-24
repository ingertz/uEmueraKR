uEmuera KR
==========

<img src="Assets/splash/icon.png" width="256"/>

Emuera(Emulator of Eramaker)의 Unity 이식판인 [uEmuera](https://github.com/xerysherry/uEmuera)를 기반으로,
**한국어 환경과 최신 Android**에 맞춰 손본 포크입니다.

era 계열 텍스트 게임을 Android 기기와 PC(Standalone)에서 실행할 수 있습니다.

원본과 달라진 점
----------------

### 호환성
- **Emuera EE 확장 일부 지원**
  - `HTML_PRINT`의 `<div>` 태그(위치, 크기, margin/padding/border, 배경색)
  - EE 추가 명령과 함수 다수
- **문자 코드 자동 판별**
  - BOM → UTF-8 → Shift-JIS(CP932) 순서로 판별합니다
  - 이제 파일을 UTF-8로 변환하지 않아도 되고, Shift-JIS 원본도 그대로 읽힙니다
- **사운드**
  - `PLAYBGM`, `PLAYSOUND` 등을 지원합니다
  - 지원 형식: `.ogg`, `.mp3`, `.wav`, `.mid`/`.midi` (MIDI는 Android 전용)

### 표시
- TextMesh Pro 기반으로 글자 렌더링을 새로 만들었습니다
- **글꼴의 실제 글자 폭으로 레이아웃을 계산**해서, 표나 테두리가 PC판과 최대한 같게 정렬됩니다
- 게임 폴더의 `font/`에 있는 글꼴을 우선 사용합니다
  - 돋움, 굴림, MS 고딕, ぉんFont 같은 게임 전용 글꼴을 따로 넣어 쓸 수 있습니다
- 탭 문자는 PC판처럼 폭 0으로 처리합니다

### 성능
- ERB 파일을 미리 읽어두면서 동시에 해석해서, 파일 수가 많은 게임의 로딩 시간을 줄였습니다

### 앱
- **한국어 UI**를 추가했습니다 (중국어, 일본어, 영어도 지원)
- **게임 폴더 경로 변경**: 옵션 메뉴에서 폴더를 직접 고를 수 있습니다
- Android 11 이상의 저장소 권한(모든 파일 접근)에 대응합니다
- Unity 6 (`6000.5.0f1`), Android 8.0(API 26) 이상, Target API 36으로 빌드합니다

설치와 사용법
-------------

### Android
1. APK를 설치하고, 처음 실행할 때 **파일 접근 권한(모든 파일 접근)**을 허용합니다.
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
| 메뉴 | 로그 저장, 타이틀로, 다시 읽기, 여백(들여쓰기) 설정 |

빌드하기
--------

1. Unity **6000.5.0f1**로 프로젝트 폴더를 엽니다.
2. `File → Build Profiles`에서 Android 또는 Windows를 고르고 빌드합니다.
3. Android 서명 키(`*.keystore`)는 저장소에 들어 있지 않습니다. 직접 만들거나 가지고 있는 키를 지정하세요.

### 최신 코드 받기 (Windows)
프로젝트 폴더가 이 저장소의 clone이라면, `update.bat`을 더블클릭하면 `master`의 최신 코드를 받아옵니다.
Unity를 켜 둔 채로 실행해도 되고, 받아온 뒤 자동으로 다시 컴파일됩니다.

> `Library/`, `Temp/`, `obj/`, `build/`, `*.csproj`, `*.sln`처럼 Unity나 IDE가 자동으로 만드는 파일은
> `.gitignore`로 제외되어 있습니다. 커밋할 대상은 `Assets/`, `Packages/`, `ProjectSettings/` 세 가지입니다.

알려진 문제
-----------

- 일부 게임에서 **초상화 이미지와 텍스트가 겹쳐 보입니다**
  - 이미지 아래쪽 줄에서 이미지 폭만큼 자리가 비워지지 않는 문제입니다
- 앱 안에서 게임 설정(`emuera.config`)을 바꿀 수 없습니다
- 디버그 모드는 지원하지 않습니다
- 일부 `G*` 그래픽 명령은 구현되지 않았거나 PC판과 결과가 다를 수 있습니다
- 문자 코드 자동 판별에 내장된 레거시 인코딩은 Shift-JIS(CP932)뿐입니다
  - CP949(EUC-KR) 파일은 UTF-8로 변환해서 넣어 주세요

크레딧 / 라이선스
-----------------

- 원작: Emuera (MinorShift)
- Unity 이식: [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera)
- Emuera EE 확장 기능은 Emuera EE 프로젝트를 참고했습니다

이 저장소는 [Apache License 2.0](LICENSE)을 따릅니다.
