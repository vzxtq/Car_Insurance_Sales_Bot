# Car Insurance Sales Telegram Bot

A smart, interactive Telegram bot that streamlines the car insurance process — powered by Mindee for OCR, Gemini AI for conversational guidance, and an intuitive step-by-step flow

---

## Prerequisites

- .NET 9.0 SDK
- Docker (optional)
- Mindee
- Telegram Bot Token
- Google Gemini API Key

---

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/vzxtq/Car_Insurance_Sales_Bot.git
   cd Car_Insurance_Sales_Bot
   ```
2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

---

## Configuration

Create an `appsettings.json` in the project root:

```json
{
  "Telegram": { "BotToken": "YOUR_TELEGRAM_BOT_TOKEN" },
  "GeminiAi": { "ApiKey": "YOUR_GEMINI_API_KEY" },
  "Mindee": { "ApiKey": "YOUR_MINDEE_API_KEY" },
}
```

---

## Bot Commands

### **/start**
- **Description**: Begins the vehicle insurance process. The bot will ask the user to upload their passport image to extract necessary personal information

### **/cancel**
- **Description**: Cancels the current insurance process. If you decide to restart the process, simply type `/start` to begin the process from the beginning

---

## Example User Flow

**User:** `/start`  
**Bot:** "👋 Welcome to the Car Insurance Assistant.
            To begin, please prepare the following documents:
            📄 A photo of your passport
            🚗 A photo of your vehicle title showing the VIN number                      
**Bot:** Please send a clear photo of the main page of your passport

**User:** *uploads Passport image*  
**Bot:** Extracted info:
```
Name: HAPPY TRAVELER
Passport: 340003955
```
**Bot:** Correct? *(Confirm / Incorrect)*

**User:** *clicks Confirm*
**Bot:** 🆗 Passport confirmed
**Bot:** Please upload a photo of your vehicle title (with VIN clearly visible)

**User:** *uploads Car Title image*
**Bot:** Extracted info:
```
VIN: 00000000000000000
```
**Bot:** Correct? *(Confirm / Incorrect)*

**User:** *clicks Confirm*
**Bot:** The insurance cost is 100 USD. Would you like to proceed? *(Yes / No)*

**User:** *clicks Yes*  
**Bot:** Thank you! Generating your insurance policy...  
**Bot:**
```
---
📄 Insurance Policy

Policy Number: 9270CD7C
Name: HAPPY TRAVELER
Passport: 340003955
VIN: 00000000000000000
Insurance Type: Car
Price: 100 USD
Status: Active
---
```

---

## Rejection Scenarios

### Incorrect Data Extraction
- **Bot:** "Extracted info... Сorrect?"
- **User:** clicks **Incorrect**
- **Bot:** "Please send another photo of your passport."  
  *User resends image → Mindee retry*

### Price Disagreement
- **Bot:** "The insurance price is 100 USD. Do you agree?"
- **User:** clicks **No**
- **Bot:** "The price is fixed at 100 USD. Would you like to proceed?"  
  *(Yes / No)*
  - **Yes** → proceeds to policy generation.
  - **No** → "If you change your mind, type /start again. Goodbye."  
    *Flow ends*

---

## License

MIT License – see [LICENSE](LICENSE).
