<div align="center">

# 목차

| [✈️ 프로젝트 소개(개발환경) ](#airplane-프로젝트-소개) |
| :---: |
| [✋ 팀 소개 ](#hand-팀-소개) |
| [🌟 주요기능 ](#star2-주요기능) |
| [☑️ 기술 스택 ](#ballot_box_with_check-기술-스택) |
| [🕸️ 와이어프레임 ](#spider_web-와이어프레임) |
| [📓 UML ](#uml) |

</div>

#

# JumpRoooooope
[필요하면 사진 넣기]


## 📖 게임 소개
### "동물 친구들과 함께 떠나는 신나는 줄넘기 여행!"

세상에서 가장 귀여운 블록 동물들이 큐브 세상에 모였습니다. 기린, 토끼, 곰, 펭귄까지! 신나는 리듬에 맞춰 줄을 넘고, 장애물을 피해 한계를 돌파하는 무한 점프 액션 게임입니다.

- 개발 환경: Unity 6000.3.2f1 <br/> Visual Studio Community 2022, Visual Studio Code
- 플랫폼: Mobile (Android)
- 장르: 캐주얼 점프 액션 / 아케이드
- 개발 기간: 2026.01.08 ~ 2026.03.05 (Android PlayStore 비공개 테스트 진행 中)

## 📺시연 영상
### [📺YouTube Link]
[![Watch the video](https://img.youtube.com/vi/NbgmZB3tmz8/maxresdefault.jpg)](https://www.youtube.com/watch?v=NbgmZB3tmz8)

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :hand: 팀 소개

| 이름 | 담당 업무 | 깃허브 주소 | 이메일 |
| :---: | :---: | :---: | :---: |
| 이경현 | MapTile, 유닛 생성 시 배치, 유닛 이동 및 애니메이션 | https://github.com/YooSeungA52 | https://velog.io/@seunga52/posts |
| 최세은 | GameManager, 몬스터 소환 및 직렬화를 활용한 스테이지 구성, 몬스터 애니메이터 배치 | https://github.com/Kaldorei00910 | https://velog.io/@c00kie/posts |



[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :star2: 주요기능

### 1. 로그인 기능
[필요하면 사진 넣기]

- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

## Lobby Scene
### 1. 상점 기능
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

### 2. 도전 과제 기능
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

### 3. 랭킹 기능
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

## Game Scene
### 1. 도전 과제 기능
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

### 2. 장애물 시스템
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

#### 2-1. 통나무
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.
- 
#### 2-2. 화살
[필요하면 사진 넣기]


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.


<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :ballot_box_with_check: 기술 스택

[필요하면 사진 넣기]

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>


## :spider_web: 와이어프레임

[필요하면 사진 넣기]

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

<a id="uml"></a>
## :notebook: UML

### ■ 클래스 다이어그램
```mermaid
classDiagram
direction LR

class GameManager {
  +static Instance
  +JumpRope jumpRope
  +PlayerController playerController
  +ObstacleSpawner obstacleSpawner
  +ThemeLoader themeLoader
  +bool IsGameStarted
  +bool IsGameOver
  +RealGameStart()
  +GameOver()
  +OnRetryButtonClicked()
}

class JumpRope {
  +UnityEvent OnRopeHitGround
  +UnityEvent OnRopeHitPlayer
  +PlayerController player
  +InitRope(startSpeed)
  +SetSpeed(newSpeed)
  +StopRope()
  +UpdateRopeColor(color)
}

class PlayerController {
  -Rigidbody rb
  -PlayerAnimationController animCtrl
  +bool IsGrounded
  +ApplyStun(seconds)
  +ResetPosition()
}

class ObstacleSpawner {
  +static Instance
  +SpawnData[] spawnInfos
  +PlayerController playerController
  +MapThemeData currentTheme
  +InitializeSpawner()
}

class ThemeLoader {
  +SetupCurrentThemeAsync(ct) MapThemeData
}

class DatabaseManager {
  +static Instance
  +UserGameData currentData
}

class MapManager {
  +static Instance
  +GetMapItemByID(id) ShopMapItemDefinition
  +GetThemeDataByID(id) MapThemeData
}

class ScoreManager {
  +static Instance
  +int currentScore
  +SetInit()
  +AddPerfectScore()
  +SetGameOverPanel()
}

class RewardUIManager {
  +InitAdButton()
}

class ObjectPool {
  +static Instance
  +RegisterPrefab(prefab, count)
  +DeactivateAllObjects()
}

class ChallengeManager {
  +static Instance
  +ReportProgress(type, value)
}

class SceneController {
  +static Instance
  +FadeIn()
  +FadeOut()
  +SceneTransitionToLobby()
}

class BGMSounds {
  +static Instance
  +PlayBGM(sceneName)
  +SetGameBGM(clip)
  +StopBgm()
}

class MapThemeData {
  +Color themeRopeColor
  +GetPrefabByType(type) GameObject
}

class ShopMapItemDefinition {
  +string id
  +GameObject prefab
  +MapThemeData mapThemeData
  +Vector3 gameLocalPosition
  +Vector3 gameLocalEuler
  +Vector3 gameLocalScale
}

%% 핵심 소유(참조) 관계
GameManager o-- JumpRope
GameManager o-- PlayerController
GameManager o-- ObstacleSpawner
GameManager o-- ThemeLoader
GameManager o-- RewardUIManager

%% 이벤트 연결
JumpRope --> PlayerController : uses(IsGrounded)
JumpRope --> ScoreManager : score check / hit ground
JumpRope --> ChallengeManager : (indirect via GameOver)

%% 테마/데이터 흐름
ThemeLoader --> DatabaseManager : reads equippedThemeID
ThemeLoader --> MapManager : loads ShopMapItemDefinition
ThemeLoader --> BGMSounds : set game BGM
ThemeLoader --> MapThemeData : returns

ObstacleSpawner --> MapThemeData : currentTheme
ObstacleSpawner --> DatabaseManager : fallback theme
ObstacleSpawner --> MapManager : GetThemeDataByID
ObstacleSpawner --> ObjectPool : spawn pooling
ObstacleSpawner --> PlayerController : stun target

GameManager --> DatabaseManager : reads buttonCase
GameManager --> ObjectPool : DeactivateAllObjects
GameManager --> ScoreManager : init/score/gameover ui
GameManager --> SceneController : fade in/out & lobby
GameManager --> BGMSounds : play scene BGM
GameManager --> ChallengeManager : report play count
```

### ■ 시퀀스 다이어그램
```mermaid
sequenceDiagram
autonumber
actor Unity as Unity(Runtime)
participant GM as GameManager
participant RUI as RewardUIManager
participant DB as DatabaseManager
participant SM as ScoreManager
participant OP as ObjectPool
participant TL as ThemeLoader
participant MM as MapManager
participant BGM as BGMSounds
participant SC as SceneController
participant JR as JumpRope
participant OS as ObstacleSpawner
participant CM as ChallengeManager

Unity->>GM: Start()
GM->>GM: InitializeGameSequence(ct)

%% 1) PrepareGameDataAsync
GM->>GM: PrepareGameDataAsync(ct)
GM->>DB: currentData 확인(buttonCase1)
alt DB.currentData 존재
  GM->>GM: SetButtonsLocation(isCase1)
end
GM->>SM: SetInit()
GM->>RUI: InitAdButton()
GM->>OP: DeactivateAllObjects()

%% 2) InitializeComponentsAsync
GM->>GM: InitializeComponentsAsync(ct)
GM->>TL: SetupCurrentThemeAsync(ct)
TL->>TL: WaitUntilUserDataReadyAsync(timeout)
TL->>DB: currentData 확인(equippedThemeID)
TL->>MM: GetMapItemByID(themeId)
MM-->>TL: ShopMapItemDefinition
TL->>BGM: SetGameBGM(clip) + StopBgm()
TL-->>GM: MapThemeData

GM->>BGM: PlayBGM("GameScene")
GM->>SC: FadeIn()

GM->>GM: (player 활성/리셋)
GM->>JR: UpdateRopeColor(themeRopeColor)
GM->>JR: OnRopeHitGround 리스너 연결(OnRopeScore)
GM->>JR: OnRopeHitPlayer 리스너 연결(GameOver)
GM->>JR: ResetPositionToAngle(180f)

GM->>OS: enabled=false
GM->>OS: currentTheme=mapData
GM->>OS: InitializeSpawner()

%% 3) Countdown
GM->>GM: StartCountdownAsync(ct)
loop 3..1 카운트다운
  GM->>GM: text=count
end
GM->>GM: text="Start!"
GM->>GM: countdownUI off

%% 4) RealGameStart
GM->>GM: RealGameStart()
GM->>JR: InitRope(initSpeed)
GM->>OS: enabled=true

%% 이벤트로 이어지는 흐름(요약)
note over JR,GM: 로프 회전 중 이벤트 발생
JR-->>GM: OnRopeHitGround()
GM->>SM: AddPerfectScore()
JR-->>GM: OnRopeHitPlayer()
GM->>GM: GameOver()
GM->>JR: StopRope()
GM->>OS: enabled=false
GM->>CM: ReportProgress(PlayCount, 1)
GM->>SM: SetGameOverPanel()
```
[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

