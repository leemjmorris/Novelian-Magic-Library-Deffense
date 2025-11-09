# 🤖 Gemini-Slack 연동 가이드

## 개요

이 가이드는 **Gemini API**를 활용하여 GitHub 이슈/PR 정보를 Slack으로 자동 전송하는 시스템 설정 방법을 설명합니다.

---

## 📋 목차

1. [사전 준비](#사전-준비)
2. [Gemini API 키 발급](#gemini-api-키-발급)
3. [Slack Webhook URL 생성](#slack-webhook-url-생성)
4. [GitHub Secrets 설정](#github-secrets-설정)
5. [워크플로우 동작 확인](#워크플로우-동작-확인)
6. [트러블슈팅](#트러블슈팅)

---

## 🛠️ 사전 준비

### 필요한 계정
- ✅ Google 계정 (Gemini API)
- ✅ Slack 워크스페이스 관리자 권한
- ✅ GitHub 레포지토리 관리자 권한

### 연동 구조

```
GitHub Issue/PR 생성
    ↓
GitHub Actions 트리거
    ↓
Gemini API 호출 (요약 생성)
    ↓
Slack Webhook 전송
    ↓
Slack 채널에 메시지 표시
```

---

## 🔑 Gemini API 키 발급

### 1. Google AI Studio 접속

1. [Google AI Studio](https://aistudio.google.com/) 접속
2. Google 계정으로 로그인

### 2. API 키 생성

1. 좌측 메뉴에서 **"Get API Key"** 클릭
2. **"Create API Key"** 버튼 클릭
3. 프로젝트 선택 (없으면 새로 생성)
4. API 키 복사 (한 번만 표시되므로 안전한 곳에 보관)

**API 키 형식:**
```
AIzaSy... (39자)
```

### 3. 사용량 확인

- [Google Cloud Console](https://console.cloud.google.com/apis/api/generativelanguage.googleapis.com)에서 사용량 모니터링
- 무료 할당량: 월 60회 요청 (2025년 기준)

---

## 📢 Slack Webhook URL 생성

### 1. Slack App 생성

1. [Slack API](https://api.slack.com/apps) 접속
2. **"Create New App"** → **"From scratch"** 선택
3. 앱 이름 입력 (예: `GitHub Notifications`)
4. 워크스페이스 선택

### 2. Incoming Webhooks 활성화

1. 좌측 메뉴 **"Incoming Webhooks"** 클릭
2. **"Activate Incoming Webhooks"** 토글 ON
3. 하단 **"Add New Webhook to Workspace"** 클릭
4. 메시지를 받을 채널 선택 (예: `#github-notifications`)
5. **"허용"** 클릭

### 3. Webhook URL 복사

**URL 형식:**
```
https://hooks.slack.com/services/T.../B.../...
```

⚠️ **보안 주의:** Webhook URL은 누구나 메시지를 보낼 수 있으므로 절대 공개하지 마세요!

---

## 🔐 GitHub Secrets 설정

### 1. 레포지토리 설정 페이지 접속

```
GitHub 레포지토리 → Settings → Secrets and variables → Actions
```

### 2. Secrets 추가

**"New repository secret"** 버튼을 클릭하여 아래 3개 Secret 추가:

#### (1) `GEMINI_API_KEY`
- **Name:** `GEMINI_API_KEY`
- **Secret:** `AIzaSy...` (발급받은 Gemini API 키)

#### (2) `SLACK_WEBHOOK_URL`
- **Name:** `SLACK_WEBHOOK_URL`
- **Secret:** `https://hooks.slack.com/services/...` (발급받은 Webhook URL)

#### (3) `NOTION_API_KEY` (선택사항)
- **Name:** `NOTION_API_KEY`
- **Secret:** `secret_...` (Notion 통합 시 필요)

### 3. 설정 확인

Secrets 목록에 다음이 표시되어야 합니다:
```
✅ GEMINI_API_KEY
✅ SLACK_WEBHOOK_URL
✅ NOTION_API_KEY (선택)
```

---

## ✅ 워크플로우 동작 확인

### 1. 테스트 이슈 생성

1. GitHub 레포지토리에서 새 이슈 생성
2. 제목: `[Test] Gemini-Slack 연동 테스트`
3. 내용: 간단한 설명 추가

### 2. GitHub Actions 확인

```
레포지토리 → Actions 탭
```

- **`Issue Notifications`** 워크플로우 실행 확인
- 성공 시: ✅ 녹색 체크
- 실패 시: ❌ 빨간 X (로그 확인)

### 3. Slack 채널 확인

Slack 채널에 다음과 같은 메시지가 표시되어야 합니다:

```
🆕 New Issue Created

[Test] Gemini-Slack 연동 테스트
#123

📝 AI Summary:
Gemini API와 Slack 연동 테스트를 위한 이슈입니다...

• Priority: Medium
• Milestone: Prototype
• Assigned: @leemjmorris

🔗 View Issue
```

---

## 🔧 워크플로우 커스터마이징

### Slack 메시지 포맷 수정

파일: `.github/workflows/issue-notifications.yml`

```yaml
- name: Send to Slack
  run: |
    python scripts/send_slack.py \
      --title "${{ github.event.issue.title }}" \
      --number "${{ github.event.issue.number }}" \
      --summary "$SUMMARY" \
      --priority "${{ github.event.issue.labels[0].name }}" \
      --url "${{ github.event.issue.html_url }}"
```

### Gemini 프롬프트 수정

파일: `scripts/summarize_with_gemini.py`

```python
prompt = f"""
다음 GitHub 이슈를 한국어로 요약해주세요:

제목: {title}
내용: {body}

요약 형식:
- 핵심 내용 (1-2문장)
- 주요 작업 항목
"""
```

---

## 🐛 트러블슈팅

### 1. Gemini API 오류

**증상:**
```
Error: API key not valid
```

**해결:**
- GitHub Secrets에서 `GEMINI_API_KEY` 확인
- API 키 유효성 확인 (39자)
- [Google AI Studio](https://aistudio.google.com/)에서 새 키 발급

### 2. Slack 메시지 전송 실패

**증상:**
```
Error: invalid_payload
```

**해결:**
- `SLACK_WEBHOOK_URL` 형식 확인
- Slack App이 채널에 초대되었는지 확인
- Webhook URL 재발급 시도

### 3. 워크플로우 실행 안 됨

**증상:**
- 이슈 생성해도 Actions 탭에 아무것도 없음

**해결:**
- `.github/workflows/` 디렉토리에 워크플로우 파일 있는지 확인
- YAML 문법 오류 확인 ([YAML Lint](https://www.yamllint.com/))
- `on:` 트리거 이벤트 확인

### 4. 중복 메시지 전송

**증상:**
- 하나의 이슈에 Slack 메시지가 여러 번 전송됨

**해결:**
- `issue-notifications.yml`의 `on.issues.types` 확인
- 불필요한 이벤트 제거 (`assigned`, `labeled` 등)

```yaml
on:
  issues:
    types: [opened]  # opened만 남기기
```

---

## 📊 모니터링

### GitHub Actions 사용량

```
레포지토리 → Settings → Billing → Actions
```

- 월 2,000분 무료 (Public 레포지토리는 무제한)

### Gemini API 사용량

```
Google Cloud Console → APIs & Services → Dashboard
```

- 일일/월간 사용량 그래프 확인

### Slack 메시지 히스토리

```
Slack 채널 → 메시지 검색
```

- `from:@GitHub Notifications` 로 필터링

---

## 🔗 관련 문서

- [GitHub Actions 문서](https://docs.github.com/en/actions)
- [Gemini API 문서](https://ai.google.dev/docs)
- [Slack API 문서](https://api.slack.com/)
- [백로그 관리 가이드](BACKLOG_GUIDE.md)

---

## 📞 지원

문제 발생 시:
1. GitHub Issues에 버그 리포트 작성
2. Slack `#tech-support` 채널에 문의
3. 팀 리더에게 직접 연락

---

**Last Updated**: 2025-11-09
