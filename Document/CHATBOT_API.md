# 🤖 DrHan AI Chatbot API Documentation

## Tổng quan
DrHan AI Chatbot là hệ thống trí tuệ nhân tạo được tích hợp vào ứng dụng DrHan để hỗ trợ người dùng Việt Nam về:
- **Quản lý dị ứng thực phẩm**: Thông tin về triệu chứng, thực phẩm cần tránh, thay thế an toàn
- **Lập kế hoạch bữa ăn**: Tư vấn thực đơn phù hợp với tình trạng dị ứng
- **Hướng dẫn sử dụng app**: Giải đáp về tính năng và cách sử dụng ứng dụng
- **Tư vấn chung**: Hỗ trợ các câu hỏi về dinh dưỡng và an toàn thực phẩm

## 🚀 Tính năng chính

### ✅ AI Thông minh
- Sử dụng Google Gemini AI với prompts được tối ưu cho người Việt
- Phân tích intent tự động để hiểu ý định người dùng
- Cung cấp phản hồi chính xác dựa trên knowledge base về dị ứng thực phẩm

### 💬 Quản lý cuộc trò chuyện
- Lưu trữ lịch sử chat với Redis cache
- Duy trì ngữ cảnh cuộc trò chuyện
- Hỗ trợ multiple conversations đồng thời

### 🎯 Phân loại thông minh
- **Allergy**: Câu hỏi về dị ứng thực phẩm
- **Mealplan**: Lập kế hoạch bữa ăn và công thức
- **App Help**: Hướng dẫn sử dụng ứng dụng
- **General**: Câu hỏi chung về dinh dưỡng

### 🆘 Xử lý khẩn cấp
- Phát hiện tình huống khẩn cấp (khó thở, sưng, shock)
- Khuyến nghị liên hệ y tế ngay lập tức
- Cung cấp hướng dẫn sơ cứu cơ bản

## 📋 API Endpoints

### 1. Chat với AI
**POST** `/api/Chatbot/chat`

Gửi tin nhắn tới AI chatbot và nhận phản hồi thông minh.

#### Request Body
```json
{
  "message": "Tôi bị dị ứng sữa bò. Triệu chứng và thực phẩm cần tránh?",
  "conversationId": "conv-12345", // Optional - ID cuộc trò chuyện
  "language": "vi", // Default: "vi"
  "category": "allergy", // Optional: allergy, mealplan, app_help, general
  "userId": "user-123", // Optional - để cá nhân hóa
  "conversationHistory": [
    {
      "role": "user",
      "content": "Câu hỏi trước đó",
      "timestamp": "2024-01-15T10:00:00Z"
    }
  ]
}
```

#### Response
```json
{
  "success": true,
  "message": "Tin nhắn đã được xử lý thành công",
  "data": {
    "response": "🥛 **Dị ứng Sữa bò**:\nTriệu chứng: Nôn mửa, tiêu chảy, phát ban...",
    "conversationId": "conv-12345",
    "confidence": 0.95,
    "category": "allergy",
    "timestamp": "2024-01-15T10:00:30Z",
    "requiresHumanSupport": false,
    "suggestedActions": [
      {
        "title": "Cập nhật hồ sơ dị ứng",
        "description": "Cập nhật thông tin dị ứng thực phẩm",
        "actionType": "navigate",
        "actionData": "/profile/allergies"
      }
    ],
    "relatedKeywords": ["dị ứng", "sữa", "thay thế"]
  }
}
```

### 2. Lấy lịch sử cuộc trò chuyện
**GET** `/api/Chatbot/conversation/{conversationId}/history`

#### Parameters
- `conversationId` (path): ID cuộc trò chuyện
- `limit` (query): Số tin nhắn tối đa (1-100, default: 20)

#### Response
```json
{
  "success": true,
  "message": "Lấy lịch sử cuộc trò chuyện thành công (5 tin nhắn)",
  "data": [
    {
      "role": "user",
      "content": "Tôi bị dị ứng sữa bò",
      "timestamp": "2024-01-15T10:00:00Z"
    },
    {
      "role": "assistant",
      "content": "Tôi hiểu bạn đang quan tâm về dị ứng sữa bò...",
      "timestamp": "2024-01-15T10:00:30Z"
    }
  ]
}
```

