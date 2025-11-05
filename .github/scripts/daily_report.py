import os
import json
import requests
from datetime import datetime, timedelta

print("=== Daily Report Generation Started ===")

# LMJ: Load environment variables
notion_token = os.environ["NOTION_TOKEN"]
report_page_id = os.environ["NOTION_REPORT_PAGE_ID"]
gemini_api_key = os.environ["GEMINI_API_KEY"]
github_token = os.environ["GITHUB_TOKEN"]
repo = os.environ["GITHUB_REPOSITORY"]
slack_webhook = os.environ.get("SLACK_WEBHOOK_URL")

# LMJ: Calculate yesterday's date
now = datetime.utcnow()
yesterday = now - timedelta(days=1)
yesterday_start = yesterday.replace(hour=0, minute=0, second=0, microsecond=0)
yesterday_end = yesterday.replace(hour=23, minute=59, second=59, microsecond=999999)

print(f"Collecting issues from {yesterday_start} to {yesterday_end}")

# LMJ: Fetch issues from GitHub
headers = {
    "Authorization": f"token {github_token}",
    "Accept": "application/vnd.github.v3+json"
}

issues_url = f"https://api.github.com/repos/{repo}/issues"
params = {
    "state": "all",
    "since": yesterday_start.isoformat() + "Z",
    "per_page": 100
}

response = requests.get(issues_url, headers=headers, params=params)
if response.status_code != 200:
    print(f"❌ GitHub API error: {response.status_code}")
    exit(1)

all_issues = response.json()

# LMJ: Filter new issues created yesterday
new_issues = []
for issue in all_issues:
    created_at = datetime.strptime(issue["created_at"], "%Y-%m-%dT%H:%M:%SZ")
    if yesterday_start <= created_at <= yesterday_end:
        if "pull_request" not in issue:
            new_issues.append(issue)

# LMJ: Filter completed issues yesterday
completed_issues = []
for issue in all_issues:
    if issue.get("closed_at"):
        closed_at = datetime.strptime(issue["closed_at"], "%Y-%m-%dT%H:%M:%SZ")
        if yesterday_start <= closed_at <= yesterday_end:
            if "pull_request" not in issue:
                completed_issues.append(issue)

# LMJ: Get all open issues for "in progress" count
open_params = {"state": "open", "per_page": 100}
open_response = requests.get(issues_url, headers=headers, params=open_params)
all_open = [i for i in open_response.json() if "pull_request" not in i] if open_response.status_code == 200 else []

print(f"New: {len(new_issues)}, Completed: {len(completed_issues)}, In Progress: {len(all_open)}")

if len(new_issues) == 0 and len(completed_issues) == 0:
    print("No issues to report")
    exit(0)

# LMJ: Classify issues by priority
def classify_issue(issue):
    labels = [label['name'].lower() for label in issue.get('labels', [])]
    if any(x in labels for x in ['critical', 'urgent', 'priority-critical']):
        return 'critical'
    elif any(x in labels for x in ['bug', 'high', 'priority-high']):
        return 'major'
    else:
        return 'normal'

critical_issues = [i for i in new_issues if classify_issue(i) == 'critical']
major_issues = [i for i in new_issues if classify_issue(i) == 'major']
normal_issues = [i for i in new_issues if classify_issue(i) == 'normal']

# LMJ: Prepare issue summary for Gemini
issues_summary = f"""신규 이슈 {len(new_issues)}건:
긴급: {len(critical_issues)}건
주요: {len(major_issues)}건
일반: {len(normal_issues)}건

완료된 이슈: {len(completed_issues)}건
"""

for issue in new_issues[:5]:  # Sample first 5
    issues_summary += f"\n- #{issue['number']}: {issue['title']}"
    labels = [label['name'] for label in issue.get('labels', [])]
    if labels:
        issues_summary += f" [라벨: {', '.join(labels)}]"

# LMJ: Generate analysis using Gemini
gemini_url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={gemini_api_key}"

