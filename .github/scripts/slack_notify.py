import os
import json
import requests

print("=== Slack Notification & Notion Sync Started ===")

# LMJ: Load environment variables
slack_webhook = os.environ.get("SLACK_WEBHOOK_URL")
notion_token = os.environ.get("NOTION_TOKEN")
database_id = os.environ.get("NOTION_DATABASE_ID")
mapping_json = os.environ.get("GITHUB_SLACK_MAPPING", "{}")
event = json.loads(os.environ["GITHUB_EVENT"])
repo = os.environ["GITHUB_REPOSITORY"]

# LMJ: Parse GitHub-Slack mapping
try:
    github_slack_map = json.loads(mapping_json)
except:
    github_slack_map = {}

# LMJ: Determine if it's an issue or PR
if "issue" in event and "pull_request" not in event["issue"]:
    item_type = "Issue"
    item = event["issue"]
    emoji = "🐛"
elif "pull_request" in event:
    item_type = "PR"
    item = event["pull_request"]
    emoji = "🔀"
else:
    print("Not an issue or PR, skipping")
    exit(0)

# LMJ: Extract data
title = item["title"]
body = item.get("body", "No description")
state = item["state"]
url = item["html_url"]
number = item["number"]
action = event["action"]
labels = [label["name"] for label in item.get("labels", [])]
assignees = item.get("assignees", [])

# LMJ: Status mapping
status_map = {
    "open": "진행중",
    "closed": "완료",
    "reopened": "재오픈"
}

action_map = {
    "opened": "생성됨",
    "closed": "완료됨",
    "reopened": "재오픈됨"
}

print(f"Processing {item_type} #{number}: {action}")

# LMJ: Sync to Notion Database
if notion_token and database_id:
    try:
        print("Syncing to Notion database...")
        
        notion_headers = {
            "Authorization": f"Bearer {notion_token}",
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json"
        }
        
        # LMJ: Check if issue already exists
        search_url = f"https://api.notion.com/v1/databases/{database_id}/query"
        search_body = {
            "filter": {
                "property": "제목",
                "title": {
                    "contains": f"#{number}"
                }
            }
        }
        
        search_response = requests.post(search_url, headers=notion_headers, json=search_body)
        
        if search_response.status_code == 200:
            results = search_response.json().get("results", [])
            
            if results and action != "opened":
                # LMJ: Update existing page
                page_id = results[0]["id"]
                update_url = f"https://api.notion.com/v1/pages/{page_id}"
                update_body = {
                    "properties": {
                        "상태": {
                            "select": {"name": status_map.get(state, "진행중")}
                        }
                    }
                }
                
                update_response = requests.patch(update_url, headers=notion_headers, json=update_body)
                if update_response.status_code == 200:
                    print(f"✅ Updated {item_type} #{number} in Notion")
                else:
                    print(f"⚠️ Notion update failed: {update_response.status_code}")
                    
            elif not results and action == "opened":
                # LMJ: Create new page
                create_url = "https://api.notion.com/v1/pages"
                
                # LMJ: Prepare page content
                page_children = []
                if body and len(body) > 0:
                    # Notion has 2000 char limit per text block
                    body_text = body[:2000]
                    page_children.append({
                        "object": "block",
                        "type": "paragraph",
                        "paragraph": {
                            "rich_text": [
                                {
                                    "type": "text",
                                    "text": {"content": body_text}
                                }
                            ]
                        }
                    })
                
                create_body = {
                    "parent": {"database_id": database_id},
                    "properties": {
                        "제목": {
                            "title": [
                                {
                                    "text": {
                                        "content": f"[{item_type} #{number}] {title}"
                                    }
                                }
                            ]
                        },
                        "타입": {
                            "select": {"name": item_type}
                        },
                        "상태": {
                            "select": {"name": status_map.get(state, "진행중")}
                        },
                        "태그": {
                            "multi_select": [{"name": label} for label in labels]
                        },
                        "GitHub URL": {
                            "url": url
                        }
                    },
                    "children": page_children
                }
                
                create_response = requests.post(create_url, headers=notion_headers, json=create_body)
                if create_response.status_code == 200:
                    print(f"✅ Created {item_type} #{number} in Notion")
                else:
                    print(f"⚠️ Notion create failed: {create_response.status_code} - {create_response.text}")
        else:
            print(f"⚠️ Notion query failed: {search_response.status_code}")
            
    except Exception as e:
        print(f"⚠️ Notion error: {e}")
else:
    print("Notion credentials not provided, skipping Notion sync")

# LMJ: Send Slack notification
if slack_webhook:
    try:
        print("Sending Slack notification...")
        
        # LMJ: Get assignees and convert to Slack mentions
        slack_mentions = []
        for assignee in assignees:
            github_username = assignee["login"]
            slack_id = github_slack_map.get(github_username)
            if slack_id:
                slack_mentions.append(f"<@{slack_id}>")
            else:
                slack_mentions.append(f"@{github_username}")

        mention_text = ", ".join(slack_mentions) if slack_mentions else "담당자 없음"
        action_text = action_map.get(action, action)

        # LMJ: Build Slack message
        body_preview = body[:300] if body else "설명 없음"
        
        message_text = f"{emoji} *{item_type} #{number} {action_text}*\n\n"
        message_text += f"*{title}*\n\n"
        message_text += f"📝 {body_preview}\n\n"
        message_text += f"👤 담당자: {mention_text}\n"
        message_text += f"🔗 {url}"

        # LMJ: Add mention at the beginning if assignees exist
        if slack_mentions:
            full_message = f"{' '.join(slack_mentions)}\n\n{message_text}"
        else:
            full_message = message_text

        slack_payload = {"text": full_message}

        # LMJ: Send to Slack
        response = requests.post(slack_webhook, json=slack_payload)
        if response.status_code == 200:
            print(f"✅ Slack notification sent for {item_type} #{number}")
        else:
            print(f"❌ Slack error: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"❌ Slack error: {e}")
else:
    print("Slack webhook not provided, skipping Slack notification")

print("=== Process Completed ===")