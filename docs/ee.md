# EE 호환

[← 목차](README.md)

원본 uEmuera는 Emuera 1824 기반이라 EE(Emuera EE)의 확장을 몰랐습니다. 이 포크는 EE 원본 소스와 비교해 맞춰 왔고,
EE의 내장 함수와 명령은 EE에서 꺼져 있는 `GROTATE`를 빼고 모두 있습니다.

<details>
<summary><b>HTML_PRINT의 &lt;div&gt; 태그</b></summary>

- 위치(`xpos`, `ypos`), 크기, margin/padding/border, 배경색을 지원합니다.
- `xpos`가 음수인 `<div>`는 EE처럼 앞 글자 위에 겹쳐 놓습니다.
- `<div>` 안의 버튼과 그림도 빠른 버튼에 모입니다 ([app.md](app.md)의 "빠른 버튼").

</details>

<details>
<summary><b>입력 명령의 확장 인자</b></summary>

- `INPUT`/`INPUTS`/`ONEINPUT`/`ONEINPUTS`/`TINPUT`/`TINPUTS`의 마우스 입력·스킵 인자
  - 마우스 입력이 켜지면 탭한 버튼이 `RESULTS:1`로, 마스크 이미지에서 누른 위치의 색이 `RESULT:3`으로 들어갑니다.
- `INPUTMOUSEKEY`에서 탭한 버튼의 값이 `RESULT:5`(숫자) 또는 `RESULTS`(문자열)로 들어갑니다.
- `BINPUT` 계열은 화면에 있는 버튼 값만 입력으로 받습니다.

</details>

<details>
<summary><b>PRINT_IMG / PRINT_RECT / PRINT_SPACE 확장</b></summary>

- `PRINT_IMG 이미지, 버튼이미지, 마스크, 폭, 높이, 세로위치`
- `PRINT_RECT`/`PRINT_SPACE`의 `px` 단위

</details>

<details>
<summary><b>ERD (요소에 이름 붙이기)</b></summary>

- `#DIM`으로 만든 변수와 이름이 같은 `.erd`(ERB 폴더 아래 전체) 또는 `.csv`(CSV 폴더)가 있으면,
  그 변수의 요소를 `변수:이름`처럼 이름으로 쓸 수 있습니다. 다차원은 `변수명@1.erd`, `변수명@2.erd` …
- `GETNUM`의 세 번째 인자(차원), `ERDNAME`, `GETNUMB`
- `emuera.config`의 `ERD機能を利用する`로 끌 수 있습니다(기본값은 켜짐).

</details>

<details>
<summary><b>EE에서 추가된 명령</b></summary>

- `INPUTANY`(숫자·문자열 모두 입력), `BREAKBUTTON`, `FORCE_BEGIN`, `SKIPLOG`
- `QUIT_AND_RESTART`/`FORCE_QUIT_AND_RESTART`는 메뉴의 "다시 읽기"와 같은 방식으로 게임을 다시 시작하고,
  `FORCE_QUIT`은 입력을 기다리지 않고 끝냅니다.
- `UPDATECHECK`와 `GAMEBASE_URL`/`GAMEBASE_VERSIONNAME` (`GAMEBASE.CSV`의 `バージョン情報URL`, `バージョン名`)
- `BEGIN`을 어느 상태에서든 쓸 수 있습니다.
- `OUTPUTLOG 파일명, 정보숨김`: 실행 폴더 아래 원하는 파일로 로그를 저장합니다.

</details>

<details>
<summary><b>그래픽 명령</b></summary>

- `GGETTEXTSIZE`: 글자를 그렸을 때의 폭을 돌려주고, 높이를 `RESULT:1`에 넣습니다.
- `GDRAWTEXT`는 그린 글자의 폭/높이를 `RESULT:1`/`RESULT:2`에 넣습니다.
- `GSETCOLOR`가 실제로 점을 찍고, `GGETCOLOR`/`SPRITEGETCOLOR`가 올바른 위치의 색을 읽습니다.
- `GDRAWLINE`(펜 색·굵기, `GDASHSTYLE`의 점선 반영), `GGETPENWIDTH`, `SPRITEDISPOSEALL`
- `SPRITEANIMECREATE`/`SPRITEANIMEADDFRAME`으로 만든 움직이는 그림이 프레임대로 움직입니다.

</details>

<details>
<summary><b>화면 배경 그래픽(CBG)과 배경 이미지</b></summary>

- `CBGSETG`, `CBGSETSPRITE`로 설정한 그림을 화면에 고정해서 표시합니다(zdepth 양수는 글자 뒤, 음수는 글자 앞).
- `CBGSETBMAPG` 버튼 맵을 터치하면 `INPUTMOUSEKEY`의 `RESULT:4`로 번호가 전달됩니다.
- `SETBGIMAGE`/`REMOVEBGIMAGE`/`CLEARBGIMAGE`: 화면 폭(모자라면 높이)에 맞춰 늘리고, 위쪽에 붙여 가운데에 두며,
  글자와 CBG 뒤에 표시합니다.

</details>

<details>
<summary><b>HTML_PRINT_ISLAND</b></summary>

- `HTML_PRINT_ISLAND` / `HTML_PRINT_ISLAND_CLEAR`: 로그에 남지 않는 HTML을 화면 위쪽에 고정해서 겹쳐 표시합니다.

</details>

<details>
<summary><b>MAP · XML · DataTable과 VarExt 저장</b></summary>

- `MAP_*`, `XML_*`, `DT_*` 함수를 지원합니다.
- `CSV/VarExt*.csv`에 적은 이름의 데이터는 세이브에 저장됩니다(바이너리 형식 세이브만, EE와 같음).
  - `GLOBAL_MAPS`, `GLOBAL_XMLS`, `GLOBAL_DTS`: `SAVEGLOBAL`로 저장, `LOADGLOBAL`로 불러옴
  - `SAVE_MAPS`, `SAVE_XMLS`, `SAVE_DTS`: 일반 세이브로 저장, 불러오기·`RESETDATA` 때 비움
  - `STATIC_MAPS`, `STATIC_XMLS`, `STATIC_DTS`: `RESETGLOBAL` 때 비움
- `RESETGLOBAL`은 EE처럼 메모리만 비웁니다. 파일까지 비우려면 게임이 이어서 `SAVEGLOBAL`을 해야 합니다.

</details>

<details>
<summary><b>설정(emuera.config)</b></summary>

- `セーブデータを圧縮して保存する`(세이브 압축)를 지원합니다 ([saves.md](saves.md)).
- `Ctrl-Zで元に戻す機能を有効にする`의 되돌리기는 설정과 관계없이 항상 켜져 있고, 화면의 아이콘으로 씁니다 ([app.md](app.md)).
- `UPDATECHECKを許可しない`, `LOADTEXTとSAVETEXTで使える拡張子`, `ERD機能を利用する`를 지원합니다.
- 아직 없는 EE 설정: 클립보드 관련(CB*), Rikaichan, `VARSIZEの次元指定をERD機能に合わせる`,
  `行連結の改行コードの置換文字列`. 뒤의 둘은 지금 동작이 EE 기본값과 같습니다.

</details>

<details>
<summary><b>사운드와 이미지 형식</b></summary>

- `PLAYBGM`, `PLAYSOUND` 등을 지원합니다. 형식: `.ogg`, `.mp3`, `.wav`, `.mid`/`.midi`(MIDI는 Android 전용)
- 이미지 형식: `.png`, `.jpg`, `.webp`, `.bmp`, `.gif`(GIF는 첫 프레임만)

</details>