prompt = f"""다음은 어제({yesterday.strftime('%Y년 %m월 %d일')}) 개발팀의 이슈 현황입니다:

{issues_summary}

다음 두 가지만 간결하게 작성해주세요:

1. 📈 트렌드 분석 (2-3문장)
   - 이슈 발생 패턴, 주요 카테고리, 특이사항 분석

2. 💬 코멘트 (2-3문장)
   - 팀에게 권장하는 우선순위와 조치사항

전문적이고 간결하게 한국어로 작성해주세요."""

gemini_payload = {
    "contents": [{
        "parts": [{
            "text": prompt
        }]
    }]
}

trend_analysis = ""
comment = ""

try:
    response = requests.post(gemini_url, json=gemini_payload)
    if response.status_code == 200:
        result = response.json()
        ai_response = result["candidates"][0]["content"]["parts"][0]["text"]
        
        # LMJ: Parse AI response
        if "📈 트렌드 분석" in ai_response and "💬 코멘트" in ai_response:
            parts = ai_response.split("💬 코멘트")
            trend_analysis = parts[0].replace("📈 트렌드 분석", "").strip()
            comment = parts[1].strip()
        else:
            trend_analysis = ai_response[:200]
            comment = "금일 이슈에 대한 신속한 대응을 권장합니다."
        
        print("✅ Gemini analysis generated")
    else:
        print(f"⚠️ Gemini API error: {response.status_code}")
        trend_analysis = "금일 이슈 발생 패턴 분석 중입니다."
        comment = "각 이슈에 대한 우선순위 검토를 권장합니다."
except Exception as e:
    print(f"⚠️ Gemini error: {e}")
    trend_analysis = "금일 이슈 발생 패턴 분석 중입니다."
    comment = "각 이슈에 대한 우선순위 검토를 권장합니다."

# LMJ: Create Notion page
notion_headers = {
    "Authorization": f"Bearer {notion_token}",
    "Notion-Version": "2022-06-28",
    "Content-Type": "application/json"
}

page_title = f"📅 {yesterday.strftime('%Y년 %m월 %d일')} 개발 현황 보고"

# LMJ: Build page content
children = []

# Header divider
children.append({
    "object": "block",
    "type": "divider",
    "divider": {}
})

# Summary section
children.append({
    "object": "block",
    "type": "heading_2",
    "heading_2": {
        "rich_text": [{"type": "text", "text": {"content": "📊 요약"}}]
    }
})

children.append({
    "object": "block",
    "type": "bulleted_list_item",
    "bulleted_list_item": {
        "rich_text": [{"type": "text", "text": {"content": f"신규 이슈: {len(new_issues)}건"}}]
    }
})

children.append({
    "object": "block",
    "type": "bulleted_list_item",
    "bulleted_list_item": {
        "rich_text": [{"type": "text", "text": {"content": f"완료된 이슈: {len(completed_issues)}건"}}]
    }
})

children.append({
    "object": "block",
    "type": "bulleted_list_item",
    "bulleted_list_item": {
        "rich_text": [{"type": "text", "text": {"content": f"진행 중: {len(all_open)}건"}}]
    }
})

# LMJ: Add critical issues section
if critical_issues:
    children.append({
        "object": "block",
        "type": "heading_2",
        "heading_2": {
            "rich_text": [{"type": "text", "text": {"content": "🚨 긴급 이슈 (즉시 처리 필요)"}}]
        }
    })
    
    for idx, issue in enumerate(critical_issues, 1):
        assignees = [a["login"] for a in issue.get("assignees", [])]
        assignee_text = f" [담당: @{', @'.join(assignees)}]" if assignees else " [담당자 없음]"
        
        children.append({
            "object": "block",
            "type": "numbered_list_item",
            "numbered_list_item": {
                "rich_text": [
                    {"type": "text", "text": {"content": f"#{issue['number']} - {issue['title']}", "link": {"url": issue['html_url']}}},
                    {"type": "text", "text": {"content": assignee_text}}
                ]
            }
        })

