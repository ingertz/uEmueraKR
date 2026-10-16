# 변경 기록

버전마다 바뀐 점입니다. 기능별 설명은 [docs/README.md](docs/README.md)에 있습니다.
받는 곳: [Releases](https://github.com/ingertz/uEmueraKR/releases)

<details open>
<summary><b>다음 버전</b> (아직 배포 안 함)</summary>

- 게임 중 메뉴에 「화면 글자 복사」 추가. 지금 화면의 글자를 클립보드에 넣습니다 ([app.md](docs/app.md))
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
