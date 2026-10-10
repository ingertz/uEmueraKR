# 인수인계 (2026-10-10)

클라우드 세션에서 하던 작업을 로컬로 옮기기 위한 정리. 다 읽고 나면 지워도 된다.

## 작업 방식

- 저장소: `ingertz/uEmueraKR`. `master`에 바로 push하고, `claude/peaceful-gauss-apso6u` 브랜치도 같은 커밋으로 맞춰 왔다.
- Unity `6000.5.0f1`. 빌드와 실행 확인은 사용자가 로컬 Unity에서 한다.
- 방침: **원본(Emuera EE, `emuera.em-master`)과 같게 동작**시키는 것이 기준. 원본 소스는 사용자가 zip으로 올려준 것을 클라우드에서만 썼고 저장소에는 없다. 로컬에서 비교하려면 그 zip을 다시 풀어서 쓴다.
- 예외: 실제 게임이 원본에 없는 기능을 쓰면 넣는다 (`MATCHALL`, `CHECK_OVERFLOW`, 캐릭터 CSV의 `_Rename` 치환 등).
- `user.keystore`(안드로이드 서명 키)는 절대 커밋하지 않는다. `.gitignore`에 `*.keystore`, `*.jks`가 들어 있다.

## 검증 방법과 주의점

- 이 환경에는 Unity가 없어서 `mcs --parse`(문법만)와, Unity 타입을 흉내 낸 가짜 정의로 돌리는 타입 검사로만 확인했다. **Unity에서 컴파일해야 확실하다.**
- 이 방식으로 놓쳤던 실수:
  - 다른 네임스페이스의 타입을 `using` 없이 쓴 것 (`MixedNum`은 `MinorShift.Emuera.GameView`에 있음)
  - `uEmuera` 네임스페이스 안에 같은 이름의 클래스가 있어서 Unity 클래스가 가려진 것. `uEmuera.Application`, `uEmuera.Font`, `uEmuera.Resources`, `uEmuera.Timer` 등이 있으니 그 안에서는 `UnityEngine.Application`처럼 이름을 다 적는다.
  - 메서드 인자를 바꾸고 호출하는 곳 하나를 놓친 것 (`EmueraContent.OnClick(e)`)
- 파일마다 BOM이 있는 것과 없는 것이 섞여 있다. 고칠 때 원래 상태를 유지한다.
- `ButtonStringCreator.cs`, `SpriteManager.cs`는 mcs가 문법 오류로 보지만 Unity(Roslyn)에서는 정상이다.

## 최근에 한 일 (새것부터)

| 커밋 | 내용 |
|---|---|
| `1f709d2` | `MATCHALL` 구현 (메가텐P 사양대로. 범위 생략 시 배열 전체 또는 전체 캐릭터) |
| `7b59e62` | `MATCHALL` 다시 등록 (원본에 없어서 지웠는데 메가텐 전투가 멈췄음) |
| `cdc939b` | 텍스트 형식 세이브도 BOM → UTF-8 → Shift-JIS 자동 판별 |
| `1c3dc6e` | TMP 글꼴 아틀라스를 90pt/1024 → 48pt/2048로 키움. 게임 폴더에 `uEmuera.log`(경고·오류) 기록 |
| `6aa781c` | 캐릭터 CSV에도 `_Rename.csv` 치환 적용 (메가텐 스킬이 전부 산바람으로 나오던 문제) |
| `93a0a95` | 화면 변화가 1초 없으면 그리기를 초당 2회로 줄임 (`RenderThrottle.cs`). 입력 후 10ms 대기 제거 |
| `ab9ac3f` | G*/SPRITE*/OUTPUTLOG를 명령으로 쓰는 경우 원본처럼 일반 함수 경로로 처리. OUTPUTLOG 인자(파일명, 정보 숨김) |
| `50e5913` | INPUT 마우스 모드의 `RESULT:3`(마스크 색), INPUTMOUSEKEY의 `RESULT:5`/`RESULTS` |
| `9737e09` | `SETBGIMAGE`/`REMOVEBGIMAGE`/`CLEARBGIMAGE` (`BGImageLayer.cs`) |
| `7adb66d` | `INPUTANY`, `BREAKBUTTON`, `FORCE_BEGIN`, `FORCE_QUIT`, `QUIT_AND_RESTART`, `UPDATECHECK`, `SKIPLOG`, `GAMEBASE_URL`/`VERSIONNAME` |
| `06a260c` | `GDRAWLINE`, `GDASHSTYLE`, `GGETPENWIDTH`, `SPRITEDISPOSEALL` |
| `9f703c0` | EE 방식 ERD (변수별 `.erd`, `GETNUM` 3번째 인자, `ERDNAME`, config `ERD機能を利用する`) |

