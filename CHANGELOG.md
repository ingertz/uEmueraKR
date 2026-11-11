# 변경 기록

버전마다 바뀐 점입니다. 기능별 설명은 [docs/README.md](docs/README.md)에 있습니다.
받는 곳: [Releases](https://github.com/ingertz/uEmueraKR/releases)

<details open>
<summary><b>다음 버전</b> (아직 배포 안 함)</summary>

- 「빠른 버튼 크기」를 줄이면 한 번에 옆으로 보이는 칸 수도 늘어남 (대 4칸 · 중 5칸 · 소 6칸) ([app.md](docs/app.md))
</details>

<details>
<summary><b>0.3.2c</b> (2026-11-11)</summary>

- 본문의 버튼을 누르고 있는 동안 선택 색(PC판에서 마우스를 올렸을 때의 색)으로 바뀜. 그림 버튼은 살짝 어두워짐 ([app.md](docs/app.md))
- 입력을 기다리는 화면에서 버튼 바로 옆(약 2mm 안)을 눌러도 가장 가까운 버튼이 눌림 ([app.md](docs/app.md))
- 게임 중 메뉴에 「빠른 버튼 크기」 추가. 누를 때마다 대 → 중 → 소로 바뀜 ([app.md](docs/app.md))
- eraJS용으로 배포된 팩(TW JS판 등)을 읽을 수 있게 함. 팩 폴더에 eraJS 파일이 있으면, eraJS처럼 해석 못 하는 줄이 있어도 시작하고 그 줄을 실행할 때 오류를 냄. eraJS 전용 문법 몇 가지(`#FUNCTION` 안의 `RETURN`, `HTML_GETPRINTEDSTREX`, `<span style='color:…'>`, 명령어 뒤에 공백 없이 붙은 `PRINTFORMW%…` 등)도 받음 ([other-engines.md](docs/other-engines.md))
- 개발: 게임을 Unity 배치 모드로 돌려 화면 글자를 뽑는 도구 `Assets/Editor/PlayTest.cs` 추가
- 개발: 엔진 테스트가 .NET SDK가 없는 PC에서도 Unity 에디터에 딸린 SDK로 돌아감 ([development.md](docs/development.md))

</details>

<details>
<summary><b>0.3.2b</b> (2026-10-17)</summary>

- 밑줄(FONTSTYLE 8)이 한글 글자 아랫부분에 겹치던 문제 수정. 한 줄의 밑줄이 PC판처럼 같은 높이로 이어지고, 버튼 사이 공백에도 그어짐(OCG2 덱 편집 화면 등) ([display.md](docs/display.md))

</details>

<details>
<summary><b>0.3.2a</b> (2026-10-16)</summary>

- 「업데이트 확인」에서 새 버전이 있으면 앱이 직접 APK를 받아 설치 화면을 띄움 ([app.md](docs/app.md))

</details>

<details>
<summary><b>0.3.1g</b> (2026-10-16)</summary>

- HTML의 `<shape type='space' param='54px'>`처럼 `px`를 붙인 크기를 글자 크기의 백분율로 잘못 읽던 문제 수정. TW에서 얼굴 그림 위 이름표가 가운데로 몰리고 홍조가 엉뚱한 자리에 나오던 원인

</details>

<details>
<summary><b>0.3.1f</b> (2026-10-16)</summary>

- 오른쪽 위 아이콘 줄에 「되돌리기」 추가. 직전 입력을 하나 취소합니다 ([app.md](docs/app.md))
  - 메인 메뉴나 조교 커맨드 선택마다 상태를 기억해 두어, 커맨드 하나를 되돌리는 건 거의 즉시 끝납니다
  - 게임 메뉴로 불러오는 게임(TW, 메가텐 등)에서도 동작합니다
- 게임 중 메뉴에 「화면 글자 복사」 추가. 지금 화면의 글자를 클립보드에 넣습니다 ([app.md](docs/app.md))
- 처리 중 표시(소녀기도중)를 언어에 맞게 표시하고, 되돌리기 아이콘과 겹치지 않게 옮김
- 메가텐 P판 Emuera의 `DICT_*`, `G_POLYGON_*`, `setting.json`의 `UseNewRandom` 지원 ([other-engines.md](docs/other-engines.md))
- 개발: Unity 없이 돌리는 엔진 테스트 추가 ([development.md](docs/development.md))

</details>

<details>
<summary><b>0.3.1e</b> (2026-10-16)</summary>

- 저장이나 불러오기가 실패하면 실제 원인을 `uEmuera.log`에 남김
- `CSV/VarExt*.csv`에 적은 MAP·XML·DataTable을 세이브에 저장(EE와 같음)
- `セーブデータを圧縮して保存する` 지원. PC의 EE에서 압축 저장한 세이브도 읽음
- 게임 목록 메뉴에 「업데이트 확인」 추가. 누를 때만 인터넷에 접속함

</details>

<details>
<summary><b>0.3.1d</b> (2026-10-13, 10-14에 파일 교체)</summary>

- 들여쓰기(여백)를 설정하면 글자가 군데군데 빠지고 점점 심해지던 문제 수정
- 10-14 교체분: 텍스트 형식 세이브를 쓰는 게임에서 「グローバルデータの保存中にエラーが発生しました」가 나며 저장이 안 되던 문제 수정

</details>

<details>
<summary><b>0.3.1c</b> (2026-09-25, 10-10에 파일 교체)</summary>

이 포크의 첫 배포판입니다. 원본 uEmuera와 달라진 점 대부분이 여기에 들어 있습니다([docs/README.md](docs/README.md)).

- 10-10 교체분에 추가된 것
  - 메가텐 P판의 `HASH_XXH32`/`HASH_XXH3`, 32비트를 넘는 그래픽 ID
  - `MATCHALL`
  - 텍스트 형식 세이브의 문자 코드 자동 판별(Shift-JIS 세이브)
  - `_Rename.csv`를 캐릭터 CSV에도 적용
  - 글꼴 아틀라스 크기 조정, 경고와 오류를 남기는 `uEmuera.log`
  - CPU별 APK

</details>
