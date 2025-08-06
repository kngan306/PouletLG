using System.ComponentModel.DataAnnotations;

namespace WebLego.Models.ViewModel
{
    public class LoginViewModel
    {
        [Display(Name = "Email hoặc Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại.")]
        public string EmailOrPhone { get; set; }  // Đổi tên để rõ ý nghĩa

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string UserPassword { get; set; }
    }
}
