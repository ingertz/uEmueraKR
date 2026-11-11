# 다른 Emuera 파생판의 기능

[← 목차](README.md)

EE에는 없지만, 그 기능을 쓰는 게임이 있어서 넣은 것들입니다. 출처가 된 파생판을 함께 적습니다.

## 메가텐 P판(ShinEraTensei)의 Emuera

메가텐 P판에 들어 있는 `Emuera.exe`를 직접 분석해서 같은 동작으로 옮겼습니다.

<details>
<summary><b>Data 폴더 안에 게임이 있는 구조</b></summary>

- `게임폴더/Data/` 안에 `ERB/`, `CSV/`가 있는 게임도 목록에 `게임폴더/Data`로 나옵니다.
- 이런 게임은 `Data/font/`와 그 바깥 `font/`의 글꼴을 모두 찾습니다(가까운 쪽 우선).

</details>

<details>
<summary><b>VARI / VARS</b></summary>

- 함수 안에서 그 자리에 지역 변수를 선언하고 값을 넣습니다. 메가텐 P판 스크립트가 600번 넘게 씁니다.
  - `VARI 이름 = 값`, `VARI 이름, 크기`
  - `VARS 이름 = "문자열"`

</details>

<details>
<summary><b>HASH_XXH32 / HASH_XXH3과 큰 그래픽 ID</b></summary>

- 문자열을 UTF-8로 바꿔 xxHash(XXH32, XXH3 64bit, 시드 0)를 구합니다.
- 메가텐 P판은 이 값에 1억을 더해 그래픽 ID로 쓰므로, 그래픽 ID가 32비트를 넘어도 받습니다.

</details>

<details>
<summary><b>DICT_*</b> (이름 붙인 사전)</summary>

- `DICT_CREATE(이름)`, `DICT_EXIST(이름)`, `DICT_CONTAINS_KEY(이름, 키)`,
  `DICT_SET_VALUE(이름, 키, 값)`, `DICT_GET_VALUE_STRING(이름, 키)`, `DICT_GET_VALUE_LONG(이름, 키)`
- 키와 값에 숫자와 문자열을 모두 쓸 수 있습니다. 세이브되지 않고, 게임을 다시 읽으면 사라집니다.
- 지금의 메가텐 P판 스크립트는 아직 쓰지 않습니다.

</details>

<details>
<summary><b>G_POLYGON_*</b> (다각형 그리기)</summary>

- `G_POLYGON_POINT_ADD(ID, x, y)`로 점을 모은 다음,
  `G_POLYGON_DRAW(ID)`는 펜(`GSETPEN`)으로 테두리를 그리고 `G_POLYGON_FILL(ID)`는 브러시(`GSETBRUSH`)로 안을 칠합니다.
- `G_POLYGON_POINT_CLEAR(ID)`로 점을 지웁니다.
- 지금의 메가텐 P판 스크립트는 아직 쓰지 않습니다.

</details>

<details>
<summary><b>setting.json의 UseNewRandom</b></summary>

- 게임 폴더의 `setting.json`에 `"UseNewRandom": true`가 있으면 `RAND`가 다른 난수 생성기를 씁니다.
  이때는 `RANDOMIZE`와 `RANDDATA`의 영향을 받지 않습니다.

</details>

<details>
<summary><b>지원하지 않는 것</b></summary>

- `SQL_*` 함수(SQLite 데이터베이스). 게임이 쓰기 시작하면 넣을 예정입니다.

</details>

## 그 밖의 파생판

<details>
<summary><b>MATCHALL</b> (AI 번역판 메가텐 등)</summary>

- `MATCHALL 변수, 검색값(, 시작, 끝)` / `MATCHALL 1차원캐릭터변수, 인덱스, 검색값(, 시작, 끝)`
- 일치한 개수를 `RESULT:0`에, 일치한 위치를 `RESULT:1`부터 차례로 넣습니다.

</details>

<details>
<summary><b>CHECK_OVERFLOW</b> (Emuera1824+v10+v3)</summary>

- `CHECK_OVERFLOW 1`이면 정수 계산이 넘칠 때 INT64 최댓값/최솟값에서 멈춥니다.

</details>

<details>
<summary><b>eraJS용 팩</b> (TW JS판 / 키매라팩 등)</summary>

- eraJS는 Emuera를 JavaScript로 다시 만든 PC용 실행기(Electron)입니다. 게임 데이터(ERB/CSV)는 Emuera와 같아서 이 앱으로 그대로 돌릴 수 있습니다.
- 게임 폴더에 `README_eraJS.txt`, `eraJS.exe`, `eraJS_settings.json`, `resources/app/engine/compiler.js` 중 하나가 있으면 eraJS용 팩으로 보고 eraJS처럼 동작합니다.
  - 해석할 수 없는 줄이 있어도 게임을 시작하고, 그 줄을 실제로 실행할 때 오류를 냅니다.
    Emuera는 시작할 때 전부 검사해서 멈춥니다.
  - 읽는 중의 경고(함수 중복 정의 등, 이 팩은 수천 줄)는 화면에 띄우지 않습니다.
  - `#FUNCTION` 안의 `RETURN 값`은 값을 돌려줍니다(`RETURNF`와 같음). `#FUNCTION`이 아닌 함수의 `RETURNF 값`은 `RESULT`(문자열이면 `RESULTS`)에 넣고 돌아갑니다.
  - 함수 안의 `#DIM`에 `LINECOUNT`, `I` 같은 내장 변수 이름을 쓸 수 있습니다. 그 함수 안에서는 지역 변수가 우선합니다.
  - `HTML_GETPRINTEDSTREX(n)`: 표시된 줄의 글자를, 색이 다른 부분만 `<span style='color:#rrggbb'>`로 감싸서 돌려줍니다.
  - HTML의 `<span style='color:#rrggbb'>`를 `<font color>`처럼 받습니다.
- eraJS의 세이브(`erajs_save00.sav` 등)는 형식이 달라서 읽지 못합니다.
- CSV에 없는 이름(`TALENT:악취민감` 등)을 쓰는 줄은 팩 쪽 오류라서, 그 줄에 닿으면 eraJS에서처럼 오류가 납니다.

</details>
