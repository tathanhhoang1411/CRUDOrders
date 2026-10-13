CRUD Orders REST API
Hệ thống REST API xây dựng bằng ASP.NET Core 8, thiết kế theo kiến trúc Clean Architecture kết hợp mô hình CQRS (MediatR), quản lý sản phẩm và đơn hàng, phân quyền người dùng và kiểm soát tồn kho chặt chẽ.

🌟 Tính năng nổi bật
✅ CRUD Sản phẩm: Đầy đủ tính năng tạo, đọc, sửa, xóa sản phẩm kèm mã code, tồn kho.
✅ Quản lý Đơn hàng & Xử lý Tồn kho: Tự động trừ tồn kho khi tạo đơn hàng, chống bán vượt tồn kho (Overselling) với Optimistic Concurrency (RowVersion).
✅ Phân quyền Role-based (RBAC):
Admin: Toàn quyền quản trị danh mục sản phẩm, xóa đơn hàng, tạo tài khoản nhân viên (Staff).
Staff: Cập nhật và xử lý luồng trạng thái đơn hàng (Pending ➔ Confirmed ➔ Shipped ➔ Completed).
User (Khách hàng): Đặt hàng, theo dõi lịch sử đơn hàng của chính mình.
✅ Bảo mật JWT: JWT Bearer Authentication, băm mật khẩu bảo mật SHA-256.
✅ Ràng buộc phân trang (Pagination Settings): Kiểm soát skip & take tập trung qua appsettings.json (MaxSkip < 100, MinSkip >= 0, MinTake >= 1).
✅ Định dạng dữ liệu chuẩn (Result Pattern): Mọi API trả về cấu trúc thống nhất { success, errorCode, errorMessage, data }.
✅ Clean Architecture & CQRS: Phân tầng rõ ràng (Core, Application, Infrastructure, Presentation), áp dụng MediatR Pipeline Behavior cho Validation & Caching.
✅ Swagger / OpenAPI: Phân nhóm trực quan theo 3 phân hệ (Authentication, Products, Orders) kèm nút Authorize Bearer token.
✅ Logging: Tích hợp Serilog ghi log ra Console và file xoay vòng theo ngày (logs/app-.log).
✅ Kiểm thử tự động (100% Passed): 23 test cases (9 Unit Tests + 14 Integration Tests dùng In-Memory DB).
✅ Containerization: Hỗ trợ Dockerfile multi-stage build sẵn sàng triển khai.
🛠 Yêu cầu môi trường
[.NET 8.0 SDK]
SQL Server (LocalDB)
🚀 Hướng dẫn Cài đặt & Chạy dự án
1. Cấu hình Database
Mở file appsettings.json (hoặc appsettings.Development.json) và chỉnh sửa chuỗi kết nối:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CRUDOrdersDb;Trusted_Connection=true;TrustServerCertificate=True;"
  }
}
2. Cập nhật Database
Dự án có cơ chế tự động seed dữ liệu mẫu (1 tài khoản Admin mặc định admin / admin123 và các sản phẩm mẫu) khi khởi động. Để tạo database: Hãy thực thi các câu lệnh sql trong file CRUD\CRUDOrders\CRUDOrders\Database\InitDatabase.sql

3. Khởi chạy Ứng dụng
dotnet run
Sau khi chạy, truy cập Swagger UI để tương tác trực quan: 👉 URL: http://localhost:5172/swagger (hoặc https://localhost:7172/swagger)

4. Chạy bộ kiểm thử (Tests)
dotnet test
(Hiện tại 23/23 tests gồm Unit Tests & Integration Tests đều Passed)

📖 HƯỚNG DẪN CHI TIẾT CÁC ENDPOINT & LỆNH CURL
Lưu ý: Địa chỉ host mặc định là http://localhost:5172. Thay thế <ADMIN_TOKEN>, <STAFF_TOKEN>, <USER_TOKEN> bằng chuỗi token nhận được sau khi đăng nhập.

