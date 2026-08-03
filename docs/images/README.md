# 이미지 자리

README에서 참조하는 이미지 파일을 이 폴더에 넣습니다.
넣은 뒤 `README.md` 의 해당 `<!-- ... -->` 주석을 해제하세요.

## 필요한 파일

| 파일명 | 위치 | 무엇을 담아야 하는가 |
|---|---|---|
| `hero.png` | README 최상단 | **대표 이미지.** 전투 중 스킬 이펙트가 크게 터지는 순간. 가로 구도, 캐릭터가 화면에 또렷하게 보이도록. 이 한 장이 첫인상을 결정합니다. |
| `change-character.gif` | 게임 소개 | **캐릭터 교체 순간.** 2~3초. 교체 → `ChangeAttack` 이 이어지는 장면이면 가장 좋습니다. 이 게임의 핵심 메커닉입니다. |
| `attribute-tree.png` | 게임 소개 | **특성 트리 UI** (`PopupAttributeView`). 특성이 여러 개 해금된 상태여야 트리 구조가 보입니다. |
| `room-assembler.png` | 에디터 툴 | **`RoomAssemblerWindow` 실행 화면.** 툴 창과 조립된 룸이 한 화면에 같이 보이게. **가장 어필력이 높은 이미지입니다** — "문제를 툴로 해결했다"가 한 장으로 전달됩니다. |

## 캡처 방법

- **인게임 스크린샷**: 에디터 플레이 중 `F5` (`Tools/스크린샷 찍기`)
  - 저장 경로는 `Assets/Editor/ScreenshotCaptureTool.cs` 상단 `saveFolder` 상수 참조
  - 데모 빌드(`isDemo`)에서는 동작하지 않습니다
- **에디터 화면**: Windows `Win + Shift + S`
- **GIF**: [ScreenToGif](https://www.screentogif.com/) 등 사용

## 권장 사양

| 항목 | 권장 |
|---|---|
| 해상도 | 가로 1280~1920px |
| PNG 용량 | 장당 500KB 이하 |
| GIF 용량 | **3MB 이하** (GitHub에서 로딩이 느려집니다) |
| GIF 길이 | 2~4초 반복 |

용량이 크면 [TinyPNG](https://tinypng.com/) 등으로 압축하세요.
저장소가 이미 큰 편이라, 이미지는 가볍게 유지하는 편이 좋습니다.
