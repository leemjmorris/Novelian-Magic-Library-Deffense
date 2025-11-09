#!/usr/bin/env python3
# LMJ : Send Slack notifications using Block Kit format

import os
import json
import requests
from datetime import datetime

def send_slack_notification():
    webhook_url = os.getenv('SLACK_WEBHOOK_URL')
    event_data = json.loads(os.getenv('GITHUB_EVENT'))
    repository = os.getenv('GITHUB_REPOSITORY')
    action = os.getenv('EVENT_ACTION')
    
    if not webhook_url:
        print("SLACK_WEBHOOK_URL is not set")
        return
    
    # LMJ : Determine event type
    if 'issue' in event_data:
        send_issue_notification(webhook_url, event_data, repository, action)
    elif 'pull_request' in event_data:
        send_pr_notification(webhook_url, event_data, repository, action)

def send_issue_notification(webhook_url, event_data, repository, action):
    issue = event_data['issue']
    
    # LMJ : Map action to Korean text
    action_text = {
        'opened': '새 Issue가 생성되었습니다',
        'closed': 'Issue가 닫혔습니다',
        'reopened': 'Issue가 다시 열렸습니다',
        'assigned': 'Issue에 담당자가 할당되었습니다',
        'labeled': 'Issue에 레이블이 추가되었습니다'
    }.get(action, f'Issue가 {action}되었습니다')
    
    # LMJ : Get emoji based on labels
    emoji = '📝'
    labels = [label['name'] for label in issue.get('labels', [])]
    if 'bug' in labels or 'fix' in labels:
        emoji = '🐛'
    elif 'enhancement' in labels or 'feature' in labels:
        emoji = '✨'
    elif 'documentation' in labels or 'docs' in labels:
        emoji = '📝'
    elif 'refactor' in labels:
        emoji = '🔧'
    elif 'data' in labels or 'csvs' in labels:
        emoji = '📊'
    elif 'meeting' in labels:
        emoji = '📅'
    elif 'feature-request' in labels:
        emoji = '💡'
    
    # LMJ : Get priority color
    color = '#808080'
    for label in labels:
        if 'Critical' in label:
            color = '#d73a4a'
        elif 'High' in label:
            color = '#fbca04'
        elif 'Medium' in label:
            color = '#0075ca'
        elif 'Low' in label:
            color = '#7cfc00'
    
    # LMJ : Build assignees text
    assignees_text = ', '.join([f"@{a['login']}" for a in issue.get('assignees', [])])
    if not assignees_text:
        assignees_text = '미할당'
    
    # LMJ : Build Block Kit message
    blocks = [
        {
            "type": "header",
            "text": {
                "type": "plain_text",
                "text": f"{emoji} {action_text}",
                "emoji": True
            }
        },
        {
            "type": "section",
            "fields": [
                {
                    "type": "mrkdwn",
                    "text": f"*Issue:*\n<{issue['html_url']}|#{issue['number']} {issue['title']}>"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*작성자:*\n@{issue['user']['login']}"
                }
            ]
        },
        {
            "type": "section",
            "fields": [
                {
                    "type": "mrkdwn",
                    "text": f"*담당자:*\n{assignees_text}"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*레이블:*\n{', '.join(labels) if labels else '없음'}"
                }
            ]
        },
        {
            "type": "divider"
        },
        {
            "type": "context",
            "elements": [
                {
                    "type": "mrkdwn",
                    "text": f"📌 Repository: {repository}"
                }
            ]
        }
    ]
    
    payload = {
        "blocks": blocks,
        "attachments": [
            {
                "color": color,
                "blocks": [
                    {
                        "type": "section",
                        "text": {
                            "type": "mrkdwn",
                            "text": issue.get('body', '설명 없음')[:500] + ('...' if len(issue.get('body', '')) > 500 else '')
                        }
                    }
                ]
            }
        ]
    }
    
    response = requests.post(webhook_url, json=payload)
    if response.status_code != 200:
        print(f"Failed to send Slack notification: {response.status_code} - {response.text}")
    else:
        print("Slack notification sent successfully")

def send_pr_notification(webhook_url, event_data, repository, action):
    pr = event_data['pull_request']
    
    # LMJ : Map action to Korean text
    action_text = {
        'opened': '새 Pull Request가 생성되었습니다',
        'closed': 'Pull Request가 닫혔습니다',
        'reopened': 'Pull Request가 다시 열렸습니다',
        'ready_for_review': 'Pull Request가 리뷰 대기 중입니다'
    }.get(action, f'Pull Request가 {action}되었습니다')
    
    emoji = '🔀'
    if pr.get('merged'):
        emoji = '✅'
        action_text = 'Pull Request가 머지되었습니다'
    elif action == 'closed' and not pr.get('merged'):
        emoji = '❌'
    
    # LMJ : Get status color
    color = '#0075ca'
    if pr.get('merged'):
        color = '#6f42c1'
    elif pr.get('draft'):
        color = '#808080'
    elif action == 'closed':
        color = '#d73a4a'
    
    # LMJ : Build Block Kit message
    blocks = [
        {
            "type": "header",
            "text": {
                "type": "plain_text",
                "text": f"{emoji} {action_text}",
                "emoji": True
            }
        },
        {
            "type": "section",
            "fields": [
                {
                    "type": "mrkdwn",
                    "text": f"*PR:*\n<{pr['html_url']}|#{pr['number']} {pr['title']}>"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*작성자:*\n@{pr['user']['login']}"
                }
            ]
        },
        {
            "type": "section",
            "fields": [
                {
                    "type": "mrkdwn",
                    "text": f"*브랜치:*\n`{pr['head']['ref']}` → `{pr['base']['ref']}`"
                },
                {
                    "type": "mrkdwn",
                    "text": f"*상태:*\n{'🟢 머지됨' if pr.get('merged') else '🟡 대기 중' if pr.get('state') == 'open' else '🔴 닫힘'}"
                }
            ]
        },
        {
            "type": "divider"
        },
        {
            "type": "context",
            "elements": [
                {
                    "type": "mrkdwn",
                    "text": f"📌 Repository: {repository}"
                }
            ]
        }
    ]
    
    payload = {
        "blocks": blocks,
        "attachments": [
            {
                "color": color,
                "blocks": [
                    {
                        "type": "section",
                        "text": {
                            "type": "mrkdwn",
                            "text": pr.get('body', '설명 없음')[:500] + ('...' if len(pr.get('body', '')) > 500 else '')
                        }
                    }
                ]
            }
        ]
    }
    
    response = requests.post(webhook_url, json=payload)
    if response.status_code != 200:
        print(f"Failed to send Slack notification: {response.status_code} - {response.text}")
    else:
        print("Slack notification sent successfully")

if __name__ == '__main__':
    send_slack_notification()