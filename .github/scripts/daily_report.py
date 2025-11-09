#!/usr/bin/env python3
# LMJ : Generate daily report using Gemini API and post to Notion

import os
import json
import requests
from datetime import datetime, timedelta
from github import Github
import google.generativeai as genai

def generate_daily_report():
    github_token = os.getenv('GITHUB_TOKEN')
    gemini_api_key = os.getenv('GEMINI_API_KEY')
    notion_token = os.getenv('NOTION_API_TOKEN')
    notion_page_id = os.getenv('NOTION_REPORT_PAGE_ID')
    slack_webhook = os.getenv('SLACK_WEBHOOK_URL')
    repository_name = os.getenv('GITHUB_REPOSITORY')
    
    if not all([github_token, gemini_api_key, notion_token, notion_page_id]):
        print("Missing required credentials")
        return
    
    # LMJ : Initialize GitHub API
    g = Github(github_token)
    repo = g.get_repo(repository_name)
    
    # LMJ : Get activities from last 24 hours
    since = datetime.utcnow() - timedelta(days=1)
    
    # LMJ : Collect issues activity
    issues_data = []
    issues = repo.get_issues(state='all', since=since)
    for issue in issues:
        if issue.pull_request:
            continue
        
        issue_info = {
            'number': issue.number,
            'title': issue.title,
            'state': issue.state,
            'labels': [label.name for label in issue.labels],
            'assignees': [assignee.login for assignee in issue.assignees],
            'created_at': issue.created_at.isoformat(),
            'updated_at': issue.updated_at.isoformat(),
            'body': issue.body or '',
            'comments_count': issue.comments
        }
        
        # LMJ : Get comments
        comments = []
        for comment in issue.get_comments(since=since):
            comments.append({
                'author': comment.user.login,
                'body': comment.body,
                'created_at': comment.created_at.isoformat()
            })
        issue_info['comments'] = comments
        
        issues_data.append(issue_info)
    
    # LMJ : Collect pull requests activity
    prs_data = []
    pulls = repo.get_pulls(state='all', sort='updated', direction='desc')
    for pr in pulls:
        if pr.updated_at < since:
            break
        
        pr_info = {
            'number': pr.number,
            'title': pr.title,
            'state': pr.state,
            'merged': pr.merged,
            'author': pr.user.login,
            'created_at': pr.created_at.isoformat(),
            'updated_at': pr.updated_at.isoformat(),
            'merged_at': pr.merged_at.isoformat() if pr.merged_at else None,
            'body': pr.body or '',
            'head_branch': pr.head.ref,
            'base_branch': pr.base.ref
        }
        prs_data.append(pr_info)
    
    # LMJ : Collect commits
    commits_data = []
    commits = repo.get_commits(since=since)
    for commit in commits:
        commit_info = {
            'sha': commit.sha[:7],
            'message': commit.commit.message,
            'author': commit.commit.author.name,
            'date': commit.commit.author.date.isoformat()
        }
        commits_data.append(commit_info)
    
    # LMJ : Prepare data summary for Gemini
    data_summary = {
        'date': datetime.now().strftime('%Y.%m.%d'),
        'issues': issues_data,
        'pull_requests': prs_data,
        'commits': commits_data,
        'team_members': [
            'leemjmorris', 'jaemoon23', 'LeeChaeBin002',
            'Kdwio', 'bigwaterplz', 'kimjiw8698-crypto'
        ]
    }
    
    # LMJ : Generate report using Gemini
    genai.configure(api_key=gemini_api_key)
    model = genai.GenerativeModel('gemini-1.5-pro')
    
    prompt = f"""
다음은 Novelian Magic Library Defense 프로젝트의 지난 24시간 동안의 GitHub 활동 데이터입니다.

```json
{json.dumps(data_summary, ensure_ascii=False, indent=2)}
```

위 데이터를 분석하여 다음 형식으로 일간 보고서를 작성해주세요:

# {data_summary['date']} 일간 보고

## 📊 오늘의 통계
- 생성된 Issue: X건
- 닫힌 Issue: X건
- 생성된 PR: X건
- 머지된 PR: X건
- 커밋 수: X개

## 🔥 Issue Handling 현황
(각 Issue를 분석하여 어떻게 처리되었는지 설명)

## ✨ 추가된 기능 및 변경사항
(머지된 PR과 커밋을 분석하여 새로운 기능이나 버그 수정 내용 요약)

## 👥 팀원별 작업 내역
### 프로그래머
- **이명진 (@leemjmorris)**: 
- **이재문 (@jaemoon23)**: 
- **이채빈 (@LeeChaeBin002)**: 

### 기획자
- **김동욱 (@Kdwio)**: 
- **김민휘 (@bigwaterplz)**: 
- **김지원 (@kimjiw8698-crypto)**: 

## ⚠️ 예상되는 문제점
(현재 진행 중인 작업을 분석하여 예상되는 문제점이나 블로커 파악)

## 💡 추천 사항
(팀의 생산성 향상을 위한 구체적인 제안)

---

**주의사항:**
1. 구체적이고 실용적인 내용으로 작성해주세요.
2. 데이터가 없는 경우 "활동 없음"이라고 표기해주세요.
3. Issue와 PR 번호를 명확히 포함해주세요.
4. 팀원별 작업은 각 팀원이 기여한 Issue, PR, 커밋을 기반으로 작성해주세요.
5. 예상 문제점과 추천사항은 기술적 관점과 프로젝트 관리 관점에서 모두 고려해주세요.
"""
    
    try:
        response = model.generate_content(prompt)
        report_content = response.text
        print("Report generated successfully")
    except Exception as e:
        print(f"Failed to generate report: {e}")
        report_content = f"# {data_summary['date']} 일간 보고\n\n리포트 생성 실패: {str(e)}"
    
    # LMJ : Post to Notion
    post_to_notion(notion_token, notion_page_id, data_summary['date'], report_content)
    
    # LMJ : Send Slack notification
    if slack_webhook:
        send_slack_summary(slack_webhook, data_summary, report_content)

