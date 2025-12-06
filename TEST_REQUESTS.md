# Test Requests

## Sample cURL Commands

### 1. Payoff Quote Request
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I want to know my payoff amount",
    "sessionId": "session-001"
  }'
```

### 2. Payment Request
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I need to make a payment of $500",
    "sessionId": "session-002"
  }'
```

### 3. Address Change Request
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I moved to a new house and need to update my address",
    "sessionId": "session-003"
  }'
```

### 4. Balance Inquiry
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What is my current loan balance?",
    "sessionId": "session-004"
  }'
```

### 5. Low Confidence Request
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Help me",
    "sessionId": "session-005"
  }'
```

### 6. Ambiguous Request
```bash
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What are my options?",
    "sessionId": "session-006"
  }'
```

### 7. Multiple Sequential Requests (Drift Testing)
```bash
# First request - PayoffQuote
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "I want to know my payoff amount",
    "sessionId": "session-drift-001"
  }'

# Second request - Payment (valid transition)
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Now I want to make a payment",
    "sessionId": "session-drift-001"
  }'

# Third request - Unrelated (might trigger drift)
curl -X POST https://localhost:5001/api/infer \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Tell me about the weather",
    "sessionId": "session-drift-001"
  }'
```

## PowerShell Test Script

```powershell
# Test all endpoints
$baseUrl = "https://localhost:5001/api"

# Test 1: Payoff Quote
$body1 = @{
    message = "I want to know my payoff amount"
    sessionId = "ps-session-001"
} | ConvertTo-Json

$response1 = Invoke-RestMethod -Uri "$baseUrl/infer" -Method Post -Body $body1 -ContentType "application/json"
Write-Host "Test 1 - Payoff Quote:" -ForegroundColor Green
$response1 | ConvertTo-Json -Depth 10

# Test 2: Payment
$body2 = @{
    message = "I need to make a payment of $500"
    sessionId = "ps-session-002"
} | ConvertTo-Json

$response2 = Invoke-RestMethod -Uri "$baseUrl/infer" -Method Post -Body $body2 -ContentType "application/json"
Write-Host "`nTest 2 - Payment:" -ForegroundColor Green
$response2 | ConvertTo-Json -Depth 10

# Test 3: Address Change
$body3 = @{
    message = "I moved to a new house and need to update my address"
    sessionId = "ps-session-003"
} | ConvertTo-Json

$response3 = Invoke-RestMethod -Uri "$baseUrl/infer" -Method Post -Body $body3 -ContentType "application/json"
Write-Host "`nTest 3 - Address Change:" -ForegroundColor Green
$response3 | ConvertTo-Json -Depth 10

# Health Check
$health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
Write-Host "`nHealth Check:" -ForegroundColor Green
$health | ConvertTo-Json -Depth 10
```

## Expected Responses

### Successful Inference (No Drift, Judge Passes)
```json
{
  "intent": "PayoffQuote",
  "confidence": 0.92,
  "judgeValid": true,
  "driftDetected": false,
  "variantUsed": "gpt4o-mini",
  "judgeSkipped": false,
  "routingStrategy": "sensitive-only"
}
```

### Judge Skipped (Low Risk)
```json
{
  "intent": "GeneralInquiry",
  "confidence": 0.85,
  "judgeValid": true,
  "driftDetected": false,
  "variantUsed": "skipped",
  "judgeSkipped": true,
  "routingStrategy": "sensitive-only"
}
```

### Judge Rejected (Requires Review)
```json
{
  "intent": "RequiresReview",
  "confidence": 0.70,
  "judgeValid": false,
  "driftDetected": false,
  "variantUsed": "gpt4o-mini",
  "judgeSkipped": false,
  "routingStrategy": "always"
}
```

### Drift Detected
```json
{
  "intent": "Other",
  "confidence": 0.65,
  "judgeValid": true,
  "driftDetected": true,
  "variantUsed": "gpt4o-mini",
  "judgeSkipped": false,
  "routingStrategy": "low-confidence"
}
```
