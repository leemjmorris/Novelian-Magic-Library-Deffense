#!/usr/bin/env python3
# LMJ : Sync GitHub issues and PRs to Notion database

import os
import json
import requests
from datetime import datetime

def sync_to_notion():
    notion_token = os.getenv('NOTION_API_TOKEN')
    database_id = os.getenv('NOTION_DATABASE_ID')
    event_data = json.loads(os.getenv('GITHUB_EVENT'))
    repository = os.getenv('GITHUB_REPOSITORY')
    action = os.getenv('EVENT_ACTION')
    
    if not notion_token or not database_id:
        print("Notion credentials not set")
        return
    
    # LMJ : Determine event type
    if 'issue' in event_data:
        sync_issue(notion_token, database_id, event_data, repository, action)
    elif 'pull_request' in event_data:
        sync_pr(notion_token, database_id, event_data, repository, action)

def sync_issue(notion_token, database_id, event_data, repository, action):
    issue = event_data['issue']
    
    # LMJ : Prepare Notion API headers
    headers = {
        'Authorization': f'Bearer {notion_token}',
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
    }
    
    # LMJ : Search for existing page
    search_url = f"https://api.notion.com/v1/databases/{database_id}/query"
    search_payload = {
        "filter": {
            "property": "Github URL",
            "url": {
                "equals": issue['html_url']
            }
        }
    }
    
    search_response = requests.post(search_url, headers=headers, json=search_payload)
    existing_pages = search_response.json().get('results', [])
    
    # LMJ : Map status
    status = '대기'
    if action == 'assigned':
        status = '진행중'
    elif action == 'closed':
        status = '완료'
    elif issue.get('state') == 'open':
        status = '진행중'
    
    # LMJ : Get labels
    labels = [label['name'] for label in issue.get('labels', [])]
    tag_list = []
    for label in labels:
        if label in ['bug', 'fix', 'enhancement', 'feature', 'documentation', 'docs', 'refactor', 'data', 'csvs']:
            tag_list.append({"name": label})
    
    # LMJ : Prepare page properties
    properties = {
        "제목": {
            "title": [
                {
                    "text": {
                        "content": f"#{issue['number']} {issue['title']}"
                    }
                }
            ]
        },
        "타입": {
            "select": {
                "name": "Issue"
            }
        },
        "상태": {
            "select": {
                "name": status
            }
        },
        "Github URL": {
            "url": issue['html_url']
        }
    }
    
    if tag_list:
        properties["태그"] = {
            "multi_select": tag_list
        }
    
    # LMJ : Create or update page
    if existing_pages:
        # Update existing page
        page_id = existing_pages[0]['id']
        update_url = f"https://api.notion.com/v1/pages/{page_id}"
        update_payload = {
            "properties": properties
        }
        response = requests.patch(update_url, headers=headers, json=update_payload)
    else:
        # Create new page
        create_url = "https://api.notion.com/v1/pages"
        create_payload = {
            "parent": {"database_id": database_id},
            "properties": properties
        }
        response = requests.post(create_url, headers=headers, json=create_payload)
    
    if response.status_code in [200, 201]:
        print(f"Successfully synced issue #{issue['number']} to Notion")
    else:
        print(f"Failed to sync to Notion: {response.status_code} - {response.text}")

def sync_pr(notion_token, database_id, event_data, repository, action):
    pr = event_data['pull_request']
    
    # LMJ : Prepare Notion API headers
    headers = {
        'Authorization': f'Bearer {notion_token}',
        'Content-Type': 'application/json',
        'Notion-Version': '2022-06-28'
    }
    
    # LMJ : Search for existing page
    search_url = f"https://api.notion.com/v1/databases/{database_id}/query"
    search_payload = {
        "filter": {
            "property": "Github URL",
            "url": {
                "equals": pr['html_url']
            }
        }
    }
    
    search_response = requests.post(search_url, headers=headers, json=search_payload)
    existing_pages = search_response.json().get('results', [])
    
    # LMJ : Map status
    status = '대기'
    if pr.get('merged'):
        status = '완료'
    elif pr.get('state') == 'closed':
        status = '보류'
    elif pr.get('state') == 'open':
        status = '진행중'
    
    # LMJ : Prepare page properties
    properties = {
        "제목": {
            "title": [
                {
                    "text": {
                        "content": f"PR #{pr['number']} {pr['title']}"
                    }
                }
            ]
        },
        "타입": {
            "select": {
                "name": "PR"
            }
        },
        "상태": {
            "select": {
                "name": status
            }
        },
        "Github URL": {
            "url": pr['html_url']
        }
    }
    
    # LMJ : Create or update page
    if existing_pages:
        # Update existing page
        page_id = existing_pages[0]['id']
        update_url = f"https://api.notion.com/v1/pages/{page_id}"
        update_payload = {
            "properties": properties
        }
        response = requests.patch(update_url, headers=headers, json=update_payload)
    else:
        # Create new page
        create_url = "https://api.notion.com/v1/pages"
        create_payload = {
            "parent": {"database_id": database_id},
            "properties": properties
        }
        response = requests.post(create_url, headers=headers, json=create_payload)
    
    if response.status_code in [200, 201]:
        print(f"Successfully synced PR #{pr['number']} to Notion")
    else:
        print(f"Failed to sync to Notion: {response.status_code} - {response.text}")

if __name__ == '__main__':
    sync_to_notion()