# Rebase All Branches

프로젝트의 모든 feature 브랜치를 main에 rebase하는 스크립트입니다.

## 사용 방법

### 방법 1: 스크립트 실행 (권장)

```bash
# 프로젝트 루트에서 실행
chmod +x scripts/rebase_all_branches.sh
./scripts/rebase_all_branches.sh
```

### 방법 2: 원라이너 (간단 실행)

```bash
git fetch origin && git checkout main && git pull origin main && for branch in $(git branch -r | grep -v '\->' | grep -v 'main' | sed 's/origin\///' | grep -E '^feature/|^fix/|^docs/|^refactor/|^csvs/'); do echo "Rebasing $branch..." && (git checkout "$branch" 2>/dev/null || git checkout -b "$branch" "origin/$branch") && git rebase main && git push origin "$branch" --force-with-lease || echo "Failed: $branch"; done && git checkout main
```

## 작동 방식

1. 원격 저장소에서 최신 정보를 가져옵니다
2. main 브랜치를 최신 상태로 업데이트합니다
3. 모든 feature/fix/docs/refactor/csvs 브랜치를 찾습니다
4. 각 브랜치를 main에 rebase합니다
5. rebase된 브랜치를 원격에 force push합니다
6. 원래 작업하던 브랜치로 돌아갑니다

## 주의사항

⚠️ **이 작업은 브랜치 히스토리를 변경합니다!**

- 작업 전에 로컬 저장소를 백업하세요
- `--force-with-lease`를 사용하여 안전하게 push합니다
- 충돌이 발생하면 해당 브랜치는 건너뛰고 다음 브랜치를 처리합니다
- 실패한 브랜치는 요약 정보에 표시됩니다

## 언제 사용하나요?

- 여러 feature 브랜치가 오래된 main 기반으로 생성되었을 때
- 모든 브랜치를 최신 main 상태로 동기화하고 싶을 때
- 작업을 시작하지 않은 빈 브랜치들을 정리할 때

## 예제 출력

```
==========================================
🔄 Rebase All Branches to Main
==========================================

📌 Current branch: main

📥 Fetching from origin...

🔄 Updating main branch...

📋 Found branches to rebase:
  ✓ feature/31-firebase-implement
  ✓ feature/32-basic-architecture-for-base-work
  ✓ feature/33-stage-tool

Continue with rebase? (y/n): y

🚀 Starting rebase process...

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔄 Processing: feature/31-firebase-implement
✅ Rebase successful: feature/31-firebase-implement
✅ Push successful: feature/31-firebase-implement

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 Summary
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Success: 3
❌ Failed: 0

✅ Done!
```
