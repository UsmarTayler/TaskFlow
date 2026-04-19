using System.ComponentModel.DataAnnotations;

namespace TaskFlow.ViewModels
{
    public class JoinOrganisationViewModel
    {
        [Required(ErrorMessage = "Invite code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Invite codes are exactly 6 characters.")]
        [Display(Name = "Invite Code")]
        public string InviteCode { get; set; } = string.Empty;
    }
}
