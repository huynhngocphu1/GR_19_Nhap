using CafebookApi.Data;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace WebCafebookApi.Pages.employee
{
    public class DangXuatModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToPage("/Employee/DangNhapEmployee");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }
    }
}


