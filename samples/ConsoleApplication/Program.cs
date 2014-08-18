using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Entity;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main()
        {
            using (var db = new MyContext())
            {
                db.Add(new Blog { BlogId = 1, Name = "ADO.NET", Url = "http://blogs.msdn.com/adonet" });
                db.Add(new Blog { BlogId = 2, Name = ".NET Framework", Url = "http://blogs.msdn.com/dotnet" });
                db.Add(new Blog { BlogId = 3, Name = "Visual Studio", Url = "http://blogs.msdn.com/visualstudio" });
                db.SaveChanges();
            }

            using (var db = new MyContext())
            {
                var blogs = db.Blogs.OrderBy(b => b.Name);
                foreach (var item in blogs)
                {
                    Console.WriteLine(item.Name);
                }
            }
        }
    }

    public class MyContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            builder.UseInMemoryStore();
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

}
