# Tôi muốn làm 1 màn hình admin có thể quản lý được user và khi My Quizzes của các user thay đổi thành published thì admin sẽ thấy được bộ đề đó và khi draft thì không
#  

KẾ HOẠCH TRIỂN KHAI: ADMIN DASHBOARD
Phase 1: Backend - Cập nhật Authentication & Phân Quyền
Đầu tiên, hệ thống cần biết được ai là "Admin" và ai là "User" thông thường để cấp quyền truy cập.

 1.1. Cập nhật JWT Token: Chỉnh sửa file AuthController.cs (hoặc JwtService.cs). Khi tạo Token, cần thêm ClaimTypes.Role (lấy từ thuộc tính Role của AppUser) vào Payload để Desktop App có thể giải mã và nhận diện được quyền của người đăng nhập.
 1.2. Tạo tài khoản Admin mặc định: Thêm chức năng seed data hoặc viết 1 script nhỏ để tạo một tài khoản có Role = "Admin" trong cơ sở dữ liệu PostgreSQL.
Phase 2: Backend - API cho Admin
Tạo một Controller mới dành riêng cho quản trị viên, được bảo vệ nghiêm ngặt.

 2.1. Khởi tạo AdminController.cs: Đặt attribute [Authorize(Roles = "Admin")] để đảm bảo chỉ Admin mới gọi được các API này.
 2.2. API Quản lý User (GET /api/admin/users): Trả về danh sách tất cả các User trong hệ thống (gồm Id, Email, DisplayName, Role, số lượng Quiz đã tạo...).
 2.3. API Quản lý Published Quizzes (GET /api/admin/quizzes): Query Database trả về danh sách các Quiz có điều kiện Published == true. (Các quiz mang trạng thái Draft / Published == false sẽ bị loại bỏ ở bước này nên Admin không thể lấy). Dữ liệu trả về cần kèm thêm thông tin tác giả (Creator's DisplayName).
Phase 3: Frontend Desktop - Tích hợp Dịch Vụ
Cập nhật Client để gọi được các API mới của Admin.

 3.1. Cập nhật ApiClient.cs:
Thêm phương thức GetUsersForAdminAsync().
Thêm phương thức GetPublishedQuizzesForAdminAsync().
 3.2. Đọc Role từ User hiện tại: Viết hàm nhỏ để decode chuỗi JWT (hoặc gọi API /auth/me) sau khi đăng nhập nhằm xác định người dùng hiện tại là Admin hay User và lưu vào trạng thái biến cục bộ.
Phase 4: Frontend Desktop - Thiết kế UI/UX & ViewModels
Xây dựng giao diện Avalonia để hiển thị.

 4.1. Điều hướng trong MainWindow.axaml:
Thêm một nút "Admin Dashboard" trên thanh menu dùng IsVisible="{Binding IsAdmin}". Nút này ẩn với người dùng thường.
Cập nhật MainWindowViewModel để xử lý sự kiện click chuyển sang View của Admin.
 4.2. Tạo AdminDashboardViewModel & AdminDashboardView.axaml:
Thiết kế màn hình tổng quan chia làm 2 Tabs: [ User Management ] và [ Published Quizzes ].
 4.3. Code cho Tab "Sắp xếp User":
Dùng thẻ <DataGrid> hoặc <ListBox> để hiển thị danh sách người dùng lấy từ API.
 4.4. Code cho Tab "Quản lý Quiz":
Hiển thị danh sách các đề thi được Public. Giao diện có thể mượn lại một phần từ LibraryView nhưng thêm cột/tiêu đề "Người tạo" (Author). (Tương lai có thể thêm nút "Xóa/Gỡ" quiz nếu vi phạm).
Phase 5: Testing
 Đăng nhập bằng user@gmail.com -> Nút Admin bị ẩn. KHÔNG xem được quiz của người khác.
 Đăng nhập bằng admin@gmail.com -> Hiện nút Admin.
 Bấm vào Admin -> Thấy list toàn bộ sinh viên.
 Vào tab Quizzes -> Chỉ thấy các Quiz nào user bấm nút chuyển thành Public/Published. Các quiz Draft không xuất hiện trên lưới.


 Nhóm chức năng mới admin
 1. Nhóm Tính năng Xử lý & Tương tác (Hành động)
Hiện tại Admin chỉ nhìn thấy danh sách, chúng ta có thể thêm các nút Hành động (Action Buttons):

Xóa / Gỡ bài (Force Unpublish / Delete Quiz): Nếu một học sinh tạo bài Quiz có nội dung rác, nhạy cảm hoặc sai kiến thức rồi Publish lên cộng đồng, Admin phải có quyền bấm nút "Gỡ khỏi cộng đồng" (Chuyển Published -> False) hoặc "Xóa vĩnh viễn" bài đó.
Khóa tài khoản (Ban/Block User): Thêm một cờ IsBanned hoặc IsActive cho AppUser. Admin có thể tạm khóa quyền đăng nhập tài khoản của học sinh nếu có hành vi lạm dụng (ví dụ spam API tạo quiz liên tục).
2. Nhóm Tính năng Kiểm duyệt & Chi tiết (Moderation)
Xem trước nội dung đề thi (Preview Quiz): Admin khi thấy một bài Quiz bên cột "Published Quizzes" có thể click đúp chuột vào để xem chi tiết toàn bộ câu hỏi và đáp án để đánh giá xem câu AI sinh ra có chất lượng không.
Quản lý Tài liệu rác (Document Manager): Người dùng có thể upload rất nhiều file PDF/DOCX làm đầy dung lượng máy chủ/Cloudinary (nơi bạn lưu file). Admin cần xem được danh sách file hệ thống và quyền Xóa các file không còn dùng đến.
Xem lịch sử của 1 User bất kỳ: Click vào 1 User ở bảng bên trái để xem cụ thể điểm số trung bình của họ, và các bài họ đã thi.
3. Nhóm Số liệu & Thống kê (Analytics Dashboard)
Thay vì nguyên một bảng dài, ta có thể thiết kế phần thanh trên cùng (Top Widget) hiển thị các con số tổng quan:

📉 Tổng số người dùng hệ thống (để biết app đang có bao nhiêu khách).
📝 Tổng số bài Quiz đã được tạo (Tổng lượng nội dung).
🎯 Tổng số lượt làm bài (Attempts).
🏆 Bảng xếp hạng (Leaderboard): Top 3 học sinh có điểm trung bình cao nhất hoặc Top những bộ đề được làm nhiều nhất.