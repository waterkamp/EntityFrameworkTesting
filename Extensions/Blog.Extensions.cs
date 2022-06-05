namespace EntityFrameworkTesting.Extensions
{
    public static class BlogExtensions
    {
        public static string ToDisplayLine(this Blog blog)
        {
            return $"Id: {blog.BlogId}, Url: {blog.Url}";
        }
    }
}
