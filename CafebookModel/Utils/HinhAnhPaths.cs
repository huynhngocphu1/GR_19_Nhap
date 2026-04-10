// Tập tin: CafebookModel/Utils/HinhAnhPaths.cs
using System.IO; // Thêm

namespace CafebookModel.Utils
{
    public static class HinhAnhPaths
    {
        // --- WPF Pack URIs (Defaults/Fallbacks) ---
        public const string DefaultAvatar = "/Assets/Images/default-avatar.png";
        public const string DefaultBookCover = "/Assets/Images/default-book-cover.png";
        public const string DefaultFoodIcon = "/Assets/Images/default-food-icon.png";

        // --- Server Relative URL Paths (Dùng /) ---
        public const string UrlAvatarNV = "/images/avatars/avatarNV";
        public const string UrlAvatarKH = "/images/avatars/avatarKH";
        public const string UrlBooks = "/images/books";
        public const string UrlFoods = "/images/foods";
    }
}