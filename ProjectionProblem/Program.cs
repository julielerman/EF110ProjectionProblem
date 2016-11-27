using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ProjectionProblem
{
  public class Parent
  {
    public Parent()
    {
      Children = new List<Child>();
    }

    public int Id { get; set; }
    public string Description { get; set; }
    public List<Child> Children { get; set; }
  }

  public class Child
  {
    public int Id { get; set; }
    public string Description { get; set; }
  }

  public class MyContext : DbContext
  {
    public DbSet<Parent> Parents { get; set; }
    public DbSet<Child> Children { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite("Data source=ProjectionProblem.db");
    }
  }

  public class Program
  {
    private static void Main(string[] args)
    {
      using (var context = new MyContext())
      {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        var parent = new Parent {Description = "Parent1"};
        parent.Children.Add(new Child {Description = "Child1"});
        parent.Children.Add(new Child {Description = "Child2"});
        context.Parents.Add(parent);
        context.SaveChanges();
      }
      using (var context1 = new MyContext()) {
        Console.WriteLine("------FixUp results multiple queries------------------------");
        context1.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
        var parents = context1.Parents.ToList();
        var children = context1.Children.ToList();
        var parent = parents.FirstOrDefault(p => p.Id == 1);
        Console.WriteLine($"Parent 1 Children Count: {parent.Children.Count}");
      }

      using (var context2 = new MyContext()) {
        context2.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
        Console.WriteLine("------FixUp results of projection------------------------");

        var newtype = context2.Parents.Select(p => new { Parent = p, p.Children }).ToList();
        var parent = newtype.FirstOrDefault(p => p.Parent.Id == 1).Parent;
        Console.WriteLine($"Parent 1 Children Count: {parent.Children.Count}");
      }
      using (var context3 = new MyContext()) {
        context3.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
        Console.WriteLine("------Filter children in projection------------------------");

        var newtype = context3.Parents.Select(p =>
          new { Parent = p, Children = p.Children.Where(c => c.Description == "Child2") }).ToList();
        var parent = newtype.FirstOrDefault(p => p.Parent.Id == 1).Parent;
        Console.WriteLine($"Parent 1 Children Count: {parent.Children.Count}");
      }
      using (var context4 = new MyContext()) {
        Console.WriteLine("------FixUp results of multiple queries with filter------------------------");
        context4.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
        var parents = context4.Parents.ToList();
        var children = context4.Children.Where(c => c.Description == "Child2").ToList();

        var parent = parents.FirstOrDefault(p => p.Id == 1);
        Console.WriteLine($"Parent 1 Children Count: {parent.Children.Count}");

      }
      using (var context5 = new MyContext()) {
        Console.WriteLine("--------Load with Filter----------------------");
        context5.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());
        var parent = context5.Parents.Find(1);

        context5.Entry(parent).Collection(p => p.Children).Query().Where(c => c.Description == "Child2").ToList();
        Console.WriteLine("wow that's a lot of queries!");
      }
    }
  }
}