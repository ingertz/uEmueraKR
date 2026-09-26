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
- **EE 확장 인자 지원**
  - `INPUT`/`INPUTS`/`ONEINPUT`/`ONEINPUTS`/`TINPUT`/`TINPUTS`의 마우스 입력·스킵 인자
    (마우스 입력이 켜지면 탭한 버튼이 `RESULTS:1`로, 마스크 이미지에서 누른 위치의 색이 `RESULT:3`으로 들어감)
  - `INPUTMOUSEKEY`에서 탭한 버튼의 값이 `RESULT:5`(숫자) 또는 `RESULTS`(문자열)로 들어감
  - `BINPUT` 계열은 화면에 있는 버튼 값만 입력으로 받음
  - `PRINT_IMG 이미지, 버튼이미지, 마스크, 폭, 높이, 세로위치`와 `PRINT_RECT`/`PRINT_SPACE`의 `px` 단위
- **ERD (EE와 같은 방식)**
  - `#DIM`으로 만든 변수와 이름이 같은 `.erd`(ERB 폴더 아래 전체) 또는 `.csv`(CSV 폴더)가 있으면,
    그 변수의 요소를 `변수:이름`처럼 이름으로 쓸 수 있습니다 (다차원은 `변수명@1.erd`, `변수명@2.erd` …)
  - `GETNUM`의 세 번째 인자(차원), `ERDNAME`, `GETNUMB` 지원
  - `emuera.config`의 `ERD機能を利用する`로 끌 수 있습니다 (기본값은 켜짐)
- **EE 명령 추가**
  - `INPUTANY`(숫자·문자열 모두 입력), `BREAKBUTTON`, `FORCE_BEGIN`, `SKIPLOG`
  - `QUIT_AND_RESTART`/`FORCE_QUIT_AND_RESTART`는 메뉴의 "다시 읽기"와 같은 방식으로 게임을 다시 시작하고, `FORCE_QUIT`은 입력을 기다리지 않고 끝냅니다
  - `UPDATECHECK`와 `GAMEBASE_URL`/`GAMEBASE_VERSIONNAME` (`GAMEBASE.CSV`의 `バージョン情報URL`, `バージョン名`)
  - EE처럼 `BEGIN`을 어느 상태에서든 쓸 수 있습니다
- **Emuera1824+v10+v3의 `CHECK_OVERFLOW` 지원**
  - `CHECK_OVERFLOW 1`이면 정수 계산이 넘칠 때 INT64 최댓값/최솟값에서 멈춥니다
- **그래픽 명령**
  - `GGETTEXTSIZE`: 글자를 그렸을 때의 폭을 돌려주고, 높이를 `RESULT:1`에 넣습니다
  - `GDRAWTEXT`는 PC판처럼 그린 글자의 폭/높이를 `RESULT:1`/`RESULT:2`에 넣습니다
  - `GSETCOLOR`가 실제로 점을 찍고, `GGETCOLOR`/`SPRITEGETCOLOR`가 올바른 위치의 색을 읽습니다
  - `GDRAWLINE`(펜 색·굵기, `GDASHSTYLE`의 점선 반영), `GGETPENWIDTH`, `SPRITEDISPOSEALL`
- **화면 배경 그래픽(CBG)**
  - `CBGSETG`, `CBGSETSPRITE`로 설정한 그림을 화면에 고정해서 표시합니다 (zdepth 양수는 글자 뒤, 음수는 글자 앞)
  - `CBGSETBMAPG` 버튼 맵을 터치하면 `INPUTMOUSEKEY`의 `RESULT:4`로 번호가 전달됩니다 (PC판과 같음)
- **배경 이미지 (`SETBGIMAGE`/`REMOVEBGIMAGE`/`CLEARBGIMAGE`)**
  - EE처럼 화면 폭(모자라면 높이)에 맞춰 늘리고, 위쪽에 붙여 가운데에 두며, 글자와 CBG 뒤에 표시합니다
- **`HTML_PRINT_ISLAND` / `HTML_PRINT_ISLAND_CLEAR`**
  - 로그에 남지 않는 HTML을 화면 위쪽에 고정해서 겹쳐 표시합니다 (Emuera.NET과 같은 동작)
- **문자 코드 자동 판별**
  - BOM → UTF-8 → Shift-JIS(CP932) 순서로 판별합니다
  - 이제 파일을 UTF-8로 변환하지 않아도 되고, Shift-JIS 원본도 그대로 읽힙니다
- **사운드**
  - `PLAYBGM`, `PLAYSOUND` 등을 지원합니다
  - 지원 형식: `.ogg`, `.mp3`, `.wav`, `.mid`/`.midi` (MIDI는 Android 전용)
- **이미지**
  - 지원 형식: `.png`, `.jpg`, `.webp`, `.bmp`, `.gif` (GIF는 첫 프레임만)

### 표시
- TextMesh Pro 기반으로 글자 렌더링을 새로 만들었습니다
- **글꼴의 실제 글자 폭으로 레이아웃을 계산**해서, 표나 테두리가 PC판과 최대한 같게 정렬됩니다
- 게임 폴더의 `font/`에 있는 글꼴을 우선 사용합니다
  - 돋움, 굴림, MS 고딕, ぉんFont 같은 게임 전용 글꼴을 따로 넣어 쓸 수 있습니다
- 탭 문자는 PC판처럼 폭 0으로 처리합니다
- `PRINT_SPACE`와 `<shape type='space'>`의 공백 폭을 글자 배치에 반영합니다
  - 초상화 옆으로 글자를 밀어 두는 화면에서 글자와 그림이 겹치지 않습니다

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

- 앱 안에서 게임 설정(`emuera.config`)을 바꿀 수 없습니다
- 디버그 모드는 지원하지 않습니다
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
