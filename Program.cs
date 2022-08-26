using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using EntityFrameworkTesting.Extensions;

namespace EntityFrameworkTesting
{
    public class Program
    {
        private static Commands? selectedCommand;

        private static StringBuilder executionLog = new StringBuilder();

        Program()
        {
            selectedCommand = Commands.Exit;
        }

        static void Main(string[] args)
        {
            while (!selectedCommand.HasValue && selectedCommand != Commands.Exit)
            {
                ShowMenu();
                selectedCommand = ReadCommand();

                if (selectedCommand == Commands.Exit)
                {
                    Environment.Exit(0);
                }

                if (!selectedCommand.HasValue)
                {
                    continue;
                }

                ExecuteSelectedCommand(selectedCommand);

                selectedCommand = null;
                Console.Clear();
                Console.Write(executionLog.ToString());
                executionLog.Clear();
                Console.WriteLine(Environment.NewLine);
            }
        }

        private static void ClearDatabase()
        {
            using var db = new BloggingContext();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            executionLog.AppendLine("All tables cleared");
        }

        private static void CreateBlog()
        {
            Console.WriteLine("How many blogs do you wanna create?");
            var amount = ReadNumber(1);
            using var db = new BloggingContext();
            for (int i = 0; i < amount; i++)
            {
                var url = "http://www.blogs.ch/blog/" + Guid.NewGuid();
                db.Add(new Blog { Url = url});
            }

            executionLog.AppendLine($"All {amount} blogs successfully created.");

            db.SaveChanges();
        }

        private static void CreateBlogPosts()
        {
            using var db = new BloggingContext();
            var blogsCount = db.Blogs.AsNoTracking().Count();
            if (blogsCount <= 0)
            {
                executionLog.AppendLine("Please create before some blogs.");
                return;
            }

            Console.WriteLine("How many posts do you wanna create per blog?");
            var amount = ReadNumber(1);

            var blogs = db.Blogs.ToList();
            blogs.ForEach(b =>
            {
                for (int i = 0; i < amount; i++)
                {
                    var guid = Guid.NewGuid();
                    b.Posts.Add(new Post{
                       Title = $"Post title {guid}",
                       Content = $"This is the content of post {guid}"
                    });
                }
            });

            db.SaveChanges();
            executionLog.AppendLine($"{amount} posts per blog successfully created");
        }

        private static void ShowPaginationExample()
        {
            using var db = new BloggingContext();
            if (db.Blogs.AsNoTracking().Count() == 0)
            {
                executionLog.AppendLine("No blogs found to show the paging example. Please create some blogs first.");
                return;
            }

            Console.WriteLine("How many blogs should be displayed per page?");
            var pageSize = ReadNumber(1);
            if (!pageSize.HasValue)
            {
                executionLog.AppendLine("Please define a valid pageSize.");
                return;
            }


            var totalPages = GetTotalPages(pageSize.GetValueOrDefault());
            var pagingOptions = new PagingOptions<Blog>{ PageSize = pageSize.GetValueOrDefault(), TotalPages = totalPages, CurrentPage = 1};
            while (pagingOptions.CurrentPage <= totalPages)
            {
                pagingOptions = ShowPagedBlogs(pagingOptions);
                pagingOptions.CurrentPage = pagingOptions.CurrentPage + 1;
            }



        }

        private static PagingOptions<Blog> ShowPagedBlogs(PagingOptions<Blog> options)
        {
            using var db = new BloggingContext();

            if (options.CurrentPage == 1)
            {
                options.PagedResult = db.Blogs
                    .OrderBy(b => b.BlogId)
                    .Take(options.PageSize).ToList();
            } else
            {
                options.PagedResult = db.Blogs
                    .OrderBy(b => b.BlogId)
                    .Where(b => b.BlogId > options.LastId)
                    .Take(options.PageSize).ToList();
            }

            options.LastId = options.PagedResult.Last().BlogId;

            Console.WriteLine(Environment.NewLine);
            Console.WriteLine($"Page {options.CurrentPage} of {options.TotalPages}:");
            Console.WriteLine($"----------------------------------------------------");

            options.PagedResult.ForEach(b => Console.WriteLine(b.ToDisplayLine()));
            Console.WriteLine(Environment.NewLine);



            if (options.CurrentPage < options.TotalPages)
            {
                Console.WriteLine("Press any key to show the next page...");
            }

            if (options.CurrentPage == options.TotalPages)
            {
                Console.WriteLine("Press any key...");
            }
            
            Console.ReadKey();


            return options;
        }