### 3. Xóa lịch sử cuộc trò chuyện
**DELETE** `/api/Chatbot/conversation/{conversationId}`

Xóa toàn bộ lịch sử của một cuộc trò chuyện.

### 4. Lấy câu hỏi gợi ý
**GET** `/api/Chatbot/suggestions`

#### Parameters
- `category` (query): general, allergy, mealplan, app_help
- `language` (query): vi, en (default: vi)

#### Response
```json
{
  "success": true,
  "message": "Lấy danh sách câu hỏi gợi ý cho danh mục 'allergy' thành công",
  "data": [
    "Dị ứng sữa bò có triệu chứng gì?",
    "Thực phẩm thay thế cho người dị ứng trứng?",
    "Dị ứng đậu phộng có nguy hiểm không?",
    "Làm thế nào để biết mình dị ứng gluten?",
    "Dị ứng hải sản cần tránh gì?"
  ]
}
```

### 5. Phân tích ý định (Intent Analysis)
**POST** `/api/Chatbot/analyze-intent`

Phân tích intent của tin nhắn để xác định danh mục và độ tin cậy.

#### Request Body
```json
{
  "message": "Tôi muốn tạo thực đơn cho người dị ứng đậu phộng",
  "language": "vi"
}
```

#### Response
```json
{
  "success": true,
  "message": "Phân tích intent thành công",
  "data": {
    "category": "mealplan",
    "confidence": 0.85,
    "message": "Tôi muốn tạo thực đơn cho người dị ứng đậu phộng"
  }
}
```

### 6. Trạng thái Chatbot
**GET** `/api/Chatbot/status`

Kiểm tra trạng thái hoạt động của chatbot.

#### Response
```json
{
  "success": true,
  "message": "Lấy trạng thái chatbot thành công",
  "data": {
    "isOnline": true,
    "version": "1.0.0",
    "supportedLanguages": ["vi", "en"],
    "supportedCategories": ["general", "allergy", "mealplan", "app_help"],
    "lastUpdated": "2024-01-15T10:00:00Z"
  }
}
```

## 🎯 Danh mục hỗ trợ

### 🛡️ Allergy (Dị ứng thực phẩm)
**Keywords**: dị ứng, allergen, phản ứng, triệu chứng, an toàn, tránh

**Thông tin được cung cấp**:
- Triệu chứng dị ứng từng loại thực phẩm
- Danh sách thực phẩm cần tránh
- Thực phẩm thay thế an toàn
- Cách xử lý khi có phản ứng dị ứng
- Thông tin về dị ứng chéo

**Dị ứng được hỗ trợ**:
- Sữa bò và sản phẩm từ sữa
- Trứng gà
- Các loại hạt (đậu phộng, hạnh nhân, v.v.)
- Gluten (lúa mì, yến mạch)
- Hải sản (tôm, cua, cá)
- Đậu nành và mè

### 🍽️ Mealplan (Kế hoạch bữa ăn)
**Keywords**: kế hoạch, bữa ăn, thực đơn, nấu ăn, dinh dưỡng, calo

**Tính năng**:
- Tư vấn kế hoạch bữa ăn theo dị ứng
- Gợi ý công thức nấu ăn an toàn
- Thông tin dinh dưỡng và calo
- Danh sách mua sắm thông minh
- Meal prep và lưu trữ thực phẩm

### 📱 App Help (Hướng dẫn ứng dụng)
**Keywords**: tính năng, sử dụng, cách, làm thế nào, hướng dẫn

**Hỗ trợ về**:
- Cách tạo hồ sơ dị ứng
- Sử dụng tính năng quét mã vạch
- Tạo và quản lý kế hoạch bữa ăn
- Tìm kiếm công thức nấu ăn
- Nâng cấp tài khoản Premium
- Quản lý dữ liệu cá nhân

### 🌟 General (Câu hỏi chung)
**Hỗ trợ về**:
- Giới thiệu về ứng dụng DrHan
- Thông tin về bảo mật dữ liệu
- Câu hỏi về dinh dưỡng chung
- Liên hệ hỗ trợ
- Tính năng miễn phí và trả phí

## ⚠️ Xử lý tình huống khẩn cấp

