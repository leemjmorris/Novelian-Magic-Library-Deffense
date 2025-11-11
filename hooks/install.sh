#!/bin/bash

echo "🔧 Installing Git Hooks..."

# LMJ : Check if .git directory exists
if [ ! -d ".git" ]; then
    echo "❌ Error: .git directory not found. Run this script in the project root."
    exit 1
fi

# LMJ : Copy post-merge hook
if [ -f "hooks/post-merge" ]; then
    cp hooks/post-merge .git/hooks/post-merge
    chmod +x .git/hooks/post-merge
    echo "✅ Installed: post-merge hook (auto-delete stale local branches)"
else
    echo "❌ Error: hooks/post-merge not found"
    exit 1
fi

echo ""
echo "🎉 Git hooks installed successfully!"
echo ""
echo "📋 What this does:"
echo "   - When you pull/merge, automatically checks for stale local branches"
echo "   - Deletes local branches that have been deleted on remote"
echo "   - Keeps main/master and your current branch safe"
echo ""
echo "🧪 Test it:"
echo "   git pull"