def post_to_notion(notion_token, parent_page_id, date, content):
    # LMJ : Prepare Notion API headers
    headers = {
        'Authorization': f'Bearer {notion_token}',
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
    }
    
    # LMJ : Create page title
    page_title = f"{date} 일간 보고"
    
    # LMJ : Convert markdown content to Notion blocks
    blocks = markdown_to_notion_blocks(content)
    
    # LMJ : Create new page
    create_url = "https://api.notion.com/v1/pages"
    create_payload = {
        "parent": {"page_id": parent_page_id},
        "properties": {
            "title": {
                "title": [
                    {
                        "text": {
                            "content": page_title
                        }
                    }
                ]
            }
        },
        "children": blocks
    }
    
    response = requests.post(create_url, headers=headers, json=create_payload)
    
    if response.status_code == 200:
        print(f"Successfully posted daily report to Notion")
        return response.json()
    else:
        print(f"Failed to post to Notion: {response.status_code} - {response.text}")
        return None

def markdown_to_notion_blocks(markdown_content):
    # LMJ : Simple markdown to Notion blocks conversion
    blocks = []
    lines = markdown_content.split('\n')
    
    for line in lines:
        line = line.strip()
        if not line:
            continue
        
        # Headings
        if line.startswith('# '):
            blocks.append({
                "object": "block",
                "type": "heading_1",
                "heading_1": {
                    "rich_text": [{"type": "text", "text": {"content": line[2:]}}]
                }
            })
        elif line.startswith('## '):
            blocks.append({
                "object": "block",
                "type": "heading_2",
                "heading_2": {
                    "rich_text": [{"type": "text", "text": {"content": line[3:]}}]
                }
            })
        elif line.startswith('### '):
            blocks.append({
                "object": "block",
                "type": "heading_3",
                "heading_3": {
                    "rich_text": [{"type": "text", "text": {"content": line[4:]}}]
                }
            })
        elif line.startswith('- '):
            blocks.append({
                "object": "block",
                "type": "bulleted_list_item",
                "bulleted_list_item": {
                    "rich_text": [{"type": "text", "text": {"content": line[2:]}}]
                }
            })
        elif line.startswith('---'):
            blocks.append({
                "object": "block",
                "type": "divider",
                "divider": {}
            })
        else:
            blocks.append({
                "object": "block",
                "type": "paragraph",
                "paragraph": {
                    "rich_text": [{"type": "text", "text": {"content": line}}]
                }
            })
    
    return blocks

def send_slack_summary(webhook_url, data_summary, report_preview):
    # LMJ : Send brief summary to Slack
    issues_count = len(data_summary['issues'])
    prs_count = len(data_summary['pull_requests'])
    commits_count = len(data_summary['commits'])
    
    # LMJ : Get first 500 chars of report as preview
    preview = report_preview[:500] + '...' if len(report_preview) > 500 else report_preview
    
    blocks = [
        {
            "type": "header",
            "text": {
                "type": "plain_text",
                "text": f"📄 {data_summary['date']} 일간 보고서 생성 완료",
                "emoji": True
            }
        },
        {
            "type": "section",
            "fields": [
                {
                    "type": "mrkdwn",
                    "text": f"*Issue:* {issues_count}건"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*PR:* {prs_count}건"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*Commits:* {commits_count}개"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*일자:* {data_summary['date']}"
                }
            ]
        },
        {
            "type": "divider"
        },
        {
            "type": "section",
            "text": {
                "type": "mrkdwn",
                "text": f"📖 *보고서 미리보기:*\n{preview}"
            }
        },
        {
            "type": "context",
            "elements": [
                {
                    "type": "mrkdwn",
                    "text": "📁 전체 보고서는 Notion에서 확인하세요."
                }
            ]
        }
    ]
    
    payload = {"blocks": blocks}
    requests.post(webhook_url, json=payload)
    print("Slack summary sent")

if __name__ == '__main__':
    generate_daily_report()