#!/bin/bash

echo "=========================================="
echo "🔄 Rebase All Branches to Main"
echo "=========================================="
echo ""

# 현재 브랜치 저장
CURRENT_BRANCH=$(git branch --show-current)
echo "📌 Current branch: $CURRENT_BRANCH"
echo ""

# 원격 저장소 최신 정보 가져오기
echo "📥 Fetching from origin..."
git fetch origin
echo ""

# main 브랜치 업데이트
echo "🔄 Updating main branch..."
git checkout main
git pull origin main
echo ""

# 모든 원격 브랜치 목록 가져오기 (main 제외)
BRANCHES=$(git branch -r | grep -v '\->' | grep -v 'main' | sed 's/origin\///' | grep -E '^feature/|^fix/|^docs/|^refactor/|^csvs/')

if [ -z "$BRANCHES" ]; then
    echo "❌ No branches found to rebase."
    git checkout "$CURRENT_BRANCH"
    exit 0
fi

echo "📋 Found branches to rebase:"
echo "$BRANCHES" | while read branch; do
    echo "  ✓ $branch"
done
echo ""

# 진행 확인
read -p "Continue with rebase? (y/n): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Aborted."
    git checkout "$CURRENT_BRANCH"
    exit 0
fi

echo ""
echo "🚀 Starting rebase process..."
echo ""

SUCCESS=0
FAILED=0

# 각 브랜치 rebase
echo "$BRANCHES" | while read branch; do
    if [ -z "$branch" ]; then
        continue
    fi
    
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "🔄 Processing: $branch"
    
    # 로컬 브랜치가 없으면 생성
    if git show-ref --verify --quiet refs/heads/"$branch"; then
        git checkout "$branch"
    else
        git checkout -b "$branch" "origin/$branch"
    fi
    
    # Rebase 실행
    if git rebase main; then
        echo "✅ Rebase successful: $branch"
        
        # Force push with lease (안전한 force push)
        if git push origin "$branch" --force-with-lease; then
            echo "✅ Push successful: $branch"
            SUCCESS=$((SUCCESS + 1))
        else
            echo "❌ Push failed: $branch"
            FAILED=$((FAILED + 1))
        fi
    else
        echo "❌ Rebase failed: $branch"
        echo "⚠️  Aborting rebase..."
        git rebase --abort
        FAILED=$((FAILED + 1))
    fi
    
    echo ""
done

# 원래 브랜치로 복귀
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Success: $SUCCESS"
echo "❌ Failed: $FAILED"
echo ""

echo "🔙 Returning to: $CURRENT_BRANCH"
git checkout "$CURRENT_BRANCH"

echo ""
echo "✅ Done!"
