using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Models
{
    public class AccountViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [StringLength(10, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.")]
        public string Phone { get; set; }

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.")]
        public string? UserPassword { get; set; }

        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("UserPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Vai trò")]
        [Required(ErrorMessage = "Phải chọn vai trò.")]
        public int RoleId { get; set; }

        public string? RoleName { get; set; } // Không bắt buộc

        [Display(Name = "Trạng thái")]
        [Required(ErrorMessage = "Phải chọn trạng thái.")]
        public string? UserStatus { get; set; }

        public List<SelectListItem> UserStatusList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Tất cả" },
            new SelectListItem { Value = "Hoạt động", Text = "Hoạt động" },
            new SelectListItem { Value = "Tạm khóa", Text = "Tạm khóa" }
        };

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string? DiscountCode { get; set; } // Không bắt buộc

        [Display(Name = "Hạng thành viên")]
        public string? CustomerRank { get; set; } // Không bắt buộc

        public bool IsCustomer => RoleName == "Khách hàng" || RoleId == 1;

        // Các trường phục vụ lọc, không bắt buộc
        [Display(Name = "Tìm theo tên")]
        public string? SearchName { get; set; }

        [Display(Name = "Tìm theo email")]
        public string? SearchEmail { get; set; }

        [Display(Name = "Vai trò")]
        public int? FilterRoleId { get; set; }

        [Display(Name = "Trạng thái")]
        public string? FilterStatus { get; set; }

        [Display(Name = "Giới tính")]
        public string? FilterGender { get; set; }

        [Display(Name = "Hạng thành viên")]
        public string? FilterRank { get; set; }

        // Dropdown hỗ trợ view, không bắt buộc
        public IEnumerable<SelectListItem>? RoleOptions { get; set; }
        public IEnumerable<SelectListItem>? StatusOptions { get; set; }
        public IEnumerable<SelectListItem>? GenderOptions { get; set; }
        public IEnumerable<SelectListItem>? RankOptions { get; set; }

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }

        public List<WebLego.DataSet.GdrService.User> Accounts { get; set; } = new();
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
        public CustomerProfile? CustomerProfile { get; set; }
    }
}