그 이전: EE 인자 형태(INPUT/PRINT_IMG 등), CBG 층, HTML_PRINT_ISLAND, BMP/GIF, CHECK_OVERFLOW, 글꼴 태그 수정 등. `git log`와 README의 "원본과 달라진 점"에 정리돼 있다.

## 아직 확인이 안 된 것 (사용자/제보자 테스트 필요)

1. **eraTW 신체정보 화면에서 div 안의 한글 일부가 빠지는 문제**
   - 그 화면에서만, 매번 똑같이 생긴다. 흔한 글자('다','가','을')가 빠지고 덜 흔한 글자는 보인다. 빠진 자리의 칸은 남아 있다.
   - 아틀라스 장이 넘치는 것을 의심해서 `1c3dc6e`에서 크게 키웠다. **고쳐졌는지 미확인.**
   - 안 고쳐졌으면 그 화면을 연 직후의 `uEmuera.log`를 받는다. 다음 후보:
     - div 자식 줄이 호스트 줄(TMP) 밑에 붙는 구조 (`EmueraLine.RenderDivPart`의 `SetParent(this.transform)`)
     - depth가 있는 줄에 붙는 Canvas(`overrideSorting`)
     - `TMPFonts.AttachDefaultFallback`이 공용 대체 글꼴(SystemCJK)의 줄 높이 정보를 새 글꼴 값으로 덮어쓰는 것
   - 해당 ERB를 받으면 그 화면이 쓰는 명령(SETFONT, `<font>`, div, depth)으로 범위를 좁힐 수 있다.
2. **메가텐 스킬 (`6aa781c`)**: 게임의 `CSV/_Rename.csv`에 `321,스킬:물어뜯기` 같은 줄이 있고 `emuera.config`의 `_Rename.csvを利用する`가 켜져 있어야 동작한다. 실제 파일로 확인 못 했다.
3. **RenderThrottle**: 애니메이션/타이머 화면이 끊기지 않는지, 발열이 줄었는지. 기다리는 시간과 줄인 빈도는 `RenderThrottle.cs` 맨 위 상수로 조절.
4. **아틀라스 48pt**: 확대했을 때 글자가 흐려 보이지 않는지.

## 남은 할 일

- **메가텐P(EmueraEE 개조판?) 전용 구문** — 사용자가 보내준 목록 기준:
  - `HASH_XXH32(str)`(32bit), `HASH_XXH3(str)`(64bit): **없음.** xxHash 구현 필요.
  - 옵션 `新しい高速なアルゴリズムを使う`: **없음.** 켜면 빠른 RAND를 쓰고 `DUMPRAND`/`INITRAND`/`RANDOMIZE`를 무시. config 항목 추가 필요.
  - `MATCHALL`, `GETCSVNOBYNAME` 계열 4개: 있음.
- 원본과 다른 점으로 남겨둔 것:
  - `FORCE_QUIT_AND_RESTART` 연속 실행 시 원본은 묻지만 여기서는 바로 에러
  - `INPUTMOUSEKEY`의 `RESULT:6`(마스크 색)은 넣지 않음 (원본은 스크립트가 넘어간 뒤에 값을 넣어서 사실상 못 씀)
  - `CALLSHARP`는 모바일에서 불가
  - 원본에 없는 함수 `GDRAWRECTANGLE`, `GETCSVNOBY*`는 유지
- 성능: G* 그래픽 명령은 CPU에서 픽셀을 계산해서 무겁다. 사용자가 "어쩔 수 없다"고 해서 보류.
- CP949(EUC-KR) 파일은 자동 판별 불가 (Shift-JIS와 구분이 안 됨). UTF-8로 변환해서 쓰도록 안내 중.

## 릴리스

- GitHub Releases에 APK 3개(`uEmuera.apk` 통합판, `arm64-v8a`, `armeabi-v7a`)를 올리는 방식. README의 다운로드 링크는 `releases/latest/download/<파일명>`이라 **파일 이름을 바꾸면 링크가 깨진다.**
- 빌드 전에 `File → Build Profiles`의 씬 목록에 `Assets/Main.unity`가 있는지 확인한다.
