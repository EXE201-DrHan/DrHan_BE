namespace DrHan.Infrastructure.KnowledgeBase;

public static class ChatbotKnowledgeBase
{
    public static readonly Dictionary<string, string> AppFeatures = new()
    {
        ["meal_planning"] = @"
🍽️ **Tính năng Lập kế hoạch bữa ăn**:
- Tạo kế hoạch bữa ăn cá nhân hóa dựa trên dị ứng thực phẩm của bạn
- Tìm kiếm công thức nấu ăn an toàn
- Tạo danh sách mua sắm tự động từ kế hoạch bữa ăn
- Theo dõi dinh dưỡng và calo
- Đề xuất món ăn thông minh bằng AI

Cách sử dụng:
1. Cập nhật hồ sơ dị ứng thực phẩm
2. Chọn loại kế hoạch (1 ngày, 1 tuần, 1 tháng)
3. Đặt mục tiêu dinh dưỡng
4. Để AI tạo kế hoạch phù hợp với bạn",

        ["allergy_management"] = @"
🛡️ **Quản lý Dị ứng Thực phẩm**:
- Tạo hồ sơ dị ứng chi tiết
- Quét mã vạch sản phẩm để kiểm tra an toàn
- Cảnh báo dị ứng chéo
- Theo dõi phản ứng dị ứng
- Danh sách thực phẩm an toàn/nguy hiểm

Dị ứng chính được hỗ trợ:
- Sữa bò và các sản phẩm từ sữa
- Trứng gà
- Các loại hạt (đậu phộng, hạnh nhân, v.v.)
- Gluten (lúa mì, yến mạch)
- Hải sản (tôm, cua, cá)
- Đậu nành
- Mè",

        ["recipe_search"] = @"
🔍 **Tìm kiếm Công thức Nấu ăn**:
- Tìm kiếm theo nguyên liệu
- Lọc theo dị ứng thực phẩm
- Phân loại theo ẩm thực (Việt Nam, Châu Á, Châu Âu)
- Đánh giá độ khó và thời gian nấu
- Lưu công thức yêu thích
- AI đề xuất công thức mới

Cách tìm kiếm hiệu quả:
- Nhập nguyên liệu bạn có sẵn
- Chọn bộ lọc dị ứng
- Đặt thời gian nấu mong muốn
- Chọn độ khó phù hợp",

        ["smart_suggestions"] = @"
🤖 **Gợi ý Thông minh AI**:
- Phân tích sở thích ăn uống
- Đề xuất món ăn theo mùa
- Cân bằng dinh dưỡng tự động
- Tránh thực phẩm gây dị ứng
- Học từ phản hồi của bạn

AI sẽ học và cải thiện:
- Món ăn bạn thích/không thích
- Thời gian nấu ăn ưa thích
- Nguyên liệu có sẵn
- Mục tiêu sức khỏe",

        ["subscription_system"] = @"
💎 **Hệ thống Đăng ký Premium**:
- Gói Cơ bản: Tính năng cơ bản miễn phí
- Gói Pro: Gợi ý AI không giới hạn, báo cáo dinh dưỡng chi tiết
- Gói Premium: Tư vấn chuyên gia, kế hoạch cá nhân hóa

Lợi ích Premium:
- Không giới hạn gợi ý AI
- Báo cáo dinh dưỡng chi tiết
- Hỗ trợ ưu tiên
- Tích hợp với thiết bị đeo"
    };

    public static readonly Dictionary<string, string> AllergyInformation = new()
    {
        ["milk_allergy"] = @"
🥛 **Dị ứng Sữa bò**:
Triệu chứng: Nôn mửa, tiêu chảy, phát ban, khó thở
Thực phẩm cần tránh:
- Sữa tươi, sữa bột, kem
- Phô mai, bơ, yaourt
- Bánh kẹo có chứa sữa
- Chocolate sữa

Thay thế an toàn:
- Sữa hạnh nhân, sữa dừa
- Sữa đậu nành, sữa yến mạch
- Phô mai thực vật
- Kem từ dừa",

        ["egg_allergy"] = @"
🥚 **Dị ứng Trứng**:
Triệu chứng: Phát ban, sưng môi, đau bụng, khó thở
Thực phẩm cần tránh:
- Trứng gà, vịt, cút
- Bánh có trứng
- Mayonnaise
- Mì trứng

Thay thế nấu ăn:
- Bột aquafaba (nước đậu)
- Chia seed + nước
- Chuối nghiền
- Baking soda + giấm",

        ["nut_allergy"] = @"
🥜 **Dị ứng Các loại Hạt**:
Triệu chứng: Sưng họng, khó thở, shock phản vệ (nghiêm trọng)
Hạt nguy hiểm:
- Đậu phộng
- Hạnh nhân, óc chó
- Hạt điều, hạt phỉ
- Hạt Brazil, hạt macadamia

Lưu ý:
- Luôn mang theo bút tiêm epinephrine
- Đọc kỹ nhãn sản phẩm
- Tránh ăn ở nơi có thể nhiễm chéo",

        ["gluten_intolerance"] = @"
🌾 **Dị ứng Gluten/Celiac**:
Triệu chứng: Đau bụng, tiêu chảy, mệt mỏi, giảm cân
Thực phẩm cần tránh:
- Lúa mì, lúa mạch, lúa mạch đen
- Bánh mì, mì ống
- Bia, một số gia vị

Thay thế không gluten:
- Gạo, ngô, khoai lang
- Bột hạnh nhân, bột dừa
- Quinoa, kiều mạch
- Bánh mì không gluten",

        ["seafood_allergy"] = @"
🦐 **Dị ứng Hải sản**:
Triệu chứng: Nổi mề đay, sưng mặt, khó thở, shock
Hải sản cần tránh:
- Tôm, cua, ghẹ
- Cá thu, cá ngừ
- Mực, bạch tuộc
- Sò, ốc, hến

Lưu ý quan trọng:
- Tránh khói nấu hải sản
- Cẩn thận với nước mắm, tương cá
- Kiểm tra nhà hàng có chế biến hải sản"
    };

