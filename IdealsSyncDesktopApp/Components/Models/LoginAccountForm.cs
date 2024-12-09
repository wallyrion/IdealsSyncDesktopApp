using System.ComponentModel.DataAnnotations;

public class LoginAccountForm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

}