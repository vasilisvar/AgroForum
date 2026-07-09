namespace AgroForum.Constants
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string Expert = "Expert";
        public const string Farmer = "Farmer";

        public static readonly string[] All = { Admin, Moderator, Expert, Farmer };
    }
}