    public static readonly Dictionary<string, List<string>> SuggestedQuestions = new()
    {
        ["app_help"] = new()
        {
            "Làm thế nào để tạo kế hoạch bữa ăn?",
            "Cách cập nhật hồ sơ dị ứng thực phẩm?",
            "Tìm công thức nấu ăn an toàn như thế nào?",
            "Cách sử dụng tính năng quét mã vạch?",
            "Làm sao để nâng cấp tài khoản Premium?",
            "Ứng dụng có miễn phí không?",
            "Cách xóa dữ liệu cá nhân?",
            "Làm thế nào để thay đổi ngôn ngữ ứng dụng?"
        },
        ["allergy"] = new()
        {
            "Dị ứng sữa bò có triệu chứng gì?",
            "Thực phẩm thay thế cho người dị ứng trứng?",
            "Dị ứng đậu phộng có nguy hiểm không?",
            "Làm thế nào để biết mình dị ứng gluten?",
            "Dị ứng hải sản cần tránh gì?",
            "Dị ứng chéo là gì?",
            "Cách xử lý khi bị phản ứng dị ứng?",
            "Trẻ em có thể hết dị ứng không?"
        },
        ["mealplan"] = new()
        {
            "Tạo kế hoạch ăn uống cho người dị ứng gluten?",
            "Món ăn nào phù hợp cho người dị ứng sữa?",
            "Kế hoạch bữa ăn 1 tuần không hải sản?",
            "Thực đơn giảm cân cho người dị ứng đậu?",
            "Món chay không chứa gluten?",
            "Bữa sáng nhanh cho người dị ứng trứng?",
            "Kế hoạch ăn cho trẻ dị ứng nhiều thứ?",
            "Món ăn vặt an toàn cho người dị ứng?"
        },
        ["general"] = new()
        {
            "DrHan là ứng dụng gì?",
            "Ứng dụng này giúp gì cho người dị ứng?",
            "Có thể tin tưởng thông tin từ AI không?",
            "Dữ liệu cá nhân có được bảo mật?",
            "Cách liên hệ hỗ trợ khi cần?",
            "Ứng dụng có hỗ trợ tiếng Việt không?",
            "Có thể sử dụng offline không?",
            "Tính năng nào miễn phí?"
        }
    };

    public static readonly Dictionary<string, List<string>> Keywords = new()
    {
        ["meal_planning"] = new() { "kế hoạch", "bữa ăn", "thực đơn", "nấu ăn", "dinh dưỡng", "calo" },
        ["allergy"] = new() { "dị ứng", "allergen", "phản ứng", "triệu chứng", "an toàn", "tránh" },
        ["recipe"] = new() { "công thức", "món ăn", "nấu", "nguyên liệu", "cách làm" },
        ["app_features"] = new() { "tính năng", "sử dụng", "cách", "làm thế nào", "hướng dẫn" },
        ["subscription"] = new() { "premium", "gói", "đăng ký", "thanh toán", "nâng cấp" },
        ["emergency"] = new() { "cấp cứu", "nguy hiểm", "khó thở", "sưng", "shock", "nghiêm trọng" }
    };

    public static readonly string SystemPrompt = @"
Bạn là AI Assistant của ứng dụng DrHan - ứng dụng quản lý dị ứng thực phẩm và lập kế hoạch bữa ăn cho người Việt Nam.

NHIỆM VỤ CHÍNH:
1. Hỗ trợ người dùng về tính năng ứng dụng DrHan
2. Cung cấp thông tin về dị ứng thực phẩm
3. Tư vấn kế hoạch bữa ăn an toàn
4. Trả lời bằng tiếng Việt thân thiện, dễ hiểu

QUY TẮC QUAN TRỌNG:
- LUÔN ưu tiên an toàn: Khuyên người dùng tham khảo bác sĩ cho vấn đề nghiêm trọng
- Không tự ý chẩn đoán bệnh
- Cung cấp thông tin chính xác từ knowledge base
- Thân thiện, hữu ích và chuyên nghiệp
- Nếu không biết, thừa nhận và đề xuất tìm hiểu thêm

PHONG CÁCH TRẢ LỜI:
- Sử dụng emoji phù hợp
- Chia nhỏ thông tin dễ đọc
- Đưa ra ví dụ cụ thể
- Gợi ý hành động tiếp theo
- Luôn hỏi thêm nếu cần làm rõ

Hãy trả lời câu hỏi của người dùng theo hướng dẫn trên.
";
} 