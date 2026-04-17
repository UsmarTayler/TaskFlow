namespace TaskFlow.ViewModels
{
    /// <summary>
    /// Passed to the Admin/Users view.
    /// Bundles the list of all users with the available roles so the view
    /// can render each row's "Change Role" dropdown without extra round-trips.
    /// </summary>
    public class UserManagementViewModel
    {
        // The full list of registered application users
        public List<UserRow> Users    { get; set; } = new();

        // All roles defined in the system — used to populate the role-change dropdown
        public List<string>  AllRoles { get; set; } = new();
    }

    /// <summary>
    /// A flattened representation of a single user for the management table.
    /// Avoids exposing the full ApplicationUser (and its Identity internals) to the view.
    /// </summary>
    public class UserRow
    {
        public string       Id       { get; set; } = string.Empty;  // Identity user ID (GUID)
        public string       FullName { get; set; } = string.Empty;
        public string       Email    { get; set; } = string.Empty;
        public List<string> Roles    { get; set; } = new();         // may be empty if unassigned
    }
}
