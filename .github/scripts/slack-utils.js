/**
 * Shared Slack utility functions for GitHub Actions workflows
 */

const https = require('https');
const fs = require('fs');
const path = require('path');

// Load user mapping
const userMappingPath = path.join(__dirname, 'user-mapping.json');
const userMapping = JSON.parse(fs.readFileSync(userMappingPath, 'utf8'));

/**
 * Get Slack user ID from GitHub username
 * @param {string} githubUsername - GitHub username
 * @returns {string|null} Slack user ID or null if not found
 */
function getSlackUserId(githubUsername) {
  return userMapping[githubUsername]?.slackId || null;
}

/**
 * Get Korean name from GitHub username
 * @param {string} githubUsername - GitHub username
 * @returns {string} Korean name or GitHub username if not found
 */
function getKoreanName(githubUsername) {
  return userMapping[githubUsername]?.koreanName || githubUsername;
}

/**
 * Get Slack mention string from GitHub username
 * @param {string} githubUsername - GitHub username
 * @returns {string} Slack mention string (e.g., '<@U123456>') or GitHub username
 */
function getSlackMention(githubUsername) {
  const slackId = getSlackUserId(githubUsername);
  return slackId ? `<@${slackId}>` : githubUsername;
}

/**
 * Send a message to Slack webhook
 * @param {object} message - Slack message object
 * @param {string} webhookUrl - Slack webhook URL (optional, uses env var if not provided)
 * @returns {Promise<string>} Response body or 'skipped'
 */
function sendToSlack(message, webhookUrl = null) {
  return new Promise((resolve, reject) => {
    const url = webhookUrl || process.env.SLACK_WEBHOOK_URL;

    if (!url) {
      console.log('⚠️ SLACK_WEBHOOK_URL not set, skipping Slack notification');
      resolve('skipped');
      return;
    }

    const data = JSON.stringify(message);
    const urlObj = new URL(url);

    const options = {
      hostname: urlObj.hostname,
      path: urlObj.pathname + urlObj.search,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
        'Content-Length': Buffer.byteLength(data)
      }
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => {
        if (res.statusCode === 200 && body === 'ok') {
          resolve(body);
        } else {
          reject(new Error(`Slack API error: ${res.statusCode} - ${body}`));
        }
      });
    });

    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

/**
 * Sanitize text for Slack (escape special characters)
 * @param {string} text - Text to sanitize
 * @param {number} maxLength - Maximum length (default: 2000)
 * @returns {string} Sanitized text
 */
function sanitizeForSlack(text, maxLength = 2000) {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\n{3,}/g, '\n\n')
    .trim()
    .substring(0, maxLength);
}

module.exports = {
  userMapping,
  getSlackUserId,
  getKoreanName,
  getSlackMention,
  sendToSlack,
  sanitizeForSlack
};