Chatbot có khả năng phát hiện các từ khóa khẩn cấp:
- `cấp cứu`, `nguy hiểm`, `khó thở`
- `sưng`, `shock`, `nghiêm trọng`
- `đau`, `nôn`

Khi phát hiện, chatbot sẽ:
1. Đặt `requiresHumanSupport: true`
2. Khuyến nghị liên hệ y tế ngay lập tức
3. Cung cấp số điện thoại cấp cứu: 115
4. Hướng dẫn sơ cứu cơ bản

## 🔧 Configuration

### Gemini AI Settings
```json
{
  "Gemini": {
    "ApiKey": "your-gemini-api-key",
    "ApiEndpoint": "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
    "Temperature": 0.3,
    "MaxTokens": 2000
  }
}
```

### Cache Settings
- **Conversation History**: 24 giờ
- **Max Messages per Conversation**: 50
- **Redis Key Pattern**: `conversation_history:{conversationId}`

## 📝 Ví dụ sử dụng

### Tình huống 1: Hỏi về dị ứng sữa
```bash
curl -X POST "https://api.drhan.com/api/Chatbot/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Con tôi 3 tuổi bị dị ứng sữa bò. Có thể cho uống sữa gì thay thế?",
    "language": "vi",
    "category": "allergy"
  }'
```

### Tình huống 2: Lập kế hoạch bữa ăn
```bash
curl -X POST "https://api.drhan.com/api/Chatbot/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Tạo thực đơn 1 tuần cho người dị ứng gluten và đậu nành",
    "conversationId": "meal-plan-123",
    "language": "vi",
    "category": "mealplan",
    "userId": "user-456"
  }'
```

### Tình huống 3: Hỏi về tính năng app
```bash
curl -X POST "https://api.drhan.com/api/Chatbot/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Làm sao để quét mã vạch kiểm tra sản phẩm an toàn?",
    "language": "vi",
    "category": "app_help"
  }'
```

## 🔒 Security & Privacy

### Bảo mật dữ liệu
- Không lưu trữ thông tin cá nhân nhạy cảm
- Lịch sử chat tự động xóa sau 24h
- Mã hóa dữ liệu trong Redis cache
- Rate limiting để tránh spam

### Privacy
- Người dùng có thể xóa lịch sử bất kỳ lúc nào
- Không chia sẻ dữ liệu với third party
- Tuân thủ GDPR và luật bảo vệ dữ liệu Việt Nam

## 🚨 Error Handling

### Lỗi phổ biến
```json
{
  "success": false,
  "message": "Tin nhắn không được để trống",
  "errors": ["Message is required"]
}
```

### Response codes
- `200`: Thành công
- `400`: Dữ liệu không hợp lệ
- `429`: Too many requests
- `500`: Lỗi server

## 📊 Monitoring

### Metrics được thu thập
- Số lượng tin nhắn xử lý
- Thời gian phản hồi trung bình
- Độ chính xác intent analysis
- Tỷ lệ câu hỏi khẩn cấp

### Logging
- Request/Response logs với Serilog
- Error tracking với detailed stack traces
- Performance monitoring với execution time

## 🔄 Future Enhancements

### Planned Features
- **Voice Input**: Hỗ trợ chat bằng giọng nói
- **Image Recognition**: Phân tích hình ảnh thực phẩm
- **Personalization**: Học từ lịch sử người dùng
- **Multi-language**: Hỗ trợ thêm tiếng Anh
- **Offline Mode**: Cache responses phổ biến

### Integration Opportunities
- **WhatsApp/Telegram Bot**: Mở rộng sang messaging platforms
- **Smart Speakers**: Tích hợp với Google Assistant
- **Wearables**: Cảnh báo dị ứng qua smartwatch
- **Healthcare**: Kết nối với hệ thống y tế

---

## 📞 Support

Nếu bạn cần hỗ trợ về API Chatbot:
- **Email**: support@drhan.com
- **Documentation**: https://docs.drhan.com/chatbot
- **GitHub Issues**: https://github.com/drhan/api/issues

---

*DrHan AI Chatbot - Đồng hành cùng bạn trong hành trình quản lý dị ứng thực phẩm một cách an toàn và thông minh! 🤖💚* 