# LMJ: Add major issues section
if major_issues:
    children.append({
        "object": "block",
        "type": "heading_2",
        "heading_2": {
            "rich_text": [{"type": "text", "text": {"content": "⚠️ 주요 이슈"}}]
        }
    })
    
    for issue in major_issues:
        assignees = [a["login"] for a in issue.get("assignees", [])]
        assignee_text = f" [담당: @{', @'.join(assignees)}]" if assignees else " [담당자 없음]"
        
        children.append({
            "object": "block",
            "type": "numbered_list_item",
            "numbered_list_item": {
                "rich_text": [
                    {"type": "text", "text": {"content": f"#{issue['number']} - {issue['title']}", "link": {"url": issue['html_url']}}},
                    {"type": "text", "text": {"content": assignee_text}}
                ]
            }
        })

# LMJ: Add normal issues section
if normal_issues:
    children.append({
        "object": "block",
        "type": "heading_2",
        "heading_2": {
            "rich_text": [{"type": "text", "text": {"content": "📝 일반 이슈"}}]
        }
    })
    
    for issue in normal_issues:
        assignees = [a["login"] for a in issue.get("assignees", [])]
        assignee_text = f" [담당: @{', @'.join(assignees)}]" if assignees else " [담당자 없음]"
        
        children.append({
            "object": "block",
            "type": "numbered_list_item",
            "numbered_list_item": {
                "rich_text": [
                    {"type": "text", "text": {"content": f"#{issue['number']} - {issue['title']}", "link": {"url": issue['html_url']}}},
                    {"type": "text", "text": {"content": assignee_text}}
                ]
            }
        })

# LMJ: Add trend analysis
children.append({
    "object": "block",
    "type": "heading_2",
    "heading_2": {
        "rich_text": [{"type": "text", "text": {"content": "📈 트렌드 분석"}}]
    }
})

children.append({
    "object": "block",
    "type": "paragraph",
    "paragraph": {
        "rich_text": [{"type": "text", "text": {"content": trend_analysis}}]
    }
})

# LMJ: Add comment
children.append({
    "object": "block",
    "type": "heading_2",
    "heading_2": {
        "rich_text": [{"type": "text", "text": {"content": "💬 코멘트"}}]
    }
})

children.append({
    "object": "block",
    "type": "paragraph",
    "paragraph": {
        "rich_text": [{"type": "text", "text": {"content": comment}}]
    }
})

# LMJ: Create page
create_page_url = "https://api.notion.com/v1/pages"
page_data = {
    "parent": {"page_id": report_page_id},
    "properties": {
        "title": {
            "title": [{"text": {"content": page_title}}]
        }
    },
    "children": children
}

notion_page_url = None
try:
    response = requests.post(create_page_url, headers=notion_headers, json=page_data)
    if response.status_code == 200:
        page_id = response.json()["id"]
        notion_page_url = f"https://notion.so/{page_id.replace('-', '')}"
        print(f"✅ Daily report created: {notion_page_url}")
    else:
        print(f"❌ Notion API error: {response.status_code} - {response.text}")
        exit(1)
except Exception as e:
    print(f"❌ Error creating Notion page: {e}")
    exit(1)

# LMJ: Send Slack notification
if slack_webhook and notion_page_url:
    try:
        slack_message = f"📅 *{yesterday.strftime('%Y년 %m월 %d일')} 일간 보고서*가 생성되었습니다.\n\n"
        slack_message += f"📊 신규 {len(new_issues)}건 | 완료 {len(completed_issues)}건 | 진행중 {len(all_open)}건\n\n"
        slack_message += f"🔗 <{notion_page_url}|보고서 보기>"
        
        slack_payload = {"text": slack_message}
        slack_response = requests.post(slack_webhook, json=slack_payload)
        
        if slack_response.status_code == 200:
            print("✅ Slack notification sent")
        else:
            print(f"⚠️ Slack notification failed: {slack_response.status_code}")
    except Exception as e:
        print(f"⚠️ Slack error: {e}")

print("=== Daily Report Generation Completed ===")