        private static int GetTotalPages(int pageSize)
        {
            using var db = new BloggingContext();
            var blogsCount = db.Blogs.AsNoTracking().Count();
            return (blogsCount % pageSize == 0) ? blogsCount / pageSize : (blogsCount / pageSize) + 1;
        }


        private static void ShowAllBlogs()
        {
            using var db = new BloggingContext();
            var blogs = db.Blogs.Include(b => b.Posts).AsNoTracking().ToList();
            if (blogs.Count == 0)
            {
                executionLog.AppendLine("There are no blogs currently created within the database.");
                return;
            }



            blogs.ForEach(b =>
            {
                executionLog.AppendLine($"Id: {b.BlogId}, Url: {b.Url}");
                    foreach (var post in b.Posts)
                    {
                     executionLog.AppendLine($"   Post: Id: {post.PostId}, Content: {post.Content}");
                    }
            });
            executionLog.AppendLine("End of list");
            
        }

        private static void ShowBlogById()
        {
            using var db = new BloggingContext();
            Console.WriteLine("Please enter valid blogId:");
            var blogId = ReadNumber(1);
            if (!blogId.HasValue) {
                executionLog.AppendLine("BlogId not valid.");
                return;
            }
            var blog = db.Blogs.SingleOrDefault(b => b.BlogId == blogId);

            if (blog == null) {
                executionLog.AppendLine($"No blog found with id: {blogId}.");
                return;
            }

            executionLog.AppendLine(blog.ToDisplayLine());
        }

        private static void FulltextSearchInBlogPostContent()
        {
           using var db = new BloggingContext();
           Console.WriteLine("Please enter a search-term (>2 characters):");
           var enteredString = Console.ReadLine();
            while (string.IsNullOrEmpty(enteredString) && enteredString.Length < 3)
            {
                Console.WriteLine("Search-term nicht gültig");
                enteredString = Console.ReadLine();
            }

            var blogs = db.Blogs.Include(b => b.Posts).Where(b => b.Posts.Any(p => p.Title.ToLower().IndexOf(enteredString.ToLower()) >= 0)).ToList();

            if (blogs.Count == 0) {
                executionLog.AppendLine("No Blogs found with the search-term within the Blog-Posts.");
            } else
            {
                executionLog.AppendLine("Search-Result:");
                executionLog.AppendLine("---------------");
                blogs.ForEach(b =>
                {
                    executionLog.AppendLine($"Id: {b.BlogId}, Url: {b.Url}");
                    foreach (var post in b.Posts)
                    {
                        executionLog.AppendLine($"   Post: Id: {post.PostId}, Content: {post.Content}");
                    }
                });
            }
        }


        private static Commands? ReadCommand()
        {
            try
            {
                var number = ReadNumber();
                if (!number.HasValue)
                {
                    throw new Exception();
                }

                return (Commands)number;

            }
            catch (Exception)
            {
                Console.Clear();
                Console.WriteLine("Please type a valid menu-number.");
                return null;
            }
        }

        private static void ShowMenu()
        {
            Console.WriteLine("PLEASE CHOOSE A TASK:");
            Console.WriteLine("-----------------------------------");
            foreach (var command in Enum.GetValues(typeof(Commands)))
            {
                Console.WriteLine($"{(int)command}: {command}");
            }
        }       

        private static int? ReadNumber(int minimumNumber = 0)
        {
            try
            {
                var enteredString = Console.ReadLine();
                var number = Convert.ToInt32(enteredString);

                if (number < minimumNumber)
                {
                    return null;
                }

                return number;

            }
            catch (Exception)
            {
                return null;
            }
        }       

        private static void ExecuteSelectedCommand(Commands? selectedCommand)
        {
            if (!selectedCommand.HasValue) { return;  }

            var type = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.FullName.Contains("Program"));

            type.GetMethod(selectedCommand.ToString(), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);
        }
    }
}
