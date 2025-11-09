# Gemini-Powered Slack 알림 설정 가이드

## 🤖 개요

GitHub Issue가 생성/완료/재오픈될 때 **Gemini API**를 사용하여 Issue 내용을 요약하고, 보기 좋게 가공한 후 **Slack**으로 자동 전송하는 시스템입니다.

## 🎯 주요 기능

- ✨ Gemini API로 Issue 내용 자동 요약 (한국어)
- 📊 중요 정보 강조 (담당자, 레이블, 마일스톤)
- 🎨 이모지를 활용한 가독성 향상
- 🔔 실시간 Slack 알림
- 🔄 Issue 상태별 다른 알림 (생성/완료/재오픈)

---

## 📋 설정 단계

### 1단계: Gemini API 키 발급

1. [Google AI Studio](https://makersuite.google.com/app/apikey) 접속
2. "Get API Key" 클릭
3. 새 프로젝트 생성 또는 기존 프로젝트 선택
4. "Create API Key" 클릭
5. 생성된 API 키 복사 (한 번만 표시되니 안전한 곳에 저장!)

**API 키 예시**: `AIzaSyA...` (실제로는 훨씬 김)

---

### 2단계: Slack Webhook URL 생성

#### A. Slack App 생성
1. [Slack API](https://api.slack.com/apps) 접속
2. "Create New App" 클릭
3. "From scratch" 선택
4. App 이름 입력 (예: "GitHub Issue Bot")
5. Workspace 선택

#### B. Incoming Webhook 활성화
1. 좌측 메뉴에서 "Incoming Webhooks" 클릭
2. "Activate Incoming Webhooks" 토글 ON
3. "Add New Webhook to Workspace" 클릭
4. 알림을 받을 채널 선택 (예: `#dev-notifications`)
5. "Allow" 클릭
6. 생성된 Webhook URL 복사

**Webhook URL 예시**: 
```
https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXX
```

---

### 3단계: GitHub Secrets 등록

1. GitHub 레포지토리 접속
   ```
   https://github.com/leemjmorris/Novelian-Magic-Library-Deffense
   ```

2. **Settings** → **Secrets and variables** → **Actions** 클릭

3. **New repository secret** 클릭하여 다음 2개 secret 추가:

#### Secret 1: GEMINI_API_KEY
- Name: `GEMINI_API_KEY`
- Secret: [1단계에서 복사한 Gemini API 키 붙여넣기]
- "Add secret" 클릭

#### Secret 2: SLACK_WEBHOOK_URL
- Name: `SLACK_WEBHOOK_URL`
- Secret: [2단계에서 복사한 Slack Webhook URL 붙여넣기]
- "Add secret" 클릭

---

## 🚀 동작 방식

### Workflow 트리거
다음 상황에서 자동으로 실행됩니다:
- ✅ Issue 생성됨 (`opened`)
- ✅ Issue 완료됨 (`closed`)
- ✅ Issue 재오픈됨 (`reopened`)

### 처리 흐름
```
1. GitHub Issue 이벤트 발생
   ↓
2. Issue 정보 수집 (제목, 내용, 담당자, 레이블 등)
   ↓
3. Gemini API에 요약 요청
   ↓
4. Gemini가 한국어로 간결하게 요약
   ↓
5. Slack으로 포맷팅된 메시지 전송
   ↓
6. 팀원들이 Slack에서 알림 확인 ✨
```

### Slack 메시지 예시
```
🆕 *Issue #45 생성됨*

📝 **[요약]**
백로그 시스템에 우선순위 필터링 기능을 추가하는 작업입니다. 
사용자가 Critical/High/Medium/Low 우선순위로 이슈를 필터링할 수 있습니다.

👤 **담당:** @leemjmorris
🏷️ **레이블:** enhancement, priority: high
📅 **마일스톤:** Sprint 3

🔗 Issue 보기
👨‍💻 작성자: @leemjmorris
```

---

## 🎨 커스터마이징

### 요약 스타일 변경
워크플로우 파일(`.github/workflows/issue-to-slack-gemini.yml`)의 `prompt` 부분을 수정하세요:

```javascript
const prompt = `다음 GitHub Issue를 한국어로 간결하고 보기 좋게 요약해주세요.
...
요구사항:
1. 핵심 내용만 2-3문장으로 요약
2. 중요한 정보는 강조
3. 이모지 사용하여 가독성 향상
...`;
```

### 트리거 이벤트 변경
워크플로우의 `on` 섹션을 수정:

```yaml
on:
  issues:
    types: [opened, closed, reopened, labeled, assigned]
    # 원하는 이벤트 추가/제거
```

### Gemini 모델 변경
필요시 다른 Gemini 모델 사용 가능:
- `gemini-2.0-flash-exp` (현재 사용 중, 빠르고 효율적)
- `gemini-pro` (더 정교한 응답)

---

## 🔍 테스트 방법

### 1. 새 Issue 생성해보기
```
1. GitHub Issues → New issue
2. 제목: "테스트: Gemini-Slack 연동"
3. 내용: "Gemini API를 통한 자동 요약 테스트입니다."
4. Create issue 클릭
```

### 2. Slack 확인
- 설정한 채널에서 알림 확인
- 요약이 제대로 생성되었는지 확인
- 링크가 올바르게 작동하는지 확인

### 3. Workflow 로그 확인
문제가 있다면:
```
GitHub → Actions 탭 → "Issue to Slack (Gemini-powered)" 클릭
→ 최근 실행 내역 확인
→ 에러 로그 확인
```

---

## ⚠️ 주의사항

### API 사용량 제한
- **Gemini API**: 무료 티어는 분당 60 요청 제한
- **Slack Webhook**: 분당 1회 제한

### Secret 보안
- ❌ 절대 API 키를 코드에 직접 작성하지 말 것
- ✅ 항상 GitHub Secrets 사용
- ✅ Secret은 읽기 전용, 수정시 새로 생성 필요

### 비용
- Gemini API: 무료 티어 사용 (충분함)
- Slack: 무료

---

## 🛠️ 문제 해결

### Gemini API 호출 실패
```
Error: Failed to call Gemini API
```
**해결방법**:
1. API 키가 올바른지 확인
2. [Google Cloud Console](https://console.cloud.google.com/apis/library/generativelanguage.googleapis.com)에서 Generative Language API 활성화
3. API 사용량 제한 확인

### Slack 메시지 전송 실패
```
Error: Failed to send to Slack
```
**해결방법**:
1. Webhook URL이 올바른지 확인
2. Slack App이 채널에 초대되었는지 확인
3. Webhook이 활성화되어 있는지 확인

### Workflow가 실행되지 않음
**해결방법**:
1. GitHub Actions가 활성화되어 있는지 확인
2. Secrets가 올바르게 등록되었는지 확인
3. `.github/workflows/` 경로가 정확한지 확인

---

## 📚 관련 문서

- [Gemini API 문서](https://ai.google.dev/docs)
- [Slack Webhook 가이드](https://api.slack.com/messaging/webhooks)
- [GitHub Actions 문서](https://docs.github.com/en/actions)

---

## 💡 추가 아이디어

### 더 많은 자동화
- PR 생성/머지시에도 알림
- Daily summary (하루 Issue 요약)
- 특정 레이블에만 알림 (예: `priority: critical`)
- 멘션 기능 (담당자를 Slack에서 자동 멘션)

### 다른 AI 모델 사용
- OpenAI GPT-4
- Claude API
- Cohere

---

**문제가 있거나 질문이 있으면 언제든 Issue를 생성해주세요!** 🚀
