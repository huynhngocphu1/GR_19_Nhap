using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebCafebookApi.Pages.Account
{
    public class DangXuatModel : PageModel
    {
        // Sửa lỗi CS8625: string -> string?
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage("/TrangChuView");
            }
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null) // Sửa lỗi CS8625
        {
            return await OnPostAsync(returnUrl);
        }
    }
}