PHÂN HỆ 1: AUTHENTICATION & PHÂN QUYỀN
1. Tạo tài khoản Quản trị viên (POST /api/v1/Auth/register-admin)
Quyền hạn: Public
Mục đích: Tạo tài khoản có quyền Admin.
curl --location --request POST 'http://localhost:5172/api/v1/Auth/register-admin' \
--header 'Content-Type: application/json' \
--data-raw '{
  "username": "superadmin",
  "email": "superadmin@example.com",
  "password": "admin123"
}'
2. Đăng nhập hệ thống (POST /api/v1/Auth/login)
Quyền hạn: Public
Mục đích: Lấy JWT Bearer token kèm thông tin Role.
curl --location --request POST 'http://localhost:5172/api/v1/Auth/login' \
--header 'Content-Type: application/json' \
--data-raw '{
  "username": "superadmin",
  "password": "admin123"
}'
3. Tạo tài khoản Nhân viên (POST /api/v1/Auth/register-staff)
Quyền hạn: Admin Only (Yêu cầu Token có quyền Admin)
Mục đích: Tài khoản Admin tạo thêm tài khoản Nhân viên (Staff) vào hệ thống.
curl --location --request POST 'http://localhost:5172/api/v1/Auth/register-staff' \
--header 'Authorization: Bearer <ADMIN_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
  "username": "staff01",
  "email": "staff01@example.com",
  "password": "staff123"
}'
PHÂN HỆ 2: SẢN PHẨM (PRODUCTS)
4. Lấy danh sách sản phẩm có phân trang (GET /api/v1/Products)
Quyền hạn: Public
Tham số Query: skip (0-99), take (1-100), keyword (tùy chọn)
curl --location --request GET 'http://localhost:5172/api/v1/Products?skip=0&take=10&keyword=Laptop'
5. Xem chi tiết sản phẩm theo ID (GET /api/v1/Products/{id})
Quyền hạn: Public
curl --location --request GET 'http://localhost:5172/api/v1/Products/1'
6. Thêm sản phẩm mới (POST /api/v1/Products)
Quyền hạn: Admin Only
curl --location --request POST 'http://localhost:5172/api/v1/Products' \
--header 'Authorization: Bearer <ADMIN_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
  "code": "SP009",
  "name": "Bàn phím cơ DareU EK87",
  "description": "Switch Brown gõ văn phòng êm ái",
  "price": 850000,
  "stock": 100
}'
7. Cập nhật sản phẩm (PUT /api/v1/Products/{id})
Quyền hạn: Admin Only
curl --location --request PUT 'http://localhost:5172/api/v1/Products/1' \
--header 'Authorization: Bearer <ADMIN_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
  "name": "Laptop Dell XPS 15 2024 (Đã cập nhật)",
  "description": "Nâng cấp RAM 32GB",
  "price": 35000000,
  "stock": 25
}'
8. Xóa sản phẩm (DELETE /api/v1/Products/{id})
Quyền hạn: Admin Only
curl --location --request DELETE 'http://localhost:5172/api/v1/Products/1' \
--header 'Authorization: Bearer <ADMIN_TOKEN>'
PHÂN HỆ 3: ĐƠN HÀNG (ORDERS)
9. Tạo đơn hàng mới (POST /api/v1/Orders)
Quyền hạn: Bất kỳ người dùng đã đăng nhập (User / Staff / Admin)
Mục đích: Tự động kiểm tra số lượng tồn kho và trừ số lượng tồn kho. Nếu không đủ hàng, trả về lỗi INSUFFICIENT_STOCK.
curl --location --request POST 'http://localhost:5172/api/v1/Orders' \
--header 'Authorization: Bearer <USER_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
  "shippingAddress": "123 Lê Lợi, Bến Nghé, Quận 1, TP.HCM",
  "items": [
    {
      "productId": 1,
      "quantity": 2
    }
  ]
}'
10. Xem danh sách đơn hàng của chính mình (GET /api/v1/Orders/user/my-orders)
Quyền hạn: Bất kỳ người dùng đã đăng nhập
curl --location --request GET 'http://localhost:5172/api/v1/Orders/user/my-orders?skip=0&take=10' \
--header 'Authorization: Bearer <USER_TOKEN>'
11. Xem chi tiết 1 đơn hàng (GET /api/v1/Orders/{id})
Quyền hạn: Đã đăng nhập
curl --location --request GET 'http://localhost:5172/api/v1/Orders/1' \
--header 'Authorization: Bearer <USER_TOKEN>'
12. Xem toàn bộ đơn hàng trong hệ thống (GET /api/v1/Orders)
Quyền hạn: Admin Only
curl --location --request GET 'http://localhost:5172/api/v1/Orders?skip=0&take=10' \
--header 'Authorization: Bearer <ADMIN_TOKEN>'
13. Nhân viên / Admin cập nhật trạng thái đơn hàng (PUT /api/v1/Orders/{id}/status)
Quyền hạn: Admin hoặc Staff
curl --location --request PUT 'http://localhost:5172/api/v1/Orders/1/status' \
--header 'Authorization: Bearer <STAFF_TOKEN>' \
--header 'Content-Type: application/json' \
--data-raw '{
  "status": "Shipped"
}'
14. Xóa / Hủy đơn hàng (DELETE /api/v1/Orders/{id})
Quyền hạn: Admin Only (Staff gọi sẽ bị từ chối 403 Forbidden)
curl --location --request DELETE 'http://localhost:5172/api/v1/Orders/1' \
--header 'Authorization: Bearer <ADMIN_TOKEN>'
⚙️ Cấu hình Phân trang (appsettings.json)
Hệ thống kiểm soát các tham số phân trang tập trung qua section Pagination:

"Pagination": {
  "MaxSkip": 99,
  "MinSkip": 0,
  "MinTake": 1,
  "MaxTake": 100
}
skip: Chỉ được phép trong khoảng từ 0 đến 99 (MaxSkip < 100).
take: Chỉ được phép trong khoảng từ 1 đến 100.
Nếu vi phạm (ví dụ skip=100, skip=-1, take=0), API phản hồi ngay HTTP 400 Bad Request kèm mã lỗi VALIDATION_ERROR:
Ví dụ kiểm thử:

# Test skip >= 100 (Kỳ vọng 400 Bad Request)
curl --location --request GET 'http://localhost:5172/api/v1/Products?skip=100&take=10'

# Test skip < 0 (Kỳ vọng 400 Bad Request)
curl --location --request GET 'http://localhost:5172/api/v1/Products?skip=-1&take=10'

# Test take < 1 (Kỳ vọng 400 Bad Request)
curl --location --request GET 'http://localhost:5172/api/v1/Products?skip=0&take=0'
📦 Định dạng Phản hồi Chuẩn (Result Pattern)
Mọi API trả về cấu trúc thống nhất:

Thành công (200 OK):

{
  "success": true,
  "errorCode": null,
  "errorMessage": null,
  "data": { ... }
}
Thất bại (400 / 404 / 401 / 403):

{
  "success": false,
  "errorCode": "INSUFFICIENT_STOCK",
  "errorMessage": "Sản phẩm Laptop không đủ số lượng tồn kho (yêu cầu: 5, tồn: 2)",
  "data": null
}
