# 원본과 달라진 점

[uEmuera](https://github.com/xerysherry/uEmuera)(원본)를 기준으로 이 포크에서 바뀐 점을 분류별로 정리했습니다.
각 문서는 항목마다 접어 두었으니, 제목을 눌러 펼쳐 보세요.

동작의 기준은 **Emuera EE**입니다. EE와 같게 맞추는 것이 원칙이고, EE에 없는 기능은 실제 게임이 필요로 할 때만 넣습니다.

| 분류 | 내용 | 문서 |
|---|---|---|
| EE 호환 | EE의 명령·함수·인자, ERD, HTML·그래픽 확장, VarExt 저장 | [ee.md](ee.md) |
| 다른 Emuera 파생판 | 메가텐 P판(ShinEraTensei) 전용 기능, `MATCHALL`, `CHECK_OVERFLOW` | [other-engines.md](other-engines.md) |
| 화면 표시 | 글자 렌더링, 글꼴, 레이아웃, `<div>` 배치 | [display.md](display.md) |
| 세이브 | 문자 코드 판별, 압축, 오류 기록 | [saves.md](saves.md) |
| 앱 기능 | 한국어 UI, 메뉴, 빠른 버튼, 업데이트 확인, 화면 글자 복사, 디버그 모드 | [app.md](app.md) |
| 성능 | 로딩 시간, 발열·배터리 | [performance.md](performance.md) |
| 개발 | 빌드, 릴리스, 테스트 | [development.md](development.md) |

버전별로 무엇이 바뀌었는지는 [CHANGELOG.md](../CHANGELOG.md)에 있습니다.

## 문서를 고칠 때

- 기능을 넣거나 동작을 바꾸면 위 분류 중 맞는 문서에 항목을 하나 추가하고, [CHANGELOG.md](../CHANGELOG.md)의 "다음 버전"에도 한 줄 적습니다.
- 항목은 `<details>`로 접고, `<summary>`에는 한 줄 요약을 씁니다.
- EE에 없는 기능이면 어느 파생판의 기능인지 적습니다.
