﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tests.FakeDomain;
using EntityFramework.Utilities;
using Tests;
using System.Data.Entity;

namespace PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            BatchIteration(25);
            BatchIteration(25);
            NormalIteration(25);
            NormalIteration(25);
            BatchIteration(2500);
            NormalIteration(2500);
            BatchIteration(25000);
            NormalIteration(25000);
            BatchIteration(50000);
            //NormalIteration(50000);
            BatchIteration(100000);
            //NormalIteration(100000);
        }


        private static void NormalIteration(int count)
        {
            Console.WriteLine("Standard iteration with " + count + " entities");
            CreateAndWarmUp();
            var stop = new Stopwatch();

            using (var db = new Context())
            {
                db.Configuration.AutoDetectChangesEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                var comments = GetEntities(count).ToList();
                stop.Start();
                foreach (var comment in comments)
                {
                    db.Comments.Add(comment);
                }
                db.SaveChanges();
                stop.Stop();
                Console.WriteLine("Insert entities: " + stop.ElapsedMilliseconds + "ms");
            }

            using (var db = new Context())
            {
                db.Configuration.AutoDetectChangesEnabled = true;
                db.Configuration.ValidateOnSaveEnabled = false;
                stop.Restart();
                var toUpdate = db.Comments.Where(c => c.Text == "a").ToList();
                foreach (var item in toUpdate)
                {
                    item.Reads++;
                }
                db.SaveChanges();
                Console.WriteLine("Update all entities with a: " + stop.ElapsedMilliseconds + "ms");
            }

            using (var db = new Context())
            {
                db.Configuration.AutoDetectChangesEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                stop.Restart();
                var toDelete = db.Comments.Where(c => c.Text == "a").ToList();
                foreach (var item in toDelete)
                {
                    db.Comments.Remove(item);
                }
                db.SaveChanges();
                stop.Stop();
                Console.WriteLine("delete all entities with a: " + stop.ElapsedMilliseconds + "ms");
            }

            using (var db = new Context())
            {
                db.Configuration.AutoDetectChangesEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                stop.Restart();
                var all = db.Comments.ToList();
                foreach (var item in all)
                {
                    db.Comments.Remove(item);
                }
                db.SaveChanges();
                stop.Stop();
                Console.WriteLine("delete all entities: " + stop.ElapsedMilliseconds + "ms");

            }
        }

        private static void BatchIteration(int count)
        {
            Console.WriteLine("Batch iteration with " + count + " entities");
            CreateAndWarmUp();
            using (var db = new Context())
            {

                var stop = new Stopwatch();
                var comments = GetEntities(count).ToList();                
                stop.Start();
                db.InsertAll(comments);
                stop.Stop();
                Console.WriteLine("Insert entities: " + stop.ElapsedMilliseconds + "ms");

                stop.Restart();
                db.UpdateAll<Comment>(x => x.Text == "a", x => x.Reads + 1);
                stop.Stop();
                Console.WriteLine("Update all entities with a: " + stop.ElapsedMilliseconds + "ms");

                stop.Restart();
                db.DeleteAll<Comment>(x => x.Text == "a");
                stop.Stop();
                Console.WriteLine("delete all entities with a: " + stop.ElapsedMilliseconds + "ms");

                stop.Restart();
                db.DeleteAll<Comment>(x => true);
                stop.Stop();
                Console.WriteLine("delete all entities: " + stop.ElapsedMilliseconds + "ms");

            }
        }

        private static void CreateAndWarmUp()
        {
            using (var db = new Context())
            {
                if (db.Database.Exists())
                {
                    db.Database.Delete();
                }
                db.Database.Create();

                //warmup
                db.Comments.Add(new Comment { Date = DateTime.Now });
                db.SaveChanges();
                db.Comments.Remove(db.Comments.First());
                db.SaveChanges();
            }
        }

        private static IEnumerable<Comment> GetEntities(int count)
        {
            var comments = Enumerable.Repeat('a', count).Select((c, i) => new Comment
            {
                Text = ((char)(c + (i % 25))).ToString(),
                Date = DateTime.Now.AddDays(i),
            });
            return comments;
        }
    }

    public class Context : DbContext
    {
        public Context()
            : base("Data Source=./; Initial Catalog=EFUTest; Integrated Security=SSPI; MultipleActiveResultSets=True")
        {

        }

        public IDbSet<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public int Reads { get; set; }
    }
}
