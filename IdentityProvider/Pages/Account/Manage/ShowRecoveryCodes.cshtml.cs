using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Idp.Swiyu.Passkeys.Sts.Pages.Account.Manage;

public class ShowRecoveryCodesModel : PageModel
{
    [TempData]
    public string[] RecoveryCodes { get; set; } = [];

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (RecoveryCodes == null || RecoveryCodes.Length == 0)
        {
            return RedirectToPage("./TwoFactorAuthentication");
        }

        return Page();
    }
}
