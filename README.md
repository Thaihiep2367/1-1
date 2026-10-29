# HỆ THỐNG THU THẬP THÔNG TIN VÀ ĐỐI SOÁT ĐOÀN PHÍ - HỘI PHÍ
## Công nghệ: .NET 10 | Clean Architecture 4 Lớp | Phân Quyền 2 Role (Public & Admin)

Dự án được xây dựng dựa trên công nghệ **.NET 10 (C# 14)** tuân thủ nghiêm ngặt mô hình **Clean Architecture 4 lớp**, kết hợp giao diện Web hiện đại, hỗ trợ chuẩn hóa nội dung chuyển khoản VietQR tự động, nén ảnh minh chứng client-side và cơ chế bảo mật phân quyền Role-Based Access Control (RBAC) với JWT Bearer Tokens.

---

## 👥 PHÂN CHIA 2 ROLE TRONG HỆ THỐNG

### 1. Vai trò Người Dùng (Sinh Viên - Public)
- **Không cần đăng nhập**: Sinh viên truy cập trực tiếp hệ thống.
- **Quyền hạn & Tính năng**:
  - Xem thông tin mức thu cố định cả năm (12 tháng: 108.000đ cho sinh viên Đã vào Đoàn, 84.000đ cho sinh viên Chưa vào Đoàn).
  - Tự động sinh mã VietQR Techcombank (NAPAS 247) có sẵn số tiền và nội dung chuyển khoản chuẩn hóa cú pháp `[MSSV] [HO TEN] Doan phi`.
  - Mở trực tiếp hơn 30 ứng dụng ngân hàng di động phổ biến tại Việt Nam (Techcombank, MB Bank, Vietcombank, VietinBank, BIDV, VPBank, TPBank, CAKE, Timo,...).
  - Tải lên ảnh chụp màn hình minh chứng giao dịch chuyển khoản (hỗ trợ kéo thả, tự động nén ảnh tối ưu dung lượng).
  - Nộp phiếu xác nhận nộp phí (`POST /api/submissions`).
  - **Giới hạn bảo mật**: Sinh viên tuyệt đối không thể xem danh sách đối soát hay số liệu tổng hợp của người khác.
  - **Kiểm soát tiếp nhận**: Nếu quản trị viên đóng cổng thu phí, toàn bộ form sinh viên sẽ bị khóa (disabled) và hiển thị banner cảnh báo cùng lý do đóng form.

### 2. Vai trò Quản Trị Viên (Admin)
- **Bắt buộc đăng nhập**: Đăng nhập qua modal xác thực quản trị viên bằng JWT Token (HS256).
- **Tài khoản quản trị mặc định**:
  - **Tên đăng nhập**: `admin`
  - **Mật khẩu**: `admin123`
  - **Role Claim**: `Admin`
- **Quyền hạn & Tính năng Quản trị**:
  - **Bảng điều khiển (Admin Dashboard)** chuyên nghiệp với Glassmorphism UI.
  - **Đóng / Mở Form Nộp Phí (Form Control)**:
    - Công tắc Toggle Đóng/Mở form tức thì (`POST /api/system/toggle-form`).
    - Nhập lý do đóng form hiển thị cho toàn bộ sinh viên (VD: "Đã hết hạn nộp đợt 1 năm học 2026-2027").
  - **Báo cáo & Thống kê tổng hợp (KPI Cards)**:
    - Tổng số hồ sơ sinh viên đã gửi.
    - Tổng số tiền thu được (VNĐ).
    - Số lượng sinh viên Đã vào Đoàn vs Chưa vào Đoàn.
    - Số lượng bản ghi có đính kèm ảnh minh chứng.
  - **Quản lý & Đối soát danh sách**:
    - Tìm kiếm nhanh theo Họ tên, MSSV, Lớp sinh hoạt, Ghi chú.
    - Lọc theo tư cách đoàn thể, tình trạng ảnh minh chứng.
    - Xem ảnh minh chứng phóng to sắc nét trong cửa sổ Lightbox, hỗ trợ tải ảnh gốc về máy.
    - Xóa bản ghi nộp sai/trùng lặp (`DELETE /api/submissions/{id}`).
  - **Công cụ Xuất Dữ liệu**:
    - Xuất toàn bộ danh sách đối soát ra file **Excel (CSV UTF-8)** chuẩn format kế toán.
    - Xóa toàn bộ dữ liệu kiểm thử (Reset) có cảnh báo an toàn.

---

## 🏛️ KIẾN TRÚC CLEAN ARCHITECTURE 4 LỚP (.NET 10)

Hệ thống tuân thủ triệt để Dependency Rule (lớp trong không tham chiếu lớp ngoài):

```
DoanPhi.slnx
├── src/
│   ├── DoanPhi.Domain/          # Tầng 1: Lõi Domain trung tâm
│   │   ├── Entities/            # FeeSubmission, SystemSetting, MembershipStatus
│   │   └── Interfaces/          # IFeeSubmissionRepository, ISystemSettingRepository
│   ├── DoanPhi.Application/     # Tầng 2: Nghiệp vụ (Use Cases, DTOs, Handlers)
│   │   ├── Common/              # Exceptions, Helpers (bỏ dấu Unicode), Interfaces
│   │   ├── DTOs/                # FeeSubmissionDtos, AuthDtos
│   │   └── Features/
│   │       ├── Auth/            # LoginCommand (xác thực & phát hành JWT)
│   │       ├── Submissions/     # SubmitFee, Delete, Queries (Stats, List, ExportCsv)
│   │       └── System/          # GetSystemStatus, ToggleFormStatus
│   ├── DoanPhi.Infrastructure/  # Tầng 3: Hạ tầng & Triển khai
│   │   ├── Auth/                # JwtTokenGenerator (phát hành token Role: Admin)
│   │   └── Persistence/         # InMemoryFeeSubmissionRepository, InMemorySystemSettingRepository
│   └── DoanPhi.API/             # Tầng 4: Presentation & Web Host
│       ├── Controllers/         # AuthController, SubmissionsController, SystemController
│       ├── Middleware/          # GlobalExceptionHandlerMiddleware
│       ├── wwwroot/             # Web Frontend tĩnh (HTML5, Tailwind, Vanilla JS)
│       └── Program.cs           # Cấu hình JWT Bearer, Swagger UI, DI Container
└── tests/
    └── DoanPhi.Tests/           # Unit Tests (xUnit, Moq - 21 tests passed 100%)
```

---

## 🔒 MA TRẬN PHÂN QUYỀN API (RBAC)

| Phương thức | Endpoint | Yêu cầu phân quyền | Mô tả chức năng |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/system/status` | **Public** | Kiểm tra trạng thái đóng/mở form nộp |
| `POST` | `/api/submissions` | **Public** | Sinh viên gửi phiếu xác nhận đóng phí |
| `POST` | `/api/auth/login` | **Public** | Đăng nhập Quản trị viên (`admin`/`admin123`) |
| `GET` | `/api/auth/verify` | `[Authorize]` | Kiểm tra hiệu lực của JWT Token |
| `POST` | `/api/system/toggle-form` | `[Authorize(Roles="Admin")]` | Đóng / Mở form nộp phí kèm lý do |
| `GET` | `/api/submissions` | `[Authorize(Roles="Admin")]` | Xem danh sách đối soát sinh viên |
| `GET` | `/api/submissions/{id}` | `[Authorize(Roles="Admin")]` | Xem chi tiết 1 hồ sơ nộp |
| `GET` | `/api/submissions/stats` | `[Authorize(Roles="Admin")]` | Thống kê tổng hợp số tiền & tỷ lệ đoàn |
| `GET` | `/api/submissions/export-csv` | `[Authorize(Roles="Admin")]` | Xuất file Excel (CSV) báo cáo |
| `DELETE` | `/api/submissions/{id}` | `[Authorize(Roles="Admin")]` | Xóa 1 bản ghi nộp phí |
| `DELETE` | `/api/submissions` | `[Authorize(Roles="Admin")]` | Xóa toàn bộ dữ liệu kiểm thử |

---

## 🚀 HƯỚNG DẪN CHẠY DỰ ÁN

### Yêu cầu môi trường
- .NET SDK 10.0+

### Chạy hệ thống
```bash
# Khởi chạy toàn bộ hệ thống API và Web giao diện
dotnet run --project src/DoanPhi.API --urls "http://localhost:5000"
```

Sau khi chạy lệnh trên, truy cập trình duyệt tại địa chỉ:
- **Giao diện Web Sinh viên & Quản trị**: [http://localhost:5000](http://localhost:5000)
- **Tài liệu Swagger API (có Bearer Auth)**: [http://localhost:5000/swagger](http://localhost:5000/swagger)

### Chạy Unit Tests
```bash
dotnet test DoanPhi.slnx
```
*(Hiện có 21 Unit Tests kiểm tra toàn diện Domain, Application Handlers, Mock Repositories và logic Auth/System).*
