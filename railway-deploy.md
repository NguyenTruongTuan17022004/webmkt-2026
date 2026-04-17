# 🚂 Railway Deploy Guide - Chi tiết từng bước

## Bước 1: Chuẩn bị GitHub Repository
```bash
# Tạo GitHub repo mới tại: https://github.com/new
# Tên repo: webmkt-2026 (hoặc tên bạn thích)

# Thêm remote origin (thay YOUR_USERNAME và REPO_NAME)
git remote add origin https://github.com/NguyenTruongTuan17022004/webmkt-2026.git

# Push code lên GitHub
git push -u origin main
```

## Bước 2: Deploy trên Railway
1. **Truy cập**: https://railway.app
2. **Đăng ký**: Sử dụng GitHub account
3. **Tạo Project**: Click "New Project" → "Deploy from GitHub repo"
4. **Connect GitHub**: Cho phép Railway truy cập repo của bạn
5. **Chọn Repo**: Tìm và chọn `webmkt-2026`
6. **Deploy**: Click "Deploy" - Railway sẽ tự động detect .NET và build

## Bước 3: Cấu hình Environment Variables
Sau khi deploy thành công, cần thêm các API keys:

1. Trong Railway dashboard → Variables (tab)
2. Thêm các biến sau:
   - Key: `GEMINI_API_KEY` | Value: [your Gemini API key]
   - Key: `OPENAI_API_KEY` | Value: [your OpenAI API key]
   - Key: `ASPNETCORE_ENVIRONMENT` | Value: `Production`

## Bước 4: Cấu hình Database (Tùy chọn)
Railway có thể tự động tạo PostgreSQL database miễn phí:
1. Trong Railway dashboard → Add → Database → PostgreSQL
2. Copy connection string
3. Thêm vào Environment Variables:
   - Key: `DATABASE_URL`
   - Value: connection string từ Railway

## Bước 5: Truy cập Website
Sau khi deploy thành công, Railway sẽ cung cấp URL:
```
https://webmkt-2026.up.railway.app
```

## Bước 6: Test AI Features
1. Truy cập `/AdminPosts/CreateWithAi` để test tạo bài viết với AI
2. Truy cập `/AdminPosts/BulkCreateWithAi` để test tạo nhiều bài cùng lúc
3. API endpoint: `/api/Ai/write-from-url` để scrape và rewrite content

## 🎯 Railway Free Tier Limits:
- ✅ 512MB RAM
- ✅ 1GB Disk
- ✅ 100 hours runtime/tháng
- ✅ Unlimited bandwidth
- ✅ Custom domains (có phí)

## ✅ Project Compatibility:
- ✅ .NET 8.0 (đã được downgrade từ .NET 10.0)
- ✅ ASP.NET Core MVC
- ✅ Entity Framework Core
- ✅ SQLite/PostgreSQL support
- ✅ Docker ready
- ✅ Static assets compatibility fixed

## 🔧 Troubleshooting:
- Nếu build fail: Check Railway logs
- Nếu runtime error: Check environment variables
- Nếu database error: Verify connection string

## 📞 Support:
Railway có Discord community rất helpful!