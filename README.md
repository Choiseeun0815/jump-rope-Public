#

### 사용버전
Unity 2022.3.17f1

#

# 완전 운빨 랜덤 디펜스 // 완운디
![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/81ef224b-f435-42a2-b3a3-aa99fb1f3f00)


<div align="center">

# 목차

| [✈️ 프로젝트 소개(개발환경) ](#airplane-프로젝트-소개) |
| :---: |
| [✋ 팀 소개 ](#hand-팀-소개) |
| [💭 기획의도 ](#thought_balloon-기획의도) |
| [🌟 주요기능 ](#star2-주요기능) |
| [⏲️ 프로젝트 수행 절차 ](#timer_clock-프로젝트-수행-절차) |
| [🕸️ 와이어프레임 ](#spider_web-와이어프레임) |
| [📓 UML ](#notebook-통합모델링-다이어그램) |
| [☑️ 트러블 슈팅 ](#ballot_box_with_check-트러블-슈팅) |

</div>

## :airplane: 프로젝트 소개

#### Team Notion을 클릭하시면, 프로젝트 진행 과정을 확인하실 수 있습니다!
### [🤝Team Notion]
https://www.notion.so/teamsparta/2dd4909bcb3c40f499d0e14a3c86e653

#### ${\textsf{\color{red}2D 랜덤 디펜스}}$
### 모든 것은 내 운빨로 승부한다!

<div align="center">

| 게임명 | **${\textsf{\color{blue}완전 운빨 랜덤 디펜스}}$** |
| :---: | :---: |
| 장르 | 3D 디펜스 |
| 개발 언어 | C# |
| 개발 환경 | Unity 2022.3.17f1 <br/> Visual Studio Community 2022 |
| 타겟 플랫폼 | PC |
| 개발 기간 | 2024.06.19 ~ 2024.06.25 |

</div>

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :hand: 팀 소개

| 이름 | 역할 | 담당 업무 | 깃허브 주소 | 블로그 |
| :---: | :---: | :---: | :---: | :---: |
| 유승아 | 팀장 | MapTile, 유닛 생성 시 배치, 유닛 이동 및 애니메이션 | https://github.com/YooSeungA52 | https://velog.io/@seunga52/posts |
| 국기웅 | 팀원 | GameManager, 몬스터 소환 및 직렬화를 활용한 스테이지 구성, 몬스터 애니메이터 배치 | https://github.com/Kaldorei00910 | https://velog.io/@c00kie/posts |
| 김정석 | 팀원 | 몬스터 이동 및 체력 시스템, 사운드 시스템, 카메라 이동 로직, 밸런스 조절 | https://github.com/RryNoel | https://gamemods.tistory.com/ |
| 박신후 | 팀원 | 유닛 랜덤 생성, 유닛 공격 및 강화 기능, 투사체 구현,  이벤트 핸들러 생성 | https://github.com/SinHoo99 | https://sinhu99.tistory.com/ |
| 이경현 | 팀원 | UI와 관련된 모든 기능, 재시작 기능, 일괄 판매 기능 | https://github.com/gstk0009?tab=repositories | https://unitylearn.tistory.com/ |

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :thought_balloon: 기획의도

### 1. 주제 선정 배경
지금까지 배운 기술을 바탕으로 모두가 즐길 수 있는 2D 랜덤 디펜스 게임을 개발하고자 함
1. 기본적으로 유닛의 위치 이동을 구현하여 캐릭터를 조작할 수 있게 하고, 유닛과 몬스터의 밸런스를 조절하여 레벨 디자인 설계
2. 유닛이 몬스터를 공격할 때는 충돌 처리를 통해 피해를 주는 로직을 작성함
3. 직관적인 UI를 구현하여 플레이어가 쉽게 게임을 진행하고 현재 상황을 파악할 수 있도록 함
4. 기술적 역량을 실제로 적용해 보고, 재미와 도전감을 제공하는 게임을 목표로 함


### 2. 프로젝 내용
- 랜덤으로 유닛을 소환하고, 몰려오는 적을 처치하며 스테이지를 진행하는 2D 디펜스 게임
- 적과 아군 유닛은 각각 3가지 종족이 있으며, 서로 상성이 존재함
- 9가지 아군 유닛 등급이 존재, 뽑기 시 랜덤하게 유닛 생성
- 업그레이드 버튼으로 특정 종족 유닛을 강화시킬 수 있음
- 10스테이지마다 보스 배치, 보다 체력이 높아 쉽게 처치할 수 없는 도전적인 스테이지

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :star2: 주요기능

### 1. 로비
![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/81b2f509-bcdf-4d22-ae7c-91f2d42e96ce)


- 게임시작 버튼을 누르면 디펜스 게임을 즐길 수 있습니다.
- 게임설명 버튼을 눌러서 게임의 조작법과 유닛, 몬스터 간의 상성을 확인할 수 있습니다.

### 2. 유닛 생성 및 배치
![md유닛 배치 및 이동하기](https://github.com/YooSeungA52/camp-week-10/assets/124012310/ae85c68c-6055-494e-bcd7-a57494bc89ce)


- 유닛 뽑기는 'E'키로 가능하며, 유닛은 드래그로 이동할 수 있습니다.
- 유닛마다 종족과 등급이 존재합니다.

### 3. 유닛 강화
![md유닛 업그레이드](https://github.com/YooSeungA52/camp-week-10/assets/124012310/c31ca258-75c1-409f-997c-895cd16bed8b)


- 강화에 필요한 골드를 소지하고 있다면 원하는 유닛을 강화시켜줄 수 있습니다.

### 4. 유닛 판매 및 일괄판매
![md유닛 판매 및 일괄판매](https://github.com/YooSeungA52/camp-week-10/assets/124012310/546dbe6d-624f-47a8-b82b-993c7e3ed513)


- 특정 유닛을 선택하여 판매할 수 있습니다.
- 종족 별로 D등급까지 일괄판매할 수 있습니다.

### 5. 스테이지 별 몬스터 등장
![md스테이지 별 몬스터 등장](https://github.com/YooSeungA52/camp-week-10/assets/124012310/7d6b2941-85a5-41aa-99fa-b38dfa31d31c)

- 스테이지가 변경되면 더 강력한 몬스터가 등장합니다.

### 6. 옵션창
![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/6876adc6-afd1-4cee-ab90-e6a1b05bc708)

- 뽑기 확률표, 상성표, 게임 다시시작, 로비로 이동할 수 있는 기능이 있습니다.

### 7. 소리 조절
![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/035239e5-efd9-4fda-a984-ee17684853d3)

- 로비와 게임 화면에서 소리를 조절할 수 있습니다.


<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :timer_clock: 프로젝트 수행 절차

| 구분 | 기간 | 활동 | 비고 |
| :---: | :---: | :---: | :---: |
| 사전 기획 | 06.19(수) | 프로젝트 기획 및 주제 선정, 프레임 워크 작성, 기획안 작성 | 프로젝트 기획 |
| 필수 기능 구현 | 06.19(수) ~ 06.21(금) | 유니티 및 GitHub 세팅, 필수적인 기본 기능 구현 | 기능 구현 |
| 추가 기능 구현 | 06.24(월) | 추가로 필요한 기능 구현, 버그 및 오류 최적화 작업, 빌드 테스트 | 기능 구현 |
| 프로젝트 정리 | 06.25(화) | 결과 보고서 작성, Read Me 작성, 최종 버그 및 오류 수정 |
| 리팩토링 및 발표 | 06.26(수) | 코드 리팩토링, 시연영상 촬영, 발표 준비 및 발표 |
| 총 개발 기간 | 6/19(수)~6/26(수) 총 1주 |

<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :spider_web: 와이어프레임

![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/aef7f568-f0dd-417d-964c-12645d0a02ff)

<br>

![Title Scene](https://github.com/YooSeungA52/camp-week-10/assets/124012310/fef0fe5d-4283-4f77-9f14-4c19eb38196d)


[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :notebook: UML
![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/8d231635-1e70-4941-aa2c-4dff7c7818cd)


<br>

[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>

## :ballot_box_with_check: 트러블 슈팅

![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/5c75bfa0-88ec-4cec-bdc8-520ebee1ac73)

<br>

![image](https://github.com/YooSeungA52/camp-week-10/assets/124012310/f15677c8-cbbd-4d48-8604-4684d3c7a411)


[:ringed_planet: 목차로 돌아가기](#목차)

<br><br>
