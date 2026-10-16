# 세이브

[← 목차](README.md)

<details>
<summary><b>텍스트 형식 세이브의 문자 코드 자동 판별</b></summary>

- PC판에서 Shift-JIS로 저장한 텍스트 형식 세이브도 그대로 읽습니다(BOM → UTF-8 → Shift-JIS 순서로 판별).

</details>

<details>
<summary><b>세이브 압축</b></summary>

- `emuera.config`의 `セーブデータを圧縮して保存する`가 켜져 있으면 바이너리 세이브를 gzip으로 압축해 저장합니다(EE와 같은 형식).
- PC의 Emuera EE에서 압축 저장한 세이브도 읽습니다.

</details>

<details>
<summary><b>MAP · XML · DataTable 저장</b></summary>

- `CSV/VarExt*.csv`로 지정한 데이터를 세이브에 넣습니다. 자세한 내용은 [ee.md](ee.md)에 있습니다.

</details>

<details>
<summary><b>저장 실패 원인 기록</b></summary>

- 화면에 나오는 오류 문구는 EE와 같지만, `SAVEGLOBAL`, `LOADGLOBAL`, 일반 세이브, 세이브 목록 확인이 실패하면
  실제 원인과 파일 경로를 게임 폴더의 `uEmuera.log`에 남깁니다.

</details>

<details>
<summary><b>텍스트 형식 게임에서 저장이 안 되던 문제</b> (0.3.1d에서 수정)</summary>

- `セーブデータをバイナリ形式で保存する:NO`인 게임에서 `LOADGLOBAL` 직후 `SAVEGLOBAL`을 하면
  「グローバルデータの保存中にエラーが発生しました」가 났습니다. 읽은 세이브 파일이 열린 채로 남은 것이 원인이었습니다.
- 지금은 세이브 파일을 여는 모든 곳에서 읽기·쓰기가 끝나면 파일을 확실히 닫습니다.

</details>
