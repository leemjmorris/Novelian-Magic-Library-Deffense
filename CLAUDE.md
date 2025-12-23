# Novelian Project Guidelines

## Language
- always speak korean

## Component References
- Only use SerializedFields to get components with inspector. only use FindByTag if the serializedfield wouldnt work. never you other types. such as find(), findobjectoftype() findobjectsoftype(), findobjectsbytype() and e.t.c

## Async Operations
- Use UniTask instead of any other methods. never use other methods when you can use UniTask. if UniTask isnt the best solution. then ask me first before using other methods.

## Calibration Process (작업 전 보고)
작업을 시작하기 전에 반드시 다음 내용을 사용자에게 보고해야 합니다:
1. **객관적 분석**: 요청된 작업의 기술적 요구사항과 현재 코드베이스 상태
2. **주관적 의견**: 해당 접근 방식에 대한 개인적 견해
3. **업계 표준**: 실제 현업에서 이런 시스템을 만들 때 사용하는 일반적인 방식
4. **장점/단점 비교**: 제안하는 방식과 대안들의 Trade-off 분석
5. **권장 사항**: 최종 추천과 그 이유

## Verification Process (작업 후 검증)
작업 완료 후 반드시 다음 검증 단계를 자동으로 수행해야 합니다:
1. **컴파일 검사**: 문법 오류, 타입 오류 확인
2. **참조 검증**: 누락된 using문, 존재하지 않는 타입/메서드 호출 확인
3. **로직 검증**: 작성한 시스템이 의도대로 동작하는지 코드 흐름 추적
4. **엣지 케이스**: null 체크, 범위 초과, 예외 상황 처리 확인
5. **기존 코드 호환성**: 수정한 코드가 기존 호출부와 충돌하지 않는지 확인
6. **수정 사항 보고**: 발견된 문제와 수정 내역을 사용자에게 보고
