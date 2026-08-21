# 새빛대학교 임시 도트 교체 안내

게임 이미지는 코드에 포함하지 않고 모두 `Assets/Resources/content`에서 불러온다. 아래 파일을 같은 이름의 PNG로 교체하면 코드를 수정하지 않아도 새 디자인이 적용된다.

## 현재 사용 중인 교체 파일

- 임시 통합 캠퍼스: `Assets/Resources/content/world/campus/campus-composite-v2.png` (3:2 비율)
- 지도 화면: `Assets/Resources/content/world/map/campus-map-v2.png` (3:2 비율)
- 총장실: `Assets/Resources/content/world/interiors/president-office-v1.png`
- 선물 아이콘: `Assets/Resources/content/items/gift-items-v1.png` (정사각형 2×2 배열)
- 편의점 점원 초상: `Assets/Resources/content/npc/portraits/shopkeeper-v1.png` (선택 파일)

## NPC 대화 초상화

NPC 초상화 PNG는 `Assets/Resources/content/npc/portraits`에, 전신 및 걷기 스프라이트는 `Assets/Resources/content/npc/character`에 넣는다. 파일이 없으면 월드 캐릭터의 얼굴 도트를 확대해 임시로 표시한다. 초상화 권장 비율은 세로형 4:5이며 배경이 투명한 PNG를 사용한다.

- 김하늘: `kim-haneul-v1.png`
- 박지훈: `park-jihun-v1.png`
- 최미숙: `choi-misuk-v1.png`
- 이도윤: `lee-doyun-v1.png`

같은 파일명으로 이미지를 교체하면 각 NPC의 대화 초상화만 변경된다.

플레이어 전신, 걷기 스프라이트, 초상화는 `Assets/Resources/content/player`에서 별도로 관리한다.

## 정식 캠퍼스 레이어

정식 도트 제작이 끝나면 다음 PNG를 추가한다. `campus-ground-v1.png`가 있으면 게임은 임시 통합 캠퍼스 대신 분리된 바닥을 자동 사용한다. 건물과 환경 PNG는 투명 배경으로 제작한다.

- 바닥과 잔디: `Assets/Resources/content/world/campus/campus-ground-v1.png`
- 도로와 보행로: `Assets/Resources/content/world/environment/roads-v1.png`
- 나무와 화단: `Assets/Resources/content/world/environment/trees-v1.png`
- 본관: `Assets/Resources/content/world/buildings/administration-v1.png`
- 중앙도서관: `Assets/Resources/content/world/buildings/library-v1.png`
- 인문사회관: `Assets/Resources/content/world/buildings/humanities-v1.png`
- 공학·자연과학관: `Assets/Resources/content/world/buildings/science-engineering-v1.png`
- 학생회관: `Assets/Resources/content/world/buildings/student-center-v1.png`
- 기숙사: `Assets/Resources/content/world/buildings/dormitory-v1.png`
- 폐쇄된 구관: `Assets/Resources/content/world/buildings/old-hall-v1.png`
- 체육관: `Assets/Resources/content/world/buildings/gym-v1.png`

파일이 없는 레이어는 건너뛰므로 하나씩 제작하고 확인할 수 있다. 각 이미지의 게임 내 위치와 크기는 별도로 고정되어 있어 다른 건물에 영향을 주지 않는다.

## 선물 아이콘 배열

| 위치 | 아이템 |
|---|---|
| 왼쪽 위 | 따뜻한 커피 |
| 오른쪽 위 | 허브차 |
| 왼쪽 아래 | 수제 쿠키 |
| 오른쪽 아래 | 매운 생선빵 |

아이콘 시트는 네 칸의 크기가 정확히 같아야 한다. 픽셀아트는 Unity에서 흐려지지 않도록 Point 필터로 표시한다.

정식 캐릭터, 건물, 도로, 나무 타일도 이후 같은 방식으로 외부 PNG 또는 스프라이트 시트로 분리한다.
