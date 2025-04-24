# Car Insurance Telegram Bot

A Telegram bot that guides users through a car insurance process using OCR, AI-generated responses, and interactive steps.

---

## Prerequisites

- .NET 9.0 SDK
- Docker (optional)
- Tesseract OCR
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
  "TesseractDataPath": "PATH_TO_TESSDATA"
}
```

---

## Bot Workflow

1. **/start**: Presents `/insurance`.
2. **/insurance**: Initiates insurance flow and dialog with AI.

---

## Example User Flow

**User:** `/start`  
**Bot:** Welcome! Use `/insurance` to start or `/chat` to talk to AI.

**User:** `/insurance`  
**Bot:** Please send a photo of your passport.

**User:** *uploads passport image*  
**Bot:** Extracted info:
```
Name: Ivan Ivanov
Passport: 123456789
```
**Bot:** Is this correct? *(inline buttons Yes / No)*

**User:** *clicks Yes*  
**Bot:** The insurance price is 100 USD. Do you agree? *(Yes / No)*

**User:** *clicks Yes*  
**Bot:** Thank you! Generating your insurance policy...  
**Bot:**
```
---
📄 Insurance Policy

Policy Number: 3E821273
Name: Ivan Ivanov
Passport: 123456789
VIN: 12345678912345678
Insurance Type: Car
Price: 100 USD
Status: Active

Thank you for using our service! ❤️
---
```

---

## Rejection Scenarios

### Incorrect OCR Extraction
- **Bot:** "Extracted info... Is this correct?"
- **User:** clicks **No**
- **Bot:** "Please send another photo of your passport."  
  *User resends image → OCR retry*

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
