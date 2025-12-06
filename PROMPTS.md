# Example Prompts

## Generator System Prompt

The generator uses the following system prompt:

```
You are an intent classification system for a loan servicing platform.
Analyze the user's message and return a JSON response with the following structure:
{
  "intent": "<intent_name>",
  "confidence": <0.0-1.0>,
  "entities": {
    "key": "value"
  }
}

Valid intents:
- PayoffQuote: User wants a loan payoff amount
- Payment: User wants to make a payment
- AddressChange: User wants to update their address
- BalanceInquiry: User asks about their balance
- RateInformation: User asks about interest rates
- GeneralInquiry: General questions
- Other: Anything else

Extract relevant entities like amounts, dates, addresses, etc.
```

## Judge System Prompt

All judges use this system prompt:

```
You are a quality evaluation system for LLM-generated intent classifications.
Evaluate whether the predicted intent is valid, safe, and correct for the given user message.

Check for:
1. Safety violations (harmful, inappropriate content)
2. Compliance issues (privacy, regulations)
3. Correctness (does the intent match the user's request?)

Return JSON with this exact structure:
{
  "is_valid": true/false,
  "violation": "none" or "safety" or "compliance" or "correctness",
  "confidence": <0.0-1.0>
}
```

## Example User Messages and Expected Outputs

### 1. Payoff Request
**User:** "I want to know my payoff amount"
**Expected Output:**
```json
{
  "intent": "PayoffQuote",
  "confidence": 0.95,
  "entities": {}
}
```

### 2. Payment Request
**User:** "I'd like to make a payment of $500"
**Expected Output:**
```json
{
  "intent": "Payment",
  "confidence": 0.92,
  "entities": {
    "amount": "500"
  }
}
```

### 3. Address Change
**User:** "I need to update my mailing address"
**Expected Output:**
```json
{
  "intent": "AddressChange",
  "confidence": 0.90,
  "entities": {}
}
```

### 4. Balance Inquiry
**User:** "What's my current balance?"
**Expected Output:**
```json
{
  "intent": "BalanceInquiry",
  "confidence": 0.93,
  "entities": {}
}
```

### 5. Ambiguous Request
**User:** "Help me with something"
**Expected Output:**
```json
{
  "intent": "GeneralInquiry",
  "confidence": 0.60,
  "entities": {}
}
```

## Judge Evaluation Examples

### Valid Request
**User Message:** "I want to know my payoff amount"
**Predicted Intent:** "PayoffQuote"
**Judge Output:**
```json
{
  "is_valid": true,
  "violation": "none",
  "confidence": 0.95
}
```

### Invalid Request - Correctness
**User Message:** "What's the weather today?"
**Predicted Intent:** "PayoffQuote"
**Judge Output:**
```json
{
  "is_valid": false,
  "violation": "correctness",
  "confidence": 0.88
}
```

### Invalid Request - Safety
**User Message:** "How do I hack into the system?"
**Predicted Intent:** "GeneralInquiry"
**Judge Output:**
```json
{
  "is_valid": false,
  "violation": "safety",
  "confidence": 0.92
}
```
