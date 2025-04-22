# Car Insurance Telegram Bot

A Telegram bot that guides users through a car insurance process using OCR, AI-generated responses, and interactive steps.

---

## 🚀 Setup Instructions

### 📦 Dependencies

Make sure you have the following installed:

- [.NET 9.0 SDK]
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) (ensure `tesseract` is installed and available in your PATH)
- A valid [Telegram Bot Token]
- Google Gemini API Key (for chat functionality)

### 🛠 Configuration

1. **Clone the repository:**

   ```bash
   git clone https://github.com/vzxtq/Car_Insurance_Sales_Bot.git
   ```

2. **Add `appsettings.json`:**

   Create an `appsettings.json` file in the root with your credentials:

   ```json
   {
     "Telegram": {
       "BotToken": "YOUR_TELEGRAM_BOT_TOKEN"
     },
     "Gemini": {
       "ApiKey": "YOUR_GEMINI_API_KEY"
     },
     "TesseractDataPath": "YOUR_PATH"
   }
   ```

3. **Restore dependencies and run:**

   ```bash
   dotnet restore
   dotnet run
   ```

---

## 🤖 Bot Workflow

The bot guides the user through the insurance process step-by-step:

1. **Start:**
   - Command: `/start`
   - Bot explains available options (`/insurance`, `/chat`)

2. **Insurance Flow:**
   - User sends `/insurance`
   - Bot asks for a passport photo
   - Photo is parsed via OCR to extract name and passport number
   - User confirms extracted data
   - Bot offers a fixed insurance price (100 USD)
   - User accepts or declines
   - If accepted, a generated insurance policy is sent

3. **AI Chat Mode:**
   - User sends `/chat`
   - Bot switches to chat mode using Gemini API

---

## 🔧 Example User Flow

**User:** /start  
**Bot:** Welcome! Use /insurance to start or /chat to talk to AI.

**User:** /insurance  
**Bot:** Please send a photo of your passport.

**User:** [uploads image]  
**Bot:** Extracted info:
```
Name: Ivan Ivanov
Passport: 123456789
```
Is this correct?

**User:** [Clicks "Yes"]  
**Bot:** The insurance price is 100 USD. Do you agree?

**User:** [Clicks "Yes"]  
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

## ⚙️ Technologies Used

- C# + .NET
- Telegram.Bot library
- Tesseract OCR
- Google Gemini API

---

## ✍️ License

MIT License